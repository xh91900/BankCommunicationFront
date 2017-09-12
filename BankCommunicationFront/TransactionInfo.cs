using MongoDB.Bson;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// 交易信息集合（表）说明（2001号和2002报文方向0/1）
    /// </summary>
    public class TransactionInfo
    {
        /// <summary>
        /// 流水号PROXY_ACCOUNT_DETAIL.ID,pk,ListNO
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 记账卡客户账号,AccountId
        /// </summary>
        public string ACBAccount { get; set; }

        /// <summary>
        /// 记账卡客户账号名称,AccountName
        /// </summary>
        public string ACBAccountN { get; set; }

        /// <summary>
        /// 银行账户类型
        /// 对公0 ；储蓄1；信用卡2；
        /// </summary>
        public int AccType { get; set; }

        /// <summary>
        /// 交易金额（分）,AMOUNT
        /// </summary>
        public int Income { get; set; }

        /// <summary>
        /// 车牌号
        /// </summary>
        public string PlateNumbers { get; set; }

        /// <summary>
        /// 交易时刻,ICTransTime
        /// </summary>
        public DateTime TransTime { get; set; }

        /// <summary>
        /// 车型：0-未知；1-客一；2-客二；3-客三；
        /// 4-客四；5-货1；6-货2；7-货3；8-货4；9-货5
        /// </summary>
        public int VehicleType { get; set; }

        /// <summary>
        /// 银行（成功/失败）扣款返回时间
        /// </summary>
        public DateTime BankChargeTime { get; set; }

        /// <summary>
        /// 扣款结果
        /// 0-转账成功；1-账户余额不足；2-源账户不存在；3-目的账户不存在；4-未授权；5-其他
        /// </summary>
        public int Result { get; set; }
    }

    /// <summary>
    /// 签约信息（2009和2011号报文方向1）
    /// </summary>
    public class BankAccountSign
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
        /// 银行方发送的委托扣款协议信息的AccountID银行账号
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息的BL_Corp用户所属企业名称
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金金额（分）
        /// </summary>
        public int CashDeposit { get; set; }

        /// <summary>
        /// 银行端的生成日期
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 0-	新增的签约交易
        /// 1-	修改的签约交易（修改保证金）
        /// 2-	保证金补缴成功交易，若没有其他扣款失败的交易则需要漂白黑名单
        /// </summary>
        public int Command { get; set; }

        /// <summary>
        /// 银行发送的文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 0-	已入中间库未同步到核心
        /// 1-	已同步到核心（终态）
        /// </summary>
        public int Status { get; set; }
    }

    /// <summary>
    /// 银行账号与ETC卡绑定信息库及其集合
    /// </summary>
    public class BankAccountidCardNo
    {
        /// <summary>
        /// 二十位流水号String[20]
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 记账卡客户账号名称
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// ETC卡号
        /// </summary>
        public string JtcardId { get; set; }

        /// <summary>
        /// ETC卡片状态
        /// </summary>
        public string CardStatus { get; set; }
    }

    /// <summary>
    /// 一卡通黑白名单
    /// </summary>
    public class BankAccountYKTSign
    {
        /// <summary>
        /// pk，accountid
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 银行方发送的委托扣款协议信息的BL_Corp用户所属企业名称
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金金额（分）
        /// </summary>
        public int CashDeposit { get; set; }

        /// <summary>
        /// 银行端的生成日期
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 0-	账户状态正常
        /// 1-	账户余额不足
        /// </summary>
        public int Status { get; set; }
    }

    /// <summary>
    /// 解约信息集合说明（2010和2012号报文方向0）
    /// </summary>
    public class BankAccountCancel
    {
        /// <summary>
        /// 流水号String[20],归集的时候自己生成
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// 两位十进制数字字符串唯一确定一家银行
        /// </summary>
        public string BankTag { get; set; }

        /// <summary>
        /// 发送给银行信息的AccountID银行账号
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 发送给银行信息的BL_Corp用户所属企业名称（扣款用户名称）
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// 保证金减少部分的金额，记账金保证金金额减少信息
        /// </summary>
        public int CashDepositCut { get; set; }

        /// <summary>
        /// 解约/解绑车辆生效时刻
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记录创建时刻
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 车牌号，记账金保证金金额减少信息
        /// </summary>
        public string PlateNumbers{ get; set; }

        /// <summary>
        /// 发送给银行的文件名（不含路径）
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 0-	已入中间库未发送
        /// 1-	已打包发送给银行
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 0-	2010号报文交易
        /// 1-	2012号报文交易
        /// </summary>
        public int Command { get; set; }
    }

    /// <summary>
    /// 记账金客户转账数据
    /// </summary>
    public class AccountTransferData
    {
        /// <summary>
        /// 流水号，作为判重依据
        /// </summary>
        public string ListNO { get; set; }

        /// <summary>
        /// 生成日期时间
        /// ccyymmddhhmmss 格式，例如 20100214085151
        /// </summary>
        public DateTime GenTime { get; set; }

        /// <summary>
        /// 记账卡客户账号名称
        /// </summary>
        public string ACBAccountN { get; set; }

        /// <summary>
        /// 记账卡客户账号
        /// </summary>
        public string ACBAccount { get; set; }

        /// <summary>
        /// 记账卡汇缴户帐号名称
        /// </summary>
        public string ACAccountN { get; set; }

        /// <summary>
        /// 记账卡汇缴户帐号
        /// </summary>
        public string ACAccount { get; set; }


        /// <summary>
        /// 记账卡客户账户类型：0—对公；1—储蓄；2—信用卡
        /// </summary>
        public int AccType { get; set; }

        /// <summary>
        /// 清算银行代码
        /// </summary>
        public string Xdzfrhhh { get; set; }

        /// <summary>
        /// 清算银行名称
        /// </summary>
        public string BankName { get; set; }

        /// <summary>
        /// 转账金额，单位为元，小数点后两位，总长不超过14位
        /// </summary>
        public int Income { get; set; }

        /// <summary>
        /// 通行交易的车牌号，当无法从车道交易准确获取车牌号时，填为“未知”
        /// </summary>
        public string PlateNumbers { get; set; }

        /// <summary>
        /// 车型：0-未知；1-客一；2-客二；3-客三
        /// 4-客四；5-货1；6-货2；7-货3；8-货4；9-货5
        /// </summary>
        public int VehicleType { get; set; }

        /// <summary>
        /// 通行交易时间，ccyymmddhhmmss 格式
        /// </summary>
        public DateTime TransTime { get; set; }
    }

    /// <summary>
    /// 报文结构
    /// </summary>
    public class MessageStruct : IRequestInfo
    {
        public string BodyLenth { get; set; }
        public string MessageNo { get; set; }
        public string SenderCode { get; set; }

        public string ReceiverCode { get; set; }

        public string FileName { get; set; }

        public string Key { get; set; }

        public string VerifyCode { get; set; }

        // 冗余银行信息
        public BankAgent BankAgent { get; set; }
    }

    /// <summary>
    /// BANK_CONFIG配置库
    /// </summary>
    public class Spara
    {
        /// <summary>
        /// pk
        /// </summary>
        public ObjectId _id;

        /// <summary>
        /// key
        /// </summary>
        public string Key;

        /// <summary>
        /// value
        /// </summary>
        public string Value;

        /// <summary>
        /// Remark
        /// </summary>
        public string Remark;
    }
}
