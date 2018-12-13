using BudgetManagerXame.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using BudgetManagerXame.Classes;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BudgetManagerXame.Controllers
{
    public class BudgetController : Controller
    {

        public async Task<ActionResult> Index(Budget budget) // Af Nikolaj
        {
            var firmInfo = await GetFirmInfoFromXena();
            if (firmInfo == "")
            {
               return RedirectToAction("LoginError", "Budget", new { e = "Kan ikke hente dine virksomheds informationer, er du logget ind?" });
            }
            JObject jsonFirmInfo = JObject.Parse(firmInfo);
            budget.firmList = AddFirmsToList(jsonFirmInfo);
            return View(budget);

        }

        private static Dictionary<string, int> AddFirmsToList(JObject jsonContent)
        {
            Dictionary<string, int> FirmList = new Dictionary<string, int>();
            string FiscalId = "";
            string FirmName = "";
            int items = int.Parse(jsonContent["Count"].ToString());
            for (int i = 0; i < items; i++)
            {
                FiscalId = jsonContent["Entities"][i]["FiscalSetupId"].ToString();
                FirmName = jsonContent["Entities"][i]["FiscalSetupName"].ToString();

                FirmList.Add(FirmName, Convert.ToInt32(FiscalId));

            }

            return FirmList;
        }

        public async Task<string> GetFirmInfoFromXena()
        {
            string token = "";
            string content = "";
            
            if (HttpContext.Request.Cookies["access_token"] == null)
            {
                return content;
            }
            else
            {
                 token = Request.Cookies["access_token"].Value;
            }
            HttpClient _client = new HttpClient();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                content = await _client.GetStringAsync("https://my.xena.biz/Api/User/XenaUserMembership?ForceNoPaging=true&ShowDeactivated=false");
            }
            catch
            {

                content = "";
            }


            return content;

        }
        public ActionResult BudgetList(Budget budget, string id, string firmName) // Af Nikolaj
        {
            budget.Fiscalid = id;
            budget.FirmName = firmName;

            DataTable dt = DB.GetAllBudgets(id);
            budget.BudgetList = dt.AsEnumerable().ToList();


            return View(budget);
        }

        // GET: Budget/Create
        public ActionResult Create(Budget budget) // Af Patrick
        {
            return View(budget);
        }
        public async Task<string> GetFinanceAccountsFromXena(Budget budget)
        {
            string token = Request.Cookies["access_token"].Value;

            HttpClient _client = new HttpClient();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string content = await _client.GetStringAsync("https://my.xena.biz/Api/Fiscal/" + budget.Fiscalid + "/LedgerSearch/FullList?ForceNoPaging=true&Page=0&PageSize=10&ShowDeactivated=false&_=1512459901335");

            return content;

        }

        // POST: Budget/Create
        [HttpPost]
        public async Task<ActionResult> Create(FormCollection collection, Budget budget, int fiscalId) // Af Patrick
        {
            Estimate estimate = new Estimate();

            try
            {
                estimate.Period = DB.GetAllPeriods();
                budget.Year = int.Parse(Request.Form["Year"]);
                budget.Description = Request.Form["Description"];

                budget.Id = DB.CreateBudget(budget);

                await AddAccountsToBudget(budget, estimate);

                return RedirectToAction("BudgetList", "Budget", new { @id = fiscalId });
            }
            catch
            {
                return RedirectToAction("Error", new { e = "" });
            }
        }

        public async Task<ActionResult> Sync(Budget budget, int id, string FiscalId, int siteId, int year, string firmName) // Af Lasse
        {
            Estimate estimate = new Estimate();
            estimate.Period = DB.GetAllPeriods();
            budget.Id = id;
            budget.Fiscalid = FiscalId;
            await AddAccountsToBudget(budget, estimate);
            if (siteId == 1)
            {
                return RedirectToAction("Create", "Estimate", new { @budgetid = id, @periodId = 1, @fiscalId = FiscalId, @firmName = firmName, @year = year});
            }
            else
            {
                return RedirectToAction("Show", "Estimate", new { @budgetid = id, @year = year, @fiscalId = FiscalId, @firmName = firmName });
               
            }

        }

        private async Task AddAccountsToBudget(Budget budget, Estimate estimate) // Af Lasse
        {

            var content = await GetFinanceAccountsFromXena(budget);

            JObject jsonContent = JObject.Parse(content);
            int items = jsonContent["Entities"].Count();

            AddFinanceAccountToBudget(budget, jsonContent, items);
            AddStartValueToFinanceAccountPeriod(budget, estimate);
        }

        private static void AddFinanceAccountToBudget(Budget budget, JObject jsonContent, int items)
        {
            var FinanceAccountId = "";
            var LedgerAccoount = "";
            var FinanceAccountDesc = "";
            var Moms = "";
            var ArticleId = "";

            for (int i = 0; i < items; i++)
            {

                FinanceAccountId = jsonContent["Entities"][i]["AccountNumber"].ToString();
                FinanceAccountDesc = jsonContent["Entities"][i]["Description"].ToString();
                LedgerAccoount = jsonContent["Entities"][i]["LedgerAccount"].ToString();
                Moms = jsonContent["Entities"][i]["LongDescription"].ToString();
                ArticleId = jsonContent["Entities"][i]["ArticleGroupId"].ToString();
                if (ArticleId == "")
                {
                    ArticleId = jsonContent["Entities"][i]["LedgerTagId"].ToString();
                }
                LedgerAccoount = DB.GetFinanceGroupName(LedgerAccoount);
                if (FinanceAccountId == "" || FinanceAccountDesc == "" || LedgerAccoount == "" || Moms.Contains("Momsfri"))
                {
                    //ingen oprettelse
                }
                else
                {
                    DB.CreateFinanceAccounts(int.Parse(FinanceAccountId.ToString()), FinanceAccountDesc.ToString(), LedgerAccoount.ToString(), ArticleId, budget);
                }
            }
        }

        private static void AddStartValueToFinanceAccountPeriod(Budget budget, Estimate estimate) // Af Lasse
        {
            estimate.FinanceAccount = DB.GetAllFinanceAccounts(budget.Id);
            foreach (var period in estimate.Period)
            {
                foreach (var financeAccount in estimate.FinanceAccount)
                {
                    DB.CreateFinanceAccountsPeriod(financeAccount.AccountId, period.Id, budget);
                }
            }
        }

        public ActionResult Error(string e) // Af Nikolaj
        {
            ViewBag.error = e;
            return View();
        }
        public ActionResult LoginError(string e) // Af Nikolaj
        {
            ViewBag.error = e;
            return View();
        }


        // GET: Budget/Delete/5
        public ActionResult Delete(int id) // Af Lasse
        {
            Budget budget = DB.GetBudget(id);
            return View(budget);
        }

        // POST: Budget/Delete/5
        [HttpPost]
        public ActionResult DeleteConfirm(int id)
        {
            string fiscalId = DB.GetFiscalId(id);
            DB.DeleteBudget(id);


            return RedirectToAction("BudgetList", "Budget", new { id = fiscalId });
        }
    }
}
