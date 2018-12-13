using BudgetManagerXame.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BudgetManagerXame.Controllers 
{
    // af: Nikolaj
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            
            return View();
        }
        [HttpPost]
        public ActionResult index(Budget budget, XenaData data)
        {
            data.id_code = Request["code"];

            GetAccessToken(data);

            HttpCookie AccessCookie= new HttpCookie("access_token");
            AccessCookie.Value = data.access_token;
            Response.Cookies.Add(AccessCookie);


            return RedirectToAction("Index", "Budget");
        }
        public void RunLogin(XenaData data)
        {
            GetTokenHelper(data);
        }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public void GetTokenHelper(XenaData data)
        {
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            queryString["response_type"] = "code id_token";
            queryString["client_id"] = "630a9884-bf16-4820-9f52-f0c2744fab77.apps.xena.biz";
            queryString["redirect_uri"] = "https://budgetmanagerxenaeksamen.azurewebsites.net/";
            queryString["scope"] = "openid testapi";
            queryString["nonce"] = RandomString(32);
            queryString["response_mode"] = "form_post";
            queryString["json"] = "true";

            Response.Redirect("https://login.xena.biz/connect/authorize?" + queryString.ToString());

        }
        public static string GetAccessToken(XenaData data)
        {
            return GetAccessTokenHelper(data).Result;
        }
        public static async Task<string> GetAccessTokenHelper(XenaData data)
        {
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("code", data.id_code),
                new KeyValuePair<string, string>("client_id", "630a9884-bf16-4820-9f52-f0c2744fab77.apps.xena.biz"),
                new KeyValuePair<string, string>("redirect_uri", "https://budgetmanagerxenaeksamen.azurewebsites.net/"),
                new KeyValuePair<string, string>("client_secret", "P8mGkLRKcp8jtoWPlcP7XH9u"),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("response_mode", "form_post"),
                new KeyValuePair<string, string>("json", "true"),


            };
            var content = new FormUrlEncodedContent(pairs);
            var client = new HttpClient();

            var response = client.PostAsync("https://login.xena.biz/connect/token?", content).Result;

            string result = await response.Content.ReadAsStringAsync();
            
            JObject json = JObject.Parse(result);
       
            data.access_token = json["access_token"].ToString();

            return json["access_token"].ToString();
        }
       
    }
}