namespace IRCLib.Data {
    public class Tag {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public Tag(string name) {
            Name = name;
            Value = null;
        }

        public Tag(string name, string value) {
            Name = name;
            Value = value;
        }

        public static Tag FromString(string raw) {
            if(!raw.Contains("=")) return new Tag(raw);

            string[] split = raw.Split('=');
            return new Tag(split[0], split[1]);
        }

        public override string ToString() {
            return Value == null ? Name : Name + "=" + Value;
        }
    }
}