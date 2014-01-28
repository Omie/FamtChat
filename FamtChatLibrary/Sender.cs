using System.Net.Sockets;
using System;

namespace FamtChatLibrary
{
    public static class Sender
    {
        /// <summary>
        /// Simple static function that is used in multiple forms
        /// </summary>        
        /// <param name="handler"></param>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public static void send(Socket handler, MessageType type, string data)
        {
            try
            {
                byte[] bt;
                MessageWrapper mw = new MessageWrapper();
                mw.MessageType = type;
                mw.Data = data;
                bt = mw.Serialize();
                handler.Send(bt);
            }
            catch (Exception)
            { }
        }
    }
}
