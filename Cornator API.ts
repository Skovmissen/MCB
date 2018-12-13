import Express, { json } from 'express';
import request from 'request';
import * as BodyParser from 'body-parser'
import * as HTTP from 'http-status-codes';
import { getServerInfo, insertApiKey, Log, DeleteAPIKey } from './sqlHandler.js'
import bodyParser = require('body-parser');
import * as sha from 'js-sha512';
import { log } from 'util';



var port = process.env.PORT || 3000;
let app = Express();
app.use(BodyParser.json());
app.use(BodyParser.urlencoded({ extended: true }))

app.use((req, resp, next) => {
    resp.header("Access-Control-Allow-Origin", "*");
    resp.header("Access-Control-Allow-Methods", "POST, GET, DELETE, OPTIONS");
    resp.header("Access-Control-Allow-Origin", "Origin, X-Requested-with,Content-type, Accept");

    next();
});

let put = 'Put';
let post = 'Post';
let get = 'Get';

 app.get('/test', async (req, res) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let testUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Test";
    let testResponse: any  = await MyRequest(serverIP, companyName, auth, testUrl, "", get);

    let responseObj = 
    {
        Api: "Status: " + HTTP.OK + " - Fully functional connection detected.",
        NavApi: testResponse
    }

     res.send(responseObj);
 });

 app.post('/Order', async (req, res) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');
    let response:any = "";
    let ReqBody = req.body;
    let OrderLineResponse: any = "";
    let auth = Buffer.from(username + ':' + password).toString('base64');

    let orderUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Order";
    let orderLineUrl = "https://" + serverIP + "/TRIMIT/ODataV4/OrderLines";
    
    for(let i=0; i<ReqBody.OrderLines.length; i++){
        let orderLineBody: any = {
           Web_Order_No: ReqBody.Web_Order_No,
           Line_No: ReqBody.OrderLines[i].Line_No,
           Item_No: ReqBody.OrderLines[i].Item_No,
           Unit_Price: ReqBody.OrderLines[i].Unit_Price,
           Quantity: ReqBody.OrderLines[i].Quantity,
           Discount_Amount: ReqBody.OrderLines[i].Discount_Amount,
           Line_Amount: ReqBody.OrderLines[i].Line_Amount,
   
       }

          OrderLineResponse = await MyRequest(serverIP, companyName, auth, orderLineUrl, JSON.stringify(orderLineBody), post);
    }
    delete ReqBody.OrderLines
    let OrderResponse: any  = await MyRequest(serverIP, companyName, auth, orderUrl, JSON.stringify(ReqBody), post);
    
   response = {
    header: OrderResponse,
    lines : OrderLineResponse
   }

     res.send(response);
 });

app.get("/Products", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let collectionMasterUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Collections?$expand=CollectionsMasters&$format=json";
    let masterItemUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Products?$expand=ProductsItems&$format=json";

    let collectionsAndMasters: any = await MyRequest(serverIP, companyName, auth, collectionMasterUrl, "", get);
    let mastersAndItems: any = await MyRequest(serverIP, companyName, auth, masterItemUrl, "", get);


    //console.log(JSON.parse(mastersAndItems).value[0].ProductsItems[1]);
    //console.log(collectionsAndMasters.value[0]);
    //console.log(collectionsAndMasters.value[0].CollectionsMasters[1]);

    //await Log(apikey, JSON.stringify(logDescription))

    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    let Products = {
        Collecions: collectionsAndMasters,
        Masters: mastersAndItems
    }
    await resp.send(Products);
});

app.get("/Inventory", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let inventoryUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Inventory";

    let Inventory: any = await MyRequest(serverIP, companyName, auth, inventoryUrl, "", get);
    
    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    await resp.send(Inventory);
});
app.get("/Inventory/:itemNo", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let itemNo = req.params.itemNo

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let inventoryUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Inventory?$filter=ItemNo eq " + "'" + itemNo + "'" + "&$format=json";

    let Inventory: any = await MyRequest(serverIP, companyName, auth, inventoryUrl, "", get);
    
    await resp.send(Inventory);
});

app.get("/Invoices", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let InvoiceUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Invoices?$expand=InvoicesInvoiceLines";

    let Invoice: any = await MyRequest(serverIP, companyName, auth, InvoiceUrl,"", get);
    
    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    await resp.send(Invoice);
});
app.get("/Invoices/:ID", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');
    let ID = req.params.ID

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let InvoiceUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Invoices?$filter=No eq " + "'" + ID + "'" + "&$expand=InvoicesInvoiceLines";

    let Invoice: any = await MyRequest(serverIP, companyName, auth, InvoiceUrl, "", get);
    
    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    await resp.send(Invoice);
});

