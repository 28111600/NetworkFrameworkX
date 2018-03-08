using System.Collections.Generic;

namespace NetworkFrameworkX.Server.Console
{
    internal class Users
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    internal class Config : ServerConfig
    {
        public List<Users> Users = new List<Users>() { new Users() { Username = "username", Password = "password" } };
    }
}