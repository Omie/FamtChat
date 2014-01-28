using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FamtChatLibrary
{
    public class DataReceivedEventArgs
    {
        public String Data { get; set; }        
        public StateObject State { get; set; }

        public DataReceivedEventArgs()
        { }

        public DataReceivedEventArgs(String data, StateObject state)
        {
            this.Data = data;            
            this.State = state;
        }
    }
}
