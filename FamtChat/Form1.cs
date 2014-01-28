using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FamtChatLibrary;
using System.Net;


namespace FamtChat
{
    public partial class ServerForm : Form
    {
        Listener listener;
        Boolean listening;
        private Dictionary<String, StateObject> ChatClients; //holds list of all connected clients

        public ServerForm()
        {
            InitializeComponent();
            listening = false;
            ChatClients = new Dictionary<string, StateObject>();
            Control.CheckForIllegalCrossThreadCalls = false; // not required now. Check and take it out.
        }

        private void btnToggle_Click(object sender, EventArgs e)
        {
            //Start/Stop server
            if (!listening)
            {
                int result;
                if (!Int32.TryParse(tbServerPort.Text, out result))
                    result = 6541;
                listener = new Listener(tbServerIp.Text , result);
                //Subscribe to ClientConnected event.
                listener.ClientConnected += new Listener.ClientConnectedHandler(this.ClientConnected);
                listener.DataReceived   +=new Listener.DataReceivedHandler(DataReceived);
                ChatClients = new Dictionary<string, StateObject>();
                listener.StartServer();
                tbServerIp.Enabled = false;
                tbServerPort.Enabled = false;
                btnToggle.ForeColor = Color.Red;
                btnToggle.Text = "Stop";
                listening = true;
            }
            else
            {
                listener.StopServer();
                listening = false;
                btnToggle.Text = "Start";
                btnToggle.ForeColor = Color.Black;
                tbServerIp.Enabled = true;
                tbServerPort.Enabled = true;
                Broadcast(MessageType.DISCONNECT, "");
            }
        }

        /// <summary>
        /// Event Fired when a Client gets connected. Following actions are performed        
        /// </summary>        
        public void ClientConnected(ClientConnectedEventArgs e)
        {
            //Send connection ACK
            Sender.send(e.ChatClient.Client, MessageType.ACK, "");
            String remoteIP = ((IPEndPoint)e.ChatClient.Client.RemoteEndPoint).Address.ToString();
            String remotePort = ((IPEndPoint)e.ChatClient.Client.RemoteEndPoint).Port.ToString();
            //Log
            AppendLog("Connected: " + remoteIP + " : " + remotePort);
        }

        /// <summary>
        /// Asynchronous Callback function which receives data from Server
        /// </summary>
        /// <param name="ar"></param>
        public void DataReceived(DataReceivedEventArgs e)
        {
            Byte[] content = e.State.buffer;
            MessageWrapper rmw = MessageWrapper.Desserialize(content);            
            switch (rmw.MessageType)
            {
                case MessageType.IDENTIFY:
                    //Client has sent its ScreeName
                    //If exists, say Duplicate
                    //Else say Success
                    if (ChatClients.ContainsKey(rmw.Data))
                    {
                        Sender.send(e.State.workSocket, MessageType.DUP_IDENTIFY, "");
                    }
                    else
                    {
                        Broadcast(MessageType.LOGGEDIN, rmw.Data);
                        ChatClients.Add(rmw.Data , e.State);
                        UpdateView(rmw.Data);
                        Sender.send(e.State.workSocket, MessageType.SUCCESS_IDENTIFY , "");
                    }
                    break;
                case MessageType.REQ_LIST:
                    //return list - ChatClients
                    String data1 = "";
                    StateObject _state;
                    foreach (String key in ChatClients.Keys)
                    {                        
                        data1 += key + ',';                        
                    }
                    Sender.send(e.State.workSocket, MessageType.REC_LIST, data1);
                    break;
                case MessageType.REM_LIST:
                    //remove from our list
                    if(ChatClients.ContainsKey(rmw.Data))
                    {
                        ChatClients.Remove(rmw.Data);
                        AppendLog("Disconnected:" + rmw.Data);
                        RefreshView();
                        Broadcast(MessageType.LOGGEDOUT, rmw.Data);
                    }
                    break;
                case MessageType.MSG_INIT:
                    //ask that particular client to connect to other one-
                    String[] data = rmw.Data.Split(new char[] {'%'});
                    //remote node name % my name % my port
                    _state = ChatClients[data[0]];                    
                    String remoteIP1 = ((IPEndPoint) e.State.tc.Client.RemoteEndPoint).Address.ToString();
                    String remotePort1 = data[2];
                    String data2 = data[1] + "%" + remoteIP1 + "%" + remotePort1;
                    //requester's name % requester's ip % requester's port
                    Sender.send(_state.workSocket, MessageType.MSG_INIT, data2);
                    break;
                default:
                    break;
            }                       
            
        }

        //handy to spam all clients
        //logged in/out notifications etc.
        private void Broadcast(MessageType type, String data)
        {
            String msg = "";
            //prepeare message
            switch (type)
            {                
                case MessageType.SUCCESS_IDENTIFY:
                    msg = data + " logged in!";
                    break;                
                case MessageType.REM_LIST:
                    msg = data + " logged out!";
                    break;
                case MessageType.DISCONNECT:
                    msg = "disconnecting" ;
                    break;
                default:
                    break;
            }
            //send notification
            foreach (StateObject value in ChatClients.Values)
            {
                Sender.send(value.workSocket, type, msg);
            }
        }

        //Helper one. Use delegates to make avoid illegal cross thread
        //calls
        public delegate void AppendLogCallback(string s);
        private void AppendLog(String msg)
        {
            if (rtbLog.InvokeRequired)
            {
                AppendLogCallback d = new AppendLogCallback(AppendLog);
                this.Invoke(d, new object[] { msg });
            }
            else
                rtbLog.AppendText(msg + "\n");
        }

        //Update Treeview when someone logs in
        public delegate void UpdateViewCallback(string s);
        private void UpdateView(string s)
        {
            if (this.tvClients.InvokeRequired)
            {
                UpdateViewCallback d = new UpdateViewCallback(UpdateView);
                this.Invoke(d, new object[] { s});
            }
            else
            {
                this.tvClients.Nodes[0].Nodes.Add(s);
            }
        }

        //Refresh treeview from ChatClients
        public delegate void RefreshViewcallback();
        private void RefreshView()
        {
            if (this.tvClients.InvokeRequired)
            {
                RefreshViewcallback d = new RefreshViewcallback(RefreshView);
                this.Invoke(d);
            }
            else
            {
                this.tvClients.Nodes[0].Nodes.Clear();
                foreach (String key in ChatClients.Keys)
                {
                    this.tvClients.Nodes[0].Nodes.Add(key);
                }
            }
        }

        //tata-bbye etc
        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Broadcast(MessageType.DISCONNECT, "");
                listener.StopServer();
            }
            catch (Exception)
            {
                //   
            }
            finally
            {
                listener = null;                
            }            

        }
    }
}
