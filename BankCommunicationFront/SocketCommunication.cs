using MongoDB.Driver;
using SuperSocket.ClientEngine;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BankCommunicationFront
{
    /// <summary>
    /// AppSession 代表一个和客户端的逻辑连接
    /// </summary>
    public class BankBatchSession : AppSession<BankBatchSession, MessageStruct>
    {

    }

    /// <summary>
    /// 服务器实例
    /// </summary>
    public class BankBatchServer : AppServer<BankBatchSession, MessageStruct>
    {
        public BankBatchServer() : base(new SuperSocket.SocketBase.Protocol.DefaultReceiveFilterFactory<MessageReceiveFilter, MessageStruct>())
        { }
    }

    /// <summary>
    /// socket通信类
    /// </summary>
    public class SocketCommunication
    {
        /// <summary>
        /// 客户端逻辑链接
        /// </summary>
        private AsyncTcpSession _SocketClient;

        /// <summary>
        /// 报文发送时间
        /// </summary>
        private DateTime _SndTime;

        /// <summary>
        /// 交易类型
        /// </summary>
        private int _TransType;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sndTime">报文发送时间</param>
        /// <param name="transType">交易类型</param>
        public SocketCommunication(DateTime sndTime, int transType = 0)
        {
            this._SocketClient = new AsyncTcpSession();
            this._SndTime = sndTime;
            this._TransType = transType;
        }

        // 建立连接
        public AsyncTcpSession Connect(IPAddress address, int port)
        {
            if (!_SocketClient.IsConnected)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_SocketClient.IsConnected)
                    {
                        break;
                    }
                    try
                    {
                        _SocketClient.Connect(new IPEndPoint(address, port));
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    // 可能是异步建立连接，等5s
                    Thread.Sleep(5000);
                }
            }

            if (!_SocketClient.IsConnected)
            {
                return null;
            }
            return _SocketClient;
        }

        // 发送a报文
        public void Send(byte[] message)
        {
            _SocketClient.DataReceived += _SocketClient_DataReceived;
            ArraySegment<byte> segment = new ArraySegment<byte>(message);
            _SocketClient.Send(segment);
        }

        // 接收b报文并解析
        private void _SocketClient_DataReceived(object sender, DataEventArgs e)
        {
            try
            {
                ProcessFile pf = new ProcessFile();
                string message = Encoding.ASCII.GetString(e.Data, 0, e.Data.Length);
                MessageStruct messageAC = new MessageStruct();
                messageAC.BodyLenth = message.Substring(0, 4);
                messageAC.MessageNo = message.Substring(4, 1);
                messageAC.SenderCode = message.Substring(5, 6);
                messageAC.ReceiverCode = message.Substring(11, 6);
                messageAC.FileName = message.Substring(17, 31);
                messageAC.Key = message.Substring(48, 1);
                messageAC.VerifyCode = message.Substring(49, 8);

                // 冗余银行信息
                MongoDBAccess<BankAgent> mongoAccess = new MongoDBAccess<BankAgent>(SYSConstant.BANK_CONFIG, SYSConstant.BANK_AGENT);
                messageAC.BankAgent = mongoAccess.FindAsByWhere(p => p.BankNo == messageAC.SenderCode, 0).FirstOrDefault();

                var logger = new LogMessage(messageAC.BankAgent.BankNo);
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = messageAC.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "接收到b报文：" + message.Substring(4, 53) });
                logger.LogInfo("接收到b报文：" + message.Substring(4, 53));

                switch ((EReceiveResult)Enum.Parse(typeof(EReceiveResult), messageAC.Key))
                {
                    case EReceiveResult.接受成功:
                        pf.BackUpFile(messageAC.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_SNDFILE").Value + DateTime.Now.ToString("yyyyMM") + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);// 将文件移动至处理成功目录下
                        pf.BackUpFile(messageAC.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_SNDFILE").Value + DateTime.Now.ToString("yyyyMM") + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);
                        if (_TransType == 2010 || _TransType == 2012)
                        {
                            MongoDBAccess<OutPutTaskWaitingDone> outPutAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                            outPutAccess.InsertOne(new OutPutTaskWaitingDone() { Status = 1, TransType = _TransType, ColName = messageAC.FileName, CreateTime = DateTime.Now.AddHours(8), SendTime = _SndTime.AddHours(8) });
                        }
                        break;// 接受成功
                    case EReceiveResult.报文a校验失败:
                        pf.BackUpFile(messageAC.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                        pf.BackUpFile(messageAC.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);
                        if (_TransType == 2010 || _TransType == 2012)
                        {
                            MongoDBAccess<OutPutTaskWaitingDone> outPutAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                            outPutAccess.InsertOne(new OutPutTaskWaitingDone() { Status = -3, TransType = _TransType, ColName = messageAC.FileName, CreateTime = DateTime.Now.AddHours(8), SendTime = _SndTime.AddHours(8) });
                        }
                        break;// 报文a校验失败
                    case EReceiveResult.ftp文件失败:
                        pf.BackUpFile(messageAC.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                        pf.BackUpFile(messageAC.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);
                        if (_TransType == 2010 || _TransType == 2012)
                        {
                            MongoDBAccess<OutPutTaskWaitingDone> outPutAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                            outPutAccess.InsertOne(new OutPutTaskWaitingDone() { Status = -3, TransType = _TransType, ColName = messageAC.FileName, CreateTime = DateTime.Now.AddHours(8), SendTime = _SndTime.AddHours(8) });
                        }
                        break;// ftp文件失败
                    case EReceiveResult.转账文件校验失败:
                        pf.BackUpFile(messageAC.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                        pf.BackUpFile(messageAC.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);
                        if (_TransType == 2010 || _TransType == 2012)
                        {
                            MongoDBAccess<OutPutTaskWaitingDone> outPutAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                            outPutAccess.InsertOne(new OutPutTaskWaitingDone() { Status = -3, TransType = _TransType, ColName = messageAC.FileName, CreateTime = DateTime.Now.AddHours(8), SendTime = _SndTime.AddHours(8) });
                        }
                        break;// 转账文件校验失败
                    default:
                        pf.BackUpFile(messageAC.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                        pf.BackUpFile(messageAC.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDSND").Value + messageAC.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value);
                        if (_TransType == 2010 || _TransType == 2012)
                        {
                            MongoDBAccess<OutPutTaskWaitingDone> outPutAccess = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.OUTPUTTASK_WAITING_DONE);
                            outPutAccess.InsertOne(new OutPutTaskWaitingDone() { Status = -3, TransType = _TransType, ColName = messageAC.FileName, CreateTime = DateTime.Now.AddHours(8), SendTime = _SndTime.AddHours(8) });
                        }
                        break;// 默认失败
                }
                logger.LogInfo("文件" + messageAC.FileName + "备份成功");
                _SocketClient.Close();
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("解析b报文异常：" + ex.ToString());
                _SocketClient.Close();
            }
        }


        /*------------------------------------------------------------------通信前置系统报文接收模块------------------------------------------------------------*/

        // 启动本地监听
        public void SetUp()
        {
            ServerConfig config = new ServerConfig()
            {
                Port = int.Parse(SYSConstant.sParam.Find(p => p.Key == "F_ServerConfig_Port").Value),//服务器监听的端口
                ListenBacklog = 2001,// 监听队列的大小
                MaxConnectionNumber = 50000,// 可允许连接的最大连接数
                ReceiveBufferSize = 1024,// 接收缓冲区大小
                MaxRequestLength = 10240// 最大允许的请求长度，默认值为1024;
            };

            BankBatchServer server = new BankBatchServer();
            if (!server.Setup(config))
            {
                LogMessage.GetLogInstance().LogError("本地网络监听启动失败，端口：" + config.Port);
                return;
            }
            if (!server.Start())
            {
                LogMessage.GetLogInstance().LogError("本地网络监听启动失败，端口：" + config.Port);
                return;
            }

            server.NewSessionConnected += Server_NewSessionConnected;
            server.NewRequestReceived += Server_NewRequestReceived;
            server.SessionClosed += Server_SessionClosed;

            LogMessage.GetLogInstance().LogInfo("本地网络监听启动成功，端口：" + config.Port + server.State.ToString());

            while (Console.ReadLine() != "Q")
            {
                continue;
            }

            server.Stop();
        }

        /// <summary>
        /// 逻辑链接关闭
        /// </summary>
        /// <param name="session">逻辑链接</param>
        /// <param name="value">关闭原因</param>
        private void Server_SessionClosed(BankBatchSession session, CloseReason value)
        {
            session.Close();
            LogMessage.GetLogInstance().LogWarn("本地网络关闭连接：" + session.RemoteEndPoint.Address + "原因" + value.ToString());
        }

        /// <summary>
        /// 通信前置系统报文接收模块
        /// </summary>
        /// <param name="session">逻辑链接</param>
        /// <param name="requestInfo">请求内容实体</param>
        private void Server_NewRequestReceived(BankBatchSession session, MessageStruct requestInfo)
        {
            ProcessReceiveRequest pr = new ProcessReceiveRequest(session, requestInfo);
            pr.ProcessRequest();
        }

        /// <summary>
        /// 逻辑链接建立
        /// </summary>
        /// <param name="session">逻辑链接</param>
        private void Server_NewSessionConnected(BankBatchSession session)
        {
            LogMessage.GetLogInstance().LogInfo("本地网络监听到链接：" + session.RemoteEndPoint.Address);
        }

    }

    /// <summary>
    /// ProcessReceiveRequest
    /// </summary>
    public class ProcessReceiveRequest
    {
        BankBatchSession session;
        MessageStruct requestInfo;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="session">逻辑链接</param>
        /// <param name="requestInfo">请求实体</param>
        public ProcessReceiveRequest(BankBatchSession session, MessageStruct requestInfo)
        {
            this.session = session;
            this.requestInfo = requestInfo;
        }

        /// <summary>
        /// 无参构造
        /// </summary>
        public ProcessReceiveRequest()
        { }

        /// <summary>
        /// 处理接收到的请求
        /// </summary>
        public void ProcessRequest()
        {
            ProcessFile pf = new ProcessFile();
            try
            {
                LogMessage logger = new LogMessage(requestInfo.BankAgent.BankNo);

                // 校验C报文
                if (VerifyMessageC(requestInfo))
                {
                    logger.LogInfo("c报文校验成功");
                    // 发送成功的d报文
                    byte[] message = BuildMessageD(requestInfo, 0);
                    session.Send(message, 0, message.Length);
                    session.Close();

                    //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "发送成功d报文：" + Encoding.ASCII.GetString(message) });
                    logger.LogInfo("发送成功d报文：" + Encoding.ASCII.GetString(message));

                    // 下载ftp文件到本地rcv目录下
                    if (!ReceiveFileContent(requestInfo))
                    {
                        ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "下载FTP文件失败：" + requestInfo.FileName });
                        return;
                    }

                    //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "下载FTP文件成功：" + requestInfo.FileName });
                    logger.LogInfo("下载FTP文件成功：" + requestInfo.FileName);

                    // 解密并校验文件
                    string[] contentLines;
                    bool verifyResult = VerifyFileContent(SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value + requestInfo.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value + requestInfo.FileName.Split('.')[0] + ".txt", requestInfo.BankAgent.BankName, requestInfo.Key, out contentLines);

                    if (verifyResult)
                    {
                        //ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "成功解密并校验文件：" + requestInfo.FileName });
                        logger.LogInfo("成功解密并校验文件：" + requestInfo.FileName);

                        string collectionName = string.Empty;
                        bool isSuccess = true;

                        // 扣款结果汇总信息，item1-扣款成功数量，item2-扣款成功金额，item3-扣款失败数量，item4-扣款失败金额
                        Tuple<int, int, int, int> Report;
                        switch ((ETransType)Enum.Parse(typeof(ETransType), contentLines[1].Split(SYSConstant.sepForColumn)[0]))
                        {
                            case ETransType.记账金客户转账结果数据: if (InsertIntoBankTrans(((int)ETransType.记账金客户转账结果数据).ToString(), contentLines, out collectionName, out isSuccess, out Report)) { InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = collectionName, CreateTime = DateTime.Now.AddHours(8), TransType = 2001, Status = 0, Key = requestInfo.Key, SuccessNum = Report.Item1, SuccessAmount = Report.Item2, FailNum = Report.Item3, FailAmount = Report.Item4 }); } break;
                            case ETransType.记账保证金转账结果数据: if (InsertIntoBankTrans(((int)ETransType.记账保证金转账结果数据).ToString(), contentLines, out collectionName, out isSuccess, out Report)) { InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = collectionName, CreateTime = DateTime.Now.AddHours(8), TransType = 2002, Status = 0, Key = requestInfo.Key, SuccessNum = Report.Item1, SuccessAmount = Report.Item2, FailNum = Report.Item3, FailAmount = Report.Item4 }); } break;
                            case ETransType.委托扣款协议信息: if (InsertSignInfoIntoBankAccount(contentLines, requestInfo.BankAgent.BankTag, out collectionName, out isSuccess)) { InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = collectionName, CreateTime = DateTime.Now.AddHours(8), TransType = 2009, Status = 1, Key = requestInfo.Key }); } break;
                            case ETransType.保证金补缴成功信息: if (InsertBondInfoIntoBankAccount(contentLines, requestInfo.BankAgent.BankTag, out collectionName, out isSuccess)) { InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = collectionName, CreateTime = DateTime.Now.AddHours(8), TransType = 2011, Status = 1, Key = requestInfo.Key }); } break;
                        }

                        // 单独处理一卡通黑白名单
                        if (contentLines[0].Split(SYSConstant.sepForColumn)[2] == "905000" && (ETransType)Enum.Parse(typeof(ETransType), contentLines[1].Split(SYSConstant.sepForColumn)[0]) == ETransType.用户车辆冀通卡与帐户绑定信息)
                        {
                            if (InsertIntoBankAccountYKT(contentLines, requestInfo.BankAgent.BankTag, out isSuccess)) { InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = SYSConstant.BANK_ACCOUNT_SIGN_YKTINCREMENT, CreateTime = DateTime.Now.AddHours(8), TransType = 2017, Status = 1, Key = requestInfo.Key }); }
                        }

                        if(isSuccess)
                        {
                            // 备份文件
                            pf.BackUpFile(requestInfo.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_RCVFILE").Value + DateTime.Now.ToString("yyyyMM") + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);// 将文件移动至处理成功目录下
                            pf.BackUpFile(requestInfo.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_SUCCESS_RCVFILE").Value + DateTime.Now.ToString("yyyyMM") + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);

                            // 日志
                            ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = contentLines[1].Split(SYSConstant.sepForColumn)[0] + "号报文处理完成,入中间库成功：" + requestInfo.FileName + "备份至成功目录" });
                            logger.LogInfo(contentLines[1].Split(SYSConstant.sepForColumn)[0] + "号报文处理完成,入中间库成功" + requestInfo.FileName + "备份至成功目录");
                        }
                        else
                        {
                            InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = "",TransType= (int)((ETransType)Enum.Parse(typeof(ETransType), contentLines[1].Split(SYSConstant.sepForColumn)[0])), CreateTime = DateTime.Now.AddHours(8), Status = -3, Key = requestInfo.Key, Remark = "银行方发送数据入mongo库失败" });
                            pf.BackUpFile(requestInfo.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                            pf.BackUpFile(requestInfo.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);
                            ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "银行方发送数据入mongo库失败：" + requestInfo.FileName + "备份至失败目录" });
                            logger.LogInfo("银行方发送数据入mongo库失败：" + requestInfo.FileName + "备份至失败目录");
                        }
                    }
                    else
                    {
                        // 将错误待处理任务数据写入中间业务数据库INPUTTASK_WAITING_DONE
                        InsertIntoInputTaskWaitingDone(new InPutTaskWaitingDone() { BankTag = requestInfo.BankAgent.BankTag, FileName = requestInfo.FileName, ColName = "", TransType = (int)((ETransType)Enum.Parse(typeof(ETransType), contentLines[1].Split(SYSConstant.sepForColumn)[0])), CreateTime = DateTime.Now.AddHours(8), Status = -3, Key = requestInfo.Key, Remark = "解密并校验文件失败" });
                        pf.BackUpFile(requestInfo.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                        pf.BackUpFile(requestInfo.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);

                        ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "解密并校验文件失败：" + requestInfo.FileName + "备份至失败目录" });
                        logger.LogInfo("解密并校验文件失败：" + requestInfo.FileName + "备份至失败目录");
                    }
                }
                else
                {
                    // 发送失败的d报文
                    byte[] b = BuildMessageD(requestInfo, 1);
                    session.Send(b, 0, b.Length);

                    ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.警告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "c报文校验失败，发送失败d报文：" + Encoding.ASCII.GetString(b) });
                    logger.LogInfo("c报文校验失败，发送失败d报文：" + Encoding.ASCII.GetString(b));
                }
                session.Close();
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.通告.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = requestInfo.MessageNo + requestInfo.SenderCode + requestInfo.ReceiverCode + requestInfo.FileName + requestInfo.Key + "处理结束" });
                //session.AppServer.Stop();
            }
            catch (Exception ex)
            {
                ShowMessage.GetFrontMessageInstance().PrintLog(new MessageLog() { BankName = requestInfo.BankAgent == null ? "" : requestInfo.BankAgent.BankName, MessageLevel = ErrorLevel.致命.ToString(), SndTime = DateTime.Now.ToString(SystemSetInfo.SqlfmtDateTime), MessageContent = "通信前置系统报文接收模块异常：" + ex.Message.ToString() });
                pf.BackUpFile(requestInfo.FileName, SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent == null ? "" : requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);// 将ftp文件移动（若已下载到本地）至异常目录下
                pf.BackUpFile(requestInfo.FileName.Split('.')[0] + ".txt", SYSConstant.sParam.Find(p => p.Key == "PATH_BACKUP_FIELDRCV").Value + requestInfo.BankAgent == null ? "" : requestInfo.BankAgent.BankName + "\\", SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);
                LogMessage.GetLogInstance().LogError("通信前置系统报文接收模块异常：" + ex.ToString());
                session.Close();
            }
        }

        // 校验报文
        private bool VerifyMessageC(MessageStruct requestInfo)
        {
            string sourceCode = requestInfo.MessageNo + requestInfo.SenderCode + requestInfo.ReceiverCode + requestInfo.FileName + requestInfo.Key;
            DESEncrypt des = new DESEncrypt(requestInfo.Key);
            string crc = DESEncrypt.GetCRC(Encoding.GetEncoding("GBK").GetBytes(sourceCode));
            return requestInfo.VerifyCode == crc;
        }

        /// <summary>
        /// 构建报文D
        /// </summary>
        /// <param name="requestInfo">接收到的消息</param>
        /// <param name="status">接受结果标志</param>
        /// <returns>string</returns>
        private byte[] BuildMessageD(MessageStruct requestInfo, int status)
        {
            string sourceCode = "d" + SystemSetInfo.SettleCenterCode + requestInfo.SenderCode + requestInfo.FileName + status;
            DESEncrypt des = new DESEncrypt(requestInfo.Key);
            string crc = DESEncrypt.GetCRC(Encoding.GetEncoding("GBK").GetBytes(sourceCode));
            string message = string.Format("{0}{1}{2}{3}{4}{5}", "d", SystemSetInfo.SettleCenterCode, requestInfo.SenderCode, requestInfo.FileName, status, crc);
            List<byte> temp = new List<byte>();
            byte[] d = new byte[4];
            //d[0] = 0x00;
            //d[1] = 0x00;
            //d[2] = 0x00;
            //d[3] = 0x35;// 长度53
            d[0] = 0;
            d[1] = 0;
            d[2] = 0;
            d[3] = 53;// 长度53
            temp.AddRange(d);
            byte[] messageD = Encoding.ASCII.GetBytes(message);
            temp.AddRange(messageD);
            return temp.ToArray();
        }

        /// <summary>
        /// 解密并校验文件
        /// </summary>
        /// <param name="inFile">源文件（带路径）</param>
        /// <param name="outFile">解密后的文件（带路径）</param>
        /// <param name="bankName">银行名称</param>
        /// <param name="content">包内容</param>
        /// <returns>bool</returns>
        public bool VerifyFileContent(string inFile, string outFile, string bankName, string key, out string[] contentLines)
        {
            DESEncrypt des = new DESEncrypt(key);
            contentLines = new string[0];
            bool result = false;
            if (des.Decrypt(inFile, outFile))
            {
                MemoryStream sourceFileStream = null;
                try
                {
                    byte[] buffer = new byte[SYSConstant.maxSizeUncompressed];
                    sourceFileStream = new MemoryStream(File.ReadAllBytes(outFile));
                    sourceFileStream.Read(buffer, 0, buffer.Length);
                    string content = Encoding.GetEncoding("GBK").GetString(buffer);
                    contentLines = content.Split(SYSConstant.sepForLine);

                    // 验证文件内容crc,暂不验证
                    //string crcStr = string.Empty;
                    //for (int i = 0; i <= contentLines.Length - 2; i++)
                    //{
                    //    crcStr += contentLines[i].Replace("|", "");
                    //}
                    //string crc = contentLines[contentLines.Length - 1].Substring(0, 8);
                    //result = des.GetCRC(crcStr) == crc;

                    result = true;
                }
                catch (Exception err)
                {
                    LogMessage.GetLogInstance().LogError(bankName + "校验报文失败：" + err.ToString());

                }
                finally
                {
                    if (sourceFileStream != null)
                        sourceFileStream.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// 从FTP下载文件
        /// </summary>
        /// <param name="message">报文</param>
        /// <returns></returns>
        private bool ReceiveFileContent(MessageStruct message)
        {
            try
            {
                if (!Directory.Exists(SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value))
                {
                    Directory.CreateDirectory(SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value);
                }

                //从FTP下载指定的文件到本地指定位置
                //确认尚未更新当日对账记录的接受时间，则认为尚未成功下载压缩文件，需要重新下载该文件
                using (Ftp ftp = new Ftp(message.BankAgent))
                {
                    if (!ftp.DownLoad(message.FileName))
                    {
                        LogMessage.GetLogInstance().LogError(message.BankAgent.BankName + "文件下载失败" + message.FileName);
                        return false;
                    }

                    return true;
                }

            }
            catch (Exception err)
            {
                LogMessage.GetLogInstance().LogError("ftp组件异常：" + err.ToString());
                return false;
            }
        }

        /// <summary>
        /// 将交易信息写入交易库
        /// </summary>
        /// <param name="transType">交易类型</param>
        /// <param name="contentLines">交易内容</param>
        /// <param name="collectionName">集合名</param>
        private bool InsertIntoBankTrans(string transType, string[] contentLines, out string collectionName,out bool isSuccess,out Tuple<int, int, int, int> report)
        {
            collectionName = string.Empty;
            isSuccess = true;
            try
            {
                List<TransactionInfo> transInfos = new List<TransactionInfo>();
                for (int i = 2; i < contentLines.Count() - 1; i++)
                {
                    TransactionInfo transInfo = new TransactionInfo();
                    string[] transInfoContent = contentLines[i].Split(SYSConstant.sepForColumn);
                    transInfo._id = transInfoContent[0];// ListNO--_id
                    transInfo.BankChargeTime = DateTime.Parse(transInfoContent[1].Substring(0, 4) + "/" + transInfoContent[1].Substring(4, 2) + "/" + transInfoContent[1].Substring(6, 2) + " " + transInfoContent[1].Substring(8, 2) + ":" + transInfoContent[1].Substring(10, 2) + ":" + transInfoContent[1].Substring(12, 2)).AddHours(8);
                    transInfo.ACBAccountN = transInfoContent[2];
                    transInfo.ACBAccount = transInfoContent[3];
                    transInfo.AccType = int.Parse(transInfoContent[6]);
                    transInfo.Income = int.Parse((decimal.Parse(transInfoContent[9]) * 100).ToString("F0"));
                    transInfo.Result = int.Parse(transInfoContent[10]);
                    transInfo.TransTime = DateTime.Parse(transInfoContent[11].Substring(0, 4) + "/" + transInfoContent[11].Substring(4, 2) + "/" + transInfoContent[11].Substring(6, 2) + " " + transInfoContent[11].Substring(8, 2) + ":" + transInfoContent[11].Substring(10, 2) + ":" + transInfoContent[11].Substring(12, 2)).AddHours(8);
                    transInfos.Add(transInfo);
                }

                // 包类型代码_文件名（不含后缀）例如,“2001_001000090020020170515091020”表示结算中心发送给工商银行的记账金客户转账请求数据集合
                collectionName = string.Format("{0}_{1}{2}{3}{4}", transType, "0", SystemSetInfo.SettleCenterCode, contentLines[0].Split(SYSConstant.sepForColumn)[2], DateTime.Now.ToString("yyyyMMddhhMMss"));
                MongoDBAccess<TransactionInfo> mongoAccess = new MongoDBAccess<TransactionInfo>(SYSConstant.BANK_TRANS, collectionName);
                mongoAccess.InsertMany(transInfos);

                StringBuilder sb = new StringBuilder();
                sb.Append("银行返回扣款结果文件" + requestInfo.FileName + "包含" + transInfos.Count + @"笔交易，");
                sb.Append("其中扣款成功" + transInfos.FindAll(p => p.Result == 0).Count + "笔，金额：" + transInfos.FindAll(p => p.Result == 0).Sum(p => p.Income / 100) + @"元，");
                sb.Append("扣款失败" + transInfos.FindAll(p => p.Result != 0).Count + "笔，金额：" + transInfos.FindAll(p => p.Result != 0).Sum(p => p.Income / 100) + "元");
                new LogMessage(requestInfo.BankAgent.BankNo).LogInfo(sb.ToString());
                report = new Tuple<int, int, int, int>(transInfos.FindAll(p => p.Result == 0).Count, transInfos.FindAll(p => p.Result == 0).Sum(p => p.Income),
                   transInfos.FindAll(p => p.Result != 0).Count, transInfos.FindAll(p => p.Result != 0).Sum(p => p.Income));

                return true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                report = null;
                LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "将交易信息写入交易库异常：" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 保证金信息入库
        /// </summary>
        /// <param name="contentLines">保证金信息</param>
        /// <param name="bankTag">bankTag</param>
        /// <param name="collectionName">集合名</param>
        private bool InsertBondInfoIntoBankAccount(string[] contentLines, string bankTag, out string collectionName,out bool isSuccess)
        {
            collectionName = string.Empty;
            isSuccess = true;
            try
            {
                List<BankAccountSign> signInfos = new List<BankAccountSign>();
                for (int i = 2; i < contentLines.Count() - 1; i++)
                {
                    BankAccountSign signInfo = new BankAccountSign();
                    string[] transInfoContent = contentLines[i].Split(SYSConstant.sepForColumn);
                    signInfo.GenTime = DateTime.Parse(transInfoContent[1].Substring(0, 4) + "/" + transInfoContent[1].Substring(4, 2) + "/" + transInfoContent[1].Substring(6, 2) + " " + transInfoContent[1].Substring(8, 2) + ":" + transInfoContent[1].Substring(10, 2) + ":" + transInfoContent[1].Substring(12, 2)).AddHours(8);// 时区问题加8小时
                    signInfo.AccountId = transInfoContent[2];
                    signInfo.AccountName = transInfoContent[3];

                    int cashDeposit = 0;
                    int.TryParse((decimal.Parse(transInfoContent[4]) * 100).ToString("F0"), out cashDeposit);
                    signInfo.CashDeposit = cashDeposit;

                    signInfo.BankTag = bankTag;//根据transInfoContent[4];去内存里找
                    signInfo.CreateTime = DateTime.Now.AddHours(8);// 时区问题加8小时

                    signInfo.Command = 2;
                    signInfo.FileName = contentLines[0].Split(SYSConstant.sepForColumn)[1];
                    signInfo.Status = 0;
                    signInfos.Add(signInfo);
                }

                collectionName = string.Format("{0}_{1}{2}{3}", ((int)ETransType.保证金补缴成功信息).ToString(), SystemSetInfo.SettleCenterCode, contentLines[0].Split(SYSConstant.sepForColumn)[2], DateTime.Now.ToString("yyyyMMddhhMMss"));
                MongoDBAccess<BankAccountSign> mongoAccess = new MongoDBAccess<BankAccountSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN);
                mongoAccess.InsertMany(signInfos);
                return true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "签约解约信息入库异常：" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 签约解约信息入库
        /// </summary>
        /// <param name="contentLines">解签约内容</param>
        /// <param name="bankTag">bankTag</param>
        /// <param name="collectionName">集合名</param>
        private bool InsertSignInfoIntoBankAccount(string[] contentLines, string bankTag, out string collectionName, out bool isSuccess)
        {
            collectionName = string.Empty;
            isSuccess = true;
            List<BankAccountSign> signInfos = new List<BankAccountSign>();
            try
            {
                // Item1：子包头信息所在行号,Item2：指令类型，Item3：子包体个数
                List<Tuple<int, string, int>> childHeadList = new List<Tuple<int, string, int>>();
                if (int.Parse(contentLines[0].Split(SYSConstant.sepForColumn)[5].Trim()) > 1)
                {
                    // 先解析并缓存子包头信息
                    for (int i = 1; i < contentLines.Count() - 1; i++)
                    {
                        string[] transInfoContent = contentLines[i].Split(SYSConstant.sepForColumn);
                        if (transInfoContent[2] == "i" || transInfoContent[2] == "u")
                        {
                            Tuple<int, string, int> childHead = new Tuple<int, string, int>(i, transInfoContent[2], int.Parse(transInfoContent[1].Trim()));
                            childHeadList.Add(childHead);
                        }
                    }
                }
                else
                {
                    string[] transInfoContent = contentLines[1].Split(SYSConstant.sepForColumn);
                    if (transInfoContent[2] == "i" || transInfoContent[2] == "u")
                    {
                        Tuple<int, string, int> childHead = new Tuple<int, string, int>(1, transInfoContent[2], int.Parse(transInfoContent[1].Trim()));
                        childHeadList.Add(childHead);
                    }
                }

                // 根据子包头信息解析子包体
                if (childHeadList != null && childHeadList.Any())
                {
                    foreach (var item in childHeadList)
                    {
                        for (int i = item.Item1 + 1; i < item.Item1 + item.Item3+1; i++)
                        {
                            BankAccountSign signInfo = new BankAccountSign();
                            string[] transInfoContent = contentLines[i].Split(SYSConstant.sepForColumn);
                            signInfo.GenTime = DateTime.Parse(transInfoContent[1].Substring(0, 4) + "/" + transInfoContent[1].Substring(4, 2) + "/" + transInfoContent[1].Substring(6, 2) + " " + transInfoContent[1].Substring(8, 2) + ":" + transInfoContent[1].Substring(10, 2) + ":" + transInfoContent[1].Substring(12, 2)).AddHours(8);// 时区问题加8小时
                            signInfo.AccountId = transInfoContent[2];
                            signInfo.AccountName = transInfoContent[5];

                            if (transInfoContent.Length == 8)
                            {
                                int cashDeposit = 0;
                                int.TryParse((decimal.Parse(transInfoContent[6]) * 100).ToString("F0"), out cashDeposit);
                                signInfo.CashDeposit = cashDeposit;
                            }

                            signInfo.BankTag = bankTag;//根据transInfoContent[4];去内存里找
                            signInfo.CreateTime = DateTime.Now.AddHours(8);// 时区问题加8小时

                            if (item.Item2 == "i")
                            {
                                signInfo.Command = 0;
                            }
                            else if (item.Item2 == "u")
                            {
                                signInfo.Command = 1;
                            }
                            signInfo.FileName = contentLines[0].Split(SYSConstant.sepForColumn)[1];
                            signInfo.Status = 0;
                            signInfos.Add(signInfo);
                        }
                    }
                    collectionName = string.Format("{0}_{1}{2}{3}", ((int)ETransType.委托扣款协议信息).ToString(), SystemSetInfo.SettleCenterCode, contentLines[0].Split(SYSConstant.sepForColumn)[2], DateTime.Now.ToString("yyyyMMddhhMMss"));
                    MongoDBAccess<BankAccountSign> mongoAccess = new MongoDBAccess<BankAccountSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN);
                    mongoAccess.InsertMany(signInfos);
                    return true;
                }
                else
                {
                    isSuccess = false;
                    LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "签约解约信息入库异常：签约文件格式错误，解析子包头信息失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "签约解约信息入库异常：" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 一卡通签约信息集合入库
        /// </summary>
        /// <param name="contentLines"></param>
        /// <param name="bankTag"></param>
        /// <returns></returns>
        private bool InsertIntoBankAccountYKT(string[] contentLines,string bankTag, out bool isSuccess)
        {
            isSuccess = true;
            try
            {
                List<BankAccountYKTSign> signInfos = new List<BankAccountYKTSign>();
                for (int i = 2; i < contentLines.Count() - 1; i++)
                {
                    BankAccountYKTSign signInfo = new BankAccountYKTSign();
                    string[] transInfoContent = contentLines[i].Split(SYSConstant.sepForColumn);
                    signInfo._id = transInfoContent[3];
                    signInfo.AccountName = transInfoContent[2];
                    signInfo.BankTag = bankTag;
                    int cashDeposit = 0;
                    int.TryParse((decimal.Parse(transInfoContent[7]) * 100).ToString("F0"), out cashDeposit);
                    signInfo.CashDeposit = cashDeposit;
                    signInfo.Status = int.Parse(transInfoContent[8]);
                    signInfo.GenTime = DateTime.Parse(transInfoContent[1].Substring(0, 4) + "/" + transInfoContent[1].Substring(4, 2) + "/" + transInfoContent[1].Substring(6, 2) + " " + transInfoContent[1].Substring(8, 2) + ":" + transInfoContent[1].Substring(10, 2) + ":" + transInfoContent[1].Substring(12, 2)).AddHours(8);// 时区问题加8小时
                    signInfo.CreateTime = DateTime.Now.AddHours(8);// 时区问题加8小时
                    signInfos.Add(signInfo);
                }
                MongoDBAccess<BankAccountYKTSign> totalAccess = new MongoDBAccess<BankAccountYKTSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN_YKTOTAL);
                List<BankAccountYKTSign> totalYKTSign = totalAccess.FindAsByWhere(p => !string.IsNullOrEmpty(p._id), 0);
                List<BankAccountYKTSign> incrementYKTSign = new List<BankAccountYKTSign>();

                foreach (BankAccountYKTSign item in signInfos.Distinct())
                {
                    var temp = totalYKTSign.Find(p => p._id == item._id);
                    if (temp != null)
                    {
                        if (temp.Status != item.Status)
                        {
                            incrementYKTSign.Add(item);

                            // 更新全量集合里的状态
                            var mUpDefinitionBuilder = new UpdateDefinitionBuilder<BankAccountYKTSign>();
                            var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, item.Status).Set(p => p.CashDeposit, item.CashDeposit);
                            totalAccess.UpdateDocs(p => p._id == item._id, mUpdateDefinition);
                        }
                    }
                    else
                    {
                        incrementYKTSign.Add(item);
                        totalAccess.InsertOne(item);
                    }
                }

                MongoDBAccess<BankAccountYKTSign> mongoAccess = new MongoDBAccess<BankAccountYKTSign>(SYSConstant.BANK_ACCOUNT, SYSConstant.BANK_ACCOUNT_SIGN_YKTINCREMENT);
                mongoAccess.InsertMany(incrementYKTSign);
                return true;
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "一卡通签约信息集合入库异常：" + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 将汇总信息写入中间业务数据库INPUTTASK_WAITING_DONE
        /// <param name="task">汇总信息</param>
        /// </summary>
        private void InsertIntoInputTaskWaitingDone(InPutTaskWaitingDone task)
        {
            try
            {
                MongoDBAccess<InPutTaskWaitingDone> mongoAccess = new MongoDBAccess<InPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                mongoAccess.InsertOne(task);
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError(requestInfo.BankAgent.BankName + "将汇总信息写入中间业务数据库异常：" + ex.ToString());
            }
        }
    }

    /// <summary>
    /// 文件处理类
    /// </summary>
    public class ProcessFile
    {
        // 备份文件
        public void BackUpFile(string fileName, string filePath, string tempPath)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            try
            {
                if (File.Exists(tempPath + fileName.Substring(fileName.LastIndexOf("\\") + 1)))
                {
                    File.Delete(filePath + fileName.Substring(fileName.LastIndexOf("\\") + 1));
                    File.Move(tempPath + fileName, filePath + fileName.Substring(fileName.LastIndexOf("\\") + 1));
                }
            }
            catch (Exception e)
            {
                LogMessage.GetLogInstance().LogError("通信前置系统报文接收模块异常：" + fileName + "文件移动失败!\r\n" + e.ToString());
            }
        }
    }
}
