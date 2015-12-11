using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

using IRCLib.Data;

namespace IRCLib {
    /// <summary>
    ///     IRC Client
    /// </summary>
    public class Client : IDisposable {
        public delegate void MessageHandler(Client client, Message message);

        private readonly Dictionary<string, MessageHandler> _handlers = new Dictionary<string, MessageHandler>();

        /// <summary>
        ///     Event invoked when a raw message is sent
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageSent;

        /// <summary>
        ///     Event invoked when a raw message is received
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageReceived;

        /// <summary>
        ///     Event invoked after a raw message has been processed
        /// </summary>
        public event EventHandler<ProcessedMessageEventArgs> MessageReceived;

        /// <summary>
        ///     Event invoked once client is succesfully connected
        /// </summary>
        public event EventHandler<EventArgs> Connected;

        /// <summary>
        ///     Event invoked when an error with the underlying connection occurs
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        private readonly byte[] _readBuffer;
        private int _readBufferIndex;
        private int _usernameTries;

        public string ServerHostname { get; protected set; }
        public int ServerPort { get; protected set; }
        public bool ServerSSL { get; protected set; }

        /// <summary>
        ///     Server address to connect to in format hostname:[port]
        ///     Port defaults to 6667 if left out
        /// </summary>
        public string ServerAddress {
            get { return ServerHostname + ":" + ServerPort; }
            set {
                if(Connection != null) {
                    throw new InvalidOperationException("Cannot change server after connecting");
                }

                string[] parts = value.Split(':');
                if(parts.Length > 2 || parts.Length == 0) {
                    throw new FormatException("Format should be hostname:port");
                }

                ServerHostname = parts[0];
                ServerPort = parts.Length > 1 ? Int32.Parse(parts[1]) : 6667;
            }
        }

        /// <summary>
        ///     If the client is currently connected to a server
        /// </summary>
        public bool IsConnected {
            get { return Connection != null && Connection.Client != null && Connection.Connected; }
        }

        protected User User { get; set; }
        protected TcpClient Connection { get; set; }
        protected SslStream SslStream { get; set; }

        public Client(string address, User user, bool ssl = false) {
            if(address == null) {
                throw new ArgumentNullException("address");
            }
            if(user == null) {
                throw new ArgumentNullException("user");
            }

            ServerAddress = address;
            ServerSSL = ssl;
            User = user;

            _readBuffer = new byte[1024];
            _readBufferIndex = 0;

            RegisterDefaultHandlers();
            RegisterHandlers();
        }

        public void Dispose() {
            if(IsConnected) {
                Quit();
            }
        }

