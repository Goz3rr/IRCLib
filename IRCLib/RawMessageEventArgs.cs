using System;

namespace IRCLib {
    public class RawMessageEventArgs : EventArgs {
        /// <summary>
        ///     Raw message contents
        /// </summary>
        public string Message { get; set; }

        public RawMessageEventArgs(string message) {
            Message = message;
        }
    }
}