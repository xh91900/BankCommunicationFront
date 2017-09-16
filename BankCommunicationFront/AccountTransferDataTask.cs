using MongoDB.Bson;
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
    /// 记账金客户转账（2001和2002号文件扣款请求）数据定时发送任务
    /// </summary>
    public class AccountTransferDataTask
    {
        // 最多几个线程同时访问
        SemaphoreSlim semLim;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AccountTransferDataTask()
        {

        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            while (true)
            {
                try
                {
                    // 设置最大线程数20
                    semLim = new SemaphoreSlim(int.Parse(SYSConstant.sParam.Find(p => p.Key == "F_ACCOUNT_TRANSFER_DATA_TASK_SEMAPHORE_SLIM").Value));
                    MongoDBAccess<BankAgent> mongoAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
                    List<BankAgent> bankAgentList = mongoAccess.FindAsByWhere(p => p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.记账金客户转账结果数据 || o.File_Task_Type == (int)ETransType.记账保证金转账结果数据), 0);// 获取所有银行配置信息
                    foreach (var item in bankAgentList)
                    {
                        DateTime startTime = DateTime.Parse(DateTime.Now.ToShortDateString() + " " + item.MrcvTime);
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
                        _HeartBeat.Elapsed += (p, q) =>
                        {
                            // 超过设置的最大线程数则阻止当前线程进入
                            semLim.Wait();
                            new SingleAccountTransferDataTask(item).ProcessSingleTask();
                            // 执行完毕给一个退出信号
                            semLim.Release();
                        };
                        _HeartBeat.AutoReset = false;
                    }
                }
                catch (AggregateException exception)
                {
                    semLim.Dispose();
                    // 记录所有子线程的异常日志
                    foreach (var ex in exception.InnerExceptions)
                    {
                        LogMessage.GetLogInstance().LogError("定时执行记账金客户转账异常：" + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    semLim.Dispose();
                    LogMessage.GetLogInstance().LogError("定时执行记账金客户转账异常：" + ex.ToString());
                }

                Thread.Sleep(int.Parse(SYSConstant.sParam.Find(p => p.Key == "F_ACCOUNT_TRANSFER_DATA_TASK_HEARTBEAT").Value) * 60 * 60 * 1000); // 每24小时启动一次)
            }
        }
    }

    /// <summary>
    /// SingleAccountTransferDataTask
    /// </summary>
    public class SingleAccountTransferDataTask
    {
        // 日志
        LogMessage logger;

        // 银行信息
        BankAgent bankAgent;

        // des 密匙
        string key;

        public SingleAccountTransferDataTask(BankAgent bankAgent)
        {
            this.logger = new LogMessage(bankAgent.BankNo);
            this.bankAgent = bankAgent;
            this.key = SYSConstant.sParam.Find(p => p.Key == "SECRET_kEY").Value + DateTime.Now.ToString(SystemSetInfo.DatefmtyyMMdd) + DESEncrypt.GetRoadom().ToString();
        }

        /// <summary>
        /// 记账金客户转账（2001和2002号文件扣款请求）数据定时发送任务
        /// </summary>
        public void ProcessSingleTask()
        {
            try
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = this.bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "开始处理记账金客户转账、记账保证金转账数据定时发送任务" });

                // 如果获取出库任务失败了，会导致该outputtask任务第二天发送。
                MongoDBAccess<OutPutTaskWaitingDone> dbAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                List<OutPutTaskWaitingDone> waitingDoneList = dbAccess.FindAsByWhere(p => p.BankTag == bankAgent.BankTag && p.TransType == (int)ETransType.记账金客户转账结果数据 && p.Status == 0 && p.PriorityLevel == 0, 0);
                ProcessWaitingDone(waitingDoneList, ETransType.记账金客户转账结果数据);

                waitingDoneList = dbAccess.FindAsByWhere(p => p.TransType == (int)ETransType.记账金客户转账结果数据 && p.Status == 0 && p.PriorityLevel == 1 && p.BankTag == bankAgent.BankTag, 0);
                ProcessWaitingDone(waitingDoneList, ETransType.记账金客户转账结果数据);

                waitingDoneList = dbAccess.FindAsByWhere(p => p.TransType == (int)ETransType.记账金客户转账结果数据 && p.Status == 0 && p.PriorityLevel == 2 && p.BankTag == bankAgent.BankTag, 0);
                ProcessWaitingDone(waitingDoneList, ETransType.记账金客户转账结果数据);

                waitingDoneList = dbAccess.FindAsByWhere(p => p.TransType == (int)ETransType.记账保证金转账结果数据 && p.Status == 0 && p.PriorityLevel == 0 && p.BankTag == bankAgent.BankTag, 0);
                ProcessWaitingDone(waitingDoneList, ETransType.记账保证金转账结果数据);

                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = this.bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "记账金客户转账、记账保证金转账数据定时发送任务处理完成" });
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("定时执行记账金客户转账异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 处理单个中间业务数据
        /// </summary>
        /// <param name="waitingDoneList"></param>
        /// <param name="dbName"></param>
        private void ProcessWaitingDone(List<OutPutTaskWaitingDone> waitingDoneList, ETransType transType)
        {
            try
            {
                MongoDBAccess<OutPutTaskWaitingDone> dbAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                if (waitingDoneList == null || !waitingDoneList.Any())
                {
                    return;
                }

                string fileName;

                foreach (var waitingDone in waitingDoneList)
                {
                    if (waitingDone.CreateTime.Date != DateTime.Now.Date)
                    {
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1).Set(p => p.Key, this.key).Set(p => p.SendTime, DateTime.Now.AddHours(8)).Set(p => p.TotalNum, 0).Set(p => p.TotalAmount, 0);
                        dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                        if (!string.IsNullOrEmpty(waitingDone.ColName))
                        {
                            new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, waitingDone.ColName).DropCollection(waitingDone.ColName);
                        }
                        logger.LogError("记账金客户转账异常：交易包" + waitingDone._id + "是前一天生成的扣款文件，不发送给银行扣款");
                        continue;
                    }

                    ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "开始处理" + waitingDone.TransType + "号报文" });
                    logger.LogInfo("开始处理" + waitingDone.TransType + "号报文");

                    // 仅2001号报文，发心跳报文
                    if (transType == ETransType.记账保证金转账结果数据 && string.IsNullOrEmpty(waitingDone.ColName))
                    {
                        return;
                    }

                    List<TransactionInfo> transactionInfos;
                    if (string.IsNullOrEmpty(waitingDone.ColName))
                    {
                        transactionInfos = new List<TransactionInfo>();
                    }
                    else
                    {
                        MongoDBAccess<TransactionInfo> access = new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, waitingDone.ColName);
                        transactionInfos = access.FindAsByWhere(p => !string.IsNullOrEmpty(p.ACBAccount), 0);// 获取所有交易
                    }
                    if (transactionInfos == null) { transactionInfos = new List<TransactionInfo>(); }
                    logger.LogInfo("结算中心发起批量扣款集合" + waitingDone.ColName + "包含" + transactionInfos.Count + "笔交易，涉及总金额：" + transactionInfos.Sum(p => p.Income / 100) + "元");
                    if (BuildAndSavePackage(transactionInfos, waitingDone.TransType, out fileName))
                    {
                        if (SndToFTP(fileName))
                        {
                            if (SndMessageA(fileName))
                            {
                                var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                                var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1).Set(p => p.Key, this.key).Set(p => p.SendTime, DateTime.Now.AddHours(8)).Set(p => p.TotalNum, transactionInfos.Count).Set(p => p.TotalAmount, transactionInfos.Sum(p => p.Income));
                                dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                                if (!string.IsNullOrEmpty(waitingDone.ColName))
                                {
                                    new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, waitingDone.ColName).DropCollection(waitingDone.ColName);
                                }
                            }
                            else
                            {
                                var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                                var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -4).Set(p => p.Key, this.key);
                                dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                            }
                        }
                        else
                        {
                            var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                            var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -2).Set(p => p.Key, this.key);
                            dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                            logger.LogError("记账金客户转账异常：交易包" + waitingDone._id + "生成的文件至ftp服务器失败");
                        }
                    }
                    else
                    {
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<OutPutTaskWaitingDone>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, -1).Set(p => p.Key, this.key);
                        dbAccess.UpdateDocs(p => p._id == waitingDone._id, mUpdateDefinition);
                        logger.LogError("记账金客户转账异常：交易包" + waitingDone._id + "生成文件、加密、存放在本地发送目录snd下失败");
                    }

                    // 每个包发送间隔十分钟
                    Thread.Sleep(1000 * 60 * int.Parse(SYSConstant.sParam.Find(p => p.Key == "F_ACCOUNT_TRANSFER_DATA_TASK_SNDSPAN").Value));
                }
            }
            catch (Exception ex)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = this.bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "处理单个中间业务数据异常" });
                LogMessage.GetLogInstance().LogError($"{bankAgent.BankName}:处理单个中间业务数据异常：{ex.ToString()}");
            }
        }

        /// <summary>
        /// 打包、加密
        /// </summary>
        /// <param name="transactionInfos">转账数据</param>
        /// <param name="transType">包类型</param>
        /// <param name="fileName">文件名</param>
        /// <returns>bool</returns>
        private bool BuildAndSavePackage(List<TransactionInfo> transactionInfos, int transType, out string fileName)
        {
            try
            {
                StringBuilder content = new StringBuilder();
                fileName = string.Format("0{0}{1}{2}.txt", SystemSetInfo.SettleCenterCode, bankAgent.BankNo, DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS));
                StreamWriter writer = null;
                FileStream fileStream = null;
                fileStream = File.Create(SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value + fileName);
                writer = new StreamWriter(fileStream, System.Text.Encoding.GetEncoding("GBK"));
                content.Append(string.Format("0|{0}|{1}|{2}|{3}|{4}|\n", fileName.Split('.')[0] + ".CCF", SystemSetInfo.SettleCenterCode, bankAgent.BankNo, DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS), "1"));
                writer.Write(content);

                content.Append(string.Format("{0}|{1}|i|\n", transType, transactionInfos.Count));
                writer.Write(string.Format("{0}|{1}|i|\n", transType, transactionInfos.Count));
                foreach (TransactionInfo info in transactionInfos)
                {
                    string data = info._id + "|" + DateTime.Now.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS) + "|" + info.ACBAccountN + "|" + info.ACBAccount + "|" + bankAgent.CreditProvisionsName + "|" + bankAgent.CreditProvisionsNo + "|" + info.AccType + "|" + bankAgent.BankNo + "|" + bankAgent.BankName + "|" + (decimal.Parse(info.Income.ToString()) / 100).ToString("F2") + "|" + info.PlateNumbers + "|" + info.VehicleType + "|" + info.TransTime.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS) + "|";
                    content.Append(data + "\n");
                    writer.Write(data + "\n");
                }
                DESEncrypt des = new DESEncrypt(key);
                writer.Write(DESEncrypt.GetCRC(Encoding.GetEncoding("GBK").GetBytes(content.ToString())));
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
        /// <returns>bool</returns>
        private bool SndToFTP(string fileName)
        {
            try
            {
                using (Ftp ftp = new Ftp(bankAgent))
                {
                    if (!ftp.UpLoad(fileName))
                    {
                        LogMessage.GetLogInstance().LogError(fileName + "文件上传至FTP失败！");
                        ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = fileName + "文件上传至FTP失败" });
                        return false;
                    }
                    //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = fileName + "文件上传至FTP" });
                    logger.LogInfo(fileName + "文件上传至FTP");

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                    logger.LogError(bankAgent.BankName + "建立连接失败，a报文发送失败" + Encoding.ASCII.GetString(messageA));
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
