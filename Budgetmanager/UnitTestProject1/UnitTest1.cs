using BudgetManagerXame.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        
       
        [TestMethod]
        public async Task<List<XenaData>> GetFinansKonti()
        {
             string uri = "https://my.xena.biz/Api/Fiscal/98512/Transaction/LedgerGroupDataDetail?ForceNoPaging=true&Page=0&PageSize=10&ShowDeactivated=false&fiscalPeriodId=172923719&FiscalDateFrom=17167&FiscalDateTo=17531&ledgerAccount=Xena_Domain_Income_Accounts_Net_Turn_Over&_=1511941562807";
            using (HttpClient httpClient = new HttpClient())
            {

                return JsonConvert.DeserializeObject<List<XenaData>>(await httpClient.GetStringAsync(uri));
            }
        }
    }
}