app.get("/CapturedInvoices/:IsCaptured", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');
    let Captured = req.params.IsCaptured

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let InvoiceUrl = "https://" + serverIP + "/TRIMIT/ODataV4/Invoices?$filter=IsCaptured eq "  + Captured + "&$expand=InvoicesInvoiceLines";

    let Invoice: any = await MyRequest(serverIP, companyName, auth, InvoiceUrl, "", get);
    
    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    await resp.send(Invoice);
});
app.put("/CaptureInvoice/:ID/", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');
    let ID = req.params.ID
    let ReqBody = req.body;
    let auth = Buffer.from(username + ':' + password).toString('base64');
    console.log(ReqBody)
    let InvoiceUrl = "https://" + serverIP + "/TRIMIT/ODataV4/InvoiceCapture(" + "'" + ID + "'" +")";

    let Invoice: any = await MyRequest(serverIP, companyName, auth, InvoiceUrl, JSON.stringify(ReqBody), put);
    
    //console.log(JSON.parse(masters).value[0].ProductswebshopItems[1].No);
    //console.log(masters.ProductswebshopItems.No);
    // let test = JSON.parse(mastersAndItems);
    // console.log(JSON.parse(test.value[0].ProductsItems[1].var1Name).DAN);

    await resp.send(Invoice);
});
app.get("/PriceList", async (req, resp) => {
    let serverIP = req.get('serverIP');
    let companyName = req.get('company');
    let username = req.get('username');
    let password = req.get('password');

    let auth = Buffer.from(username + ':' + password).toString('base64');

    let logDescription = {
        text: "Get Products request",
        companyName: companyName,
        serverIP: serverIP,
    }

    let priceURL = "https://" + serverIP + "/TRIMIT/ODataV4/PriceList";


    let price: any = await MyRequest(serverIP, companyName, auth, priceURL, "", get);


    await resp.send(price);
});

async function MyRequest(serverIP, companyName, auth, url, body, method) {
    let options = {
        method: method,
        url: url,
        body: body,

        headers:
        {
            "If-Match": "*",
            company: companyName,
            Authorization: "Basic " + auth,
            "Content-Type": "application/json",
        },
    };

    return new Promise((resolve, reject) => {
        request(options, async function (error: any, response: any, body: any) {
            if (error)
            {
                
                resolve("error");
            }
            else
            {
                
                
                resolve(JSON.parse(body));
            }

        });
    });

}

app.listen(port, function () {
    console.log("server listing on port" + port);
});


// async function ServerInfo(apikey: string) {
//     return new Promise(async (resolve, reject) => {
//         const info = await getServerInfo(apikey); // true            
//         resolve(info);
//     });
// }


// app.delete('/ApiKey', async (req, res) => {
//     let apikey = req.body.apikey;
//     const info = await DeleteAPIKey(apikey)

//     if (info == "OK") {
//         res.sendStatus(HTTP.OK)
//     }
//     else {
//         res.sendStatus(HTTP.BAD_REQUEST)
//     }
// });
// app.post('/ApiKey', async (req, res) => {
//     let apikey = req.body.apikey;
//     let companyName = req.body.companyName;
//     let serverIP = req.body.serverIP;
//     let username = req.body.username;
//     let password = req.body.password;
//     let firmName = req.body.ServiceName;
//     let cardName = req.body.CardName;
//     let indexstart = serverIP.indexOf("//");
//     let temp = serverIP.substr(indexstart + 2);
//     let indexEnd = temp.indexOf("/");
//     serverIP = temp.substring(0, indexEnd);

//     let logDescription = {
//         text: "CreateAPIKEY request",
//         companyName: companyName,
//         serverIP: serverIP,
//         username: username,
//         apiKey: apikey,
//         firmName: firmName
//     }

//     await Log(apikey, JSON.stringify(logDescription))
//     const info = await insertApiKey(apikey, companyName, serverIP, username, password, firmName, cardName);
//     console.log("INFO LOG HER:" + info);

//     if (info == "OK") {
//         res.sendStatus(HTTP.OK)
//     }
//     else {
//         res.sendStatus(HTTP.BAD_REQUEST)
//     }
// });