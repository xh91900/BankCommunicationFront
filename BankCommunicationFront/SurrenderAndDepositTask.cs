using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace BankCommunicationFront
{
    /// <summary>
    /// 解约信息及保证金金额减少信息（2010和2012号报文）定时发送任务
    /// </summary>
    public class CancellationAndDepositTask
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public CancellationAndDepositTask()
        {
        }

        // 解约信息及保证金金额减少信息（2010和2012号报文）定时发送任务
        public void Start()
        {
            while (true)
            {
                try
                {
                    MongoDBAccess<BankAgent> mongoAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
                    List<BankAgent> bankAgentList = mongoAccess.FindAsByWhere(p => p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.解约信息), 0);// 获取所有银行配置信息
                    foreach (var bankAgent in bankAgentList)
                    {
                        var singleTask = new SingleCancellationAndDepositTask(bankAgent);
                        singleTask.ProcessSingleOne(0, ETransType.解约信息);
                    }

                    bankAgentList = mongoAccess.FindAsByWhere(p => p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.记账金保证金金额减少信息), 0);
                    foreach (var bankAgent in bankAgentList)
                    {
                        var singleTask = new SingleCancellationAndDepositTask(bankAgent);
                        singleTask.ProcessSingleOne(1, ETransType.记账金保证金金额减少信息);
                    }

                    bankAgentList = mongoAccess.FindAsByWhere(p => !p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.解约信息), 0);// 获取所有不支持的银行配置信息
                    foreach (var bankAgent in bankAgentList)
                    {
                        var singleTask = new SingleCancellationAndDepositTask(bankAgent);
                        singleTask.ProcessNoSupportOne(0);
                    }

                    bankAgentList = mongoAccess.FindAsByWhere(p => !p.BankSupportTasks.Any(o => o.File_Task_Type == (int)ETransType.记账金保证金金额减少信息), 0);// 获取所有不支持的银行配置信息
                    foreach (var bankAgent in bankAgentList)
                    {
                        var singleTask = new SingleCancellationAndDepositTask(bankAgent);
                        singleTask.ProcessNoSupportOne(1);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.GetLogInstance().LogError("解约信息及保证金金额减少信息处理异常：" + ex.ToString());
                }
                Thread.Sleep(int.Parse(SYSConstant.sParam.Find(p => p.Key == "F_CANCELLATION_AND_DEPOSIT_TASK_HEARTBEAT").Value) * 60 * 1000);// 一小时启动一次
            }
        }
    }

    public class SingleCancellationAndDepositTask
    {
        // 日志
        LogMessage logger;

        // 银行信息
        BankAgent bankAgent;

        // des 密匙
        string key;

        public SingleCancellationAndDepositTask(BankAgent bankAgent)
        {
            this.logger = new LogMessage(bankAgent.BankNo);
            this.bankAgent = bankAgent;
            this.key = SYSConstant.sParam.Find(p => p.Key == "SECRET_kEY").Value + DateTime.Now.ToString(SystemSetInfo.DatefmtyyMMdd) + DESEncrypt.GetRoadom().ToString();
        }

        /// <summary>
        /// 处理单个解约信息及保证金金额减少信息
        /// </summary>
        /// <param name="bankAgentList">银行信息</param>
        /// <param name="command">操作类型</param>
        /// <param name="transType">交易类型</param>
        public void ProcessSingleOne(int command, ETransType transType)
        {
            try
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "开始处理" + transType.ToString() });
                logger.LogInfo("开始处理" + transType.ToString());
                string fileName;
                MongoDBAccess<BankAccountCancel> access = new MongoDBAccess<BankAccountCancel>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_CANCEL);
                List<BankAccountCancel> accountCancelList = access.FindAsByWhere(p => p.BankTag.Equals(bankAgent.BankTag) && p.Status == 0 && p.Command == command, 0);
                if (accountCancelList != null && accountCancelList.Any())
                {
                    // 取出的数据打包、加密
                    if (BuildPackage(out fileName, accountCancelList, transType))
                    {
                        if (SendToSFTP(fileName))
                        {
                            if (SendMessageA(fileName, (int)transType))
                            {
                                foreach (var accountCancel in accountCancelList)
                                {
                                    // 更新（2）取出的所有数据，FILENAME为发送的文件名（不含路径），STATUS为1
                                    var mUpDefinitionBuilder = new UpdateDefinitionBuilder<BankAccountCancel>();
                                    var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1).Set(o => o.FileName, fileName);
                                    access.UpdateDocs(p => p._id == accountCancel._id, mUpdateDefinition);
                                }
                            }
                            else
                            {
                                InsertIntoOutputTaskWaitingDone(new OutPutTaskWaitingDone() { BankTag = bankAgent.BankTag, Status = -4, SendTime = DateTime.Now.AddHours(8), FileName = fileName, Key = this.key });
                            }
                        }
                        else
                        {
                            LogMessage.GetLogInstance().LogError("解约信息及保证金金额减少信息上传ftp失败");
                            InsertIntoOutputTaskWaitingDone(new OutPutTaskWaitingDone() { BankTag = bankAgent.BankTag, Status = -2, SendTime = DateTime.Now.AddHours(8), FileName = fileName, Key = this.key });
                        }
                    }
                    else
                    {
                        LogMessage.GetLogInstance().LogError("解约信息及保证金金额减少信息打包加密失败");
                        InsertIntoOutputTaskWaitingDone(new OutPutTaskWaitingDone() { BankTag = bankAgent.BankTag, Status = -1, SendTime = DateTime.Now.AddHours(8), FileName = fileName, Key = this.key });
                    }
                }
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = transType.ToString() + "处理完成" });
                logger.LogInfo(transType.ToString() + "处理完成");
            }
            catch (Exception ex)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "处理单个解约信息及保证金金额减少信息异常" });

                LogMessage.GetLogInstance().LogError("处理单个解约信息及保证金金额减少信息异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 处理不支持的银行
        /// </summary>
        /// <param name="command">0-	2010号报文交易,1-	2012号报文交易</param>
        public void ProcessNoSupportOne(int command)
        {
            try
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "开始处理不支持解约及保证金金额减少的银行" });
                logger.LogInfo("开始处理不支持解约及保证金金额减少的银行");
                MongoDBAccess<BankAccountCancel> access = new MongoDBAccess<BankAccountCancel>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_CANCEL);
                List<BankAccountCancel> accountCancelList = access.FindAsByWhere(p => p.BankTag.Equals(bankAgent.BankTag) && p.Status == 0 && p.Command == command, 0);
                if (accountCancelList != null && accountCancelList.Any())
                {
                    foreach (var accountCancel in accountCancelList)
                    {
                        // 更新（2）取出的所有数据，FILENAME为发送的文件名（不含路径），STATUS为1
                        var mUpDefinitionBuilder = new UpdateDefinitionBuilder<BankAccountCancel>();
                        var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 1).Set(o => o.FileName, "");
                        access.UpdateDocs(p => p._id == accountCancel._id, mUpdateDefinition);
                    }
                }
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "不支持解约及保证金金额减少的银行处理完成" });
                logger.LogInfo("不支持解约及保证金金额减少的银行处理完成");
            }
            catch (Exception ex)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "不支持解约及保证金金额减少的银行处理异常" });
                logger.LogInfo("不支持解约及保证金金额减少的银行处理异常：" + ex.ToString());
            }
        }

        /// <summary>
        /// 打包、加密
        /// </summary>
        /// <returns></returns>
        private bool BuildPackage(out string fileName, List<BankAccountCancel> accountCancelList, ETransType transType)
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

                content += string.Format("{0}|{1}|i|\n", (int)transType, accountCancelList.Count);
                writer.Write(string.Format("{0}|{1}|i|\n", (int)transType, accountCancelList.Count));
                if (transType == ETransType.解约信息)
                {
                    foreach (BankAccountCancel info in accountCancelList)
                    {
                        string data = info._id + "|" + info.GenTime.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS) + "|" + info.AccountId + "|" + info.AccountName + "|";
                        content += data + "\n";
                        writer.Write(data + "\n");
                    }
                }
                else if (transType == ETransType.记账金保证金金额减少信息)
                {
                    foreach (BankAccountCancel info in accountCancelList)
                    {
                        string data = info._id + "|" + info.GenTime.ToString(SystemSetInfo.DatefmtyyyyMMddHHMMSS) + "|" + info.AccountId + "|" + info.AccountName + "|" + info.PlateNumbers + "|" + (decimal.Parse(info.CashDepositCut.ToString()) / 100).ToString("F2") + "|";
                        content += data;
                        writer.Write(data + "\n");
                    }
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
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = transType.ToString() + "打包加密生成文件失败" });
                fileName = string.Empty;
                LogMessage.GetLogInstance().LogError(bankAgent.BankName+ transType.ToString() + "加密打包失败：" + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// 上传sftp
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        private bool SendToSFTP(string fileName)
        {
            try
            {
                using (Ftp ftp = new Ftp(bankAgent))
                {
                    if (!ftp.UpLoad(fileName))
                    {
                        LogMessage.GetLogInstance().LogError("上传包失败！");
                        ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = fileName + "文件上传至FTP失败" });
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
        /// 发送报文A
        /// </summary>
        /// <returns></returns>
        private bool SendMessageA(string fileName,int transType)
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
                SocketCommunication socket = new SocketCommunication(DateTime.Now, transType);
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

        /// <summary>
        /// 将汇总信息写入中间业务数据库OutPUTTASK_WAITING_DONE
        /// <param name="task">汇总信息</param>
        /// </summary>
        private void InsertIntoOutputTaskWaitingDone(OutPutTaskWaitingDone task)
        {
            try
            {
                MongoDBAccess<OutPutTaskWaitingDone> mongoAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                mongoAccess.InsertOne(task);
                logger.LogInfo("入OUTPUTTASK_WAITING_DONE中间库成功");
                //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "入OUTPUTTASK_WAITING_DONE中间库成功" });
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("将汇总信息写入中间业务数据库异常：" + ex.ToString());
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = bankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "将汇总信息写入中间业务数据库异常" });
            }
        }
    }
}
