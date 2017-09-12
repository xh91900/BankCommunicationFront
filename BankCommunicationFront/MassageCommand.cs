using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System.IO;

namespace BankCommunicationFront
{
    public class MassageCommand : CommandBase<AppSession, StringRequestInfo>
    {
        /// <summary>
        /// 等待返回b号报文
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestInfo"></param>
        public override void ExecuteCommand(AppSession session, StringRequestInfo requestInfo)
        {
            LogMessage logger = new LogMessage("ErrorLog");
            if (requestInfo.Body == "")
            {
                // 将文件移动到成功或者失败目录下
                BackUpFile("", SystemSetInfo.pathBackupSuccessSndFile);
                // 记录日志
            }
            else
            {
                logger.LogError(new MassageLog() { BankName="",SndTime=DateTime.Now.ToString(),MassageLevel=ErrorLevel.致命.ToString(),MassageContent= "b号非法" });
                // 记录日志
            }
        }

        // 备份文件
        public void BackUpFile(string fileName, string filePath)
        {
            LogMessage logger= new LogMessage("ErrorLog");
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            try
            {
                if (File.Exists(filePath + fileName.Substring(fileName.LastIndexOf("\\") + 1)))
                {
                    File.Delete(filePath + fileName.Substring(fileName.LastIndexOf("\\") + 1));
                    File.Move(SystemSetInfo.pathReceive + fileName, filePath + fileName.Substring(fileName.LastIndexOf("\\") + 1));
                }
            }
            catch (Exception e)
            {
                logger.LogError(new MassageLog() { BankName = "", SndTime = DateTime.Now.ToString(), MassageLevel = ErrorLevel.致命.ToString(), MassageContent = fileName + "文件移动失败!\r\n" + e.ToString() });
            }
        }
    }
}
