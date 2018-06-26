using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MsgServer.SocketServer;
using MsgServer.SocketServer.Filter;
using MsgServer.SocketServer.OfflineMessage;
using MsgServer.SocketServer.SocketPool;

namespace MsgServer.ServerHost.ViewModel
{
    public class SocketServerVM : INotifyPropertyChanged
    {
        #region 字段属性
        private bool _isRun;
        private int _onlineCount;
        private string _userListenIP;
        private int _userListenPort;
        private int _heartInterval;
        private int _retryNumber;
        private int _retryInterval;
        private string _adminListenIP;
        private int _adminListenPort;
        private string _message;
        private string _sendUsers;
        public bool IsRun
        {
            get { return _isRun; }
            set
            {
                _isRun = value;
                this.NotifyPropertyChanged("IsRun");
                this.NotifyPropertyChanged("IsStop");
                this.NotifyPropertyChanged("StatusStr");
            }
        }

        public bool IsStop
        {
            get { return !this.IsRun; }
        }
        public string StatusStr
        {
            get { return this.IsRun ? "正在运行" : "停止"; }
        }

        public int OnlineCount
        {
            get { return _onlineCount; }
            set
            {
                _onlineCount = value;
                this.NotifyPropertyChanged("OnlineCount");
            }
        }

        public string UserListenIp
        {
            get { return _userListenIP; }
            set { _userListenIP = value; }
        }

        public int UserListenPort
        {
            get { return _userListenPort; }
            set { _userListenPort = value; }
        }

        public int HeartInterval
        {
            get { return _heartInterval; }
            set { _heartInterval = value; }
        }

        public int RetryNumber
        {
            get { return _retryNumber; }
            set { _retryNumber = value; }
        }

        public int RetryInterval
        {
            get { return _retryInterval; }
            set { _retryInterval = value; }
        }

        public string AdminListenIp
        {
            get { return _adminListenIP; }
            set { _adminListenIP = value; }
        }

        public int AdminListenPort
        {
            get { return _adminListenPort; }
            set { _adminListenPort = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public string SendUsers
        {
            get { return _sendUsers; }
            set { _sendUsers = value; }
        }
#endregion

        public Action<string> LogAction;
        private SocketPoolManager UserListen;
        private SocketSpreader AdminListen;

        public SocketServerVM()
        {
            this.IsRun = false;
            this.OnlineCount = 0;
            this.UserListenIp = "127.0.0.1";
            this.UserListenPort = 3366;
            this.AdminListenIp = "127.0.0.1";
            this.AdminListenPort = 3365;
            this.HeartInterval = 10000;
            this.RetryNumber = 3;
            this.RetryInterval = 30000;
        }

        public void StartServer()
        {
            var userFiter = new SocketHandshake();
            userFiter.Register(new SocketServerBLL.UserConnectFilter());
            UserListen = SocketServerFactory.SocketServerManager(
                new OfflineMsgMemory(),
                userFiter,
                heartFreq: this.HeartInterval,
                retryNumber: this.RetryNumber,
                retyInterval: this.RetryInterval
            );
            UserListen.MessageFormatting = (msg) => msg + ";";
            UserListen.NotifyComplete += (userID, result) =>
            {
                Log(string.Format("send {0} {1}",userID,result?"success":"failed"));
            };
            UserListen.Listen(this.UserListenIp, this.UserListenPort);
            Log(string.Format("user listen address {0}:{1}",UserListenIp,UserListenPort));

            if (!String.IsNullOrEmpty(this.AdminListenIp))
            {
                var adminFiter = new SocketHandshake();
                adminFiter.Register(new SocketServerBLL.AdminConnectFilter());
                AdminListen = SocketServerFactory.SocketAdminManager(UserListen, adminFiter);
                AdminListen.Listen(this.AdminListenIp, this.AdminListenPort);
                Log(string.Format("admin notify listen address {0}:{1}", AdminListenIp, AdminListenPort));
            }
            this.IsRun = true;
        }

        public void StopServer()
        {
            if (UserListen != null) 
            {
                UserListen.Dispose();
                Log("user listen closed");
            }
            if (AdminListen != null)
            {
                AdminListen.Dispose();
                Log("admin notify listen closed");
            }
            this.IsRun = false;
        }

        public void Notify()
        {
            if (string.IsNullOrEmpty(this.Message))
            {
                return;
            }
            if (string.IsNullOrEmpty(this.SendUsers))
            {
                UserListen.Notify(this.Message);
            }
            else
            {
                string[] users = this.SendUsers.Split(';');
                foreach (var user in users)
                {
                    UserListen.Notify(this.Message, user);
                }
            }
        }

        public void RefreshOnline()
        {
            this.OnlineCount = UserListen.OnlineCount();
        }

        private void Log(string logString)
        {
            if (this.LogAction != null)
            {
                this.LogAction(logString);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
