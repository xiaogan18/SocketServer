using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgServer.SocketServer
{
    public class SocketServerFactory
    {
        /// <summary>
        /// 创建socket服务
        /// </summary>
        /// <returns></returns>
        public static SocketPool.SocketPoolManager SocketServerManager()
        {
            return SocketPool.SocketPoolManager.Init(heartFreq: 0);
        }
        /// <summary>
        /// 创建socket服务
        /// </summary>
        /// <param name="heartFreq">心跳间隔</param>
        /// <returns></returns>
        public static SocketPool.SocketPoolManager SocketServerManager(int heartFreq)
        {
            return SocketPool.SocketPoolManager.Init(heartFreq: heartFreq);
        }
        /// <summary>
        /// 创建socket服务
        /// </summary>
        /// <param name="heartFreq">心跳间隔</param>
        /// <param name="offlineContainer">离线消息存储容器(不需要可null)</param>
        /// <param name="socketFilter">socket过滤器(不需要可null)</param>
        /// <param name="retryNumber">允许失败重试次数</param>
        /// <param name="retyInterval">失败重试间隔</param>
        /// <returns></returns>
        public static SocketPool.SocketPoolManager SocketServerManager(OfflineMessage.IOfflineContainer offlineContainer,Filter.ISocketFilter socketFilter,int heartFreq=0,int retryNumber=3,int retyInterval=30000)
        {
            var model= SocketPool.SocketPoolManager.Init(heartFreq: heartFreq,retryNumber:retryNumber,retryInterval:retyInterval);
            model.OfflineContainer = offlineContainer;
            model.SocketFilter = socketFilter;
            return model;
        }

        /// <summary>
        /// 创建socket广播
        /// </summary>
        /// <param name="socketServer"></param>
        /// <param name="adminFiler"></param>
        /// <returns></returns>
        public static SocketSpreader SocketAdminManager(SocketPool.SocketPoolManager socketServer,Filter.ISocketFilter adminFiler)
        {
            //格式 message;user1;user2;user3
            Action<string> action = (msg) =>
            {
                var arr = msg.Split(';');
                if (arr.Length > 1)
                {
                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(arr[i]))
                        {
                            socketServer.Notify(arr[0], arr[i]);
                        }
                    }
                }
                else
                {
                    socketServer.Notify(msg);
                }
            };
            return new SocketSpreader(action, adminFiler);
        }
    }
}
