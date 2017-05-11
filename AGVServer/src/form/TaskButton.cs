﻿using System;
using System.Drawing;
using System.Windows.Forms;
using AGV.task;
using AGV.forklift;
using AGV.util;
using System.Diagnostics;
using AGV.init;

namespace AGV.form {
	public partial class TaskButton : Button
    {
        private Object ob = null;
        public SingleTask st = null; //每个一个按钮对应一个任务
        public TaskButton()
        {
            InitializeComponent();
            this.BackColor = Color.White;
        }

        delegate void setButtonTextCallBack(string text);
        public void setButtonText(string text)
        {
            if (this.InvokeRequired)
            {
                setButtonTextCallBack stcb = new setButtonTextCallBack(setButtonText);
                this.Invoke(stcb, new object[] { text });
            }
            else
            {
                this.Text = text;
            }
        }

        public void bindValue(Object ob)
        {
            this.ob = ob;
        }

        public Object getBindValue()
        {
            return ob;
        }

        public void setSingleTask(SingleTask st)
        {
            this.st = st;
        }

        public void click(object sender, EventArgs e)
        {
			string cmd = "cmd=set task by name;name=" + this.Name + ";";
			AGVLog.WriteError("向AGV下达命令: " + cmd, new StackFrame(true));
			Console.WriteLine("向AGV下达命令: " + cmd);
			if (AGVEngine.getInstance().isAgvReady()) {
				ForkLiftWrappersService.getInstance().getForkLiftByNunber(1).getAGVSocketClient().SendMessage(cmd);
			} else {
				MessageBox.Show("环境中AGV尚未准备妥当");
			}
        }

    }
}
