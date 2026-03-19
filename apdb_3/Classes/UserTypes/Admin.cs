using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes.UserTypes
{
    public class Admin : User
    {
        public Admin()
        {
            this.PermissionLevel = 2;
        }
    }
}
