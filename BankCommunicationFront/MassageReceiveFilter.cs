using SuperSocket.Facility.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// MessageReceiveFilter
    /// </summary>
    public class MessageReceiveFilter: FixedSizeReceiveFilter<MessageStruct>
    {
        public MessageReceiveFilter() : base(SystemSetInfo.FixedSize)
        { }

        /// <summary>
        /// 解析收到的固定长度报文
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="toBeCopied"></param>
        /// <returns></returns>
        protected override MessageStruct ProcessMatchedRequest(byte[] buffer, int offset, int length, bool toBeCopied)
        {
            MessageStruct messageAC = new MessageStruct();
            try
            {
                string message = Encoding.ASCII.GetString(buffer, offset, length);
                messageAC.BodyLenth = message.Substring(0, 4);
                messageAC.MessageNo = message.Substring(4, 1);
                messageAC.SenderCode = message.Substring(5, 6);
                messageAC.ReceiverCode = message.Substring(11, 6);
                messageAC.FileName = message.Substring(17, 31);
                messageAC.Key = message.Substring(48, 16);
                messageAC.VerifyCode = message.Substring(64, 8);

                // 冗余银行信息
                MongoDBAccess<BankAgent> mongoAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
                messageAC.BankAgent = mongoAccess.FindAsByWhere(p => p.BankNo == messageAC.SenderCode, 0).FirstOrDefault();

                var logger = new LogMessage(messageAC.BankAgent.BankNo);
                logger.LogInfo("接收到c报文：" + message.Substring(4, 68));
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = messageAC.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "接收到c报文：" + message.Substring(4, 68) });
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("解析c报文异常：" + ex.ToString());
            }

            return messageAC;
        }
    }
}
