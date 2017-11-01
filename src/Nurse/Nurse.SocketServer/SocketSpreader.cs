using Nurse.SocketServer.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nurse.SocketServer
{
    /// <summary>
    /// Socket广播
    /// 接收来自指定端口的消息并转发
    /// </summary>
    public class SocketSpreader:IDisposable
    {
        private Socket currSocket;
        private Task listenTask;

        private ISocketFilter handshake;
        public ISocketFilter Handshake { get => handshake; set => handshake = value; }

        private Action<string> _spreadCallback;
        public Action<string> SpreadCallback { get => _spreadCallback; set => _spreadCallback = value; }

        public SocketSpreader(Action<string> spreadCallback)
        {
            this.SpreadCallback = spreadCallback;
        }
        public SocketSpreader(Action<string> spreadCallback, ISocketFilter handshakeContext)
        {
            this.SpreadCallback = spreadCallback;
            this.Handshake = handshakeContext;
        }


        public void Listen(string ip,int port)
        {
            currSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            currSocket.Bind(ipPoint);
            currSocket.Listen(0);
            listenTask = new Task(() =>
            {
                while (true)
                {
                    Socket newConn;
                    try
                    {
                        newConn = currSocket.Accept();
                    }
                    catch (SocketException e)
                    {
                        break;
                    }
                    if (Handshake != null)
                    {
                        //握手认证
                        Handshake.VerifyAsync(newConn,
                            (userStr,result) => {
                                if (result)
                                {
                                    receiveAsync(newConn);
                                }
                                else
                                {
                                    //认证不通过
                                    newConn.Send(Encoding.Default.GetBytes(SocketErrResult.Refuse));
                                    newConn.Close();
                                }
                            });
                    }
                    else
                    {
                        receiveAsync(newConn);
                    }
                }
            });
            listenTask.Start();
            Console.WriteLine("spreader listen address {0}:{1}",ip,port);
        }
        private void receiveAsync(Socket socket)
        {
            new Task(() => 
            {
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[128];
                        int len = socket.Receive(buffer);
                        if (len > 0)
                        {
                            var msg = Encoding.Default.GetString(buffer, 0, len);
                            if (SpreadCallback != null)
                            {
                                SpreadCallback(msg);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }catch(SocketException e)
                    {
                        break;
                    }
                }
            }).Start();
        }

        public void Dispose()
        {
            currSocket.Close();
            listenTask.Wait();
            listenTask.Dispose();
        }
    }
}
