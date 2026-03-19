using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        private int PermissionLevel { get; set; }

        public User GetUser(string username)
        {
            User user = Database.GetRecord<User>("users", "Username", username);
            return user;
        }

        public void CreateUser() 
        {
            Database.AddRecord("users", this);
        }
    }
}
