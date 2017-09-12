using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    public static class SYSConstant
    {
        #region MongoDB数据库名

        public const string TRANSFER_RCVPACK = "TRANSFER_RCVPACK";

        /// <summary>
        /// 共用的配置库
        /// </summary>
        public const string BANK_CONFIG = "BANK_CONFIG";

        /// <summary>
        /// 交易库
        /// </summary>
        public const string BANK_TRANS = "BANK_TRANS";

        /// <summary>
        /// 签约解约信息库
        /// </summary>
        public const string BANK_ACCOUNT = "BANK_ACCOUNT";

        /// <summary>
        /// 任务库
        /// </summary>
        public const string BANK_TASK = "BANK_TASK";

        /// <summary>
        /// 银行账号与ETC卡绑定信息库
        /// </summary>
        public const string BANK_ACCOUNTID_CARDNO = "BANK_ACCOUNTID_CARDNO";

        #endregion

        #region MongoDB集合名

        // 输入定时处理任务数据集合
        public const string INPUTTASK_WAITING_DONE = "INPUTTASK_WAITING_DONE";

        // 输出定时处理任务数据集合
        public const string OUTPUTTASK_WAITING_DONE = "OUTPUTTASK_WAITING_DONE";

        // 核心库中的银行渠道表ISSUE.BANK_AGENT相同
        public const string BANK_AGENT = "BANK_AGENT";

        // S_PARA集合
        public const string S_PARA = "S_PARA";

        // 签约信息集合（2009和2011号报文方向1）
        public const string BANK_ACCOUNT_SIGN = "BANK_ACCOUNT_SIGN";

        // 一卡通签约信息全量集合
        public const string BANK_ACCOUNT_SIGN_YKTOTAL = "BANK_ACCOUNT_SIGN_YKTOTAL";

        // 一卡通签约信息增量集合
        public const string BANK_ACCOUNT_SIGN_YKTINCREMENT = "BANK_ACCOUNT_SIGN_YKTINCREMENT";

        /// <summary>
        /// 解约信息集合（2010和2012号报文方向0）
        /// </summary>
        public const string BANK_ACCOUNT_CANCEL = "BANK_ACCOUNT_CANCEL";

        #endregion

        //自定义属性
        //分割符号
        public static char[] sepForLine = new char[2] { '\r', '\n' };
        public static char[] sepForColumn = new char[1] { '|' };

        public static int maxSizeUncompressed = 1024 * 1024 * 30; // 30MB;

        /// <summary>
        /// 应用配置参数集合
        /// </summary>
        public static List<Spara> sParam;
        public static void GetParam()
        {
            MongoDBAccess<Spara> mongoAccess = new MongoDBAccess<Spara>(SYSConstant.BANK_CONFIG, SYSConstant.S_PARA);
            List<Spara> sPara = mongoAccess.FindAsByWhere(p => p.Key != null, 0);
            sParam = sPara;
        }
    }

    public class SystemSetInfo
    {
        // 接收成功文件备份保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_RCVFILE").Value
        public static string pathBackupSuccessRcvFile = @"E:\FTP\SUCCESSRCV\";

        // 发送成功文件备份保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_SNDFILE").Value
        public static string pathBackupSuccessSndFile = @"E:\FTP\SUCCESSSND\";

        // 结算中心处理失败文件备份保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value
        public static string pathBackupFieldRcv = @"E:\FTP\FAILRCV\";

        // 发送失败文件备份保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value
        public static string pathBackupFieldSnd = @"E:\FTP\FAILSND\";

        // 结算中心接收文件保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value
        public static string pathReceive = @"E:\FTP\RCV\";

        // 结算中心发送文件保存路径
        // SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value
        public static string pathSnd = @"E:\FTP\SND\";

        #region 时间格式
        public const string SqlfmtDateTime = "yyyy-MM-dd HH:mm:ss";//统一时间字符串格式
        public const string XmlDTimeFormat = "yyyy-MM-ddTHH:mm:ss";//经测试格式为2013-05-22T13:38:21
        public const string DateyyyyMMddHHmm = "yyyyMMddHHmm";
        public const string DateTimeFormate = "yyyyMMdd HHmmss";//统一时间字符串格式
        public const string SqlfmtDate = "yyyy-MM-dd";
        //public const string SqlfmtTime = "HH:mm:ss";
        public const string DatefmtYyyymmdd = "yyyyMMdd";//年月日
        public const string DatefmtyyyyMM = "yyyyMM";//年月
        public const string Datefmtyyyy = "yyyy";//年
        public const string DatefmtyyyyMMddHH = "yyyyMMddHH";
        public const string DatefmtyyyyMMddHHMMSS = "yyyyMMddHHmmss";
        public const string DatefmtyyMMdd = "yyMMdd";
        #endregion

        // 结算中心编码
        public const string SettleCenterCode = "010000";

        // 固定请求大小协议长度
        public const int FixedSize = 72;

        // 密钥前七位
        // SYSConstant.sParam.Find(p => p.Key == "SECRET_kEY").Value
        public const string SecretKey = "hbetcst";
    }

    /// <summary>
    /// 信息等级
    /// </summary>
    public enum ErrorLevel
    {
        致命 = 1,
        严重 = 2,
        警告 = 3,
        通告 = 4
    }

    /// <summary>
    /// 错误码
    /// </summary>
    public enum ErrorCode
    {
        成功 = 100,// 服务器已成功处理了请求
        已创建 = 101, // 请求成功并且服务器创建了新的资源
        已接受 = 102, // 服务器已接受请求，但尚未处理
        非授权信息 = 103 // 服务器已成功处理了请求，但返回的信息可能来自另一来源
    }

    /// <summary>
    /// 交易类型
    /// </summary>
    public enum ETransType
    {
        记账金客户转账结果数据=2001,
        记账保证金转账结果数据=2002,
        委托扣款协议信息=2009,
        解约信息=2010,
        保证金补缴成功信息 =2011,
        记账金保证金金额减少信息=2012,
        用户车辆冀通卡与帐户绑定信息=2008
    }

    /// <summary>
    /// 接受结果标志
    /// </summary>
    public enum EReceiveResult
    {
        接受成功=0,
        报文a校验失败=1,
        ftp文件失败=2,
        转账文件校验失败=3
    }
}
