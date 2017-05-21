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
using System.IO;

namespace AGV.socket {

	public class AGVSocketClient {
		private TcpClient tcpClient = null;
		private string stx = ((char)2).ToString();
		private string lastMsgAboutSend = "";
		private string etx = ((char)3).ToString();
		private byte readTimeOutTimes = 0;  //��ȡ��Ϣ��ʱ����
		public delegate void handleRecvMessageCallback(int fID, byte[] buffer, int length);  //��Ϣ����ص�����
		public object clientLock = new object();

		private handleRecvMessageCallback receiveMsgCallback = null;

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
			return getTcpConnect(forkLiftWrapper.getForkLift().ip, forkLiftWrapper.getForkLift().port);
		}

		private void initTcpClient() {
			tcpClient.ReceiveTimeout = AGVConstant.TCPCONNECT_REVOUT;
			tcpClient.SendTimeout = AGVConstant.TCPCONNECT_SENDOUT;
		}

		private TcpClient getTcpConnect(string ip, int port) {
			try {
				if (tcpClient == null) {
					(tcpClient = new TcpClient()).Connect(ip, port);
					initTcpClient();
					lastMsgAboutSend = "���ӵ�AGV" + "�ɹ���" + "(ip:" + ip + ",port:" + port + ")";
					AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
				}
				return tcpClient;
			} catch (Exception ex) {
				lastMsgAboutSend = "���ӵ� ip: " + ip + " port: " + port + " ʧ��" + ex.Message + "5������»�ȡ����";
				AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
				Closeclient();
				return getTcpConnect(ip, port);
			}
		}

		public void registerRecvMessageCallback(handleRecvMessageCallback hrmCallback) {
			this.receiveMsgCallback = hrmCallback;
		}

		public void startRecvMsg() {
			ThreadFactory.newThread(new ThreadStart(receive)).Start();
		}

		private void receive() {
			while (true) {
				try {
					byte[] buffer = new byte[512];
					Socket msock;
					msock = getTcpClient().Client;
					Array.Clear(buffer, 0, buffer.Length);
					getTcpClient().GetStream();

					int bytes = msock.Receive(buffer);
					string receiveStr = Encoding.ASCII.GetString(buffer).Trim();

					readTimeOutTimes = 0; //��ȡ��ʱ��������
					if (receiveMsgCallback != null) {
						receiveMsgCallback(forkLiftWrapper.getForkLift().id, buffer, bytes);
					}
					if (!"��������AGV��Ϣ".Equals(lastMsgAboutSend)) {
						lastMsgAboutSend = "��������AGV��Ϣ";
						AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
					}
				} catch (SocketException ex) {
					if (ex.ErrorCode == 10060 && readTimeOutTimes < 10) //��ʱ��������10�Σ��ر�socket��������
					{
						lastMsgAboutSend = "��ȡ��Ϣ��ʱ" + "��ϵͳ�Ժ���������AGV";
						AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
						readTimeOutTimes++;
						continue;
					}
					lastMsgAboutSend = "��ȡ��Ϣ����ϵͳ�Ժ���������AGV";
					AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
					Closeclient();
				} catch (IOException ex) {
					lastMsgAboutSend = "��ȡ��Ϣ����ϵͳ�Ժ���������AGV";
					AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
					Closeclient();
				} catch (Exception ex) {
					lastMsgAboutSend = "��ȡ��Ϣ����ϵͳ�Ժ���������AGV";
					AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
					Closeclient();
				}
			}
		}

		private string lastMessage;

		public bool SendMessage(string sendMessage) {
			if (!sendMessage.Equals(lastMessage)) {
				AGVLog.WriteSendInfo(sendMessage, new StackFrame(true));
			} else {
			}
			lastMessage = sendMessage;

			Socket msock;
			try {
				msock = getTcpClient().Client;
				byte[] data = Encoding.ASCII.GetBytes(sendMessage);
				DBDao.getDao().InsertConnectMsg(sendMessage, "SendMessage");
				msock.Send(data);
				if (!"������Ϣ�ɹ�".Equals(lastMsgAboutSend)) {
					lastMsgAboutSend = "������Ϣ�ɹ�";
					AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
				}
				return true;
			} catch (Exception se) {
				lastMsgAboutSend = "������Ϣ����" + se.Message + "��ϵͳ�Ժ���������AGV";
				AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
				Closeclient();
				return false;
			}
		}

		private void Closeclient() {
			try {
				if (tcpClient != null) {
					tcpClient.Client.Close();
					tcpClient.Close();
					tcpClient = null;
				}
				Thread.Sleep(5000);
			} catch (Exception ex) {
				lastMsgAboutSend = "�ر�socket����" + ex.Message + "��ϵͳ�Ժ���������AGV";
				AGVLog.WriteConnectInfo(lastMsgAboutSend, new StackFrame(true));
			}
		}
	}
}
