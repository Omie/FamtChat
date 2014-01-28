using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace FamtChatLibrary
{
    /// <summary>
    /// Wrapper class to keep TcpServer stuff at once place.
    /// It listens for connection.
    /// Raises events, provides extracted data etc.
    /// </summary>
    public class Listener
    {
        public delegate void ClientConnectedHandler(ClientConnectedEventArgs e);
        public event ClientConnectedHandler ClientConnected;

        public delegate void DataReceivedHandler(DataReceivedEventArgs e);
        public event DataReceivedHandler DataReceived;

        private TcpListener tcpServer;
        private TcpClient tcpClient;
        private Thread th;
        String ip;
        int port;

        public Listener()
        { }

        public Listener(String ipaddr, int port)
        {
            this.ip = ipaddr;
            this.port = port;
            this.ClientConnected += new Listener.ClientConnectedHandler(this._ClientConnected);
        }
        
        /// <summary>
        /// This function spawns new thread for TCP communication
        /// </summary>
        public void StartServer()
        {            
            th = new Thread(new ThreadStart(StartListen));
            th.Start();
        }

        /// <summary>
        /// Server listens on the given port and accepts the connection from Client.
        /// As soon as the connection id made a dialog box opens up for Chatting.
        /// </summary>
        private void StartListen()
        {

            IPAddress localAddr = IPAddress.Parse(this.ip);
            tcpServer = new TcpListener(localAddr, this.port);
            tcpServer.Start();

            // Keep on accepting Client Connection
            while (true)
            {
                // New Client connected, call event
                tcpClient = tcpServer.AcceptTcpClient();
                ClientConnected(new ClientConnectedEventArgs(tcpClient));
            }
        }

        /// <summary>
        /// Function to stop the TCP communication. It kills the thread and closes client connection
        /// </summary>
        public void StopServer()
        {
            if (tcpServer != null)
            {
                // Abort Listening Thread and Stop listening
                tcpServer.Stop();
                th.Abort();                
            }            
        }

        /// <summary>
        /// Event Fired when a Client gets connected. Following actions are performed
        /// 1. Update Tree view
        /// 2. Open a chat box to chat with client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ClientConnected(ClientConnectedEventArgs e)
        {
            StateObject state = new StateObject();
            state.tc = e.ChatClient;
            state.workSocket = e.ChatClient.Client;
            //Call Asynchronous Receive Function
            e.ChatClient.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(OnReceive), state);            
        }

        /// <summary>
        /// Asynchronous Callback function which receives data from Server
        /// </summary>
        /// <param name="ar"></param>
        public void OnReceive(IAsyncResult ar)
        {
            String content = String.Empty;
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead;
            if (handler.Connected)
            {
                // Read data from the client socket. 
                try
                {
                    bytesRead = handler.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        // There  might be more data, so store the data received so far.
                        state.sb.Remove(0, state.sb.Length);
                        state.sb.Append(Encoding.ASCII.GetString(
                                         state.buffer, 0, bytesRead));                        
                        content = state.sb.ToString();
                        DataReceived(new DataReceivedEventArgs(content, state));                        
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);
                    }
                }
                catch (SocketException socketException)
                {
                    //WSAECONNRESET, the other side closed impolitely
                    if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
                    {
                        // Complete the disconnect request.
                        String remoteIP = ((IPEndPoint)handler.RemoteEndPoint).Address.ToString();
                        String remotePort = ((IPEndPoint)handler.RemoteEndPoint).Port.ToString();
                        //this.owner.DisconnectClient(remoteIP, remotePort);
                        handler.Close();
                        handler = null;
                    }
                }
                // Eat up exception....Hmmmm I'm loving eat!!!
                catch (Exception)
                {
                    //
                }
            }
        }
    }
}
