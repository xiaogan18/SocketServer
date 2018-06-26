using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgServer.Tests
{
    class TestFilter : MsgServer.SocketServer.Filter.ICustomFilter
    {
        public bool Verify(string userStr)
        {
            var userInfo = userStr.Split(';');
            if (userInfo.Length > 1 && userInfo[1] == "123456")
            {
                return true;
            }
            return false;
        }
    }
}
