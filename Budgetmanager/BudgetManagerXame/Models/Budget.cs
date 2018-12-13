using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Web;

namespace BudgetManagerXame.Models
{
    public class Budget //af Anders
    {
        public int Id { get; set; }
        [Required]
        public int Year { get; set; }
        public string Description { get; set; }
        public string Fiscalid { get; set; }
        public string FirmName { get; set; }
        public List<DataRow> BudgetList { get; set; }
        public Dictionary<string,int> firmList { get; set; }
        public string firmId { get; set; }
    }
}