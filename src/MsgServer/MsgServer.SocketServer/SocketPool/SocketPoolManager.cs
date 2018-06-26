using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using MsgServer.SocketServer.OfflineMessage;
using MsgServer.SocketServer.Filter;

namespace MsgServer.SocketServer.SocketPool
{
    public class SocketPoolManager : IDisposable
    {
        #region 属性、字段
        /// <summary>
        /// 连接池
        /// </summary>
        private SocketPoolBase currentPool;

        private SocketSenderQueue _currentSender;
        /// <summary>
        /// 消息发送队列
        /// </summary>
        public SocketSenderQueue CurrentSender { get { return _currentSender; } set { _currentSender = value; } }
        /// <summary>
        /// 监听任务
        /// </summary>
        private Task listenTask;
        /// <summary>
        /// 心跳计时器
        /// </summary>
        private Timer heartTimer;
        /// <summary>
        /// 本机监听socket
        /// </summary>
        private Socket currSocket;


        /// <summary>
        /// 过滤器
        /// </summary>
        private ISocketFilter _socketFilter;
        /// <summary>
        /// 连接过滤器
        /// </summary>
        public ISocketFilter SocketFilter { get { return _socketFilter; } set { _socketFilter = value; } }

        private IOfflineContainer _offlineContainer;
        /// <summary>
        /// 离线消息容器
        /// </summary>
        public IOfflineContainer OfflineContainer { get { return _offlineContainer; } set { _offlineContainer = value; } }
        
        private Action<string, bool> _notifyComplete;
        /// <summary>
        /// 通知发送完成回调
        /// </summary>
        public Action<string, bool> NotifyComplete { get { return _notifyComplete; } set { _notifyComplete = value; } }

        /// <summary>
        /// 消息格式化
        /// </summary>
        public Func<string,string> MessageFormatting { get; set; }

        #endregion
        private static object _lock = new object();
        private static SocketPoolManager _model;
        public static SocketPoolManager Init(int heartFreq = 60000, int retryNumber = 3,int retryInterval=30000)
        {
            if (_model == null)
            {
                lock (_lock)
                {
                    if (_model == null)
                    {
                        _model = new SocketPoolManager(heartFreq);
                        _model.CurrentSender.RetryNumber = retryNumber;
                        _model.CurrentSender.RetryInterval = retryInterval;
                    }
                }
            }
            return _model;
        }
        private SocketPoolManager(int heartFreq)
        {
            //初始化内存socket池
            currentPool = new SocketPoolMemory();
            //初始化消息发送器
            CurrentSender = new SocketSenderQueue(currentPool);

            heartTimer= CurrentSender.InitTimingNotify(completeHeartCallback,heartFrequency: heartFreq);
        }

        /// <summary>
        /// 启动监听
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Listen(string ip, int port)
        {
            if (currSocket != null)
            {
                throw new Exception("已经开始监听");
            }
            currSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            currSocket.Bind(ipPoint);
            currSocket.Listen(0);
            //开启接收连接任务
            listenTask = new Task(() =>
            {
            while (true)
            {
                    //新的连接加入到连接池
                    try
                    {
                        var newSocket = currSocket.Accept();
                        ThreadPool.QueueUserWorkItem(new WaitCallback(filterInvoke),newSocket);
                        
                    }
                    catch (SocketException e)
                    {
                        break;
                    }
                }
            });
            listenTask.Start();
            Console.WriteLine("listen address {0}:{1}", ip, port);
        }
        /// <summary>
        /// 向连接池中所有用户发送通知
        /// </summary>
        /// <param name="message"></param>
        public void Notify(string message,int msgLifeHour=1)
        {
            foreach(var conn in currentPool.GetAll())
            {
                var arg = new SocketSenderArgs() {
                    ConnectID=conn.Key,
                    Message=message,
                    MessageType=SocketMessageType.Notification,
                    OnSendComplete= completeNotifyCallback,
                    MessageFormatting = this.MessageFormatting,
                    LifeHours = msgLifeHour
                };
                CurrentSender.Enqueue(arg);
            }
        }
        public void Notify(string message,string userID,int msgLifeHour=1)
        {
            
            byte[] buffer = Encoding.Default.GetBytes(message);
            var arg = new SocketSenderArgs()
            {
                ConnectID = userID,
                Message = message,
                MessageType = SocketMessageType.Notification,
                OnSendComplete = completeNotifyCallback,
                MessageFormatting = this.MessageFormatting,
                LifeHours = msgLifeHour
            };
            CurrentSender.Enqueue(arg);
        }

