using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MsgServer.SocketServer.SocketPool
{
    public class SocketPoolMemory : SocketPoolBase
    {
        private ConcurrentDictionary<string, Socket> _pool;
        protected ConcurrentDictionary<string, Socket> Pool { get { return _pool; } set { _pool = value; } }

        public SocketPoolMemory():base()
        {
            Pool = new ConcurrentDictionary<string, Socket>();
        }

        protected override void Add(string userID, Socket socket)
        {
            Pool.TryAdd(userID, socket);
        }

        public override int Count()
        {
            if (Pool == null)
                return 0;
            return Pool.Count(a=>a.Value!=null);
        }

        public override Socket Get(string userID)
        {
            Socket socket;
            Pool.TryGetValue(userID, out socket);
            return socket;
        }

        protected override Socket Remove(string userID)
        {
            Socket socket;
            Pool.TryRemove(userID, out socket);
            return socket;
            //return this.Update(userID, null);
        }

        protected override Socket Update(string userID, Socket socket)
        {
            Socket old=null;
            if(this.Pool.ContainsKey(userID))
            {
                old= this.Get(userID);
                Pool[userID] = socket;
            }

            return old;
        }
        public override KeyValuePair<string, Socket>[] GetAll()
        {
            return Pool.ToArray();
        }
        public override void Dispose()
        {
            base.Dispose();
            foreach(var s in Pool)
            {
                s.Value.Close();
            }
            Pool.Clear();
        }
    }
}
