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
    public abstract class SocketPoolBase 
    {
        protected object _lock = new object();
        
        
        public SocketPoolBase()
        {
            
        }

        public void AddConnection(Socket socket,string userID)
        {
            Socket old= this.Update(userID,socket);
            if(old!=null){
                //同个user标识只能在线一个设备
                old.Close();
            }
            else
            {
                this.Add(userID, socket);
            }
            Console.WriteLine("{0} coming",userID);
        }

        public void CloseConnection(string userID)
        {
            var s = this.Update(userID,null);
            if (s != null)
            {
                s.Close();
            }
        }
        

        #region 抽象方法
        protected abstract void Add(string userID,Socket socket);
        protected abstract Socket Remove(string userID);
        protected abstract Socket Update(string userID, Socket socket);
        public abstract Socket Get(string userID);
        public abstract int Count();
        public abstract KeyValuePair<string, Socket>[] GetAll();
        #endregion

        public virtual void Dispose()
        {
            
        }

    }
}
