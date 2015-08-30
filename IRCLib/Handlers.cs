using System;

using IRCLib.Data;

namespace IRCLib {
    /// <summary>
    ///     Handler attribute to define handler for IRC command
    /// </summary>
    public class Handler : Attribute {
        /// <summary>
        ///     Command to listen for, not case sensitive
        /// </summary>
        public string Message { get; private set; }

        public Handler(string message) {
            Message = message;
        }
    }

    public static class Handlers {
        [Handler("PING")]
        public static void PingHandler(Client client, Message message) {
            client.SendRaw("PONG :{0}", message.Parameters[0]);
        }
    }
}