        public int OnlineCount()
        {
            return currentPool.Count();
        }

        #region 私有方法
        /// <summary>
        /// 心跳完成回调
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="e"></param>
        private void completeHeartCallback(SocketSenderArgs arg, SocketError e)
        {
            switch (e)
            {
                case SocketError.HostDown:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                    this.currentPool.CloseConnection(arg.ConnectID);
                    break;
                
            }
        }
        /// <summary>
        /// 通知发送完成回调
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="e"></param>
        private void completeNotifyCallback(SocketSenderArgs arg,SocketError e)
        {
            bool notifyResult;
            switch (e)
            {
                case SocketError.Success:
                    notifyResult = true;
                    Console.WriteLine("notify suceess {0}",arg.ConnectID);
                    break;
                default:
                    notifyResult = false;
                    notifyFailed(arg);
                    Console.WriteLine("notify failed {0}", arg.ConnectID);
                    break;
            }
            if (this.NotifyComplete != null)
            {
                this.NotifyComplete(arg.ConnectID, notifyResult);
            }
        }
        /// <summary>
        /// 用户上线执行
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="userID"></param>
        private void userUpLine(Socket socket,string userID)
        {
            if (this.OfflineContainer == null)
                return;
            var messages = this.OfflineContainer.TakeOut(userID);
            if (messages == null || messages.Length == 0)
                return;
            foreach(var msg in messages)
            {
                this.Notify(msg.Message, userID);
            }
        }
        /// <summary>
        /// 消息发送失败处理
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="message"></param>
        private void notifyFailed(SocketSenderArgs arg)
        {
            if (this.OfflineContainer == null)
                return;
            if(arg.LifeHours<=0)
                return;
            OfflineMessageArgs temp = new OfflineMessageArgs
            {
                CreateTime = DateTime.Now,
                UserID = arg.ConnectID,
                Message = arg.Message,
                LiveHours = arg.LifeHours
            };
            this.OfflineContainer.Add(temp);
        }
        /// <summary>
        /// 调用过滤器
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="callback"></param>
        private void filterInvoke(object obj)
        {
            var socket = obj as Socket;
            if (this.SocketFilter==null)
            {
                filterSuccess(socket.RemoteEndPoint.ToString(), socket);
            }
            else
            {
                this.SocketFilter.VerifyAsync(socket, (userStr, result) => {
                    if (result)
                    {
                        filterSuccess(userStr.Split(';')[0], socket);
                    }
                    else
                    {
                        //过滤不通过
                        socket.TrySend(Encoding.Default.GetBytes(SocketErrResult.Refuse));
                        socket.Close();
                    }
                });
            }
        }
        /// <summary>
        /// 过滤通过
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="socket"></param>
        private void filterSuccess(string userID,Socket socket)
        {
            //var userid = socket.RemoteEndPoint.ToString();
            currentPool.AddConnection(socket, userID);
            //触发上线方法
            userUpLine(socket, userID);
        }

        #endregion

        public void Dispose()
        {
            heartTimer.Dispose();
            CurrentSender.Dispose();
            currentPool.Dispose();
            currSocket.Dispose();
            OfflineContainer.Dispose();
            _model = null;
            Console.WriteLine("资源清理完成");
        }
    }
}
