namespace IRCLib {
    /// <summary>
    ///     Class that holds information to authenticate with an IRC server
    /// </summary>
    public class User {
        public string NickName { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
        public string Password { get; set; }

        /// <summary>
        ///     Create a new User
        /// </summary>
        /// <param name="name">Value to assign to all names</param>
        /// <param name="password">Password, defaults to * for no password</param>
        public User(string name, string password = "*") {
            NickName = name;
            UserName = name;
            RealName = name;
            Password = password;
        }
    }
}
