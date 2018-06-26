using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MsgServer.SocketServer.Filter
{
    public interface ISocketFilter
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="socket">新加入的连接</param>
        /// <returns></returns>
        bool Verify(Socket socket,out string userStr);
        /// <summary>
        /// 异步验证
        /// </summary>
        /// <param name="socket">新加入的连接</param>
        /// <param name="success">完成回调</param>
        void VerifyAsync(Socket socket, Action<string,bool> callback);
        /// <summary>
        /// 注册自定义过滤器
        /// </summary>
        /// <param name="customFilter"></param>
        void Register(ICustomFilter customFilter);
    }
}
