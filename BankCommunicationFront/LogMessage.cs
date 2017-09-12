using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// 日志管理类
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        private ILog logger;

        /// <summary>
        /// 初始化日志组件
        /// </summary>
        public static void InitLog4Net()
        {
            var logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlConfigurator.ConfigureAndWatch(logCfg);
        }

        /// <summary>
        /// 日志管理单例
        /// </summary>
        private static LogMessage errorLogger = new LogMessage("ErrorLog");
        public static LogMessage GetLogInstance()
        {
            return errorLogger;
        }


        /// <summary>
        /// 构造函数根据loggerName创建logger实例
        /// </summary>
        /// <param name="loggerName">自定义的日志对象<logger>的name的值</param>
        public LogMessage(string loggerName)
        {
            logger = LogManager.GetLogger(loggerName);
        }

        /// <summary>
        /// 构造函数根据Type创建logger实例
        /// </summary>
        /// <param name="type">type</param>
        public LogMessage(Type type)
        {
            logger = LogManager.GetLogger(type);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogError(string logContent)
        {
            logger.Error(logContent + "\n");
        }

        /// <summary>
        /// 记录消息
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogInfo(string logContent)
        {
            logger.Info(logContent + "\n");
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogWarn(string logContent)
        {
            logger.Warn(logContent + "\n");
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="logContent">日志类容</param>
        public void LogFatal(string logContent)
        {
            logger.Fatal(logContent + "\n");
        }
    }

    /// <summary>
    /// ShowMessageHandler
    /// </summary>
    public class ShowMessage
    {
        public delegate void ShowMessageEventHandler(MessageLog message);
        public static event ShowMessageEventHandler ShowMessageEvent;

        /// <summary>
        /// locker对象
        /// </summary>
        private object locker = new object();

        /// <summary>
        /// 界面消息单例
        /// </summary>
        private static ShowMessage showMessage = new ShowMessage();
        public static ShowMessage GetFrontMessageInstance()
        {
            return showMessage;
        }

        /// <summary>
        /// 前置打印消息
        /// </summary>
        /// <param name="message">消息实体</param>
        public void PrintLog(MessageLog message)
        {
            lock(locker)
            {
                if(ShowMessageEvent!=null)
                {
                    ShowMessageEvent(message);
                }
            }
        }
    }

    /// <summary>
    /// 前置用户界面消息结构
    /// </summary>
    public class MessageLog
    {
        public string BankName { get; set; }

        public string SndTime { get; set; }

        public string MessageLevel { get; set; }

        public string MessageContent { get; set; }
    }
}
