using DhanSutra;
using DhanSutra.Models;
using DhanSutra.Pdf;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using DhanSutra.Validation;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DhanSutra
{
    public partial class MainForm : Form
    {
        private readonly string _connectionString1 = "Data Source=billing.db;Version=3;BusyTimeout=5000;";
        private DatabaseService db = new DatabaseService();
        public MainForm()
        {
            // ✅ Step 1: Define a safe, user-visible folder
            string dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DhanSutra\\DhanSutra\\DhanSutraData"
            );

            // ✅ Step 2: Create folder if missing
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
                Console.WriteLine("📁 Created folder: " + dataFolder);
            }

            // ✅ Step 3: Full database path
            string dbFile = Path.Combine(dataFolder, "billing.db");

            // ✅ Step 4: Final connection string
            _connectionString1 = $"Data Source={dbFile};Version=3;";
            Console.WriteLine("📂 Database path: " + dbFile);

            InitializeComponent();
            InitializeWebViewAsync();
           
        }
       
        private async void InitializeWebViewAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.OpenDevToolsWindow();


            // Handle messages from React (JS)
            webView.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;

            // Map local folder to virtual domain (so it can load index.html)
            //it has to used after build created for react
            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            //    "app", "webapp", CoreWebView2HostResourceAccessKind.Allow);

            // Load your React app (or test HTML)
            webView.Source = new Uri("http://localhost:3000");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
     "invoices.local",
     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Invoices"),
     Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
 );
        }

        private void WebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            Console.WriteLine("✅ WebMessageReceived fired!");
            Console.WriteLine("📨 Raw message from React: " + e.WebMessageAsJson);
            try
            {
                var message = e.WebMessageAsJson;
                var req = JsonConvert.DeserializeObject<WebRequestMessage>(message);


                
                switch (req.Action)
                {
                    case "AddItem":
                        var item = JsonConvert.DeserializeObject<Item>(req.Payload.ToString());
                        try
                        {

                            var errors = db.ValidateItem(item);
                            if (errors.Count > 0)
                            {
                                
                                var response = new
                                {
                                    Type = "AddItem",
                                    Status = "Validation Failed Error",
                                    Message = "Validation Failed",
                                    Data = item
                                };
                            }
                            else
                            {
                                db.AddItem(item); // ✅ SQLite insertion via DatabaseService
                                                  // ✅ Structured success message
                                var response = new
                                {
                                    Type = "AddItem",
                                    Status = "Success",
                                    Message = "Item added successfully",
                                    Data = item
                                };

                                webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                            }


                           

                            
                        }
                        catch (Exception ex)
                        {
                            // ❌ Structured error message
                            var error = new
                            {
                                Type = "AddItem",
                                Status = "Error",
                                Message = ex.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                        }
                        break;

                    case "AddItemDetails":
                        try
                        {

                               
                            var details = JsonConvert.DeserializeObject<ItemDetails>(req.Payload.ToString());

                            var errors = db.ValidateInventoryDetails(details);

                            

                            ItemLedger itemledger=new ItemLedger();
                            itemledger.ItemId = details.Item_Id;
                            itemledger.BatchNo = details.BatchNo;   
                            itemledger.Date = details.Date.ToString("yyyy-MM-dd HH:mm:ss");
                            itemledger.TxnType = "Purchase";
                            itemledger.RefNo = details.refno;
                            itemledger.Qty = details.Quantity;
                            itemledger.Rate = details.PurchasePrice;
                            itemledger.DiscountPercent = details.DiscountPercent;
                            itemledger.NetRate = details.NetPurchasePrice;
                            itemledger.TotalAmount  = details.Amount;
                            itemledger.Remarks = details.Description;
                            itemledger.CreatedBy = details.CreatedBy;
                            itemledger.CreatedAt = details.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                            string status = "Error";
                            //string message = "Failed to save inventory details.";

                            using (var conn = new SQLiteConnection(_connectionString1))
                            {
                                conn.Open();
                                Console.WriteLine("🧠 Using DB file: " + conn.FileName);
                                using (var txn = conn.BeginTransaction())
                                {
                                    try
                                    {
                                        bool success1 = db.AddItemDetails(details, conn, txn);
                                        bool success2 = db.AddItemLedger(itemledger, conn, txn);
                                        bool success3 = db.UpdateItemBalance(itemledger, conn, txn);

                                        if (success1 && success2 && success3)
                                        {
                                            txn.Commit();
                                            status = "Success";
                                            message = "Inventory details saved successfully.";
                                        }
                                        else
                                        {
                                            txn.Rollback();
                                            status = "Error";
                                            message = "One or more operations failed. Transaction rolled back.";
                                        }
                                    }
                                    catch (Exception innerEx)
                                    {
                                        txn.Rollback();
                                        status = "Error";
                                        message = "Database error: " + innerEx.Message;
                                    }
                                }
                            }

                            // ✅ Send response to React
                            var response = new
                            {
                                Type = "AddItemDetails",
                                Status = status,
                                Message = message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                        }
                        catch (Exception ex)
                        {
                            var error = new
                            {
                                Type = "AddItemDetails",
                                Status = "Error",
                                Message = ex.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                        }
                        break;

                    case "GetItems":
                        try
                        {
                            var items = db.GetItems();

                            var response = new
                            {
                                Type = "GetItems",
                                Status = "Success",
                                Message = "Fetched all items successfully",
                                Data = items
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                        }
                        catch (Exception ex)
                        {
                            var error = new
                            {
                                Type = "GetItems",
                                Status = "Error",
                                Message = ex.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                        }
                        break;
                    case "GetItemsForInvoice":
                        try
                        {
                            var items = db.GetItemsForInvoice();

                            var response = new
                            {
                                Type = "GetItemsForInvoice",
                                Status = "Success",
                                Message = "Fetched all items successfully",
                                Data = items
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                        }
                        catch (Exception ex)
                        {
                            var error = new
                            {
                                Type = "GetItemsForInvoice",
                                Status = "Error",
                                Message = ex.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                        }
                        break;
                    case "GetItemNameById":
                        {
                            var payload = (JObject)req.Payload;
                            int id = payload["id"].Value<int>();
                            string name = db.GetItemNameById(id);

                            // Send back the result to React
                            var response = new
                            {
                                Action = "GetItemNameByIdResponse",
                                Result = name
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            
                        }
                        break;

                    case "GetCategoryById":
                        {
                            var payload = (JObject)req.Payload;
                            int id = payload["id"].Value<int>();
                            string name = db.GetCategoryById(id);

                            // Send back the result to React
                            var response = new
                            {
                                Action = "GetCategoryByIdResponse",
                                Result = name
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                        }
                        break;

                    case "GetUnitNameById":
                        {
                            var payload = (JObject)req.Payload;
                            int id = payload["id"].Value<int>();
                            string name = db.GetUnitNameById(id);

                            // Send back the result to React
                            var response = new
                            {
                                Action = "GetUnitNameByIdResponse",
                                Result = name
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                        }
                        break;

                    case "GetGSTById":
                        {
                            var payload = (JObject)req.Payload;
                            int id = payload["id"].Value<int>();
                            string name = db.GetGSTById(id);

                            // Send back the result to React
                            var response = new
                            {
                                Action = "GetGSTByIdResponse",
                                Result = name
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                        }
                        break;


                    case "GetCategoryList":
                        {
                            var categories = db.GetAllCategories(); // method to fetch all rows
                            var response = new
                            {
                                Type = "GetCategoryList",
                                Status = "Success",
                                Data = categories
                            };
                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "GetAllUnitsList":
                        {
                            var units = db.GetAllUnits(); // method to fetch all rows
                            var response = new
                            {
                                Type = "GetAllUnits",
                                Status = "Success",
                                Data = units
                            };
                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "GetAllGstList":
                        {
                            var gst = db.GetAllGst(); // method to fetch all rows
                            var response = new
                            {
                                Type = "GetAllGst",
                                Status = "Success",
                                Data = gst
                            };
                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "GetItemDetails":
                        try
                        {
                            // 🔍 Read the JSON payload as an object
                            string payloadJson = req.Payload.ToString();
                            Console.WriteLine($"📩 Raw Payload JSON: {payloadJson}");

                            int itemId = 0;

                            // Try to parse JSON object
                            try
                            {
                                var payloadDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
                                if (payloadDict != null && payloadDict.ContainsKey("Item_Id"))
                                {
                                    itemId = Convert.ToInt32(payloadDict["Item_Id"]);
                                }
                            }
                            catch
                            {
                                // Fallback: sometimes React might send just a number
                                int.TryParse(payloadJson, out itemId);
                            }

                            Console.WriteLine($"📩 Extracted item_id = {itemId}");

                            // ✅ Now safely query the database
                            var details = db.GetItemDetails(itemId);
                            var response = new
                            {
                                Type = "GetItemDetails",
                                Status = "Success",
                                Message = "Fetched all items successfully",
                                Data = details
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                        }
                        catch (Exception ex)
                        {
                            var error = new
                            {
                                Type = "GetItemDetails",
                                Status = "Error",
                                Message = ex.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                        }
                        break;

                    case "deleteItem":
                        {
                            var payload = (JObject)req.Payload;
                            int itemid = payload["Item_Id"].Value<int>();
                            bool deleted = db.DeleteItemIfNoInventory(itemid);

                            var result = new
                            {
                                Type = "deleteItemResponse",
                                Status = "Success",
                                Message = deleted
                                    ? "Item deleted successfully."
                                    : "Cannot delete — related inventory exists, clear inventory first ."
                                                                                                   
                            };

                            // Send response back to React
                            webView.CoreWebView2.PostWebMessageAsJson(Newtonsoft.Json.JsonConvert.SerializeObject(result));
                            break;
                        }
                    case "searchItems":
                        {
                            var payload = JObject.Parse(req.Payload.ToString());
                            string queryText = payload["query"]?.ToString() ?? "";

                            var items = db.SearchItems(queryText);

                            var result = new
                            {
                                action = "searchItemsResponse",
                                items = items
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                Newtonsoft.Json.JsonConvert.SerializeObject(result)
                            );
                            break;
                        }
                    case "updateItem":
                        {
                            var payload = JObject.Parse(req.Payload.ToString());

                            int itemId = Convert.ToInt32(payload["id"]);
                            string name = payload["name"]?.ToString();
                            string itemCode = payload["itemcode"]?.ToString();
                            int? categoryId = payload["categoryid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["categoryid"]);
                            string date = payload["date"]?.ToString();
                            string description = payload["description"]?.ToString();
                            int? unitId = payload["unitid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["unitid"]);
                            int? gstId = payload["gstid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["gstid"]);

                            Item itemforvalidation = new Item();
                            itemforvalidation.Name = name;
                            itemforvalidation.ItemCode = itemCode;
                            itemforvalidation.CategoryId = categoryId ?? 0;
                            if (DateTime.TryParse(date, out DateTime parsedDate))
                            {
                                itemforvalidation.Date = parsedDate;
                            }
                            itemforvalidation.Description = description;
                            itemforvalidation.UnitId = unitId ?? 0;
                            itemforvalidation.GstId = gstId ?? 0;

                            var errors = db.ValidateItem(itemforvalidation);
                            if (errors.Count > 0)
                            {

                                var response = new
                                {
                                    action = "updateItem",
                                    success = false,
                                    message = errors.ToString()
                                };
                            }
                            else
                            {

                                bool updated = db.UpdateItem(itemId, name, itemCode, categoryId, date, description, unitId, gstId);

                                var result = new
                                {
                                    action = "updateItem",
                                    success = updated,
                                    message = updated ? "✅ Item updated successfully." : "⚠️ Item update failed."
                                };
                                webView.CoreWebView2.PostWebMessageAsJson(Newtonsoft.Json.JsonConvert.SerializeObject(result));
                            }

                           
                            break;
                        }
                    case "GetItemList":
                        {
                            var items = db.GetItemList(); // your SQLite query
                            var result = new
                            {
                                Type = "GetItemList",
                                Status = "Success",
                                Data = items
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(result));
                            break;
                        }
                    case "searchInventory":
                        {
                            var payload = JObject.Parse(req.Payload.ToString());
                            string queryText = payload["query"]?.ToString() ?? "";

                            // Call your database service method
                            var inventoryList = db.SearchInventory(queryText);

                            var result = new
                            {
                                action = "searchInventoryResponse",
                                items = inventoryList,
                                Status = "Success"
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                Newtonsoft.Json.JsonConvert.SerializeObject(result)
                            );
                            break;
                        }

                    
                    case "updateInventory":
                        {
                            var payload = JObject.Parse(req.Payload.ToString());

                            var itemId = payload["item_id"]?.ToString();
                            var stritemId = payload["item_id"]?.ToString();
                            var batchNo = payload["batchNo"]?.ToString();
                            var refno = payload["refno"]?.ToString();
                            var hsnCode = payload["hsnCode"]?.ToString();

                            string NormalizeDate(string input)
                            {
                                if (string.IsNullOrWhiteSpace(input)) return null;
                                if (DateTime.TryParse(input, out DateTime dt))
                                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                                return null;
                            }

                            var date = NormalizeDate(payload["date"]?.ToString());

                            var quantity = payload["quantity"]?.ToString();
                            var purchasePrice = payload["purchasePrice"]?.ToString();
                            var discountPercent = payload["discountPercent"]?.ToString();
                            var netPurchasePrice = payload["netpurchasePrice"]?.ToString();
                            var amount = payload["amount"]?.ToString();
                            var salesPrice = payload["salesPrice"]?.ToString();
                            var mrp = payload["mrp"]?.ToString();
                            var goodsOrServices = payload["goodsOrServices"]?.ToString();
                            var description = payload["description"]?.ToString();
                            var mfgDate = NormalizeDate(payload["mfgdate"]?.ToString());
                            var expDate = NormalizeDate(payload["expdate"]?.ToString());
                            var modelno = payload["modelno"]?.ToString();
                            var brand = payload["brand"]?.ToString();
                            var size = payload["size"]?.ToString();
                            var color = payload["color"]?.ToString();
                            var weight = payload["weight"]?.ToString();
                            var dimension = payload["dimension"]?.ToString();
                            var invbatchno = payload["invbatchno"]?.ToString();

                            bool success = false;

                            using (var conn = new SQLiteConnection(_connectionString1))
                            {
                                conn.Open();

                                using (var transaction = conn.BeginTransaction())
                                {
                                    try
                                    {
                                        success = db.UpdateInventoryRecord(
                                            conn, transaction,
                                            itemId, batchNo, refno, hsnCode, date, quantity, purchasePrice,
                                            discountPercent, netPurchasePrice, amount, salesPrice, mrp,
                                            goodsOrServices, description, mfgDate, expDate, modelno,
                                            brand, size, color, weight, dimension, invbatchno);

                                        bool success_ledger = db.UpdateItemLedger(
                                            conn, transaction,
                                            itemId, batchNo, refno, date, quantity, purchasePrice,
                                            discountPercent, netPurchasePrice, amount, description, invbatchno);

                                        bool success_itembalance_batchno =
                                            db.UpdateItemBalanceForBatchNo(conn, transaction, itemId, batchNo, invbatchno);

                                        bool success_itembalance_forquantity =
                                            db.UpdateItemBalance_ForChangeInQuantity(conn, transaction,
                                                stritemId, batchNo, invbatchno, quantity);

                                        if (success && success_ledger && success_itembalance_batchno && success_itembalance_forquantity)
                                        {
                                            transaction.Commit();
                                            success = true;
                                        }
                                        else
                                        {
                                            transaction.Rollback();
                                            success = false;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.Rollback();
                                        Console.WriteLine("Error updating inventory & ledger: " + ex.Message);
                                        success = false;
                                    }
                                }
                            }

                            var result = new
                            {
                                action = "updateInventoryResponse",
                                success = success,
                                message = success ? "Inventory updated successfully." : "Update failed."
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                Newtonsoft.Json.JsonConvert.SerializeObject(result)
                            );

                            break;
                        }

                    case "getLastInventoryItem":
                        {
                            var lastItem = db.GetLastItemWithInventory();

                            var result = new
                            {
                                action = "getLastInventoryItemResponse",
                                success = lastItem != null,
                                data = lastItem
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                Newtonsoft.Json.JsonConvert.SerializeObject(result)
                            );

                            break;
                        }
                    case "GetCompanyProfile":
                        {
                            var profile = db.GetCompanyProfile();

                            // Convert logo blob → base64
                            string logoBase64 = null;
                            if (profile?.Logo != null)
                            {
                                try { logoBase64 = Convert.ToBase64String(profile.Logo); }
                                catch { logoBase64 = null; }
                            }

                            var resp = new
                            {
                                action = "GetCompanyProfileResponse",
                                success = profile != null,
                                profile = profile != null ? new
                                {
                                    Id = profile.Id,
                                    CompanyName = profile.CompanyName,
                                    AddressLine1 = profile.AddressLine1,
                                    AddressLine2 = profile.AddressLine2,
                                    City = profile.City,
                                    State = profile.State,
                                    Pincode = profile.Pincode,
                                    Country = profile.Country,

                                    GSTIN = profile.GSTIN,
                                    PAN = profile.PAN,

                                    Email = profile.Email,
                                    Phone = profile.Phone,

                                    BankName = profile.BankName,
                                    BankAccount = profile.BankAccount,
                                    IFSC = profile.IFSC,
                                    BranchName = profile.BranchName,

                                    InvoicePrefix = profile.InvoicePrefix,
                                    InvoiceStartNo = profile.InvoiceStartNo,
                                    CurrentInvoiceNo = profile.CurrentInvoiceNo,

                                    LogoBase64 = logoBase64,

                                    CreatedBy = profile.CreatedBy,
                                    CreatedAt = profile.CreatedAt
                                } : null
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(resp)
                            );

                            break;
                        }

                    case "SaveCompanyProfile":
                        {
                            // Convert payload to JObject
                            var p = JObject.FromObject(req.Payload);

                            // Extract profile JSON
                            //var p = payload["profile"];

                            // Build model from payload
                            var model = new CompanyProfile
                            {
                                Id = p["Id"]?.ToObject<int>() ?? 0,
                                CompanyName = p["CompanyName"]?.ToString(),
                                AddressLine1 = p["AddressLine1"]?.ToString(),
                                AddressLine2 = p["AddressLine2"]?.ToString(),
                                City = p["City"]?.ToString(),
                                State = p["State"]?.ToString(),
                                Pincode = p["Pincode"]?.ToString(),
                                Country = p["Country"]?.ToString(),
                                GSTIN = p["GSTIN"]?.ToString(),
                                PAN = p["PAN"]?.ToString(),
                                Email = p["Email"]?.ToString(),
                                Phone = p["Phone"]?.ToString(),
                                BankName = p["BankName"]?.ToString(),
                                BankAccount = p["BankAccount"]?.ToString(),
                                IFSC = p["IFSC"]?.ToString(),
                                BranchName = p["BranchName"]?.ToString(),
                                InvoicePrefix = p["InvoicePrefix"]?.ToString(),
                                InvoiceStartNo = p["InvoiceStartNo"]?.ToObject<int>() ?? 1,
                                CurrentInvoiceNo = p["CurrentInvoiceNo"]?.ToObject<int>() ?? 1,
                                CreatedBy = p["CreatedBy"]?.ToString()
                            };

                            // ⭐ CORRECT PLACE FOR LOGO BASE64 HANDLING ⭐
                            var logoBase64 = p["LogoBase64"]?.ToString();
                            if (!string.IsNullOrEmpty(logoBase64))
                            {
                                try { model.Logo = Convert.FromBase64String(logoBase64); }
                                catch { model.Logo = null; }
                            }
                            else
                            {
                                model.Logo = null;
                            }

                            // Save into database
                            bool ok = db.SaveCompanyProfile(model);

                            // Send response back
                            var saved = db.GetCompanyProfile();
                            string logoOut = saved?.Logo != null ? Convert.ToBase64String(saved.Logo) : null;

                            var response = new
                            {
                                action = "SaveCompanyProfileResponse",
                                success = ok,
                                message = ok ? "Company profile saved." : "Save failed.",
                                profile = new
                                {
                                    Id = saved.Id,
                                    CompanyName = saved.CompanyName,
                                    AddressLine1 = saved.AddressLine1,
                                    AddressLine2 = saved.AddressLine2,
                                    City = saved.City,
                                    State = saved.State,
                                    Pincode = saved.Pincode,
                                    Country = saved.Country,
                                    GSTIN = saved.GSTIN,
                                    PAN = saved.PAN,
                                    Email = saved.Email,
                                    Phone = saved.Phone,
                                    BankName = saved.BankName,
                                    BankAccount = saved.BankAccount,
                                    IFSC = saved.IFSC,
                                    BranchName = saved.BranchName,
                                    InvoicePrefix = saved.InvoicePrefix,
                                    InvoiceStartNo = saved.InvoiceStartNo,
                                    CurrentInvoiceNo = saved.CurrentInvoiceNo,
                                    LogoBase64 = logoOut,
                                    CreatedBy = saved.CreatedBy,
                                    CreatedAt = saved.CreatedAt
                                }
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "CreateInvoice":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                {
                                    var errResp = new
                                    {
                                        action = "CreateInvoiceResponse",
                                        success = false,
                                        message = "Invalid payload"
                                    };
                                    webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(errResp));
                                    break;
                                }


                                                               
                                // Convert to CreateInvoiceDto
                                var dto = payload.ToObject<CreateInvoiceDto>();


                                
                                var Invoicedto = payload.ToObject<InvoiceDto>();


                                //InvoiceDto invoice = MapFromJson(msg.Payload);

                                var errors = InvoiceValidator.Validate(Invoicedto);
                                if (errors.Any())
                                {
                                    // send failure back to UI - use the same shape as client expects
                                    var respvalidation = new
                                    {
                                        action = "CreateInvoiceResponse",
                                        success = false,
                                        errors = errors.Select(err => new { field = err.Field, message = err.Message }).ToList()
                                    };
                                    // stringify and post message back to webview
                                    webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(respvalidation));
                                    return;
                                }


                                //var validationErrors = db.ValidateInvoice(Invoicedto);
                                //if (validationErrors.Count > 0)
                                //{
                                //    SendMessage(new
                                //    {
                                //        action = "SaveInvoiceResponse",
                                //        success = false,
                                //        errors = validationErrors
                                //    });
                                //    break;
                                //}

                                if (dto == null)
                                {
                                    var errResp = new
                                    {
                                        action = "CreateInvoiceResponse",
                                        success = false,
                                        message = "Unable to parse invoice payload"
                                    };
                                    webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(errResp));
                                    break;
                                }

                                // Validate company profile ID (optional)
                                if (dto.CompanyId == 0)
                                {
                                    var errResp = new
                                    {
                                        action = "CreateInvoiceResponse",
                                        success = false,
                                        message = "Missing CompanyProfileId"
                                    };
                                    webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(errResp));
                                    break;
                                }

                                // ⭐ SINGLE CALL — does customer insert/update + invoice + items + next invoice no
                                // ⭐ Now returns both invoiceId and invoiceNo

                                //if (validationErrors.Count > 0)
                                //{

                                //    var errResp = new
                                //    {
                                //        action = "CreateInvoiceResponse",
                                //        success = false,
                                //        message = "Validation errors",
                                //        errors = validationErrors
                                //    };
                                //    webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(errResp));
                                //    break;
                                //}

                                    var result = db.CreateInvoice(dto);
                                long invoiceId = result.invoiceId;
                                string invoiceNo = result.invoiceNo;

                                var resp = new
                                {
                                    action = "CreateInvoiceResponse",
                                    success = invoiceId > 0,
                                    invoiceId = invoiceId,
                                    invoiceNo = invoiceNo,
                                    message = invoiceId > 0 ? "Invoice saved successfully" : "Invoice save failed"
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }
                            catch (Exception ex)
                            {
                                var resp = new
                                {
                                    action = "CreateInvoiceResponse",
                                    success = false,
                                    message = "Save failed: " + ex.Message
                                };
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }

                            break;
                        }



                    case "GetInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            int id = payload["Id"]?.ToObject<int>() ?? 0;

                            var invoice = db.GetInvoice(id);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetInvoiceResponse",
                                    success = invoice != null,
                                    invoice
                                })
                            );
                            break;
                        }
                    case "GetItemBalance":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null) break;

                                int itemid = payload["ItemId"]?.ToObject<int>() ?? 0;
                                int lineindex = payload["LineIndex"]?.ToObject<int>() ?? 0;

                                int bal = db.GetItemBalance(itemid);

                                var resp = new
                                {
                                    action = "GetItemBalanceResponse",
                                    itemId = itemid,
                                    balance = bal,
                                    lineIndex = lineindex
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }
                            catch (Exception ex)
                            {
                                var resp = new
                                {
                                    action = "GetItemBalanceResponse",
                                    success = false,
                                    message = "Error Fetching Balance: " + ex.Message
                                };
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }

                            break;
                        }
                    case "GetItemBalanceBatchWise":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null) break;

                                int itemid = payload["ItemId"]?.ToObject<int>() ?? 0;
                                string batchno = payload["BatchNo"]?.ToObject<string>();
                                int lineindex = payload["LineIndex"]?.ToObject<int>() ?? 0;

                                int bal = db.GetItemBalanceBatchWise(itemid,batchno);

                                var resp = new
                                {
                                    action = "GetItemBalanceBatchWiseResponse",
                                    itemId = itemid,
                                    batchno = batchno,
                                    balance = bal,
                                    lineIndex = lineindex
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }
                            catch (Exception ex)
                            {
                                var resp = new
                                {
                                    action = "GetItemBalanceResponse",
                                    success = false,
                                    message = "Error Fetching Balance: " + ex.Message
                                };
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(resp));
                            }

                            break;
                        }
                    case "PrintInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long invoiceId = payload["InvoiceId"]?.ToObject<long>() ?? 0;
                            

                            // 1) Load invoice from DB
                            var invoice = db.GetInvoice(invoiceId);

                            // 2) Load company profile from DB
                            var company = db.GetCompanyProfile();

                            // 3) Convert Models.InvoiceLoadDto to Pdf.InvoiceLoadDto
                            var pdfInvoice = new DhanSutra.Pdf.InvoiceLoadDto
                            {
                                Id = invoice.Id,
                                InvoiceNo = invoice.InvoiceNo,
                                InvoiceNum = invoice.InvoiceNum,
                                InvoiceDate = invoice.InvoiceDate,
                                CompanyProfileId = invoice.CompanyProfileId,
                                CustomerId = invoice.CustomerId,
                                CustomerName = invoice.CustomerName,
                                CustomerPhone = invoice.CustomerPhone,
                                CustomerState = invoice.CustomerState,
                                CustomerAddress = invoice.CustomerAddress,
                                SubTotal = invoice.SubTotal,
                                TotalTax = invoice.TotalTax,
                                TotalAmount = invoice.TotalAmount,
                                RoundOff = invoice.RoundOff,
                                Items = invoice.Items?.ConvertAll(x => new DhanSutra.Pdf.InvoiceItemDto
                                {
                                    ItemId = x.ItemId,
                                    BatchNo = x.BatchNo,
                                    HsnCode = x.HsnCode,
                                    Qty = x.Qty,
                                    Rate = x.Rate,
                                    DiscountPercent = x.DiscountPercent,
                                    GstPercent = x.GstPercent,
                                    GstValue = x.GstValue,
                                    CgstPercent = x.CgstPercent,
                                    CgstValue = x.CgstValue,
                                    SgstPercent = x.SgstPercent,
                                    SgstValue = x.SgstValue,
                                    IgstPercent = x.IgstPercent,
                                    IgstValue = x.IgstValue,
                                    LineSubTotal = x.LineSubTotal,
                                    LineTotal = x.LineTotal
                                }), // If item types differ, map accordingly
                            };

                            // 4) Convert Models.CompanyProfile to Pdf.CompanyProfileDto
                            var pdfCompany = new DhanSutra.Pdf.CompanyProfileDto
                            {
                                CompanyName = company.CompanyName,
                                AddressLine1 = company.AddressLine1,
                                AddressLine2 = company.AddressLine2,
                                City = company.City,
                                State = company.State,
                                Pincode = company.Pincode,
                                //Country = company.Country,
                                GSTIN = company.GSTIN,
                                PAN = company.PAN,
                                Email = company.Email,
                                Phone = company.Phone,
                                BankName = company.BankName,
                                BankAccount = company.BankAccount,
                                IFSC = company.IFSC,
                                BranchName = company.BranchName,
                                InvoicePrefix = company.InvoicePrefix,
                                InvoiceStartNo = company.InvoiceStartNo,
                                CurrentInvoiceNo = company.CurrentInvoiceNo,
                                Logo = company.Logo,
                                CreatedBy = company.CreatedBy,
                                CreatedAt = company.CreatedAt
                            };

                            // 5) Create the PDF
                            var doc = new InvoiceDocument(pdfInvoice, pdfCompany);

                            // 6) Generate PDF file path
                            string pdfPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                "Invoices",
                                "invoice-" + invoice.InvoiceNo + ".pdf"
                            );

                            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath));

                            // 7) Save PDF
                            //var bytes = doc.GeneratePdfBytes();

                            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                            var bytes = doc.GeneratePdf();

                            File.WriteAllBytes(pdfPath, bytes);

                            // 8) Send back PDF path to frontend
                            var response = new
                            {
                                action = "PrintInvoiceResponse",
                                success = true,
                                pdfPath = pdfPath
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "GetCustomers":
                        {
                            var customers = db.GetCustomers();
                            var response = new
                            {
                                action = "GetCustomersResponse",
                                customers = customers
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "getInvoiceNumbersByDate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            string date = payload["date"]?.ToObject<string>() ;

                            
                            var result = db.GetInvoiceNumbersByDate(date);

                            var response = new
                            {
                                action = "invoiceNumbersByDateResult",
                                data = result
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                            
                        }

                    



                }
            }
            catch (Exception ex)
            {
                 webView.CoreWebView2.PostWebMessageAsString("Error: " + ex.Message);
            }
        }
        private void webView_Click(object sender, EventArgs e)
        {

        }
        public static byte[] GenerateInvoicePdfBytes(InvoiceFullDto invoice)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                // Fonts
                var fontH = new XFont("Arial", 14, XFontStyle.Bold);
                var font = new XFont("Arial", 10, XFontStyle.Regular);

                // Logo
                if (invoice.CompanyLogo != null)
                {
                    using (var lms = new MemoryStream(invoice.CompanyLogo))
                    {
                        var img = XImage.FromStream(() => lms);
                        gfx.DrawImage(img, 20, 20, 100, 60);
                    }
                }

                // Company name at top
                gfx.DrawString(invoice.CompanyName, fontH, XBrushes.Black, new XPoint(140, 40));
                gfx.DrawString(invoice.CompanyAddressLine1 ?? "", font, XBrushes.Black, new XPoint(140, 60));
                gfx.DrawString($"GSTIN: {invoice.CompanyGstin ?? ""}", font, XBrushes.Black, new XPoint(140, 75));

                // Invoice header
                gfx.DrawString($"Invoice: {invoice.InvoiceNo}", fontH, XBrushes.Black, new XPoint(400, 40));
                gfx.DrawString($"Date: {invoice.InvoiceDate:yyyy-MM-dd}", font, XBrushes.Black, new XPoint(400, 60));

                // Items header + table (you will need to write row drawing logic)
                double y = 110;
                gfx.DrawString("S.No", font, XBrushes.Black, new XPoint(30, y));
                gfx.DrawString("Item", font, XBrushes.Black, new XPoint(80, y));
                gfx.DrawString("Qty", font, XBrushes.Black, new XPoint(320, y));
                gfx.DrawString("Rate", font, XBrushes.Black, new XPoint(360, y));
                gfx.DrawString("Amount", font, XBrushes.Black, new XPoint(430, y));
                y += 18;

                int i = 1;
                foreach (var line in invoice.Items)
                {
                    gfx.DrawString(i.ToString(), font, XBrushes.Black, new XPoint(30, y));
                    gfx.DrawString(line.HsnCode ?? line.ItemId.ToString(), font, XBrushes.Black, new XPoint(80, y));
                    gfx.DrawString(line.Qty.ToString("0.##"), font, XBrushes.Black, new XPoint(320, y));
                    gfx.DrawString(line.Rate.ToString("0.00"), font, XBrushes.Black, new XPoint(360, y));
                    gfx.DrawString(line.LineTotal.ToString("0.00"), font, XBrushes.Black, new XPoint(430, y));
                    y += 16;
                    i++;
                    if (y > page.Height - 120) { page = doc.AddPage(); gfx = XGraphics.FromPdfPage(page); y = 40; }
                }

                // Totals
                gfx.DrawString($"Subtotal: {invoice.SubTotal:0.00}", fontH, XBrushes.Black, new XPoint(350, y + 20));
                gfx.DrawString($"Total Tax: {invoice.TotalTax:0.00}", fontH, XBrushes.Black, new XPoint(350, y + 40));
                gfx.DrawString($"Total Amount: {invoice.TotalAmount:0.00}", fontH, XBrushes.Black, new XPoint(350, y + 60));

                doc.Save(ms);
                return ms.ToArray();
            }

        }
    }
    public class WebRequestMessage
    {
        public string Action { get; set; }
        public object Payload { get; set; }
    }
}
