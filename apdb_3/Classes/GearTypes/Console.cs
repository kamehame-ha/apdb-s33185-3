using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes.GearTypes
{
    public class Console : Gear
    {
        public string Brand { get; set; }
        public Console()
        {
            this.LendBaseDuration = 62;
        }
    }
}
