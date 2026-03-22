using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes
{
    public class Gear
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool Broken { get; set; }

        public Gear GetGear(string id)
        {
            Gear gear = Database.GetRecord<Gear>("gear", "Id", id);
            return gear;
        }

        public void CreateGear()
        {
            Database.AddRecord("gear", this);
        }

        public void DeleteGear()
        {
            Database.DeleteRecord("gear", this);
        }
    }
}
