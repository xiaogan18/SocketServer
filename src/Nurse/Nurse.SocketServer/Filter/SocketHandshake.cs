using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nurse.SocketServer.Filter
{
    public class SocketHandshake:ISocketFilter
    {
        private List<ICustomFilter> customFilters;
        private string publicKey { get; set; }
        private string privateKey { get; set; }

        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        public SocketHandshake()
        {
            customFilters = new List<ICustomFilter>();
            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
        }
        /// <summary>
        /// 开始握手
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool Verify(Socket socket, out string userStr)
        {
            //发送公钥
            socket.Send(Encoding.Default.GetBytes(publicKey));
            //接收凭据
            byte[] buffer=new byte[128];
            socket.Receive(buffer);
            //解密凭据
            userStr = decrypt(buffer);
            //调用委托验证
            return VerifyCallback(userStr);
        }
        public void VerifyAsync(Socket socket,Action<string,bool> callback)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            {
                //发送公钥
                var socketParam = obj as Socket;
                if (!socketParam.TrySend(Encoding.Default.GetBytes(publicKey)))
                {
                    callback(null, false);
                }
                byte[] buffer = new byte[128];
                if (Receive(socketParam, out buffer))
                {
                    //解密凭据
                    var userStr = decrypt(buffer);
                    //var userStr = Encoding.Default.GetString(buffer);

                    if (callback != null)
                        callback(userStr, userStr != null && VerifyCallback(userStr));
                }
            }),socket);
            
        }
        /// <summary>
        /// 注册自定义过滤器
        /// </summary>
        /// <param name="customFilter"></param>
        public void Register(ICustomFilter customFilter)
        {
            if(customFilters.IndexOf(customFilter)<0)
            {
                customFilters.Add(customFilter);
            }
        }


        private bool VerifyCallback(string userStr)
        {
            if (this.customFilters == null || this.customFilters.Count==0)
                return true;
            foreach(var filter in customFilters)
            {
                if (!filter.Verify(userStr))
                {
                    return false;
                }
            }
            return true;
        }

        private string decrypt(byte[] buffer)
        {
            try
            {
                var b= this.rsa.Decrypt(buffer, false);
                return Encoding.Default.GetString(b);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 接收客户端凭据
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private bool Receive(Socket socket,out byte[] buffer,int timeout=30000)
        {
            var task = new Task<byte[]>(() =>
              {
                  var bytes = new byte[512];
                  int recLength;
                  try
                  {
                      recLength=socket.Receive(bytes);
                  }
                  catch
                  {
                      return null;
                  }
                  return bytes.Take(recLength).ToArray();
              });
            task.Start();
            if (task.Wait(timeout) && task.Result!=null)
            {
                buffer = task.Result;
                return true;
            }
            else
            {
                buffer = null;
                socket.TrySend(Encoding.Default.GetBytes(SocketErrResult.Timeout));
                //socket.Close();
                return false;
            }
        }

    }
}
