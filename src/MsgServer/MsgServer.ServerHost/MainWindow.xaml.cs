using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MsgServer.ServerHost.ViewModel;

namespace MsgServer.ServerHost
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel.SocketServerVM vmodel;
        public MainWindow()
        {
            InitializeComponent();
            vmodel=new SocketServerVM();
            vmodel.LogAction = (msg) =>
            {
                msg = string.Format("[{0}] {1}\r\n", DateTime.Now, msg);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    txtLog.AppendText(msg);
                }));
            };
            this.DataContext = vmodel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                vmodel.StartServer();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            vmodel.StopServer();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            vmodel.Notify();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            vmodel.RefreshOnline();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            this.txtLog.Clear();
        }
    }
}
