using System;

namespace IRCLib {
    public class RawMessageEventArgs : EventArgs {
        public string Message { get; set; }

        public RawMessageEventArgs(string message) {
            Message = message;
        }
    }
}