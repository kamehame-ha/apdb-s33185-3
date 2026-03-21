using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes
{
    public class Borrow
    {
        public DateTime BorrowStart { get; set; }
        public DateTime BorrowEnd { get; set; }
        public string ClientUsername { get; set; }
        public string GearId { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public Borrow GetBorrow(string id)
        {
            Borrow borrow = Database.GetRecord<Borrow>("borrows", "Id", id);
            return borrow;
        }

        public void CreateBorrow()
        {
            Database.AddRecord("borrows", this);
        }

        public void DeleteBorrow()
        {
            Database.DeleteRecord("borrows", this);
        }


    }
}
