using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgServer.SocketServer.Filter;

namespace MsgServer.ServerHost.SocketServerBLL
{
    public class AdminConnectFilter:ICustomFilter
    {

        public bool Verify(string userStr)
        {
            if (userStr == "123456") { return true; }
            return false;
        }
    }
}
