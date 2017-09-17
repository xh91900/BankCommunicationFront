using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BankCommunicationFront
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static ObservableCollection<MessageLog> messageCollection = new ObservableCollection<MessageLog>();

        static ObservableCollection<ExceptionTaskWaitingDone> exceptionCollection = new ObservableCollection<ExceptionTaskWaitingDone>();
        public MainWindow()
        {
            InitializeComponent();
            this.dgInfo.ItemsSource = messageCollection;

            this.exceptionItem.ItemsSource = exceptionCollection;
        }

        private void lblClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }
        private void lblMin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void ShowMessage_ShowMessageEvent(MessageLog message)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (messageCollection.Count > 25)
                {
                    messageCollection.RemoveAt(0);
                }
                messageCollection.Add(message);
            });
        }

        /// <summary>
        /// 杀死进程程序退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 重置系统配置参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_GetSpara_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SYSConstant.GetParam();
                MessageBox.Show("更新系统参数成功！", "消息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "更新系统参数失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "CCF Files (*.ccf)|*.ccf;*.txt"
            };
            openFileDialog.Multiselect = true;
            var result = openFileDialog.ShowDialog();
            if (result.Value)
            {
                ProcessReceiveRequest pr = new ProcessReceiveRequest();
                string[] files = openFileDialog.FileNames;
                string[] contentLines;
                for (int i = 0; i < files.Length; i++)
                {
                    if(pr.VerifyFileContent(files[i], files[i].Split('.')[0] + ".txt", "", "", out contentLines))
                    {

                    }
                }
            }
        }

        // 双击事件
        private void exceptionItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DataGrid d = (DataGrid)sender;
                MongoDBAccess<InPutTaskWaitingDone> dbAccess = new MongoDBAccess<InPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                var mUpDefinitionBuilder = new UpdateDefinitionBuilder<InPutTaskWaitingDone>();
                var mUpdateDefinition = mUpDefinitionBuilder.Set(p => p.Status, 0);
                dbAccess.UpdateDocs(p => p._id == ((ExceptionTaskWaitingDone)d.CurrentItem)._id, mUpdateDefinition);
                exceptionCollection.Remove((ExceptionTaskWaitingDone)d.CurrentItem);
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("获取处理银行批量扣款异常数据异常：" + ex.ToString());
            }
        }

        // 获取处理银行批量扣款异常数据
        private void Button_GetExceptionData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MongoDBAccess<InPutTaskWaitingDone> mongoAccess = new MongoDBAccess<InPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                mongoAccess.FindAsByWhere(p => p.Status < 0 && p.CreateTime > DateTime.Now.AddDays(-1).Date, 0).ForEach(p =>
                {
                    exceptionCollection.Add(new ExceptionTaskWaitingDone() { _id = p._id, BankTag = p.BankTag, TransType = p.TransType, DbName = p.DbName, ColName = p.ColName, FileName = p.FileName, Status = p.Status, CreateTime = p.CreateTime, Remark = p.Remark, Key = p.Key, OperationType = "银行发送扣款结果包异常数据" });
                });

                //MongoDBAccess<OutPutTaskWaitingDone> access = new MongoDBAccess<OutPutTaskWaitingDone>(SYSConstant.BANK_TASK, SYSConstant.INPUTTASK_WAITING_DONE);
                //access.FindAsByWhere(p => p.Status < 0 && p.CreateTime > DateTime.Now.AddDays(-1).Date, 0).ForEach(p =>
                //{
                //    exceptionCollection.Add(new ExceptionTaskWaitingDone() { _id = p._id, BankTag = p.BankTag, TransType = p.TransType, DbName = p.DbName, ColName = p.ColName, FileName = p.FileName, Status = p.Status, CreateTime = p.CreateTime, Remark = p.Remark, Key = p.Key, OperationType = "结算中心发送扣款包异常数据" });
                //});
            }
            catch (Exception ex)
            {
                LogMessage.GetLogInstance().LogError("获取处理银行批量扣款异常数据异常：" + ex.ToString());
            }
        }
    }
    
}
