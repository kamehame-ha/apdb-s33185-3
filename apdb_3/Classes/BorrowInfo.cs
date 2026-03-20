using System;
using System.Collections.Generic;
using System.Text;

namespace apdb_3.Classes
{
    public class BorrowInfo
    {
        public DateTime BorrowStart { get; set; }
        public DateTime BorrowEnd { get; set; }
        public bool WasReturnedInTime { get; set; }
        public int? OvertimeFee { get; set; }
        public string CurrentHolderUsername { get; set; }
    }

    
}
