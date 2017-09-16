using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// 银行账号与ETC卡绑定信息（2008号报文）定时发送任务
    /// </summary>
    public class BankAccountBingETCTask
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public BankAccountBingETCTask()
        {
            
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            while (true)
            {
                DateTime startTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + SYSConstant.sParam.Find(p => p.Key == "F_BANK_ACCOUNT_BINGETC_TASK_HEARTBEAT").Value);
                if (startTime.TimeOfDay < DateTime.Now.TimeOfDay)
                {
                    startTime = startTime.AddDays(1);
                }
                TimeSpan timeSpan = startTime - DateTime.Now;
                double interval = timeSpan.TotalMilliseconds;

                // 创建心跳
                System.Timers.Timer _HeartBeat = new System.Timers.Timer();
                _HeartBeat.Interval = System.Math.Abs(interval);
                _HeartBeat.Enabled = true;
                _HeartBeat.Elapsed += _HeartBeat_Elapsed;
                _HeartBeat.AutoReset = false;

                Thread.Sleep(24 * 60 * 60 * 1000);//每24小时启动一次
            }
        }

        private void _HeartBeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            try
            {
                MongoDBAccess<OutPutTaskWaitingDone> mongoAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                List<OutPutTaskWaitingDone> waitingDoneList = mongoAccess.FindAsByWhere(p => p.TransType == 2008 && p.Status == 0, 0);

                if (waitingDoneList != null && waitingDoneList.Any())
                {
                    MongoDBAccess<BankAgent> mongoBankAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
                    foreach (OutPutTaskWaitingDone waitingDone in waitingDoneList)
                    {
                        BankAgent bankAgent = mongoBankAccess.FindAsByWhere(p => p.BankTag.Equals(waitingDone.BankTag), 0).FirstOrDefault();
                        new SingleBankAccountBingETCTask(bankAgent).ProcessSingleTask(waitingDone);
                    }
                }
            }
            catch (Exception)
            {
                LogMessage.GetLogInstance().LogError("银行账号与ETC卡绑定信息（2008号报文）定时发送任务异常：" + ex.ToString());
            }
        }
    }


    /// <summary>
    /// SingleBankAccountBingETCTask
    /// </summary>
    public class SingleBankAccountBingETCTask
    {
        // 日志
        LogMessage logger;

        // 银行信息
        BankAgent bankAgent;

        // des 密匙
        string key;

        // 构造函数
        public SingleBankAccountBingETCTask(BankAgent bankAgent)
        {
            this.logger = new LogMessage(bankAgent.BankNo);
            this.bankAgent = bankAgent;
            this.key = SYSConstant.sParam.Find(p => p.Key == "SECRET_kEY").Value + DateTime.Now.ToString(SystemSetInfo.DatefmtyyMMdd) + DESEncrypt.GetRoadom().ToString();
        }
            /// <summary>
            /// 处理单个银行账号与ETC卡绑定信息（2008号报文）定时发送任务
            /// </summary>
            /// <param name="waitingDone"></param>
        public void ProcessSingleTask(OutPutTaskWaitingDone waitingDone)
        {
            try
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = this.bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "开始处理银行账号与ETC卡绑定信息定时发送任务" });
                logger.LogInfo("开始处理银行账号与ETC卡绑定信息定时发送任务");
                MongoDBAccess<BankAccountidCardNo> mongoAccess = new MongoDBAccess<BankAccountidCardNo>(SYSConstant.BANK_ACCOUNTID_CARDNO, waitingDone.ColName);
                List<BankAccountidCardNo> infoList = mongoAccess.FindAsByWhere(p => !string.IsNullOrEmpty(p.AccountId), 0);
                MongoDBAccess<OutPutTaskWaitingDone> mongoTaskAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                string fileName = string.Empty;
                if (!BuildPackage(out fileName, infoList, bankAgent, waitingDone.TransType))
                {
                    var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                    var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -1);
                    mongoTaskAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                    return;
                }
                if (SendToSFTP(fileName, bankAgent))
                {
                    if (SndMessageA(fileName))
                    {
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1).Set(p => p.SendTime, DateTime.Now.AddHours(8));
                        mongoTaskAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);

                        // Drop掉COLNAME对应集合
                        mongoAccess.DropCollection(waitingDone.ColName);
                    }
                    else
                    {
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -4);
                        mongoTaskAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                    }
                }
                else
                {
                    var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                    var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -2);
                    mongoTaskAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                }
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = this.bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "银行账号与ETC卡绑定信息定时发送任务处理完成" });
                logger.LogInfo("银行账号与ETC卡绑定信息定时发送任务处理完成");
            }
            catch (Exception ex)
            {
                logger.LogError("银行账号与ETC卡绑定信息（2008号报文）定时发送任务处理失败" + ex.ToString());
            }
        }

        /// <summary>
        /// 打包、加密
        /// </summary>
        /// <returns></returns>
        public bool BuildPackage(out string fileName, List<BankAccountidCardNo> infoList, BankAgent bankAgent, int transType)
        {
            try
            {
                string content = string.Empty;
                fileName = string.Format("0{0}{1}{2}.txt", SystemSetInfo.SettleCenterCode, bankAgent.BankNo, DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS));
                StreamWriter writer = null;
                FileStream fileStream = null;
                fileStream = File.Create(SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value + fileName);
                writer = new StreamWriter(fileStream, System.Text.Encoding.GetEncoding("GBK"));
                content = string.Format("0|{0}|{1}|{2}|{3}|{4}|\n", fileName.Split('.')[0] + ".CCF", SystemSetInfo.SettleCenterCode, bankAgent.BankNo, DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS), "1");
                writer.Write(content);

                content += string.Format("{0}|{1}|i|\n", transType, infoList.Count);
                writer.Write(string.Format("{0}|{1}|i|\n", transType, infoList.Count));
                foreach (BankAccountidCardNo info in infoList)
                {
                    string data = info._id + "|" + DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS) + "|" + info.AccountId + "|" + info.JtcardId + "|" + info.CardStatus + "|";
                    content += data + "\n";
                    writer.Write(data + "\n");
                }
                DESEncrypt des = new DESEncrypt(key);
                writer.Write(DESEncrypt.GetCRC(Encoding.GetEncoding("GBK").GetBytes(content)));

                writer.Flush();
                writer.Close();
                fileStream.Close();

                bool encryptResult = des.Encrypt(SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value + fileName, SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value + fileName.Split('.')[0] + ".CCF");
                fileName = fileName.Split('.')[0] + ".CCF";

                //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "生成文件" + fileName });
                logger.LogInfo("生成文件" + fileName);

                return encryptResult;
            }
            catch (Exception e)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = transType + "文件打包加密生成文件失败" });
                fileName = string.Empty;
                LogMessage.GetLogInstance().LogError(bankAgent.BankName + transType + "文件加密打包失败：" + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// 上传sftp
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        private bool SendToSFTP(string fileName, BankAgent bankAgent)
        {
            bool result = false;
            using (Ftp ftp = new Ftp(bankAgent))
            {
                result = ftp.UpLoad(fileName);
            }
            if (!result)
            {
                LogMessage.GetLogInstance().LogError(fileName + "文件上传至FTP失败！");
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = fileName + "文件上传至FTP失败" });
                return false;
            }
            logger.LogInfo(fileName + "文件上传至FTP");
            return result;
        }

        /// <summary>
        /// 根据对方银行端口发送实时a号报文
        /// </summary>
        /// <param name="bankAgent">银行信息</param>
        private bool SndMessageA(string fileName)
        {
            List<byte> temp = new List<byte>();
            byte[] a = new byte[4];
            a[0] = 0;
            a[1] = 0;
            a[2] = 0;
            a[3] = 68;
            temp.AddRange(a);
            string sourceCode = "a" + SystemSetInfo.SettleCenterCode + bankAgent.BankNo + fileName + key;

            try
            {
                DESEncrypt des = new DESEncrypt(key);
                sourceCode += DESEncrypt.GetCRC(Encoding.GetEncoding("GBK").GetBytes(sourceCode));
                byte[] messageA = Encoding.ASCII.GetBytes(sourceCode);
                temp.AddRange(messageA);
                SocketCommunication socket = new SocketCommunication(DateTime.Now);
                if (socket.Connect(IPAddress.Parse(bankAgent.IPAddress), bankAgent.Port) != null)
                {
                    socket.Send(temp.ToArray());
                }
                else
                {
                    ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "建立连接失败，a报文发送失败" + Encoding.ASCII.GetString(temp.ToArray()) });
                    logger.LogInfo(bankAgent.BankName + "建立连接失败，a报文发送失败" + Encoding.ASCII.GetString(messageA));
                    LogMessage.GetLogInstance().LogError(bankAgent.BankName + "建立连接失败，a报文发送失败" + Encoding.ASCII.GetString(messageA));
                    return false;
                }
                logger.LogInfo(bankAgent.BankName + "a报文发送成功：" + Encoding.ASCII.GetString(messageA));
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "a报文发送失败" + Encoding.ASCII.GetString(temp.ToArray()) });
                LogMessage.GetLogInstance().LogError(bankAgent.BankName + "发送a报文失败！原因：" + ex.ToString());
                return false;
            }
        }
    }
}
