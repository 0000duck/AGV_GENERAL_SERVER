﻿using AGV.bean;
using AGV.init;
using AGV.locked;
using AGV.task;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace AGV.dao {
	class TaskexeDao {
		private static TaskexeDao dao = null;

		private static string selectTaskexe = " SELECT  `uuid`,  `time`,  taskid,  opflag FROM  taskexe_s2c_task ";

		private string selectTaskexeStartOrPauseSql = "SELECT  `uuid`,  `time`,  taskid,  opflag FROM  taskexe_s2c_sop WHERE delflag=0 AND (taskid='sysPause' OR taskid='sysContinue') ORDER BY `time` DESC LIMIT 0,1";

		private string selectTaskexeTaskSql = selectTaskexe + " WHERE delflag=0 AND (opflag='New' OR opflag='Send') ORDER BY  opflag DESC,`time` LIMIT 0,10 ";

		private string selectTaskexeByUuidSql = selectTaskexe + " WHERE delflag=0 AND `uuid`= ";

		private string checkTaskexeIsOverSql = selectTaskexe + " where delflag=0 and (opflag='New' or opflag='Send') ";

		private TaskexeDao() {
		}

		public static TaskexeDao getDao() {
			if (dao == null) {
				dao = new TaskexeDao();
			}
			return dao;
		}

		public List<TaskexeBean> getTaskexeTaskList() {
			return DBDao.getDao().SelectTaskexeBySql(selectTaskexeTaskSql);
		}

		public bool checkTaskexeIsOver() {
			List<TaskexeBean> tbList = DBDao.getDao().SelectTaskexeBySql(checkTaskexeIsOverSql);
			if (tbList == null || tbList.Count <= 0) {
				return true;
			} else {
				return false;
			}
		}

		public List<TaskexeBean> getTaskexeStartOrPauseList() {
			return DBDao.getDao().SelectTaskexeBySql(selectTaskexeStartOrPauseSql);
		}

		public TaskexeBean selectTaskexeByUuid(string uuid) {
			List<TaskexeBean> tbList = DBDao.getDao().SelectTaskexeBySql(selectTaskexeByUuidSql + "'" + uuid + "'");
			if (tbList == null || tbList.Count <= 0) {
				return null;
			} else {
				return tbList[0];
			};
		}

		public void sendTaskexeBean(TaskexeBean taskexeBean) {
			string sql = " update taskexe_s2c_task set opflag = 'Send' where `uuid` = '" + taskexeBean.getUuid() + "'";
			DBDao.getDao().DeleteWithSql(sql);
		}

		public void InsertTaskexePause(string msg) {
			InsertTaskexeSysInfo(msg, "sysPause");
		}

		public void InsertTaskexeSysInfo(string msg) {
			InsertTaskexeSysInfo(msg, "sysInfo");
		}

		private void InsertTaskexeSysInfo(string msg, string type) {
			string sql = "insert into taskexe_s2c_sop (`uuid`,taskid,opflag,msg) " + "values(uuid(),'" + type
					+ "','" + "New" + "','" + msg + "')";
			try {
				lock (DBDao.getDao().getLockDB()) {
					DBDao.getDao().execNonQuery(sql);
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

	}
}