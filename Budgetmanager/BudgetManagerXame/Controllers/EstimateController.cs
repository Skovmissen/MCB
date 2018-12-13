using BudgetManagerXame.Classes;
using BudgetManagerXame.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Script.Services;

namespace BudgetManagerXame.Controllers
{
    public class EstimateController : Controller // Af Nikolaj & Lasse
    {

        // GET: Estimate/Create
        public ActionResult Create(Estimate estimate, int budgetId, int periodId, int fiscalId, string firmName, int year)
        {

            estimate.Fap = DB.GetAllFinanceAccountsEstimates(budgetId, periodId);
            estimate.Period = DB.GetAllPeriods();
            estimate.FinanceGroup = DB.GetAllFinanceGroups();
            estimate.FinanceAccount = DB.GetAllFinanceAccounts(budgetId);
            //estimate.Fap = DB.GetAllFinanceAccountsEstimates(budgetId, periodId);
            ViewBag.PeriodId = estimate.Period[periodId - 1].Name;
            ViewBag.BudgetId = budgetId;
            ViewBag.FiscalId = DB.GetFiscalId(budgetId);
            ViewBag.Year = year;
            ViewBag.FirmName = firmName;
            return View(estimate);
        }

        // POST: Estimate/Create
        [HttpPost, ActionName("Create")]
        public ActionResult UpdateBudget(Estimate estimate)
        {
            int budgetId = 0;
            int periodId = 0;
            foreach (var item in estimate.Fap)
            {
                DB.UpdateFinanceAccountsPeriod(item.AccountId, item.PeriodId, item.BudgetId, item.Estimate);
                budgetId = item.BudgetId;
                periodId = item.PeriodId;

            }
            string fiscalId = DB.GetFiscalId(budgetId);
            return RedirectToAction("Create", "Estimate", new { budgetId = budgetId, periodId = periodId });


        }

        public void UpdateDBNoRefresh()
        {
            int accountId = Convert.ToInt32(Request["AccountId"]);
            int budgetId = Convert.ToInt32(Request["BudgetId"]);
            int periodId = Convert.ToInt32(Request["PeriodId"]);
            double estimate = Convert.ToDouble(Request["Estimate"]);


            DB.UpdateFinanceAccountsPeriod(accountId, periodId, budgetId, estimate);
        }

        public async Task<string> GetJsonString(string fiscalId, DateTime dateFrom, DateTime dateTo, int budgetYear)
        {
            string fiscalPeriodId = "";
            var periodContent = "";
            string content = "";
            TimeSpan tempFrom;
            TimeSpan tempTo;
            string token = Request.Cookies["access_token"].Value;

            HttpClient _client = new HttpClient();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                periodContent = await _client.GetStringAsync("https://my.xena.biz/Api/Fiscal/" + fiscalId + "/FiscalPeriod/");
            }
            catch
            {

                RedirectToAction("Error", "Budget", null);
            }


            JObject jsonContent = JObject.Parse(periodContent);
            int items = jsonContent["Entities"].Count();
            for (int i = 0; i < items; i++)
            {
                int year = int.Parse(jsonContent["Entities"][i]["FiscalPeriodStartDaysFriendly"].ToString().Substring(6, 4));
                if (year == budgetYear)
                {
                    fiscalPeriodId = jsonContent["Entities"][i]["Id"].ToString();
                }

            }


            string epochDate = "01-01-1970";
            DateTime Epoch = DateTime.Parse(epochDate);

            tempFrom = dateFrom.Subtract(Epoch);
            tempTo = dateTo.Subtract(Epoch);

            double daysFrom = tempFrom.TotalDays;
            double daysTo = tempTo.TotalDays;
            try
            {
                content = await _client.GetStringAsync("https://my.xena.biz/Api/Fiscal/" + fiscalId + "/Transaction/LedgerAccountSpecificationReport?ForceNoPaging=false&Page=0&PageSize=100&ShowDeactivated=false&fiscalPeriodId=" + fiscalPeriodId + "&FiscalDateFrom=" + daysFrom + "&FiscalDateTo=" + daysTo + "&articleGroupId=&ledgerTagId=&vatId=&bearerId=&departmentId=&purposeId=&limitToArticleGroup=false&limitToLedgerTag=false&limitToVat=false&limitToBearer=false&limitToDepartment=false&limitToPurpose=false&_=1512993209273");
            }
            catch
            {

                content = "";
            }

