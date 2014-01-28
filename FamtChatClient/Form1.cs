using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using FamtChatLibrary;

namespace FamtChatClient
{
    public partial class Form1 : Form
    {
        TcpClient ServerClient; //Connection with main server.
        IPAddress foreignAddr; //server address
        public Form1()
        {
            InitializeComponent();            
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //Connect. Sort of Login
                pbStatus.Visible = true;
                if (string.IsNullOrWhiteSpace(tbClientIp.Text))
                {
                    MessageBox.Show("Please enter you ip address.");
                    return;
                }
                foreignAddr = IPAddress.Parse(tbServerIp.Text);
                int port;
                if (!Int32.TryParse(tbServerPort.Text, out port))
                    port = 6541;

                ServerClient = new TcpClient();
                ServerClient.Client.Connect(new IPEndPoint(foreignAddr, port));

                StateObject state = new StateObject();
                state.workSocket = ServerClient.Client;                
                Listener _listener = new Listener();
                _listener.DataReceived += new Listener.DataReceivedHandler(DataReceived);

                //Call Asynchronous Receive Function
                ServerClient.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(_listener.OnReceive), state);

                ToggleSettings(false); 
                lblStatus.Text = "Connected";
                pbStatus.Visible = false;
            }
            catch (Exception)
            {
                lblStatus.Text = "Error !";
                ToggleSettings(true);
                pbStatus.Visible = false;
            }
        }

        /// <summary>
        /// Disconnect from Server
        /// </summary>        
        private void DisconnectClient()
        {
            if (ServerClient != null)
            {
                try
                {                    
                    pbStatus.Visible = true;
                    ServerClient.Close();
                }
                catch (Exception)
                {
                    //eat it                    
                }
                finally
                {
                    ToggleSettings(true);
                    pbStatus.Visible = false;
                    lblStatus.Text = "Disconnected";
                    this.tvUsers.Nodes[0].Nodes.Clear();
                }
            }
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
                case MessageType.ACK:
                    //Server wants to know your name
                    Sender.send(e.State.workSocket , MessageType.IDENTIFY, tbScreenName.Text);                    
                    break;
                case MessageType.DUP_IDENTIFY:
                    //your name is already taken
                    MessageBox.Show("This name is already taken, please choose a different one!");
                    DisconnectClient();
                    break;
                case MessageType.FAILED_IDENTIFY:
                    //something went wrong
                    break;                
                case MessageType.SUCCESS_IDENTIFY:
                    //logged in, request list of connected clients
                    Sender.send(e.State.workSocket, MessageType.REQ_LIST, "");
                    break;
                case MessageType.REC_LIST:
                    //handle received list of online clients
                    UpdateList(rmw.Data);                    
                    break;
                case MessageType.MSG_INIT:
                    //someone is trying to have chat with you
                    //connect to them
                    //this is done this way to keep another Listener out of this form.
                    //putting it inside ChatDialog instead.
                    Thread t = new Thread(new ParameterizedThreadStart(spawnChatDialog));                    
                    //remoteIP, remote port
                    t.Start(rmw.Data);
                    break;
                case MessageType.LOGGEDIN:
                case MessageType.LOGGEDOUT:
                    Sender.send(e.State.workSocket, MessageType.REQ_LIST, "");
                    //In both cases, just update the list
                    //Use it for Logged-in / Logged-out notifications
                    break;
                case MessageType.DISCONNECT:
                    //server is killed
                    DisconnectClient();
                    break;
                default:
                    break;
            }
        }
        
        public delegate void UpdateListCallback(string s);
        private void UpdateList(String data)
        {
            //handle received list of connected clients
            //its a comma-seperated string
            if (this.InvokeRequired)
            {
                UpdateListCallback d = new UpdateListCallback(UpdateList);
                this.Invoke(d, new object[] { data });
            }
            else
            {
                string[] clients = data.Split(new char[] { ',' });
                this.tvUsers.Nodes[0].Nodes.Clear();
                foreach (String item in clients)
                {                    
                    if (item != "")
                        this.tvUsers.Nodes[0].Nodes.Add(item);   
                }
            }
        }

        private int my_child_port = 0;
        private void tvUsers_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //Create a connection and wait for client to connect
            if (my_child_port == 0)
            {
                if (!Int32.TryParse(tbClientPort.Text, out my_child_port))
                    my_child_port = 10987;
            }
            String data = e.Node.Text + "%" + tbScreenName.Text  + "%" + my_child_port;
            //remote node name % my port
            new ChatDialog(tbClientIp.Text,  this.my_child_port, ChatDialogType.WAIT, tbScreenName.Text,
                e.Node.Text).Show();
            this.my_child_port++;
            //tell our server to convey our message to other partner
            Sender.send(ServerClient.Client, MessageType.MSG_INIT, data);
        }
        
        private void spawnChatDialog(Object param)
        {
            //this is called when someone wants to get connected
            //someone is waiting and we now need to connect.
            String param2 = (string)param;            
            String[] _data = param2.Split(new char[] {'%'});
            //name % ip % port
            String ip = _data[1];
            int port;
            Int32.TryParse(_data[2], out port);
            new ChatDialog(ip, port, ChatDialogType.CONNECT, tbScreenName.Text, _data[0]).ShowDialog();            
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {   
            //refresh the list manually. Not needed though.
            pbStatus.Visible = true;
            lblStatus.Text = "Refreshing List";
            Sender.send(ServerClient.Client, MessageType.REQ_LIST, "");
            lblStatus.Text = "Ready";
            pbStatus.Visible = false;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            //Disconnect
            Sender.send(ServerClient.Client, MessageType.REM_LIST, tbScreenName.Text);
            DisconnectClient();
            ToggleSettings(true);
            this.tvUsers.Nodes[0].Nodes.Clear();
        }

        private void ToggleSettings(bool newstate)
        {
            //Enable Disable controls.
            tbScreenName.Enabled = newstate;
            tbClientIp.Enabled = newstate;
            tbClientPort.Enabled = newstate;
            tbServerIp.Enabled = newstate;
            tbServerPort.Enabled = newstate;
            btnConnect.Enabled = newstate;
            btnRefresh.Enabled = !newstate;
            btnDisconnect.Enabled = !newstate;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Take care, bbye, etc.           
            try
            {
                DisconnectClient();
            }
            catch (Exception)
            {
                //eat   
            }
            finally
            {
                ServerClient = null;                
            }   
        }
    }
}
