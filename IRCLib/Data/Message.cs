using System;
using System.Collections.Generic;
using System.Linq;

namespace IRCLib.Data {
    /// <summary>
    ///     Processed IRC message
    /// </summary>
    public class Message {
        /// <summary>
        ///     Raw content of message
        /// </summary>
        public string RawData { get; private set; }

        /// <summary>
        ///     Source of message
        /// </summary>
        public Source Source { get; private set; }

        /// <summary>
        ///     Command that is matched against handlers
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        ///     Tags in message
        /// </summary>
        public Tag[] Tags { get; private set; }

        /// <summary>
        ///     Parameters for command
        /// </summary>
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

        /// <summary>
        ///     Get tag by name
        /// </summary>
        /// <param name="name">Tag to look for</param>
        /// <returns>Tag or null if not found</returns>
        public Tag GetTag(string name) {
            if(Tags == null || Tags.Length == 0) return null;

            return Tags.FirstOrDefault(t => t.Name == name);
        }

        public override string ToString() {
            return RawData;
        }
    }
}