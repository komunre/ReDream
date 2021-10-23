using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReDream.Shared
{
    public class Message
    {
        byte[] msgBytes;
        string msg;
    }
    public class Networking
    {
        public List<Message> messages = new();
    }
}
