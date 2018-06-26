using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgServer.SocketServer.Filter;

namespace MsgServer.ServerHost.SocketServerBLL
{
    public class UserConnectFilter:ICustomFilter
    {
        public bool Verify(string userStr)
        {
            var userInfo = userStr.Split(';');
            if (userInfo.Length > 1 )
            {
                return true;
            }
            return false;
        }
    }
}
