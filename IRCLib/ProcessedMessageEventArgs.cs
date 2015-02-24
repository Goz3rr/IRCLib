using System;

using IRCLib.Data;

namespace IRCLib {
    public class ProcessedMessageEventArgs : EventArgs {

        public Message Message { get; set; }

        public ProcessedMessageEventArgs(Message message) {
            Message = message;
        }
    }
}