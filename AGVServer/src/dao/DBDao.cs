using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySql.Data.MySqlClient;

using AGV.util;
using AGV.task;
using AGV.init;
using AGV.forklift;
using AGV.bean;

namespace AGV.dao {
	public class DBDao {
		private static DBDao dao = null;
		private object lockDB = new object();

		private DBDao() {
		}

		public object getLockDB() {
			return lockDB;
		}

		public MySqlDataReader execQuery(String querySql) {
			return new MySqlCommand(querySql, DBConnect.getConnection()).ExecuteReader();
		}

		public MySqlDataReader execNonQuery(String querySql) {
			return new MySqlCommand(querySql, DBConnect.getConnection()).ExecuteReader();
		}

		public object ExecuteScalar(String querySql) {
			return new MySqlCommand(querySql, DBConnect.getConnection()).ExecuteScalar();
		}
		public static DBDao getDao() {
			if (dao == null) {
				dao = new DBDao();
			}
			return dao;
		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void InsertConnectMsg(string msg, string flag) {
			string sql = "insert into connect_msg (uuid,msg,flag) values (uuid(),REPLACE('" + msg + "','\0',''),'" + flag + "') ";
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void InsertConnectListenMsg(string msg, string flag) {
			string sql = "insert into connect_listen_msg (uuid,msg,flag) values (uuid(),'" + msg + "','" + flag + "') ";
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		public List<ForkLiftWrapper> getForkLiftWrapperList() {
			string query = "select * from forklift order by number";
			List<ForkLiftWrapper> list = new List<ForkLiftWrapper>();
			AGVLog.WriteInfo("SelectForkList sql = " + query, new StackFrame(true));
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(query);
					while (dataReader.Read()) {
						ForkLiftItem fl = new ForkLiftItem();
						fl.id = int.Parse(dataReader["id"] + "");
						fl.forklift_number = int.Parse(dataReader["number"] + "");
						fl.ip = dataReader["ip"] + "";
						fl.port = int.Parse(dataReader["port"] + "");
						list.Add(new ForkLiftWrapper(fl));
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(" sql err " + ex.ToString());
			}
			return list;
		}

		public List<User> SelectUserList() {
			string query = "select * from user";
			List<User> list = new List<User>();
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(query);
					while (dataReader.Read()) {
						User user = new User();
						user.id = int.Parse(dataReader["id"] + "");
						user.userType = (USER_TYPE_T)int.Parse(dataReader["userType"] + "");
						user.userPasswd = dataReader["userPasswd"] + "";
						user.userName = dataReader["userName"] + "";

						list.Add(user);
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine("ex " + ex.ToString());
			}

			return list;

		}

		public SingleTask SelectSingleTaskByName(string taskName)  //һ��taskNameֻ��Ӧһ����¼
		{
			string query = "select * from singletask";

			SingleTask st = new SingleTask();

			//Open connection
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(query);
					while (dataReader.Read()) {
						st.taskID = int.Parse(dataReader["id"] + "");
						st.taskName = dataReader["taskName"] + "";
						st.taskUsed = bool.Parse(dataReader["taskUsed"] + "");
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine("ex " + ex.ToString());
			}

			return st;

		}

		/// <summary>
		/// ֻ��ѯʹ�õ�������Ҫ��ѯ�������������ɵ�����
		/// </summary>
		public List<SingleTask> SelectSingleTaskList() {
			string query = "select * from singleTask where taskUsed = 1 order by id";
			List<SingleTask> list = new List<SingleTask>();

			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(query);
					while (dataReader.Read()) {
						SingleTask st = new SingleTask();
						st.taskID = int.Parse(dataReader["id"] + "");
						st.taskName = dataReader["taskName"] + "";
						st.taskText = dataReader["taskText"] + "";
						st.taskUsed = Convert.ToBoolean(int.Parse(dataReader["taskUsed"] + ""));
						st.taskType = (TASKTYPE_T)int.Parse(dataReader["taskType"] + "");
						st.setAllocid(dataReader["allocid"] + "");
						st.setAllocOpType(dataReader["allocOpType"] + "");
						list.Add(st);
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
			return list;
		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void InsertTaskRecord(TASKSTAT_T taskRecordStat, SingleTask st) {
			string sql = "INSERT INTO `taskrecord` (`taskRecordStat`, `singleTask`) VALUES ( " + (int)taskRecordStat + ", " + st.taskID + ");";
			AGVLog.WriteInfo("InsertTaskRecord sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}


		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void InsertTaskRecord(TaskRecord tr) {
			string sql = "INSERT INTO `taskrecord` (`taskRecordStat`, `singleTask`) VALUES ( " + (int)tr.taskRecordStat + ", " + tr.singleTask.taskID + ");";
			AGVLog.WriteInfo("InsertTaskRecord sql = " + sql, new StackFrame(true));

			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void InsertTaskRecordBak(TaskRecord tr) {
			float now = DateTime.Now.Hour * 60 * 60 + DateTime.Now.Minute * 60 + DateTime.Now.Second;  //��ǰʱ�������
			float excute_min = (now - (tr.updateTime.Hour * 60 * 60 + tr.updateTime.Minute * 60 + tr.updateTime.Second)) / 60;
			if (excute_min > 20 || excute_min < 3) {
				excute_min = 6;  //�����쳣����
			}
			string sql = "INSERT INTO `taskrecord_bak` (`taskRecordStat`, `forklift`, `singleTask`, `taskRecordExcuteMinute`) VALUES ( " + (int)tr.taskRecordStat + ", " + (int)tr.forkLiftWrapper.getForkLift().id + ", " + tr.singleTask.taskID + ", " + excute_min + ");";
			AGVLog.WriteInfo("InsertTaskRecordBak sql = " + sql, new StackFrame(true));


			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

		}
		/// <summary>
		/// �Ƴ������¼
		/// </summary>
		public void RemoveTaskRecord(SingleTask st, TASKSTAT_T taskRecordStat) {
			string sql = "delete from taskrecord where taskRecordStat = " + (int)taskRecordStat + " and singleTask = " + st.taskID;
			AGVLog.WriteInfo("RemoveTaskRecord sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					Console.WriteLine("removeTaskRecor sql = " + sql);
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// �Ƴ������¼
		/// </summary>
		public void RemoveTaskRecord(TaskRecord tr) {
			string sql = "delete from taskrecord where taskRecordStat = " + (int)tr.taskRecordStat + " and forklift = " + tr.forkLiftWrapper.getForkLift().id + " and singleTask = " + tr.singleTask.taskID;
			AGVLog.WriteInfo("RemoveTaskRecord sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// ���������¼
		/// </summary>
		public void UpdateTaskRecord(TaskRecord tr) {

			string sql;
			if (tr.forkLiftWrapper == null) {
				sql = "update taskrecord set taskRecordStat = " + (int)tr.taskRecordStat + ", forklift = NULL , taskLevel = " + tr.taskLevel + " where taskRecordStat != 4 and singleTask = " + tr.singleTask.taskID;
			} else {
				sql = "update taskrecord set taskRecordStat = " + (int)tr.taskRecordStat + ", forklift = " + tr.forkLiftWrapper.getForkLift().id + " , taskLevel = " + tr.taskLevel + " where taskRecordStat != 4 and singleTask = " + tr.singleTask.taskID;
				;
			}

			AGVLog.WriteInfo("UpdateTaskRecord sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					Console.WriteLine("UpdateTaskRecord sql = " + sql);
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();

				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// ���³���״̬
		/// </summary>
		public void updateForkLift(ForkLiftWrapper fl) {
			string sql = "update forklift set currentTask = \"" + fl.getForkLift().currentTask + "\", taskStep = " +
				(int)fl.getForkLift().taskStep + " where id = " + fl.getForkLift().id;

			AGVLog.WriteInfo("updateForkLift sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();

				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// ��ȡ�����¼
		/// </summary>
		public List<TaskRecord> SelectTaskRecordBySql(string sql) {
			List<TaskRecord> taskRecordList = new List<TaskRecord>();
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(sql);
					while (dataReader.Read()) {
						TaskRecord taskRecord = new TaskRecord();
						taskRecord.taskRecordID = int.Parse(dataReader["taskRecordID"] + "");

						taskRecord.taskRecordStat = (TASKSTAT_T)int.Parse(dataReader["taskRecordStat"] + "");
						taskRecord.taskLevel = int.Parse(dataReader["taskLevel"] + "");
						try {
							if (!String.IsNullOrEmpty(dataReader["forklift"].ToString())) {
								taskRecord.forkLiftWrapper = AGVCacheData.getForkLiftByID(int.Parse(dataReader["forklift"] + ""));
							}
						} catch (FormatException fx) {
							Console.WriteLine("message = " + fx.Message);
						}
						taskRecord.singleTask = SingleTaskDao.getDao().getSingleTaskByID(int.Parse(dataReader["singleTask"] + ""));
						taskRecord.taskRecordName = taskRecord.singleTask.taskName;
						taskRecord.updateTime = (DateTime)(dataReader["taskRecordUpdateTime"]);

						taskRecordList.Add(taskRecord);
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
			return taskRecordList;
		}

		/// <summary>
		/// ��ȡ����ָ���¼
		/// </summary>
		public List<TaskexeBean> SelectTaskexeBySql(string sql) {
			List<TaskexeBean> taskexeBeanList = new List<TaskexeBean>();
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execQuery(sql);
					while (dataReader.Read()) {
						TaskexeBean taskexeBean = new TaskexeBean();
						taskexeBean.setUuid(dataReader["uuid"] + "");
						taskexeBean.setTime(dataReader["time"] + "");
						taskexeBean.setTaskid(dataReader["taskid"] + "");
						taskexeBean.setOpflag(dataReader["opflag"] + "");

						taskexeBeanList.Add(taskexeBean);
					}
					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
			return taskexeBeanList;
		}

		public void DeleteWithSql(string sql) {
			string query = sql;
			AGVLog.WriteInfo("DeleteWithSql sql = " + sql, new StackFrame(true));
			try {
				lock (lockDB) {
					MySqlDataReader dataReader = execNonQuery(sql);
					dataReader.Close();
				}
			} catch (Exception) {
				Console.WriteLine(" delete sql err : " + sql);
			}

		}

		public List<string>[] SelectWithSql(string sql) {
			string query = sql;

			List<string>[] list = new List<string>[3];
			list[0] = new List<string>();
			list[1] = new List<string>();
			list[2] = new List<string>();

			try {
				lock (lockDB) {
					MySqlDataReader dataReader = this.execQuery(query);

					while (dataReader.Read()) {
						list[0].Add(dataReader["id"] + "");
					}

					dataReader.Close();
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

			return list;
		}

		public int Count(string conditionSql) {
			string query = "SELECT Count(*) from " + conditionSql;
			int Count = -1;

			try {
				lock (lockDB) {
					Count = int.Parse(this.ExecuteScalar(query) + "");
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

			return Count;
		}

		public int selectMaxBySql(string sql) {
			Console.WriteLine(" max sql " + sql);
			int Count = -1;

			try {
				lock (lockDB) {
					Count = int.Parse(this.ExecuteScalar(sql) + "");
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

			return Count;
		}

	}
}