            return content;

        }
        [HttpGet]
        public async Task<ActionResult> Show(Estimate estimate, int budgetId, int year, string fiscalId, string firmName)
        {


            estimate.TotalDic = new Dictionary<int, int>();
            estimate.TotalSumGroup = new Dictionary<string, int>();
            estimate.Period = DB.GetAllPeriods();
            estimate.Fap = DB.GetAllFinanceAccountsEstimates(budgetId);
            estimate.FinanceGroup = DB.GetAllFinanceGroups();
            estimate.FinanceAccount = DB.GetAllFinanceAccounts(budgetId);
            ViewBag.Year = DB.GetBudgetYear(budgetId);
            ViewBag.BudgetId = budgetId;
            ViewBag.FiscalId = DB.GetFiscalId(budgetId);
            ViewBag.FirmName = firmName;
            foreach (var item in estimate.FinanceAccount)
            {
                estimate.TotalDic.Add(item.AccountId, DB.GetSumOfEstimates(budgetId, item.AccountId));
            }
            foreach (var item in estimate.FinanceGroup)
            {
                estimate.TotalSumGroup.Add(item.Name, 0);

            }
            foreach (var item in estimate.FinanceAccount)
            {
                if (estimate.TotalSumGroup.ContainsKey(item.FinanceGroup))
                {
                    estimate.TotalSumGroup[item.FinanceGroup] = estimate.TotalSumGroup[item.FinanceGroup] + DB.GetSumOfEstimatesOnGroups(budgetId, item.AccountId);
                }
                else
                {
                    estimate.TotalSumGroup.Add(item.FinanceGroup, DB.GetSumOfEstimatesOnGroups(budgetId, item.AccountId));
                }

            }
            Dictionary<string, string> dateOfYear = addDatesOfYear();

            int periodCounter = 1;

            List<CompareData> CDList = new List<CompareData>();
            foreach (var date in dateOfYear)
            {


                string tempYear = year.ToString();
                DateTime dateFrom = Convert.ToDateTime(date.Key + "-" + tempYear, CultureInfo.GetCultureInfo("en-GB").DateTimeFormat);
                DateTime dateTo = Convert.ToDateTime(date.Value + "-" + tempYear, CultureInfo.GetCultureInfo("en-GB").DateTimeFormat);

                var content = await GetJsonString(fiscalId, dateFrom.Date, dateTo.Date, year);

                if (content == "")
                {
                    return RedirectToAction("Error", "Budget", new { e = "Xena har ingen data for dette budget år" });
                }

                JObject jsonContent = JObject.Parse(content);

                int items = jsonContent["Entities"].Count();



                for (int i = 0; i < items; i++)
                {
                    foreach (var account in estimate.FinanceAccount)
                    {


                        if (account.AccountId.ToString() == jsonContent["Entities"][i]["Number"].ToString())
                        {
                            CompareData CD = new CompareData();
                            double tempEnd = double.Parse(jsonContent["Entities"][i]["EndBalance"].ToString());
                            double tempStart = double.Parse(jsonContent["Entities"][i]["StartingBalance"].ToString());
                            CD.XenaAmount = ((int)tempEnd - (int)tempStart) / 1000;
                            CD.XenaAccountId = int.Parse(jsonContent["Entities"][i]["Number"].ToString());
                            CD.XenaPeriodId = periodCounter;
                            CD.GroupName = jsonContent["Entities"][i]["LedgerAccountTranslated"].ToString();

                            CDList.Add(CD);

                        }

                    }

                }



                periodCounter++;

            }
            ViewBag.CDList = CDList;
            return View(estimate);
        }
        public Dictionary<string, string> addDatesOfYear()
        {
            Dictionary<string, string> Dates = new Dictionary<string, string>();


            Dates.Add("01-01", "31-01");
            Dates.Add("01-02", "28-02");
            Dates.Add("01-03", "31-03");
            Dates.Add("01-04", "30-04");
            Dates.Add("01-05", "31-05");
            Dates.Add("01-06", "30-06");
            Dates.Add("01-07", "31-07");
            Dates.Add("01-08", "31-08");
            Dates.Add("01-09", "30-09");
            Dates.Add("01-10", "31-10");
            Dates.Add("01-11", "30-11");
            Dates.Add("01-12", "31-12");



            return Dates;
        }
    }
}
