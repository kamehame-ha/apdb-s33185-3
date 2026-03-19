using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int PermissionLevel { get; set; }

        public User GetUser(string username)
        {
            return new User();
        }
    }
}
