using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

namespace BankCommunicationFront
{
    public class Ftp : IDisposable
    {
        private string remoteHost;
        private int remotePort = 8080;
        private static string userName = "";
        private static string userPassWord = "";
        private static string urlPostToBank = "";
        private static string urlFetchFromBank = "";

        private Socket socketControl;
        Stream requestStream;
        FileStream upLoadFileStream;
        FtpWebResponse uploadResponse;
        FtpWebRequest uploadRequest;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <remarks>每一次启动FTP都会读一次配置信息，这样，更改配置之后不需要重新启动服务就能自动生效</remarks>
        public Ftp(BankAgent bankAgent)
        {
            //读取系统配置参数
            try
            {
                userName = bankAgent.FTPUserName;
                userPassWord = bankAgent.FTPPwd;

                // C:\ETCFTP\PSBCSND\
                urlPostToBank = bankAgent.SndFtpDri;

                // ftp://20.133.8.235/PSBCRCV/BankFile/06/
                urlFetchFromBank = bankAgent.RcvFtpDri;
                remoteHost = bankAgent.FTPHost;
                remotePort = bankAgent.FTPPort;
            }
            catch (Exception err)
            {
                LogMessage.GetLogInstance().LogError("读取FTP配置信息 出现异常！" + err.Message);
            }

            //建立连接
            ConnectToFTP();
        }

        /// <summary>
        ///  建立FTP连接
        /// </summary>
        private void ConnectToFTP()
        {
            Socket socketControlInFunc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep;
            try
            {
                ep = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);
                socketControlInFunc.Connect(ep);
            }
            catch (Exception err)
            {
                LogMessage.GetLogInstance().LogError("无法建立FTP连接！" + err.Message);
            }
        }

        /// <summary>
        /// 关闭FTP连接
        /// </summary>
        public void DisConnectToFtp()
        {
            if (socketControl != null)
            {
                try
                {
                    socketControl.Close();
                    socketControl = null;
                }
                catch (Exception err)
                {
                    LogMessage.GetLogInstance().LogError("关闭FTP链接异常 !\r\n" + err.Message);
                }
            }
        }

        /// <summary>
        /// 将指定位置的压缩文件上传到网络地址的远程的位置
        /// </summary>
        /// <param name="fileName">文件名</param>
        public bool UpLoad(string fileName)
        {
            bool bRet = true;
            FileInfo fileInfo = new FileInfo(SYSConstant.sParam.Find(p => p.Key == "PATH_SND").Value + fileName);

            try
            {
                //uploadRequest = (FtpWebRequest)WebRequest.Create(new Uri(urlPostToBank + (DateTime.Now.Month < 10 ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString()) + "/" + fileName.Substring(fileName.LastIndexOf("\\") + 1)));
                uploadRequest = (FtpWebRequest)WebRequest.Create(new Uri(urlPostToBank + fileName.Substring(17, 2) + "/" + fileName.Substring(fileName.LastIndexOf("\\") + 1)));

                uploadRequest.Proxy = null;
                uploadRequest.Credentials = new NetworkCredential(userName, userPassWord);
                uploadRequest.UseBinary = true;
                uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;

                requestStream = uploadRequest.GetRequestStream();
                upLoadFileStream = fileInfo.OpenRead();    //File.Open(fileName, FileMode.Open);

                int bytesRead;
                byte[] buffer = new byte[fileInfo.Length];

                while (true)
                {
                    bytesRead = upLoadFileStream.Read(buffer, 0, (int)fileInfo.Length);
                    if (bytesRead == 0)
                        break;
                    requestStream.Write(buffer, 0, bytesRead);
                }
                requestStream.Flush();
                requestStream.Close();

                uploadResponse = (FtpWebResponse)uploadRequest.GetResponse();
            }
            catch (UriFormatException err)
            {
                LogMessage.GetLogInstance().LogError("利用FTP上传数据 出现异常 \r\n UriFormatException :" + err.Message);
                bRet = false;
            }
            catch (IOException err)
            {
                LogMessage.GetLogInstance().LogError("利用FTP上传数据 出现异常 \r\n IOException:" + err.Message);
                bRet = false;
            }
            catch (WebException err)
            {
                LogMessage.GetLogInstance().LogError("利用FTP上传数据 出现异常 \r\n WebException:" + err.Message);
                bRet = false;
            }
            finally
            {
                if (uploadResponse != null)
                    uploadResponse.Close();
                if (upLoadFileStream != null)
                    upLoadFileStream.Close();
                if (requestStream != null)
                    requestStream.Close();

                //关闭FTP连接
                DisConnectToFtp();
            }

            return bRet;
        }

        /// <summary>
        /// 下载网络远程的指定的位置指定的文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        public bool DownLoad(string fileName)
        {
            bool bRet = true;
            FtpWebResponse response = null;
            Stream inStream = null;
            Stream outStream = null;
            try
            {
                //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(urlFetchFromBank + (DateTime.Now.Month < 10 ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString()) + "/" + fileName));
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(urlFetchFromBank + fileName.Substring(17, 2) + "/" + fileName));
                request.Credentials = new NetworkCredential(userName, userPassWord);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UseBinary = true;
                request.Proxy = null;

                response = (FtpWebResponse)request.GetResponse();

                inStream = response.GetResponseStream();
                // @"E:\FTP\RCV\"
                outStream = File.OpenWrite(SYSConstant.sParam.Find(p => p.Key == "PATH_RECEIVE").Value + fileName);
                byte[] buffer = new byte[fileName.Length];
                int size = 0;
                while ((size = inStream.Read(buffer, 0, fileName.Length)) > 0)
                {
                    outStream.Write(buffer, 0, size);
                }

                outStream.Flush();
            }
            catch (Exception err)
            {
                LogMessage.GetLogInstance().LogError("接受FTP文件 出现异常！\r\n" + err.Message);
                bRet = false;
            }
            finally
            {
                if (inStream != null)
                    inStream.Close();
                if (outStream != null)
                    outStream.Close();
                if (response != null)
                    response.Close();

                //关闭FTP连接
                DisConnectToFtp();
            }
            return bRet;
        }

        #region IDisposable 成员

        /// <summary>
        /// 重写Finalize方法，当垃圾收集执行时，该方法将被调用！	
        /// </summary>
        ~Ftp()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);//因为对象的资源被显示清理，这里阻止垃圾收集器调用Finalize方法
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogMessage.GetLogInstance().LogInfo("显示关闭非托管资源！");
            }
            else
            {
                LogMessage.GetLogInstance().LogInfo("垃圾收集器关闭非托管资源！");
            }
            DisConnectToFtp();

        }
        #endregion
    }
}
