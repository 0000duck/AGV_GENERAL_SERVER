using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using AGV.util;
using AGV.init;
using AGV.forklift;
using AGV.tools;
using AGV.dao;
using System.Windows.Forms;
using AGV.taskexe;
using AGV.command;

namespace AGV.socket {

	public class AGVSocketClient {
		private TcpClient tcpClient = null;
		private bool connectStatus = false;  //����״̬
		private string stx = ((char)2).ToString();
		private string etx = ((char)3).ToString();
		private bool isConnectThread = false;   //�����߳��Ƿ���
		private byte readTimeOutTimes = 0;  //��ȡ��Ϣ��ʱ����
		private bool isRecvMsgFlag = false;  //�Ƿ������ݽ��մ����߳�
		public delegate void handleRecvMessageCallback(int fID,byte[] buffer,int length);  //��Ϣ����ص�����
		public delegate void handleReconnectCallback(ForkLiftWrapper forkLiftWrapper,bool status);  //�����ص�����
		public object clientLock = new object();

		private handleRecvMessageCallback hrmCallback = null;
		private handleReconnectCallback hrctCallback = null;

		private ForkLiftWrapper forkLiftWrapper = null;

		public AGVSocketClient(ForkLiftWrapper forkLiftWrapper) {
			this.forkLiftWrapper = forkLiftWrapper;
		}

		public AGVSocketClient() {
		}

		public void setForkLiftWrapper(ForkLiftWrapper forkLiftWrapper) {
			this.forkLiftWrapper = forkLiftWrapper;
		}

		public ForkLiftWrapper getForkLiftWrapper() {
			return this.forkLiftWrapper;
		}

		private TcpClient getTcpClient() {
			while (tcpClient == null) {
				Console.WriteLine("client is null wait 1 second");
				AGVLog.WriteWarn("client is null, wait 1 second",new StackFrame(true));
				Thread.Sleep(1);
			}

			return tcpClient;
		}

		private void setTcpClient() {
			tcpClient.ReceiveTimeout = AGVConstant.TCPCONNECT_REVOUT;
			tcpClient.SendTimeout = AGVConstant.TCPCONNECT_SENDOUT;
			connectStatus = true;
		}

		public void TcpConnect(string ip,int port) {
			try {
				if (tcpClient == null) {
					(tcpClient = new TcpClient()).Connect(ip,port);
					setTcpClient();

					AGVLog.WriteInfo("connect ip: " + ip + " port: " + port + "succee",new StackFrame(true));
					Console.WriteLine("connect ip: " + ip + " port: " + port + "succee");
				}
			} catch (Exception ex) {
				AGVLog.WriteError("Connect ip: " + ip + " port: " + port + " fail" + ex.Message,new StackFrame(true));
				Console.WriteLine("Connect ip: " + ip + " port: " + port + " fail");

				Closeclient();
			}
		}
		/// <summary>
		/// �����복�ӽ�������
		/// </summary>
		public void reConnect() {
			Console.WriteLine("ConnectStatus:   " + connectStatus);
			AGVLog.WriteInfo("ConnectStatus: " + connectStatus,new StackFrame(true));

			while (forkLiftWrapper.getForkLift().isUsed == 1 && !connectStatus) {
				isConnectThread = true; //����������־true
				Thread.Sleep(5000);  //5��������һ��

				Console.WriteLine("start to reconnect");
				AGVLog.WriteWarn("start to reconnect",new StackFrame(true));

				TcpConnect(forkLiftWrapper.getForkLift().ip,forkLiftWrapper.getForkLift().port);

				if (connectStatus == true) {
					AGVLog.WriteInfo("reconnect ip: " + forkLiftWrapper.getForkLift().ip + " :" + forkLiftWrapper.getForkLift().port + "success",new StackFrame(true));
					Console.WriteLine("reconnect ip: " + forkLiftWrapper.getForkLift().ip + ":" + forkLiftWrapper.getForkLift().port + "success");
					AGVLog.WriteInfo("restart recv thread",new StackFrame(true));

					//hrctCallback?.Invoke(forkLiftWrapper, true);
					if (hrctCallback != null) {
						hrctCallback(forkLiftWrapper,true);
					}
				}
			}
			isConnectThread = false; //�����ɹ�

		}

		/// <summary>
		/// ��Ϣ����ص�����ע��
		/// </summary>
		public void registerRecvMessageCallback(handleRecvMessageCallback hrmCallback) {
			this.hrmCallback = hrmCallback;
		}

		/// <summary>
		/// ע�������ص�����
		/// </summary>
		public void registerReconnectCallback(handleReconnectCallback hrctCallback) {
			this.hrctCallback = hrctCallback;
		}

