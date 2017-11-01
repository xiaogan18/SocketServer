using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nurse.SocketServer
{
    public class SocketErrResult
    {
        /// <summary>
        /// 拒绝连接
        /// </summary>
        public const string Refuse = "401";

        public const string Timeout = "402";
    }
}
