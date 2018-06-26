using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MsgServer.SocketServer
{
    public static class StaticExtend
    {
        public static bool TrySend(this Socket _this,byte[] data)
        {
            try
            {
                _this.Send(data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
