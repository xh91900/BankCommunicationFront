//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using WinSCP;//Install-Package WinSCP;Installing 'WinSCP 5.9.3'.

//namespace BankCommunicationFront
//{
//    public class TPSFTPWinSCP : IDisposable
//    {
//        Session session;
//        SessionOptions sessionOptions;
//        TransferOptions transferOptions;
//        LogMessage log;

//        string url_snd = @"C:\ETCFTP\PSBCSND\";
//        string url_rcv = @"C:\ETCFTP\PSBCRCV\";
//        string dir_rcv = @"E:\FTP\RCV\";

//        public TPSFTPWinSCP()
//        {
//            log = new LogMessage("ErrorLog");
//            session = new Session();
//            sessionOptions = new SessionOptions();
//            sessionOptions.Protocol = Protocol.Ftp;
//            sessionOptions.HostName = @"ftp://192.168.0.19";
//            sessionOptions.PortNumber = int.Parse("21");
//            //sessionOptions.SshPrivateKeyPath = bankFtpSet.PrivateKeyFile;
//            //sessionOptions.PrivateKeyPassphrase = bankFtpSet.PassPhrase;//SshPrivateKeyPassphrase
//            sessionOptions.UserName = "PSBCETC";
//            sessionOptions.Password = "111111";
//            //sessionOptions.SshHostKeyFingerprint = "ssh-rsa 2048 83:87:d7:ed:08:7e:fd:cf:1d:37:4d:58:89:e1:b9:47";//软研实验室192.168.1.101服务器rsa2的密钥指纹
//            //sessionOptions.SshHostKeyFingerprint = "ssh-rsa 97:0d:de:48:3d:9c:a1:ae:78:ac:95:d4:e9:7a:d5:08";//数据中心10.0.3.151服务器rsa2的密钥指纹
//            //sessionOptions.SshHostKeyFingerprint = "";


//            transferOptions = new TransferOptions();
//            transferOptions.TransferMode = TransferMode.Binary;
//        }

//        /// <summary>
//        /// 上传文件至SFTP服务器,上传同名文件会覆盖！
//        /// </summary>
//        /// <param name="fileName">带路径的文件名</param>
//        /// <returns></returns>
//        public bool UpLoadFile(string fileName)
//        {
//            try
//            {
//                // Connect
//                session.Open(sessionOptions);

//                TransferOperationResult transferResult;
//                transferResult = session.PutFiles(fileName, url_snd, false, transferOptions);//"/home/user/"

//                // Throw on any error
//                transferResult.Check();

//                // Print results
//                //foreach (TransferEventArgs transfer in transferResult.Transfers)
//                //{
//                //    Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
//                //}

//            }
//            catch (Exception err)
//            {
//                log.LogError("FTP上传数据出现异常:" + err.Message);
//                return false;
//            }

//            return true;
//        }

//        /// <summary>
//        /// 下载SFTP服务器上的文件,下载同名文件会覆盖！
//        /// </summary>
//        /// <param name="fileName">不带路径的文件名</param>
//        /// <returns></returns>
//        public bool DownLoad(string fileName)
//        {
//            try
//            {
//                // Connect
//                session.Open(sessionOptions);

//                TransferOperationResult transferResult;
//                transferResult = session.GetFiles(url_rcv + fileName, dir_rcv, false, transferOptions);

//                // Throw on any error
//                transferResult.Check();

//                // Print results
//                //foreach (TransferEventArgs transfer in transferResult.Transfers)
//                //{
//                //    Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
//                //}

//            }
//            catch (Exception err)
//            {
//                LogMessage.GetLogInstance().LogError("FTP下载数据出现异常:" + err.Message);
//                return false;
//            }

//            return true;
//        }

//        public bool DownLoadFile(string fileName)
//        {
//            return DownLoad(fileName);
//        }

//        /// <summary>
//        /// 释放资源
//        /// </summary>
//        public void Dispose()
//        {
//            if (session != null)
//            {
//                session.Dispose();
//                session = null;
//            }
//        }
//    }
//}
