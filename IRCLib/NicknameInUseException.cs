using System;

namespace IRCLib {
    public class NicknameInUseException : Exception {
        public int Tries { get; private set; }
        public string Name { get; private set; }

        public NicknameInUseException(int tries, string name) {
            Tries = tries;
            Name = name;
        }
    }
}
