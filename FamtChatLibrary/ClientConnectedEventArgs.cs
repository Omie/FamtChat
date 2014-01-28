using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FamtChatLibrary
{
    public class ClientConnectedEventArgs
    {
        public TcpClient ChatClient{ get; set; }

        public ClientConnectedEventArgs()
        { }

        public ClientConnectedEventArgs(TcpClient cc)
        {
            this.ChatClient = cc;
        }
    }
}
