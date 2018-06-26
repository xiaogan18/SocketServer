using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgServer.SocketServer.SocketPool;
using MsgServer.SocketServer;
using MsgServer.SocketServer.OfflineMessage;
using MsgServer.SocketServer.Filter;

namespace MsgServer.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var userFiter = new SocketHandshake();
            userFiter.Register(new TestFilter());
            var manager = SocketServerFactory.SocketServerManager(
                new OfflineMsgMemory(),
                userFiter,
                heartFreq:0,
                retryNumber:3,
                retyInterval:10000
                );
            manager.Listen("127.0.0.1", 3366);


            var adminFiter = new SocketHandshake();
            adminFiter.Register(new TestAdminFiter());
            SocketSpreader spreader = SocketServerFactory.SocketAdminManager(manager, adminFiter);
            spreader.Listen("127.0.0.1",3365);


            while (true)
            {
                string msg = Console.ReadLine();
                if (msg.Equals("end"))
                {
                    manager.Dispose();
                    spreader.Dispose();
                    break;
                }
                else if (msg.StartsWith("send"))
                {
                    var temp = msg.Split(' ');
                    if (temp.Length < 3)
                    {
                        Console.WriteLine("参数错误");
                    }
                    else
                    {
                        manager.Notify(temp[2], temp[1]);
                    }
                }
                else
                {
                    manager.Notify(msg);
                }
            }
            Console.ReadKey();
            Main(null);
        }
    }
}
