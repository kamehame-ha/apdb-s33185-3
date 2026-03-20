using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes.GearTypes
{
    public class Laptop : Gear
    {
        public string Processor { get; set; }
        public Laptop()
        {
            this.LendBaseDuration = 31;
        }
    }
}
