using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using SpeechLib;
using System.Speech.Synthesis;
using System.Threading;
using log4net.Core;
using log4net;
using log4net.Config;
using System.IO;
using Maticsoft.DBUtility;
using System.Web;
using System.Net;

namespace SpVoice
{
    public partial class Form1 : Form
    {
        private static readonly ILog log = LogManager.GetLogger("test.logger");
        private bool logWatching = true;
        private bool runing;
        private log4net.Appender.MemoryAppender logger;
        private Thread logWatcher;
        private Thread threadOne;
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            this.Closing += new CancelEventHandler(Form1_Closing);
            logger = new log4net.Appender.MemoryAppender();
            log4net.Config.BasicConfigurator.Configure(logger);
            logWatcher = new Thread(new ThreadStart(LogWatcher));
            logWatcher.Start();

        }

        void Form1_Closing(object sender, CancelEventArgs e)
        {
            runing = false;
            logWatching = false;
            logWatcher.Join();
            if (threadOne != null && threadOne.IsAlive)
            {
                threadOne.Join();
            }

        }

        delegate void delOneStr(string log);
        void AppendLog(string _log)
        {
            if (txtLog.InvokeRequired)
            {
                delOneStr dd = new delOneStr(AppendLog);
                txtLog.Invoke(dd, new object[] { _log });
            }
            else
            {
                StringBuilder builder;
                //设置窗口显示日志的长度
                if (txtLog.Lines.Length > 990)
                {
                    builder = new StringBuilder(txtLog.Text);
                    try
                    {
                        int txtlog2;
                        txtlog2 = txtLog.Text.IndexOf('\r', 3000);
                        builder.Remove(0, txtlog2 + 2);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                    builder.Append(_log);
                    txtLog.Clear();
                    log.Info("清空窗口日志!");
                }
                else
                {
                    txtLog.AppendText(_log);
                }
            }
        }
        private void LogWatcher()
        {
            while (logWatching)
            {
                try
                {
                    LoggingEvent[] events = logger.GetEvents();
                    if (events != null && events.Length > 0)
                    {
                        logger.Clear();
                        foreach (LoggingEvent ev in events)
                        {
                            string line = ev.TimeStamp.ToString(" yyyy-MM-dd HH:mm:ss.fff") + " [" + ev.Level + "] " + ev.RenderedMessage + "\r\n";//定义日志格式
                            AppendLog(line);
                        }
                    }
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
            }
        }

        private void Action()
        {
            runing = true;
            threadOne = new Thread(Run);
            threadOne.Start();
        }
        private void Run()
        {
            string sql = "SELECT * FROM (select ROWNUM RN,  a.main_title ,b.template_type,c.username,a.createddate, case when (select max(cl_datetime) from flow_node where flow_id = a.flow_id) is null then a.createddate   else (select max(cl_datetime) from flow_node where flow_id = a.flow_id)  end modifieddate,B.DAIBAN_WARNTIME ,d.clr,d.node_id,a.main_sid from flow_maindoc a,it_task_flowtemp b,users c ,flow_node d where   a.main_mission_id=b.flow_mission_mid and d.clr=c.userid and a.flow_id=d.flow_id  and  d.step_status=2 ) T WHERE  to_char((to_date(T.modifieddate,'yyyy-MM-dd hh24:mi:ss')+(T.DAIBAN_WARNTIME*60)/(24*60*60)))<TO_CHAR(sysdate, 'yyyy-MM-dd hh24:mi:ss')";
            //模板审核
            string sql2 = "select rownum rn, t.* from (SELECT a.template_name ,a.template_type,b.USERNAME,a.createddate FROM IT_TASK_FLOWTEMP a ,USERS b,flow_maindoc c ,flow_node d  WHERE (  c.flow_id=d.flow_id and  a.sid=c.main_sid and d.step_status=2 and  a.status=1 and  d.clr=b.userid and  a.template_name is not null and a.flow_mission_mid is not null) order by a.createddate desc)t";
            DataTable dt = new DataTable();
            while (runing)
            {
                try
                {
                    dt = DbHelperOra.Query(sql).Tables[0];
                    Monitor.Enter(this.threadOne);//锁定，保持同步
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (runing)
                            {
                                string notice = "事务名称：" + dr["main_title"].ToString() + "，处理人：" + dr["username"].ToString();
                                log.Info(notice);
                                Play(notice);
                                string mbsql = "select mobile from users where userid in(select USERID from IT_TASK_DEPARTMENT t WHERE IS_SONDEPARTMENT_LEADER=1 AND SON_DEPARTMENTID=(select SON_DEPARTMENTID from IT_TASK_DEPARTMENT where USERID='" + dr["clr"].ToString() + "') union select '" + dr["clr"].ToString() + "' USERID from dual)";
                                DataTable dt3 = DbHelperOra.Query(mbsql).Tables[0];
                                if (dt3.Rows.Count > 0)
                                {
                                    foreach (DataRow dr2 in dt3.Rows)
                                    { }
                                }
                                string mob = "18666279916,15818724380";
                                string sql4 = " select count(*) from IT_TASK_SMS where main_sid='" + dr["main_sid"].ToString() + "' and node_id='" + dr["node_id"].ToString() + "'";
                                DataTable dt2 = DbHelperOra.Query(sql4).Tables[0];
                                if (int.Parse(dt2.Rows[0][0].ToString()) == 0)
                                {
                string sql3 = @"insert into INFOSMS(sysid,moduleid,instanceid,timecreate,status,sms_id,sms_to,sms_content,sms_jgdm, sms_lsh,sms_yxjb,sms_shyh)values('gop','42AC49A441DA45D9BFB63AF762B20653', SYS_guid(),sysdate,0,SYS_guid(),'" + mob + @"', '" + notice + @"', '999,999',SEQ_INFOSMS_LSH.Nextval, 3, '')";
                                    string sql5 = "insert into IT_TASK_SMS(main_sid,node_id,clr)values('" + dr["main_sid"].ToString() + "','" + dr["node_id"].ToString() + "','" + dr["clr"].ToString() + "')";
                                    DbHelperOra.ExecuteSql(sql3);
                                    DbHelperOra.ExecuteSql(sql5);
                                    log.Info("发送短信通知...");
                                }
                            }
                            else
                            {
                                log.Info("正在停止...");
                                break;
                            }
                            Thread.Sleep(3000);
                        }
                    }
                    else
                    {
                        log.Info(Thread.CurrentThread.Name + "没有待办！");
                    }

                    Monitor.Exit(this.threadOne);//取消锁定
                    Thread.Sleep(Convert.ToInt32(taktTime.Text));//线程运行时间间隔
                }
                catch (Exception ex)
                { log.Error(ex.Message); }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            this.button2.Enabled = true;
            log.Info("线程正在启动");
            Action();
        }


        private void Play(object text)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            PromptBuilder builder = new PromptBuilder(new System.Globalization.CultureInfo("zh-CN"));
            synth.SetOutputToDefaultAudioDevice();
            if (SelectVoice.Items.Count > 0)
            {
                synth.SelectVoice(SelectVoice.SelectedItem.ToString()); //语音选择
            }
            synth.Rate = Convert.ToInt32(voiceRate.Text);//语速
            builder.AppendText(text.ToString());
            synth.Speak(builder);
            synth.Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            runing = false;


            if (threadOne != null && threadOne.IsAlive)
            {

                threadOne.Join();
            }
            else
            {
                threadOne.Abort();
            }

            log.Info("线程已停止");

            this.button1.Enabled = true;
            this.button2.Enabled = false;

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            this.button1.Enabled = true;
            this.button2.Enabled = false;
            SpeechSynthesizer synth2 = new SpeechSynthesizer();
            InstalledVoice[] Voices = synth2.GetInstalledVoices().ToArray();
            if (Voices.Length > 0)
            {
                for (int i = 0; i < Voices.Length; i++)
                {
                    SelectVoice.Items.Add(Voices[i].VoiceInfo.Name);
                }
                SelectVoice.SelectedItem = Voices[0].VoiceInfo.Name;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void taktTime_TextChanged(object sender, EventArgs e)
        {

        }





    }
}
