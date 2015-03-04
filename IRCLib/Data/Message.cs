using System;
using System.Collections.Generic;
using System.Linq;

namespace IRCLib.Data {
    public class Message {
        public string RawData { get; private set; }

        public Source Source { get; private set; }
        public string Command { get; private set; }
        public Tag[] Tags { get; private set; }
        public string[] Parameters { get; private set; }

        public Message(string message) {
            RawData = message;

            Parameters = new string[0];
            Tags = new Tag[0];

            if(message.StartsWith("@")) {
                string rawTags = message.Substring(1, message.IndexOf(' '));
                message = message.Substring(message.IndexOf(' ') + 1);

                Tags = rawTags.Split(';').Select(Tag.FromString).ToArray();
            }

            if(message.StartsWith(":")) {
                Source = new Source(message.Substring(1, message.IndexOf(' ') - 1));
                message = message.Substring(message.IndexOf(' ') + 1);
            }

            if(message.Contains(" ")) {
                Command = message.Remove(message.IndexOf(' '));
                message = message.Substring(message.IndexOf(' ') + 1);

                List<string> parameters = new List<string>();
                while(!String.IsNullOrEmpty(message)) {
                    if(message.StartsWith(":")) {
                        parameters.Add(message.Substring(1));
                        break;
                    }

                    if(!message.Contains(" ")) {
                        parameters.Add(message);
                        break;
                    }

                    parameters.Add(message.Remove(message.IndexOf(' ')));
                    message = message.Substring(message.IndexOf(' ') + 1);
                }
                Parameters = parameters.ToArray();
            } else Command = message;
        }

        public Tag GetTag(string name) {
            if(Tags == null || Tags.Length == 0) return null;

            try {
                return Tags.First(t => t.Name == name);
            } catch(Exception) {
                return null;
            }
        }

        public override string ToString() {
            return RawData;
        }
    }
}