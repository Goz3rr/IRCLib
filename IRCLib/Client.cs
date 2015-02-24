using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

using IRCLib.Data;

namespace IRCLib {
    public class Client : IDisposable {
        public delegate void MessageHandler(Client client, Message message);

        private readonly Dictionary<string, MessageHandler> handlers = new Dictionary<string, MessageHandler>();

        public event EventHandler<RawMessageEventArgs> RawMessageSent;
        public event EventHandler<RawMessageEventArgs> RawMessageReceived;
        public event EventHandler<ProcessedMessageEventArgs> MessageReceived;
        public event EventHandler<EventArgs> Connected;

        private byte[] ReadBuffer { get; set; }
        private int ReadBufferIndex { get; set; }

        private string ServerHostname { get; set; }
        private int ServerPort { get; set; }
        private bool ServerSSL { get; set; }

        public string ServerAddress {
            get { return ServerHostname + ":" + ServerPort; }
            set {
                string[] parts = value.Split(':');
                if(parts.Length > 2 || parts.Length == 0) throw new FormatException("Format should be hostname:port");
                ServerHostname = parts[0];
                ServerPort = parts.Length > 1 ? Int32.Parse(parts[1]) : 6667;
            }
        }

        public bool IsConnected {
            get { return Connection != null && Connection.Client != null && Connection.Connected; }
        }

        protected User User { get; set; }
        //public Socket Socket { get; set; }
        protected TcpClient Connection { get; set; }

        public Client(string address, User user, bool ssl = false) {
            if(address == null) throw new ArgumentNullException("address");
            if(user == null) throw new ArgumentNullException("user");

            ServerAddress = address;
            ServerSSL = ssl;
            User = user;

            ReadBuffer = new byte[1024];
            ReadBufferIndex = 0;

            RegisterDefaultHandlers();
            RegisterHandlers();
        }

        public void Dispose() {
            //if(Socket.Connected) Quit();
            //else Socket.Dispose();

            if(IsConnected) Quit();
        }

        public void Connect() {
            /*
            if(Socket != null && Socket.Connected) throw new InvalidOperationException("Already connected to a server");
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.BeginConnect(ServerHostname, ServerPort, ConnectComplete, null);
            */

            if(IsConnected) throw new InvalidOperationException("Already connected to a server");
            Connection = new TcpClient();
            Connection.BeginConnect(ServerHostname, ServerPort, ConnectComplete, null);
        }

        public void Quit(string reason = null) {
            if(reason == null) SendRaw("QUIT");
            else SendRaw("QUIT :{0}", reason);

            //Socket.Disconnect(false);
            Connection.Client.Disconnect(false);
            Connection.Close();
        }

        public void SendRaw(string message) {
            byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
            //Socket.BeginSend(data, 0, data.Length, SocketFlags.None, MessageSent, message);
            Connection.Client.BeginSend(data, 0, data.Length, SocketFlags.None, MessageSent, message);

            if(RawMessageSent != null) RawMessageSent.Invoke(this, new RawMessageEventArgs(message));
        }

        public void SendRaw(string format, params object[] args) {
            string message = String.Format(format, args);
            SendRaw(message);
        }

        public void SetHandler(string message, MessageHandler handler) {
            handlers[message.ToUpper()] = handler;
        }

        private void RegisterHandlersForType(Type type) {
            MethodInfo[] methods = type.GetMethods();

            foreach(MethodInfo method in methods) {
                Handler token = Attribute.GetCustomAttribute(method, typeof(Handler), false) as Handler;
                if(token == null) continue;
                MessageHandler func = Delegate.CreateDelegate(typeof(MessageHandler), method) as MessageHandler;
                if(func == null) continue;

                SetHandler(token.Message, func);
            }
        }

        public void RegisterDefaultHandlers() {
            RegisterHandlersForType(typeof(Handlers));
        }

        public void RegisterHandlers() {
            foreach(Type type in Assembly.GetEntryAssembly().GetTypes()) {
                RegisterHandlersForType(type);
            }
        }

        private void ConnectComplete(IAsyncResult result) {
            if(Connection == null || Connection.Client == null) return;

            try {
                //Socket.EndConnect(result);
                Connection.EndConnect(result);
                //Socket.BeginReceive(ReadBuffer, ReadBufferIndex, ReadBuffer.Length, SocketFlags.None, DataRecieved, null);
                Connection.Client.BeginReceive(ReadBuffer, ReadBufferIndex, ReadBuffer.Length, SocketFlags.None, DataRecieved, null);

                if(User != null) {
                    SendRaw("PASS {0}", User.Password);
                    SendRaw("NICK {0}", User.NickName);
                    SendRaw("USER {0} 0 * :{1}", User.UserName, User.RealName);
                }

                if(Connected != null) Connected.Invoke(this, new EventArgs());
            } catch(SocketException) {}
        }

        private void DataRecieved(IAsyncResult result) {
            if(!IsConnected || !Connection.Client.Connected) return;

            SocketError error;
            //int length = Socket.EndReceive(result, out error) + ReadBufferIndex;
            int length = Connection.Client.EndReceive(result, out error) + ReadBufferIndex;
            if(error != SocketError.Success) {
                Debug.WriteLine("ERROR: {0}", error);
                return;
            }

            ReadBufferIndex = 0;
            while(length > 0) {
                int messageLength = Array.IndexOf(ReadBuffer, (byte)'\n', 0, length);
                if(messageLength == -1) {
                    ReadBufferIndex = length;
                    break;
                }
                messageLength++;

                string rawMessage = Encoding.UTF8.GetString(ReadBuffer, 0, messageLength - 2);
                if(RawMessageReceived != null) RawMessageReceived.Invoke(this, new RawMessageEventArgs(rawMessage));

                Message message = new Message(rawMessage);
                if(handlers.ContainsKey(message.Command.ToUpper())) handlers[message.Command.ToUpper()](this, message);
                else Debug.WriteLine("Missing handler for command {0} ({1})", message.Command.ToUpper(), message.RawData);

                if(MessageReceived != null) MessageReceived.Invoke(this, new ProcessedMessageEventArgs(message));

                Array.Copy(ReadBuffer, messageLength, ReadBuffer, 0, length - messageLength);
                length -= messageLength;
            }

            try {
                //Socket.BeginReceive(ReadBuffer, ReadBufferIndex, ReadBuffer.Length - ReadBufferIndex, SocketFlags.None, DataRecieved, null);
                Connection.Client.BeginReceive(ReadBuffer, ReadBufferIndex, ReadBuffer.Length - ReadBufferIndex, SocketFlags.None, DataRecieved, null);
            } catch(SocketException e) {
                if(e.SocketErrorCode != SocketError.NotConnected && e.SocketErrorCode != SocketError.Shutdown) throw;
            }
        }

        private void MessageSent(IAsyncResult result) {
            if(Connection == null || Connection.Client == null) return;

            SocketError error;
            //Socket.EndSend(result, out error);
            Connection.Client.EndSend(result, out error);
            if(error != SocketError.Success) Debug.WriteLine("ERROR: {0}", error);
        }
    }
}