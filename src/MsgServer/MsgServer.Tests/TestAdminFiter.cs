using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgServer.Tests
{
    public class TestAdminFiter : MsgServer.SocketServer.Filter.ICustomFilter
    {
        public bool Verify(string userStr)
        {
            if (userStr == "root") { return true; }
            return false;
        }
    }
}
