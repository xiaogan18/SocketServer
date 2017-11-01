using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nurse.SocketServer.Filter
{
    public interface ICustomFilter
    {
        /// <summary>
        /// 过滤验证
        /// </summary>
        /// <param name="userStr">从客户端得到的身份标识</param>
        /// <returns>验证结果</returns>
        bool Verify(string userStr);
    }
}
