using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BudgetManagerXame.Models
{
    public class CompareData //af Anders
    {
        public int XenaAmount { get; set; }
        public int XenaPeriodId { get; set; }
        public int XenaAccountId { get; set; }
        public int XenaLedgerTag { get; set; }
        public string GroupName { get; set; }
        public int XenaTotal { get; set; }
    }
}