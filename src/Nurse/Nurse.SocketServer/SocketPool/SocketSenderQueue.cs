using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nurse.SocketServer.SocketPool
{
    public class SocketSenderQueue:IDisposable
    {
        private ConcurrentQueue<SocketSenderArgs> concurrentQueue;
        private SocketPoolBase socketPool;
        private Task sendTask;
        private int _lock=0;

        private int _retryNumber;
        /// <summary>
        /// 消息失败后重试次数
        /// </summary>
        public int RetryNumber { get { return _retryNumber; } set { _retryNumber = value; } }
        private int _retryInterval;
        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        public int RetryInterval { get { return _retryInterval; } set { _retryInterval = value; } }
        
        public SocketSenderQueue(SocketPoolBase pool,int retryNumber=3,int retryInterval=30000)
        {
            this.RetryNumber = retryNumber;
            this.RetryInterval = retryInterval;
            socketPool = pool;
            concurrentQueue = new ConcurrentQueue<SocketSenderArgs>();
            sendTask = new Task(send);
            sendTask.Start();
        }

        /// <summary>
        /// 启动一个心跳计时器
        /// </summary>
        /// <param name="heartString"></param>
        /// <param name="heartFrequency"></param>
        /// <returns></returns>
        public Timer InitTimingNotify(Action<SocketSenderArgs, SocketError> sendFailedCallBack, string heartString="0",int heartFrequency=60000)
        {
            SocketSenderArgs arg = new SocketSenderArgs()
            {
                Message = Encoding.Default.GetBytes(heartString),
                MessageType = SocketMessageType.HeartTick,
                OnSendComplete=sendFailedCallBack
            };
            return new Timer(new TimerCallback(
                (obj)=> 
                {
                    this.Enqueue(obj as SocketSenderArgs);
                })
                , arg, heartFrequency, heartFrequency);
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="args"></param>
        public void Enqueue(SocketSenderArgs args)
        {
            concurrentQueue.Enqueue(args);
        }
        #region 私有方法
        private void send()
        {
            while (_lock==0)
            {
                SocketSenderArgs arg;
                if(concurrentQueue.TryDequeue(out arg))
                {
                    Action<object> action;
                    switch (arg.MessageType)
                    {
                        case SocketMessageType.HeartTick:
                            action = sendTickAction;
                            break;
                        case SocketMessageType.Notification:
                            action = sendAction;
                            break;
                        default:
                            action = sendAction;
                            break;
                    }
                    //未到消息发送时间，重回队列
                    if (arg.SendTime!=null && DateTime.Now<arg.SendTime)
                    {
                        this.Enqueue(arg);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(
                            new WaitCallback(action), arg);
                    }
                }
                else
                {
                    //队列已空，休息一会
                    Thread.Sleep(1000);
                }
            }
        }
        /// <summary>
        /// 发送广播消息方法
        /// </summary>
        /// <param name="obj"></param>
        private void sendAction(object obj)
        {
            var arg = obj as SocketSenderArgs;
            var connSocket = socketPool.Get(arg.ConnectID);
            SocketError err;
            if (connSocket != null)
            {
                try
                {
                    connSocket.Send(arg.Message);
                    err = SocketError.Success;
                }catch(SocketException e)
                {
                    //失败重试
                    if (arg.RetryTimes < this.RetryNumber)
                    {
                        arg.RetryTimes++;   //重试次数+1
                        arg.SendTime = DateTime.Now.AddMilliseconds(this.RetryInterval);
                        this.Enqueue(arg);
                        return;
                    }
                    else
                    {
                        err = e.SocketErrorCode;
                    }
                }
            }
            else
            {
                //不在线
                err = SocketError.NotConnected;
            }
            arg.SendComplete(err);
            //Console.WriteLine("notiy {0} {1}",arg.ConnectID,arg.Message);
        }
        /// <summary>
        /// 发送心跳消息方法
        /// </summary>
        /// <param name="obj"></param>
        private void sendTickAction(object obj)
        {
            var arg = obj as SocketSenderArgs;
            foreach(var s in this.socketPool.GetAll())
            {
                SocketError err;
                try
                {
                    if (s.Value != null)
                    {
                        s.Value.Send(arg.Message);
                        err = SocketError.Success;
                    }
                    else
                    {
                        err = SocketError.NotConnected;
                    }
                }catch(SocketException e)
                {
                    err = e.SocketErrorCode;
                }
                arg.ConnectID = s.Key;
                arg.SendComplete(err);
            }

            //Console.WriteLine("tick {0} terminal",this.socketPool.Count());
        }
        #endregion
        public void Dispose()
        {
            Interlocked.Exchange(ref _lock,1);
            sendTask.Wait();
            sendTask.Dispose();
        }
    }
    public class SocketSenderArgs
    {
        public byte[] Message { get; set; }
        public SocketMessageType MessageType { get; set; }
        public DateTime? SendTime { get; set; }
        public int RetryTimes { get; set; }
        public string ConnectID { get; set; }
        //public Socket ConnectSocket { get; set; }
        public Action<SocketSenderArgs, SocketError> OnSendComplete { get; set; }

        public void SendComplete(SocketError err)
        {
            if(this.OnSendComplete!=null)
                this.OnSendComplete(this, err);
        }
        
    }
    public enum SocketMessageType
    {
        /// <summary>
        /// 心跳
        /// </summary>
        HeartTick=0,
        /// <summary>
        /// 广播通知
        /// </summary>
        Notification=1
    }
}