        /// <summary>
        ///     Connects to server
        /// </summary>
        public bool Connect() {
            if(IsConnected) {
                throw new InvalidOperationException("Already connected to a server");
            }

            Connection = new TcpClient();
            try {
                Connection.Connect(ServerHostname, ServerPort);

                if (ServerSSL) {
                    SslStream = new SslStream(Connection.GetStream());
                    SslStream.AuthenticateAsClient(ServerHostname);
                }

                Connection.Client.BeginReceive(_readBuffer, _readBufferIndex, _readBuffer.Length, SocketFlags.None, DataRecieved, null);

                if (User != null) {
                    SendRaw("PASS {0}", User.Password);
                    SendRaw("NICK {0}", User.NickName);
                    SendRaw("USER {0} 0 * :{1}", User.UserName, User.RealName);
                }

                if (Connected != null) {
                    Connected.Invoke(this, EventArgs.Empty);
                }
            } catch(SocketException e) {
                if (Error != null) {
                    Error.Invoke(this, new ErrorEventArgs(e));
                }

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Disconnect from server
        /// </summary>
        /// <param name="reason">Optional reason to send to server</param>
        public void Quit(string reason = null) {
            if(reason == null) {
                SendRaw("QUIT");
            } else {
                SendRaw("QUIT :{0}", reason);
            }

            if(SslStream != null) {
                SslStream.Close();
            }

            Connection.Client.Disconnect(false);
            Connection.Close();
        }

        /// <summary>
        ///     Send a raw message to server
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendRaw(string message) {
            byte[] data = Encoding.UTF8.GetBytes(message + (message.EndsWith("\r\n") ? "" : "\r\n"));
            Connection.Client.BeginSend(data, 0, data.Length, SocketFlags.None, MessageSent, message);

            if(RawMessageSent != null) {
                RawMessageSent.Invoke(this, new RawMessageEventArgs(message));
            }
        }

        /// <summary>
        ///     Send a raw formatted message to server
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void SendRaw(string format, params object[] args) {
            string message = String.Format(format, args);
            SendRaw(message);
        }

        /// <summary>
        ///     Set the handler for a command
        /// </summary>
        /// <param name="message">Command to listen for, case insensitive</param>
        /// <param name="handler">Handler to call when command is received</param>
        public void SetHandler(string message, MessageHandler handler) {
            _handlers[message.ToUpper()] = handler;
        }

        private void RegisterHandlersForType(Type type) {
            MethodInfo[] methods = type.GetMethods();

            foreach(MethodInfo method in methods) {
                Handler token = Attribute.GetCustomAttribute(method, typeof(Handler), false) as Handler;
                if(token == null) {
                    continue;
                }

                MessageHandler func = Delegate.CreateDelegate(typeof(MessageHandler), method) as MessageHandler;
                if(func == null) {
                    continue;
                }

                SetHandler(token.Message, func);
            }
        }

        private void RegisterDefaultHandlers() {
            RegisterHandlersForType(typeof(Handlers));
            SetHandler("433", RetryNickname);
        }

        private void RetryNickname(Client client, Message message) {
            if(_usernameTries > 5) {
                if(Error != null) {
                    Error.Invoke(this, new ErrorEventArgs(new NicknameInUseException(_usernameTries, User.NickName)));
                }
                return;
            }

            _usernameTries++;
            if(_usernameTries < 4) {
                SendRaw("NICK {0}-{1}", User.NickName, _usernameTries);
            } else {
                SendRaw("NICK {0}-{1}", User.NickName.Substring(0, 7), _usernameTries - 3);
            }
        }
        
        private void RegisterHandlers() {
            foreach(Type type in Assembly.GetEntryAssembly().GetTypes()) {
                RegisterHandlersForType(type);
            }
        }

        private void DataRecieved(IAsyncResult result) {
            if(!IsConnected || !Connection.Client.Connected) {
                return;
            }

            SocketError error;
            int length = Connection.Client.EndReceive(result, out error) + _readBufferIndex;
            if(error != SocketError.Success) {
                Debug.WriteLine("ERROR: {0}", error.ToString());
                return;
            }

            _readBufferIndex = 0;
            while(length > 0) {
                int messageLength = Array.IndexOf(_readBuffer, (byte)'\n', 0, length);
                if(messageLength == -1) {
                    _readBufferIndex = length;
                    break;
                }
                messageLength++;

                string rawMessage = Encoding.UTF8.GetString(_readBuffer, 0, messageLength - 2);
                if(RawMessageReceived != null) {
                    RawMessageReceived.Invoke(this, new RawMessageEventArgs(rawMessage));
                }

                Message message = new Message(rawMessage);
                if(_handlers.ContainsKey(message.Command.ToUpper())) {
                    _handlers[message.Command.ToUpper()](this, message);
                } else {
                    Debug.WriteLine(String.Format("Missing handler for command {0} ({1})", message.Command.ToUpper(), message.RawData));
                }

                if(MessageReceived != null) {
                    MessageReceived.Invoke(this, new ProcessedMessageEventArgs(message));
                }

                Array.Copy(_readBuffer, messageLength, _readBuffer, 0, length - messageLength);
                length -= messageLength;
            }

            try {
                Connection.Client.BeginReceive(_readBuffer, _readBufferIndex, _readBuffer.Length - _readBufferIndex, SocketFlags.None, DataRecieved, null);
            } catch(SocketException e) {
                if(e.SocketErrorCode != SocketError.NotConnected && e.SocketErrorCode != SocketError.Shutdown) {
                    if(Error != null) {
                        Error.Invoke(this, new ErrorEventArgs(e));    
                    }

                    throw;
                }
            }
        }

        private void MessageSent(IAsyncResult result) {
            if(Connection == null || Connection.Client == null) {
                return;
            }

            SocketError error;
            Connection.Client.EndSend(result, out error);
            if(error != SocketError.Success) {
                Debug.WriteLine("ERROR: {0}", error.ToString());
            }
        }
    }
}