using System;

using IRCLib.Data;

namespace IRCLib {
    public class Handler : Attribute {
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
