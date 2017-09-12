using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// BANK_AGENT集合，其与核心库中的ISSUE.BANK_AGENT相同
    /// </summary>
    public class BankAgent
    {
        /// <summary>
        /// pk
        /// </summary>
        public ObjectId _id { get; set; }

        // 银行代码(弃)
        //public string Bank_Code { get; set; }

        /// <summary>
        /// FTPHost
        /// </summary>
        public string FTPHost { get; set; }

        /// <summary>
        /// 两位数字标识，唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 银行行号
        /// </summary>
        public string BankNo { get; set; }

        /// <summary>
        /// 银行名称
        /// </summary>
        public string BankName { get; set; }

        /// <summary>
        /// 记账卡备付金户账号名称
        /// </summary>
        public string CreditProvisionsName { get; set; }

        /// <summary>
        /// 记账卡备付金户账号
        /// </summary>
        public string CreditProvisionsNo { get; set; }

        /// <summary>
        /// 报文交易数上限
        /// </summary>
        public int TransCountMax { get; set; }

        /// <summary>
        /// 银行上午接收扣款请求文件的时间点
        /// </summary>
        public string MrcvTime { get; set; }

        /// <summary>
        /// 银行下午午接收扣款请求文件的时间点
        /// </summary>
        public string ArcvTime { get; set; }

        /// <summary>
        /// 银行系统IP地址
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 银行系统端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// ftp用户名
        /// </summary>
        public string FTPUserName { get; set; }

        /// <summary>
        /// ftp密码
        /// </summary>
        public string FTPPwd { get; set; }

        /// <summary>
        /// ftp端口号
        /// </summary>
        public int FTPPort { get; set; }

        /// <summary>
        /// ETC方从此目录接收银行文件
        /// </summary>
        public string RcvFtpDri { get; set; }

        /// <summary>
        /// ETC方从此目录发送文件给银行方
        /// </summary>
        public string SndFtpDri { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 银行支持任务
        /// </summary>
        public List<BankSupportTask> BankSupportTasks = new List<BankSupportTask>();
    }

    /// <summary>
    /// 银行支持任务表
    /// </summary>
    public class BankSupportTask
    {
        /// <summary>
        /// 主键 自增
        /// </summary>
        //public int Id { get; set; }

        ///// <summary>
        ///// 银行代码，固定6位
        ///// </summary>
        //public string Bank_Code { get; set; }

        /// <summary>
        /// 负2001-旧版记账金客户转账数据
        /// 2001-新版记账金客户转账数据
        /// 2002-记账金保证金转账数据
        /// 负2009-旧版签约信息
        /// 2009-新版签约信息
        /// 2010-解约信息
        /// 2011-记账金保证金补缴成功信息
        /// 2012-记账金保证金金额减少信息
        /// </summary>
        public int File_Task_Type { get; set; }

        /// <summary>
        /// 对FILETASKTYPE的详细说明
        /// </summary>
        public string Remark { get; set; }
    }
}