		public void startRecvMsg() {
			this.isRecvMsgFlag = true;  //���ý��ձ�־
			ThreadFactory.newBackgroudThread(new ParameterizedThreadStart(receive)).Start();
		}

		/// <summary>
		/// �������ݽ��մ����߳�
		/// </summary>
		public void setRecvFlag(bool status) {
			this.isRecvMsgFlag = status;
		}

		private void receive(object fl) {
			//ForkLiftWrapper forklift = new ForkLiftWrapper();
			//forklift.setForkLift((ForkLiftItem)fl);
			byte[] buffer = new byte[512];
			Socket msock;
			TcpClient vClient = null;
			Console.WriteLine("receive ConnectStatus: " + connectStatus);
			if (connectStatus == false) //�������״̬
				return;

			while (isRecvMsgFlag) {
				try {
					vClient = getTcpClient();
					msock = tcpClient.Client;
					Array.Clear(buffer,0,buffer.Length);
					tcpClient.GetStream();

					int bytes = msock.Receive(buffer);
					string receiveStr = Encoding.ASCII.GetString(buffer).Trim();
					CommandService.getInstance().setLatestMsgFromClient(receiveStr);
					DBDao.getDao().InsertConnectMsg(receiveStr,"receive");

					readTimeOutTimes = 0; //��ȡ��ʱ��������
					if (hrmCallback != null) {
						hrmCallback(forkLiftWrapper.getForkLift().id,buffer,bytes);
					}
				} catch (SocketException ex) {
					if (ex.ErrorCode == 10060 && readTimeOutTimes < 10) //��ʱ��������10�Σ��ر�socket��������
					{
						AGVLog.WriteWarn("read msg timeout",new StackFrame(true));
						Console.WriteLine("read msg timeout");
						readTimeOutTimes++;
						continue;
					}
					AGVLog.WriteError("��ȡ��Ϣ����" + ex.ErrorCode,new StackFrame(true));
					Console.WriteLine("recv msg client close" + ex.ErrorCode);
					Closeclient();
				} catch (Exception ex) {
					Closeclient();
				}
			}
		}

		public void SendMessage(string sendMessage) {
			AGVLog.WriteSendInfo(sendMessage,new StackFrame(true));
			Socket msock;
			try {
				if (tcpClient == null || connectStatus == false) {
					Exception ex = new Exception("connect err");
					throw (ex);
				}

				msock = tcpClient.Client;
				byte[] data = Encoding.ASCII.GetBytes(sendMessage);
				DBDao.getDao().InsertConnectMsg(sendMessage,"SendMessage");
				msock.Send(data);

			} catch (Exception se) {
				AGVLog.WriteError("������Ϣ����" + se.Message,new StackFrame(true));
				Console.WriteLine("send message error" + se.Message);
				Closeclient();
			}
		}

		public void Sendbuffer(byte[] buffer) {
			if (tcpClient == null || connectStatus == false) //�������״̬
				return;

			Socket msock;
			try {
				msock = tcpClient.Client;
				DBDao.getDao().InsertConnectMsg(Encoding.ASCII.GetString(buffer),"Sendbuffer");
				msock.Send(buffer);
			} catch (Exception ex) {
				AGVLog.WriteError("������Ϣ����" + ex.Message,new StackFrame(true));
				Console.WriteLine("send message error");
				Closeclient();
			}
		}

		/// <summary>
		/// �����������߳�
		/// </summary>
		private void startReconnectThread() {
			ThreadFactory.newBackgroudThread(new ThreadStart(reConnect)).Start();
		}

		public void Closeclient() {
			try {
				connectStatus = false;
				if (tcpClient != null) {
					AGVLog.WriteInfo("�ر�socket",new StackFrame(true));
					tcpClient.Client.Close();
					tcpClient.Close();
					tcpClient = null;
					if (isConnectThread == false && forkLiftWrapper.getForkLift().isUsed == 1)  //connect�Ͽ������복������
					{
						/*DialogResult dr = MessageBox.Show("�Ƿ���������SOCKET", "", MessageBoxButtons.YesNo);
						if (dr == DialogResult.No) {
							Console.WriteLine(" cancelItemClick cancel ");
							return;
						} else {
							startReconnectThread();
						}*/
						startReconnectThread();
					}

					if (forkLiftWrapper.getForkLift().isUsed == 1) {
						if (hrctCallback != null) {
							hrctCallback(forkLiftWrapper,true);
						}
					}

				}
			} catch (Exception ex) {
				AGVLog.WriteError("�ر�socket����" + ex.Message,new StackFrame(true));
				Console.WriteLine("close socket fail");
			}
			Console.WriteLine("client is null now");
			AGVLog.WriteWarn("client is null now",new StackFrame(true));
		}
	}
}
