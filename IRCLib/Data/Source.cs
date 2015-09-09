namespace IRCLib.Data {
    /// <summary>
    ///     Source of an IRC message
    /// </summary>
    public class Source {
        public string Name { get; private set; }
        public string User { get; private set; }
        public string Host { get; private set; }

        public Source(string raw) {
            if(raw.Contains("@")) {
                string[] split = raw.Split('@');
                if(split[0].Contains("!")) {
                    string[] names = split[0].Split('!');
                    Name = names[0];
                    User = names[0];
                } else {
                    Name = split[0];
                    User = split[0];
                }

                Host = split[1];
            } else {
                Host = raw;
            }
        }

        public override string ToString() {
            string result = "";

            if(Name != null) {
                result = Name;
                if(User != null) {
                    result += "!" + User;
                }
            }

            if(Host != null) {
                if(result == "") {
                    result = Host;
                } else {
                    result += "@" + Host;
                }
            }

            return result;
        }
    }
}