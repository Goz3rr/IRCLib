namespace IRCLib {
    public class User {
        public string NickName { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
        public string Password { get; set; }

        public User(string name, string password = "*") {
            NickName = name;
            UserName = name;
            RealName = name;
            Password = password;
        }
    }
}
