using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// 输入定时处理任务数据集合
    /// </summary>
    public class InPutTaskWaitingDone
    {
        /// <summary>
        /// pk
        /// </summary>
        public ObjectId _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 交易类型
        /// 2001：记账金客户转账数据
        /// 2002：记账保证金客户转账数据
        /// 2009：委托扣款协议信息
        /// 2011：保证金补缴程序信息
        /// </summary>
        public int TransType { get; set; }

        /// <summary>
        /// 库名
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// 集合名
        /// </summary>
        public string ColName { get; set; }

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 1：已同步到核心（终态）
        /// 0：文件已入中间库待同步到核心
        /// -1：ftp下载文件失败（文件不存在/ftp服务器通信失败等原因）
        /// -2：解密文件失败
        /// -3：校验文件失败
        /// -4：其他错误原因
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 加解密密钥
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 扣款成功数量
        /// </summary>
        public int SuccessNum { get; set; }

        /// <summary>
        /// 扣款成功金额（分）
        /// </summary>
        public long SuccessAmount { get; set; }

        /// <summary>
        /// 扣款失败数量
        /// </summary>
        public int FailNum { get; set; }

        /// <summary>
        /// 扣款失败金额（分）
        /// </summary>
        public long FailAmount { get; set; }
    }

    /// <summary>
    /// 输出定时处理任务数据集合
    /// </summary>
    public class OutPutTaskWaitingDone
    {
        /// <summary>
        /// PK
        /// </summary>
        public ObjectId _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 交易类型
        /// 2001：记账金客户转账数据
        /// 2002：记账保证金客户转账数据
        /// 2010：解约信息
        /// </summary>
        public int TransType { get; set; }

        /// <summary>
        /// 当TRANSTYPE为2001时本字段为0代表T-1日的未扣款交易；为1代表T-1日之前的未扣款交易和银行扣款失败交易
        /// </summary>
        public int PriorityLevel { get; set; }

        /// <summary>
        /// 库名
        /// </summary>
        public string DbName { get; set; }

        /// <summary>
        /// 集合名
        /// </summary>
        public string ColName { get; set; }

        /// <summary>
        /// 文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 1：文件已上传到ftp（终态）
        /// 0：中间数据已生成,待ftp发送
        /// -1：文件生成失败
        /// -2：上传ftp失败
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 文件上传ftp时刻
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 加解密密钥
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 待扣款总笔数
        /// </summary>
        public int TotalNum { get; set; }

        /// <summary>
        /// 待扣款总金额（分）
        /// </summary>
        public long TotalAmount { get; set; }
    }

    /// <summary>
    /// 异常任务集合
    /// </summary>
    public class ExceptionTaskWaitingDone : InPutTaskWaitingDone
    {
        public string OperationType { get; set; }
    }

}
