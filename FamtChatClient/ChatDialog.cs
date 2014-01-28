using System.Windows.Forms;
using System.Net.Sockets;
using FamtChatLibrary;
using System.Net;
using System.Text;
using System;

namespace FamtChatClient
{
    public enum ChatDialogType
    {
        WAIT = 10,
        CONNECT = 20        
    }
    
    public partial class ChatDialog : Form
    {

        Listener listener;
        TcpClient connectedTo;
        IPAddress foreignAddr;
        String MyName;
        String PartnerName="";

        public ChatDialog()
        {
            InitializeComponent();            
        }

        public void initstuff(String ipaddr, int port)
        {
            InitializeComponent();
            listener = new Listener(ipaddr, port);
            //Subscribe to ClientConnected event.
            listener.ClientConnected += new Listener.ClientConnectedHandler(this.ClientConnected);
            listener.DataReceived += new Listener.DataReceivedHandler(DataReceived);            
        }

        public ChatDialog(String ipaddr, int port, ChatDialogType _type, String MyName, 
                            String PartnerName)
        {
            this.MyName = MyName;
            this.PartnerName = PartnerName;
            
            if (_type == ChatDialogType.WAIT)
            {
                //start Client and wait for someone
                initstuff(ipaddr, port);
                listener.StartServer();
            }
            else if (_type == ChatDialogType.CONNECT)
            {
                //Connect to someone
                initstuff("127.0.0.1", port);
                //127.0.0.1 because this listner isn't really listening for anything
                //we need it only for that OnReceive callback function.
                connectedTo = new TcpClient();
                foreignAddr = IPAddress.Parse(ipaddr);
                connectedTo.Client.Connect(new IPEndPoint(foreignAddr, port));
                StateObject state = new StateObject();
                state.workSocket = connectedTo.Client;
                //Call Asynchronous Receive Function            
                connectedTo.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(listener.OnReceive), state);
            }
            this.Text = "FamtChat :: " + PartnerName;
        }                

        /// <summary>
        /// Callback for when client is connected
        /// </summary>        
        public void ClientConnected(ClientConnectedEventArgs e)
        {            
            connectedTo = e.ChatClient;            
        }

        /// <summary>
        /// Listener gives us nice output in its DataReceived event.
        /// </summary>        
        public void DataReceived(DataReceivedEventArgs e)
        {
            byte[] content = e.State.buffer;
            MessageWrapper rmw = MessageWrapper.Desserialize(content);
            switch (rmw.MessageType)
            {             
                //MessageType can further be extended for multimedia support.
                //handle that here in same fashion
                case MessageType.MSG:
                    //next part should be intended username
                    //if exists, return error
                    UpdateChat(rmw.Data, this.PartnerName);
                    break;
                default:
                    break;
            }
        }

        public delegate void UpdateChatCallback(string s, String sender);
        private void UpdateChat(string data, String sender)
        {   
            //Push message to RichTextBox
            if (this.InvokeRequired)
            {
                UpdateChatCallback d = new UpdateChatCallback(UpdateChat);
                this.Invoke(d, new object[] { data, sender });
            }
            else
            {                
                rtbChat.AppendText(sender + ": " + data + "\n");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {   
            //Send chat
            //push to our RTB too.
            UpdateChat(tbMessage.Text, this.MyName);
            Sender.send(connectedTo.Client, MessageType.MSG, tbMessage.Text);
            tbMessage.Text = "";
        }

    }
}
