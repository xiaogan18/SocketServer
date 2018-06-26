using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MsgServer.SocketServer.OfflineMessage
{
    public class OfflineMsgMemory : IOfflineContainer
    {
        private List<OfflineMessageArgs> list;
        private object _lock = new object();
        private Timer timer;

        private int _defaultLiveHour;
        public int DefaultLiveHour { get { return _defaultLiveHour; } set { _defaultLiveHour = value; } }

        public OfflineMsgMemory(int defaultLiveHour = 1,int selfClearInterval=600000)
        {
            this.DefaultLiveHour = defaultLiveHour;
            list = new List<OfflineMessageArgs>();
            //定时清除无效消息
            timer = new Timer(
                new TimerCallback((obj) =>
                {
                    this.ClearDied();
                }),null,0,selfClearInterval);
        }

        public void Add(OfflineMessageArgs arg)
        {
            if (arg.LiveHours <= 0)
                arg.LiveHours = this.DefaultLiveHour;
            arg.ID = Guid.NewGuid().ToString();
            lock (_lock)
            {
                list.Add(arg);
            }
        }

        public int ClearDied()
        {
            lock (_lock)
            {
                return list.RemoveAll(a => isDied(a));
            }
        }

        public OfflineMessageArgs[] Get(string userID)
        {
            return list.FindAll(a => !isDied(a) && a.UserID.Equals(userID)).ToArray();
        }

        public OfflineMessageArgs[] GetAllLiving()
        {
            lock (_lock)
            {
                return list.FindAll(a => !isDied(a)).ToArray();
            }
        }

        public void Remove(string userID)
        {
            var temp = this.Get(userID);
            if (temp == null || temp.Length == 0)
                return;
            lock (_lock)
            {
                list.RemoveAll(a=>a.UserID.Equals(userID));
            }
        }

        public void RemoveMessage(string ID)
        {
            var temp = list.Find(a => a.ID.Equals(ID));
            if (temp != null)
            {
                lock (_lock)
                {
                    list.Remove(temp);
                }
            }
        }

        public OfflineMessageArgs[] TakeOut(string userID)
        {
            var arr = this.Get(userID);
            this.Remove(userID);
            return arr;
        }

        private bool isDied(OfflineMessageArgs arg)
        {
            var dieTime = arg.CreateTime.AddHours(arg.LiveHours);
            if (DateTime.Now > dieTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}
