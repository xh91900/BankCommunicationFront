using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BankCommunicationFront
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        static void Main()
        {
            // 初始化日志组件
            LogMessage.InitLog4Net();

            // 初始化系统配置
            SYSConstant.GetParam();

            // 2009
            Task.Run(() =>
            {
                SocketCommunication socket = new SocketCommunication(DateTime.Now);
                socket.SetUp();
            });

            // 2001\2002
            Task.Run(() =>
            {
                AccountTransferDataTask atdTask = new AccountTransferDataTask();
                atdTask.Start();
            });

            // 2010\2012
            Task.Run(() =>
            {
                CancellationAndDepositTask cadTask = new CancellationAndDepositTask();
                cadTask.Start();
            });

            // 2008
            Task.Run(() =>
            {
                BankAccountBingETCTask babTask = new BankAccountBingETCTask();
                babTask.Start();
            });

            Application app = new Application();
            MainWindow win = new MainWindow();
            ShowMessage.ShowMessageEvent += win.ShowMessage_ShowMessageEvent;
            app.Run(win);
        }
    }
}
