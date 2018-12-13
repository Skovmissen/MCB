using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace BudgetManagerXame.Models
{
    public class Estimate //af Anders
    {
        public IList<Period> Period { get; set; }
        public IList<Budget> Budget { get; set; }
        public IList<FinanceAccount> FinanceAccount { get; set; }
        public IList<FinanceGroup> FinanceGroup { get; set; }
        public IList<FinanceAccountPeriod> Fap { get; set; }
        public IList<XenaData> xena { get; set; }
        public int[] Data { get; set; }
        public Dictionary<int,int> TotalDic { get; set; }
        public Dictionary<string, int> TotalSumGroup { get; set; }
        public DateTime? dateFrom { get; set; }
        public DateTime? dateTo { get; set; }
      
    }
}