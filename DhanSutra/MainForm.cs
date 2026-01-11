using DhanSutra;
using DhanSutra.Models;
using DhanSutra.Pdf;
using DhanSutra.Validation;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DhanSutra
{
    public partial class MainForm : Form
    {
        private readonly string _connectionString1 = "Data Source=billing.db;Version=3;BusyTimeout=5000;";
        private DatabaseService db = new DatabaseService();
        private UserDto _currentUser;

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

            db.EnsureDefaultAdmin();

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

                    //case "AddItemDetails":
                    //    try
                    //    {

                               
                    //        var details = JsonConvert.DeserializeObject<ItemDetails>(req.Payload.ToString());

                    //        var errors = db.ValidateInventoryDetails(details);

                            

                    //        ItemLedger itemledger=new ItemLedger();
                    //        itemledger.ItemId = details.Item_Id;
                    //        itemledger.BatchNo = details.BatchNo;   
                    //        itemledger.Date = details.Date.ToString("yyyy-MM-dd HH:mm:ss");
                    //        itemledger.TxnType = "Purchase";
                    //        itemledger.RefNo = details.refno;
                    //        itemledger.Qty = details.Quantity;
                    //        itemledger.Rate = details.PurchasePrice;
                    //        itemledger.DiscountPercent = details.DiscountPercent;
                    //        itemledger.NetRate = details.NetPurchasePrice;
                    //        itemledger.TotalAmount  = details.Amount;
                    //        itemledger.Remarks = details.Description;
                    //        itemledger.CreatedBy = details.CreatedBy;
                    //        itemledger.CreatedAt = details.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    //        string status = "Error";
                    //        //string message = "Failed to save inventory details.";

                    //        using (var conn = new SQLiteConnection(_connectionString1))
                    //        {
                    //            conn.Open();
                    //            Console.WriteLine("🧠 Using DB file: " + conn.FileName);
                    //            using (var txn = conn.BeginTransaction())
                    //            {
                    //                try
                    //                {
                    //                    bool success1 = db.AddItemDetails(details, conn, txn);
                    //                    bool success2 = db.AddItemLedger(itemledger, conn, txn);
                    //                    bool success3 = db.UpdateItemBalance(itemledger, conn, txn);

                    //                    if (success1 && success2 && success3)
                    //                    {
                    //                        txn.Commit();
                    //                        status = "Success";
                    //                        message = "Inventory details saved successfully.";
                    //                    }
                    //                    else
                    //                    {
                    //                        txn.Rollback();
                    //                        status = "Error";
                    //                        message = "One or more operations failed. Transaction rolled back.";
                    //                    }
                    //                }
                    //                catch (Exception innerEx)
                    //                {
                    //                    txn.Rollback();
                    //                    status = "Error";
                    //                    message = "Database error: " + innerEx.Message;
                    //                }
                    //            }
                    //        }

                    //        // ✅ Send response to React
                    //        var response = new
                    //        {
                    //            Type = "AddItemDetails",
                    //            Status = status,
                    //            Message = message
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        var error = new
                    //        {
                    //            Type = "AddItemDetails",
                    //            Status = "Error",
                    //            Message = ex.Message
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                    //    }
                    //    break;

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
                    case "GetItemsForPurchaseInvoice":
                        try
                        {
                            var items = db.GetItemsForPurchaseInvoice();

                            var response = new
                            {
                                Type = "GetItemsForPurchaseInvoice",
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
                            try
                            {
                                var payload = (JObject)req.Payload;
                                int id = payload["Id"].Value<int>();

                                var category = db.GetCategoryById(id);

                                var response = new
                                {
                                    Type = "GetCategoryById",
                                    Status = "Success",
                                    Data = category
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                var error = new
                                {
                                    Type = "GetCategoryById",
                                    Status = "Error",
                                    Message = ex.Message
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(error)
                                );
                            }

                            break;
                        }



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

                    //case "GetItemDetails":
                    //    try
                    //    {
                    //        // 🔍 Read the JSON payload as an object
                    //        string payloadJson = req.Payload.ToString();
                    //        Console.WriteLine($"📩 Raw Payload JSON: {payloadJson}");

                    //        int itemId = 0;

                    //        // Try to parse JSON object
                    //        try
                    //        {
                    //            var payloadDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
                    //            if (payloadDict != null && payloadDict.ContainsKey("Item_Id"))
                    //            {
                    //                itemId = Convert.ToInt32(payloadDict["Item_Id"]);
                    //            }
                    //        }
                    //        catch
                    //        {
                    //            // Fallback: sometimes React might send just a number
                    //            int.TryParse(payloadJson, out itemId);
                    //        }

                    //        Console.WriteLine($"📩 Extracted item_id = {itemId}");

                    //        // ✅ Now safely query the database
                    //        var details = db.GetItemDetails(itemId);
                    //        var response = new
                    //        {
                    //            Type = "GetItemDetails",
                    //            Status = "Success",
                    //            Message = "Fetched all items successfully",
                    //            Data = details
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        var error = new
                    //        {
                    //            Type = "GetItemDetails",
                    //            Status = "Error",
                    //            Message = ex.Message
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(error));
                    //    }
                    //    break;

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
                            string hsnCode = payload["hsncode"]?.ToString();
                            int? categoryId = payload["categoryid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["categoryid"]);
                            string date = payload["date"]?.ToString();
                            string description = payload["description"]?.ToString();
                            int? unitId = payload["unitid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["unitid"]);
                            int? gstId = payload["gstid"]?.Type == JTokenType.Null ? (int?)null : Convert.ToInt32(payload["gstid"]);
                            decimal reorderlevel = Convert.ToDecimal(payload["reorderlevel"]);

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

                                bool updated = db.UpdateItem(itemId, name, itemCode,hsnCode, categoryId, date, description, unitId, gstId,reorderlevel);

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
                    //case "searchInventory":
                    //    {
                    //        var payload = JObject.Parse(req.Payload.ToString());
                    //        string queryText = payload["query"]?.ToString() ?? "";

                    //        // Call your database service method
                    //        var inventoryList = db.SearchInventory(queryText);

                    //        var result = new
                    //        {
                    //            action = "searchInventoryResponse",
                    //            items = inventoryList,
                    //            Status = "Success"
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(
                    //            Newtonsoft.Json.JsonConvert.SerializeObject(result)
                    //        );
                    //        break;
                    //    }

                    
                    //case "updateInventory":
                    //    {
                    //        var payload = JObject.Parse(req.Payload.ToString());

                    //        var itemId = payload["item_id"]?.ToString();
                    //        var stritemId = payload["item_id"]?.ToString();
                    //        var batchNo = payload["batchNo"]?.ToString();
                    //        var refno = payload["refno"]?.ToString();
                    //        //var hsnCode = payload["hsnCode"]?.ToString();

                    //        string NormalizeDate(string input)
                    //        {
                    //            if (string.IsNullOrWhiteSpace(input)) return null;
                    //            if (DateTime.TryParse(input, out DateTime dt))
                    //                return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    //            return null;
                    //        }

                    //        var date = NormalizeDate(payload["date"]?.ToString());

                    //        var quantity = payload["quantity"]?.ToString();
                    //        var purchasePrice = payload["purchasePrice"]?.ToString();
                    //        var discountPercent = payload["discountPercent"]?.ToString();
                    //        var netPurchasePrice = payload["netpurchasePrice"]?.ToString();
                    //        var amount = payload["amount"]?.ToString();
                    //        var salesPrice = payload["salesPrice"]?.ToString();
                    //        var mrp = payload["mrp"]?.ToString();
                    //        var goodsOrServices = payload["goodsOrServices"]?.ToString();
                    //        var description = payload["description"]?.ToString();
                    //        var mfgDate = NormalizeDate(payload["mfgdate"]?.ToString());
                    //        var expDate = NormalizeDate(payload["expdate"]?.ToString());
                    //        var modelno = payload["modelno"]?.ToString();
                    //        var brand = payload["brand"]?.ToString();
                    //        var size = payload["size"]?.ToString();
                    //        var color = payload["color"]?.ToString();
                    //        var weight = payload["weight"]?.ToString();
                    //        var dimension = payload["dimension"]?.ToString();
                    //        var invbatchno = payload["invbatchno"]?.ToString();
                    //        var supplierid = ((int)payload["supplierId"]);

                    //        bool success = false;

                    //        using (var conn = new SQLiteConnection(_connectionString1))
                    //        {
                    //            conn.Open();

                    //            using (var transaction = conn.BeginTransaction())
                    //            {
                    //                try
                    //                {
                    //                    success = db.UpdateInventoryRecord(
                    //                        conn, transaction,
                    //                        itemId, batchNo, refno,  date, quantity, purchasePrice,
                    //                        discountPercent, netPurchasePrice, amount, salesPrice, mrp,
                    //                        goodsOrServices, description, mfgDate, expDate, modelno,
                    //                        brand, size, color, weight, dimension, invbatchno,supplierid);

                    //                    bool success_ledger = db.UpdateItemLedger(
                    //                        conn, transaction,
                    //                        itemId, batchNo, refno, date, quantity, purchasePrice,
                    //                        discountPercent, netPurchasePrice, amount, description, invbatchno);

                    //                    bool success_itembalance_batchno =
                    //                        db.UpdateItemBalanceForBatchNo(conn, transaction, itemId, batchNo, invbatchno);

                    //                    bool success_itembalance_forquantity =
                    //                        db.UpdateItemBalance_ForChangeInQuantity(conn, transaction,
                    //                            stritemId, batchNo, invbatchno, quantity);

                    //                    if (success && success_ledger && success_itembalance_batchno && success_itembalance_forquantity)
                    //                    {
                    //                        transaction.Commit();
                    //                        success = true;
                    //                    }
                    //                    else
                    //                    {
                    //                        transaction.Rollback();
                    //                        success = false;
                    //                    }
                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    transaction.Rollback();
                    //                    Console.WriteLine("Error updating inventory & ledger: " + ex.Message);
                    //                    success = false;
                    //                }
                    //            }
                    //        }

                    //        var result = new
                    //        {
                    //            action = "updateInventoryResponse",
                    //            success = success,
                    //            message = success ? "Inventory updated successfully." : "Update failed."
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(
                    //            Newtonsoft.Json.JsonConvert.SerializeObject(result)
                    //        );

                    //        break;
                    //    }

                    //case "getLastInventoryItem":
                    //    {
                    //        var lastItem = db.GetLastItemWithInventory();

                    //        var result = new
                    //        {
                    //            action = "getLastInventoryItemResponse",
                    //            success = lastItem != null,
                    //            data = lastItem
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(
                    //            Newtonsoft.Json.JsonConvert.SerializeObject(result)
                    //        );

                    //        break;
                    //    }
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
                                    CanEditInvoiceStartNo = profile.CurrentInvoiceNo == null || profile.CurrentInvoiceNo <= profile.InvoiceStartNo,
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
                            // -------- Invoice number normalization --------
                            int? startNo = p["InvoiceStartNo"]?.Type == JTokenType.Null
                                ? (int?)null
                                : p["InvoiceStartNo"]?.ToObject<int>();

                            int? currentNo = p["CurrentInvoiceNo"]?.Type == JTokenType.Null
                                ? (int?)null
                                : p["CurrentInvoiceNo"]?.ToObject<int>();

                            // RULE: CurrentInvoiceNo must never be NULL in DB
                            if (currentNo == null)
                            {
                                if (startNo == null)
                                    throw new Exception("Invoice Start No is required.");

                                currentNo = startNo;
                            }
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
                                InvoiceStartNo = startNo,
                                CurrentInvoiceNo = currentNo,

                                CreatedBy = p["CreatedBy"]?.ToString()
                            };
                            if (model.CurrentInvoiceNo == null && model.InvoiceStartNo != null)
                            {
                                // this is OK → preview will show start no
                            }
                            else if (model.CurrentInvoiceNo != null && model.InvoiceStartNo == null)
                            {
                                throw new Exception("Invoice Start No cannot be null when Current Invoice No exists.");
                            }
                            var existing = db.GetCompanyProfile();

                            if (existing != null)
                            {
                                bool invoicesStarted =
                                    existing.CurrentInvoiceNo != null &&
                                    existing.CurrentInvoiceNo > existing.InvoiceStartNo;

                                if (invoicesStarted &&
                                    model.InvoiceStartNo != existing.InvoiceStartNo)
                                {
                                    throw new Exception(
                                        "Invoice Start Number cannot be changed after invoices are created."
                                    );
                                }
                            }

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
                    case "SaveOpeningStock":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            try
                            {
                                db.SaveOpeningStock(payload);

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveOpeningStockResponse",
                                        success = true,
                                        message = "Opening stock saved successfully."
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveOpeningStockResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }
                    case "GetOpeningStock":
                        {
                            var result = db.GetOpeningStock();

                            var response = new
                            {
                                action = "GetOpeningStockResponse",
                                exists = result.Header != null,
                                header = result.Header,
                                items = result.Items
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }
                    case "GetCashBankAccounts":
                        {
                            var data = db.GetCashBankAccounts();

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetCashBankAccountsResponse",
                                    data
                                })
                            );
                            break;
                        }
                    case "GetExpenseAccounts":
                        {
                            var data = db.GetExpenseAccounts();

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetExpenseAccountsResponse",
                                    data
                                })
                            );
                            break;
                        }
                    case "GetAccountsForVoucherSide":
                        {
                            var payload = req.Payload as JObject;

                            string voucherType = payload.Value<string>("VoucherType");
                            string side = payload.Value<string>("Side"); // Debit / Credit

                            var data = db.GetAccountsForVoucherSide(voucherType, side);


                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetAccountsForVoucherSideResponse",
                                    side,
                                    data
                                })
                            );
                            break;
                                                    
                        }

                    //case "GetAccountsForVoucher":
                    //    {
                    //        var payload = req.Payload as JObject;

                    //        string voucherType = payload?.Value<string>("VoucherType");

                    //        if (string.IsNullOrWhiteSpace(voucherType))
                    //            voucherType = "JV"; // safe default

                    //        var data = db.GetAccountsForVoucher(voucherType);

                    //        webView.CoreWebView2.PostWebMessageAsJson(
                    //            JsonConvert.SerializeObject(new
                    //            {
                    //                action = "GetAccountsForVoucherResponse",
                    //                data
                    //            })
                    //        );
                    //        break;
                    //    }
                    case "GetNextVoucherNo":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string voucherType = payload.Value<string>("VoucherType");

                            var voucherNo = db.GetNextVoucherNo(voucherType);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetNextVoucherNoResponse",
                                    voucherNo
                                })
                            );
                            break;
                        }
                    case "SaveVoucher":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            var dto = payload.ToObject<VoucherDto>();

                            // Load accounts used
                            var accountIds = dto.Lines
                                .Select(l => l.AccountId)
                                .Distinct()
                                .ToList();

                            var accountMap = db.LoadAccountsByIds(accountIds)
                                .ToDictionary(a => a.AccountId);

                            // 🔐 VALIDATE (NO EXCEPTION)
                            var validation = db.ValidateVoucherLines(
                                dto.VoucherType,
                                dto.Lines,
                                accountMap
                            );

                            if (!validation.IsValid)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveVoucherResponse",
                                        success = false,
                                        message = validation.Message
                                    })
                                );
                                break;
                            }

                            // ✅ SAFE TO SAVE
                            db.SaveVoucher(dto);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "SaveVoucherResponse",
                                    success = true
                                })
                            );

                            break;
                        }

                    case "LoadVoucherById":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long journalEntryId = payload.Value<long>("JournalEntryId");

                            var data = db.LoadVoucherById(journalEntryId);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "LoadVoucherByIdResponse",
                                    data
                                })
                            );
                            break;
                        }
                    case "GetVoucherIdsByDate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string date = payload.Value<string>("Date");

                            var data = db.GetVoucherIdsByDate(date);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetVoucherIdsByDateResponse",
                                    data
                                })
                            );
                            break;
                        }

                    case "ReverseVoucher":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long journalEntryId = payload.Value<long>("JournalEntryId");

                            try
                            {
                                db.ReverseVoucher(journalEntryId);

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseVoucherResponse",
                                        success = true
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseVoucherResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }


                    case "SaveStockAdjustment":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                var result = db.SaveStockAdjustment(
                                    date: p.Value<string>("Date"),
                                    type: p.Value<string>("AdjustmentType"),
                                    reason: p.Value<string>("Reason"),
                                    notes: p.Value<string>("Notes"),
                                    createdBy: p.Value<string>("CreatedBy"),
                                    items: p["Items"].ToObject<List<StockAdjustmentItemDto>>()
                                );

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveStockAdjustmentResponse",
                                        success = true,
                                        AdjustmentId = result.AdjustmentId,
                                        AdjustmentNo = result.AdjustmentNo,
                                        message = "Stock adjustment saved successfully."
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveStockAdjustmentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }
                    case "GetRecentStockAdjustments":
                        {
                            var list = db.GetRecentStockAdjustments();

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetRecentStockAdjustmentsResponse",
                                    data = list
                                })
                            );
                            break;
                        }

                    case "LoadStockAdjustment":
                        {
                            var p = JObject.FromObject(req.Payload);
                            long id = p.Value<long>("AdjustmentId");

                            try
                            {
                                var detail = db.GetStockAdjustmentDetail(id);

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "LoadStockAdjustmentResponse",
                                        success = true,
                                        data = detail
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "LoadStockAdjustmentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }
                    case "LoadExpenseVoucher":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                long id = p.Value<long>("ExpenseVoucherId");

                                var data = db.LoadExpenseVoucher(id);

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "LoadExpenseVoucherResponse",
                                        success = true,
                                        data
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "LoadExpenseVoucherResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }
                    case "SaveExpensePayment":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                db.SaveExpensePayment(
                                    expenseVoucherId: p.Value<long>("ExpenseVoucherId"),
                                    paymentDate: p.Value<string>("PaymentDate"),
                                    paidViaAccountId: p.Value<long>("PaidViaAccountId"),
                                    amount: p.Value<decimal>("Amount"),
                                    notes: p.Value<string>("Notes"),
                                    createdBy: p.Value<string>("CreatedBy")
                                );

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveExpensePaymentResponse",
                                        success = true
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveExpensePaymentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }
                    case "ReverseExpenseVoucher":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                db.ReverseExpenseVoucher(
                                    expenseVoucherId: p.Value<long>("ExpenseVoucherId"),
                                    reversedBy: p.Value<string>("ReversedBy")
                                );

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseExpenseVoucherResponse",
                                        success = true
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseExpenseVoucherResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }
                            break;
                        }

                    case "GetExpenseVouchers":
                        {
                            try
                            {
                                var list = db.GetExpenseVouchers();

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "GetExpenseVouchersResponse",
                                        success = true,
                                        data = list
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "GetExpenseVouchersResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }

                    case "SaveExpenseVoucher":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                var items = p["Items"]?.ToObject<List<ExpenseItemDto>>();

                                var result = db.SaveExpenseVoucher(
                                    date: p.Value<string>("Date"),
                                    paymentMode: p.Value<string>("PaymentMode"),
                                    //paidViaAccountId: p["PaidVia"]?.Type == JTokenType.Null
                                    //    ? null
                                    //    : p["PaidVia"]?.ToObject<long?>(),
                                    totalAmount: p.Value<decimal>("TotalAmount"),
                                    notes: p.Value<string>("Notes"),
                                    createdBy: p.Value<string>("CreatedBy"),
                                    items: items
                                );

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveExpenseVoucherResponse",
                                        success = true,
                                        VoucherNo = result.VoucherNo,
                                        ExpenseVoucherId = result.ExpenseVoucherId
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveExpenseVoucherResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }




                    case "ReverseStockAdjustment":
                        {
                            var p = JObject.FromObject(req.Payload);

                            try
                            {
                                var result = db.ReverseStockAdjustment(
                                    p.Value<long>("AdjustmentId"),
                                    p.Value<string>("ReversedBy")
                                );

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseStockAdjustmentResponse",
                                        success = true,
                                        AdjustmentNo = result.AdjustmentNo
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "ReverseStockAdjustmentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }

                            break;
                        }

                    case "GetCurrentStockForAdjustment":
                        {
                            var p = JObject.FromObject(req.Payload);
                            long itemId = p.Value<long>("ItemId");

                            using (var conn = new SQLiteConnection(_connectionString1))
                            {
                                conn.Open();
                                using (var txn = conn.BeginTransaction())
                                {
                                    var (qty, rate) = db.GetCurrentStockAndRate(conn, txn, itemId);
                                    txn.Commit();

                                    var response = new
                                    {
                                        action = "GetCurrentStockForAdjustmentResponse",
                                        ItemId = itemId,
                                        CurrentQty = qty,
                                        Rate = rate
                                    };

                                    webView.CoreWebView2.PostWebMessageAsJson(
                                        JsonConvert.SerializeObject(response)
                                    );
                                }
                            }
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
                    case "getPurchaseNetRate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "getPurchaseNetRateResult",
                                        success = false,
                                        message = "Invalid payload"
                                    })
                                );
                                break;
                            }

                            long itemId = payload.Value<long>("ItemId");
                            string batchNo = payload.Value<string>("BatchNo");
                            int lineindex = payload["LineIndex"]?.ToObject<int>() ?? 0;
                            decimal? netRate = db.GetPurchaseNetRate(itemId, batchNo);

                            var response = new
                            {
                                action = "getPurchaseNetRateResult",
                                success = true,
                                lineIndex = lineindex,
                                netRate = netRate   // null if not found
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );

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
                                    action = "GetItemBalanceBatchWiseResponse",
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
                                PaymentMode=invoice.PaymentMode,
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
                            // 6) Create unique filename
                            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Invoices"));

                            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                            string fileName = $"invoice-{invoice.InvoiceNo}-{timestamp}.pdf";

                            string pdfPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                "Invoices",
                                fileName
                            );

                            // 7) Save PDF
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
                                action = "GetCustomersResult",
                                data = customers
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

                    //case "LoadInvoiceForReturn":
                    //    {
                    //        var payload = req.Payload as JObject;
                    //        if (payload == null) break;

                    //        int invoiceId = payload["InvoiceId"].ToObject<int>();
                    //        var inv = db.LoadInvoiceForReturn(invoiceId);

                    //        var response = new
                    //        {
                    //            action = "LoadInvoiceForReturnResponse",
                    //            type = "LoadInvoiceForReturnResponse",
                    //            invoice = new
                    //            {
                    //                inv.Id,
                    //                inv.InvoiceNo,
                    //                inv.CustomerId,
                    //                inv.CustomerName,
                    //                Items = inv.ReturnItems  // 🔥 IMPORTANT
                    //            }
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(
                    //            JsonConvert.SerializeObject(response)
                    //        );
                    //        break;
                    //    }

                    case "SaveSalesReturn":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            var dto = payload.ToObject<SalesReturnDto>();

                            var result = db.SaveSalesReturn(dto);

                            var response = new
                            {
                                action = "SaveSalesReturnResponse",
                                success = result.Success,
                                returnId = result.ReturnId
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                          
                        }
                    //case "SearchSalesReturns":
                    //    {
                    //        var payload = req.Payload as JObject;
                    //        if (payload == null) break;

                    //        var date = payload["Date"].ToObject<string>();
                    //        var list = db.SearchSalesReturns(date);
                    //        var response = new
                    //        {
                    //            action = "SearchSalesReturnsResponse",
                    //            returns = list     // 🔥 must use "returns"
                    //        };
                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                    //        break;


                    //    }
                    //case "LoadSalesReturnDetail":
                    //    {
                    //        var payload = req.Payload as JObject;
                    //        if (payload == null) break;
                    //        int id = payload["ReturnId"].ToObject<int>();

                    //        var data = db.LoadSalesReturnDetail(id);
                    //        var response = new
                    //        {
                    //            action = "LoadSalesReturnDetailResponse",
                    //            returnData = data    // 🔥 must use "returns"
                    //        };
                    //        webView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(response));
                    //        break;

                    //    }
                    case "UpdateSalesInvoice":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                    throw new Exception("Invalid payload for UpdateSalesInvoice.");

                                // 🔹 Deserialize DTO
                                var dto = payload.ToObject<SalesInvoiceDto>();

                                if (dto == null || dto.InvoiceId <= 0)
                                    throw new Exception("Invalid InvoiceId.");

                                // 🔹 Call service
                                var result = db.UpdateSalesInvoice(dto);

                                // 🔹 Send response back to frontend
                                var response = new
                                {
                                    action = "UpdateSalesInvoiceResponse",
                                    success = result.Success,
                                    message = result.Message,
                                    newInvoiceId = result.NewInvoiceId
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "UpdateSalesInvoiceResponse",
                                    success = false,
                                    message = ex.Message,
                                    newInvoiceId = 0
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }

                            break;
                        }
                    case "GetDashboardOutstanding":
                        {
                            var rows = db.GetDashboardOutstanding();
                            var response = new
                            {
                                action = "GetDashboardOutstandingResult",
                                data = rows
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                         
                        }
                    case "getDashboardStockAlerts":
                        {
                            var data = db.GetDashboardStockAlerts();

                            var response = new
                            {
                                action = "getDashboardStockAlertsResult",
                                rows = data
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }


                    case "LoadSalesInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long invoiceId = payload["InvoiceId"]?.ToObject<long>() ?? 0;

                            var data = db.LoadSalesInvoice(invoiceId);
                            var response = new
                            {
                                action = "LoadSalesInvoiceResponse",
                                data = data    // 🔥 must use "returns"
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                            
                        }
                    case "CanEditSalesInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long invoiceId = payload.Value<long>("InvoiceId");

                            bool editable = db.CanEditSalesInvoice(invoiceId);

                            var response = new
                            {
                                action = "CanEditSalesInvoiceResponse",
                                Editable = editable,
                                InvoiceId = invoiceId   // 🔥 IMPORTANT
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }


                    case "SavePurchasePayment":
                        {
                            //var dto = req.Payload.ToObject<PurchasePaymentDto>();
                            var dto = ((JObject)req.Payload).ToObject<PurchasePaymentDto>();
                            try
                            {
                                var result = db.SavePurchasePayment(dto);

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SavePurchasePaymentResponse",
                                        success = true,
                                        amount = result.Amount,
                                        newPaidAmount = result.NewPaidAmount,
                                        newBalanceAmount = result.NewBalanceAmount
                                    })
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SavePurchasePaymentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }
                            break;
                        }



                    //case "PrintSalesReturn":
                    //    {
                    //        var payload = req.Payload as JObject;
                    //        if (payload == null) break;

                    //        long returnId = payload["ReturnId"]?.ToObject<long>() ?? 0;

                    //        // 1) Load sales return from DB
                    //        var sr = db.GetSalesReturn(returnId);

                    //        // 2) Load company profile
                    //        var company = db.GetCompanyProfileSR();

                    //        // 3) Map to Pdf DTO
                    //        var pdfSR = new DhanSutra.Pdf.SalesReturnLoadDto
                    //        {
                    //            Id = sr.Id,
                    //            ReturnNo = sr.ReturnNo,
                    //            ReturnNum = sr.ReturnNum,
                    //            ReturnDate = sr.ReturnDate,
                    //            InvoiceNo = sr.InvoiceNo,
                    //            CustomerId = sr.CustomerId,
                    //            CustomerName = sr.CustomerName,
                    //            CustomerPhone = sr.CustomerPhone,
                    //            CustomerState = sr.CustomerState,
                    //            CustomerAddress = sr.CustomerAddress,
                    //            SubTotal = sr.SubTotal,
                    //            TotalTax = sr.TotalTax,
                    //            TotalAmount = sr.TotalAmount,
                    //            RoundOff = sr.RoundOff,
                    //            Notes = sr.Notes,
                    //            Items = sr.Items?.ConvertAll(x => new DhanSutra.Pdf.SalesReturnItemForPrintDto
                    //            {
                    //                ItemId = x.ItemId,
                    //                BatchNo = x.BatchNo,
                    //                Qty = x.Qty,
                    //                Rate = x.Rate,
                    //                DiscountPercent = x.DiscountPercent,
                    //                GstPercent = x.GstPercent,
                    //                GstValue = x.GstValue,
                    //                CgstPercent = x.CgstPercent,
                    //                CgstValue = x.CgstValue,
                    //                SgstPercent = x.SgstPercent,
                    //                SgstValue = x.SgstValue,
                    //                IgstPercent = x.IgstPercent,
                    //                IgstValue = x.IgstValue,
                    //                LineSubTotal = x.LineSubTotal,
                    //                LineTotal = x.LineTotal
                    //            })
                    //        };

                    //        // 4) PDF Doc
                    //        var doc = new SalesReturnDocument(pdfSR, company);


                    //        var fileName = $"SalesReturn_{sr.ReturnNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    //        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Invoices", fileName);

                    //        Directory.CreateDirectory(Path.GetDirectoryName(filePath));





                    //        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                    //        var bytes = doc.GeneratePdf();
                    //        File.WriteAllBytes(filePath, bytes);

                    //        // 6) Send path to frontend
                    //        var response = new
                    //        {
                    //            action = "PrintSalesReturnResponse",
                    //            success = true,
                    //            pdfPath = filePath
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        break;
                    //    }



                    //case "SearchInvoicesForReturn":
                    //    {
                    //        var payload = req.Payload as JObject;
                    //        if (payload == null) break;
                    //        var date = payload["Date"]?.ToObject<string>();


                    //        var list = db.SearchInvoicesForReturn(date);
                    //        var response = new
                    //        {
                    //            action = "SearchInvoicesForReturnResponse",
                    //            type = "SearchInvoicesForReturnResponse",
                    //            invoices = list
                    //        };

                    //        webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        break;

                    //    }
                    case "searchCustomers":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            var kw = payload["Keyword"]?.ToObject<string>() ?? "";
                                                        
                            var list = db.SearchCustomers(kw);
                           var response = new
                            {
                                action = "searchCustomers",
                                success = true,
                                data = list
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "loadCustomer":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            int id = payload["CustomerId"]?.ToObject<int>() ?? 0;
                                                       
                            var data = db.LoadCustomer(id);
                            var response = new
                            {
                                action = "loadCustomer",
                                success = data != null,
                                data
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "saveCustomer":
                        {

                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            var dto = payload?.ToObject<CustomerDto>();


                            
                            var result = db.SaveCustomer(dto);

                            var response = new
                            {
                                action = "saveCustomer",
                                success = result,
                                message = result ? "Customer saved successfully" : "Failed to save"
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "deleteCustomer":
                        {

                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            int id = payload["CustomerId"]?.ToObject<int>() ?? 0;

                            var ok = db.DeleteCustomer(id);

                            var response = new
                            {
                                action = "deleteCustomer",
                                success = ok,
                                message = ok ? "Deleted successfully" : "Cannot delete"
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "searchSuppliers":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            string keyword = payload["Keyword"]?.ToObject<string>() ?? "";

                            var list = db.SearchSuppliers(keyword);
                            var response = new
                            {
                                action = "searchSuppliers",
                                success = true,
                                data = list
                        };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        
                        }
                    case "loadSupplier":
                        {
                            
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            long id = payload["SupplierId"]?.ToObject<long>() ?? 0;
                            
                            var s = db.GetSupplier(id);
                            if (s == null)
                            {
                                var response = new
                                {
                                    action = "loadSupplier",
                                    success = false,
                                    error = "Supplier not found"
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                            else
                            {
                                var response = new
                                {
                                    action = "loadSupplier",
                                    success = true,
                                    data = s
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                            }
                    case "saveSupplier":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            var dto = payload?.ToObject<SupplierDto>();

                            bool isNew = dto.SupplierId == 0;   // capture state before saving
                            //string currentUser = "Admin";
                            long id = db.SaveSupplier(dto);

                            if (dto == null)
                            {
                                
                                var response = new
                                {
                                    action = "saveSupplier",
                                    success = false,
                                    error = "Invalid supplier data"
                            }
                            ;
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                            else
                            {
                                var response = new
                                {
                                    action = "saveSupplier",
                                    success = true,
                                    data = new { SupplierId = id },
                                    message = isNew ? "Supplier created successfully." : "Supplier updated successfully.",
                                }
                            ;

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                        }

                    case "deleteSupplier":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            long id = payload["SupplierId"]?.ToObject<long>() ?? 0;
                                                      
                            bool ok = db.DeleteSupplier(id);
                            if (!ok)
                            {
                                var response = new
                                {
                                    action = "deleteSupplier",
                                    error = "Delete failed or supplier not found"
                                    

                                };
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                            else
                            {
                                var response = new
                                {
                                    action = "deleteSupplier",
                                    success = ok,
                                    message= "Supplier Deleted successfully."

                                };

                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;
                            }
                                

                            
                                //resp.Error = "Delete failed or supplier not found";
                            
                        }
                    case "GetAllSuppliers":
                        {
                            var suppliers = db.GetAllSuppliers();
                            var response = new
                            {
                                action = "GetAllSuppliers",
                                success = true,
                                data=suppliers

                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                            //return JsonConvert.SerializeObject(new { success = true, data = suppliers });
                        }
                    //case "SearchPurchaseItemsByDate":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null) break;
                    //            string dateStr = payload["Date"]?.ToObject<string>();

                    //            DateTime date = DateTime.Parse(dateStr);
                    //            var items = db.SearchPurchaseItemsByDate(date);

                    //            var response = new
                    //            {
                    //                action = "SearchPurchaseItemsByDateResponse",
                    //                success = true,
                    //                items = items
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "SearchPurchaseItemsByDate",
                    //                success = false,
                    //                error = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }

                    //        break;
                    //    }
                    //case "SearchPurchaseReturns":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null) break;

                    //            // accept both "Date" and "date" casing just in case
                    //            var dateToken = payload["Date"] ?? payload["date"];
                    //            if (dateToken == null)
                    //            {
                    //                var respErr = new { action = "SearchPurchaseReturnsResponse", success = false, error = "Date not provided" };
                    //                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(respErr));
                    //                break;
                    //            }

                    //            string dateStr = dateToken.ToObject<string>();
                    //            DateTime date = DateTime.Parse(dateStr);

                    //            var returns = db.SearchPurchaseReturns(date);

                    //            var response = new
                    //            {
                    //                action = "SearchPurchaseReturnsResponse",
                    //                success = true,
                    //                returns = returns
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "SearchPurchaseReturnsResponse",
                    //                success = false,
                    //                error = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }

                    //        break;
                    //    }


                    //case "LoadPurchaseForReturn":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null) break;
                    //            long itemDetailsId = payload["ItemDetailsId"].ToObject<long>();

                                

                    //            var detail = db.LoadPurchaseForReturn(itemDetailsId);

                    //            var response = new
                    //            {
                    //                action = "LoadPurchaseForReturnResponse",
                    //                success = true,
                    //                data = detail
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "LoadPurchaseForReturn",
                    //                success = false,
                    //                error = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }

                    //        break;
                    //    }
                    //case "LoadPurchaseReturnDetail":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null) break;

                    //            long returnId = payload["ReturnId"]?.ToObject<long>() ?? 0;

                    //            var data = db.GetPurchaseReturnDetail(returnId);

                    //            var response = new
                    //            {
                    //                action = "LoadPurchaseReturnDetailResponse",
                    //                success = true,
                    //                returnData = data
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(
                    //                JsonConvert.SerializeObject(response)
                    //            );
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "LoadPurchaseReturnDetailResponse",
                    //                success = false,
                    //                error = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(
                    //                JsonConvert.SerializeObject(response)
                    //            );
                    //        }

                    //        break;
                    //    }


                    //case "SavePurchaseReturn":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null) break;

                    //            var dto = payload.ToObject<PurchaseReturnDto>();
                    //            //var dto = payload.ToObject<PurchaseReturnDto>();

                    //            var result = db.SavePurchaseReturn(dto);

                    //            var response = new
                    //            {
                    //                action = "SavePurchaseReturnResponse",
                    //                success = true,
                    //                data = new
                    //                {
                    //                    returnId = result.ReturnId,
                    //                    returnNo = result.ReturnNo,
                    //                    returnNum = result.ReturnNum
                    //                }
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "SavePurchaseReturn",
                    //                success = false,
                    //                error = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                    //        }

                    //        break;
                    //    }
                    case "GetNextPurchaseInvoiceNum":
                        {
                            long nextNum = db.GetNextPurchaseInvoiceNum();

                            string fy = GetFinancialYear();   // EX: "24-25"
                            string padded = nextNum.ToString("D5"); // 00001, 00002

                            string invoiceNo = $"PINV-{fy}-{padded}";

                            var response = new
                            {
                                action = "GetNextPurchaseInvoiceNumResponse",
                                nextNum = nextNum,
                                fy = fy,
                                invoiceNo = invoiceNo
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "GetNextSalesInvoiceNum":
                        {
                            long nextNum = db.GetNextSalesInvoiceNum();

                            string fy = GetFinancialYear();   // EX: "24-25"
                            string padded = nextNum.ToString("D5"); // 00001, 00002

                            string invoiceNo = $"SINV-{fy}-{padded}";

                            var response = new
                            {
                                action = "GetNextSalesInvoiceNumResponse",
                                nextNum = nextNum,
                                fy = fy,
                                invoiceNo = invoiceNo
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "GetNextInvoiceNumberFromCompanyProfile":
                        {
                            var result = db.GetNextInvoiceNumberFromCompanyProfile();

                            //string fy = GetFinancialYear();   // EX: "24-25"
                            //string padded = nextNum.ToString("D5"); // 00001, 00002

                            //string invoiceNo = $"SINV-{fy}-{padded}";

                            var response = new
                            {
                                action = "GetNextInvoiceNumberFromCompanyProfileResponse",
                                nextNum = result.InvoiceNum,
                                invoiceNo = result.InvoiceNo
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "CheckDuplicatePurchaseInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long supplierId = payload.Value<long>("SupplierId");
                            string invoiceNo = payload.Value<string>("InvoiceNo")?.Trim();

                            bool exists = db.CheckDuplicatePurchaseInvoice(supplierId, invoiceNo);

                            var response = new
                            {
                                action = "CheckDuplicatePurchaseInvoiceResponse",
                                exists = exists
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                    case "SavePurchaseInvoice":
                        {
                            

                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                    break;

                                // Deserialize into PurchaseInvoiceDto
                                var dto = payload.ToObject<PurchaseInvoiceDto>();

                                var errors = db.ValidatePurchaseInvoice(dto);
                                if (errors.Any())
                                {
                                    var response = new
                                    {
                                        action = "SavePurchaseInvoiceResponse",
                                        success = false,
                                       
                                        message = "validation Error."
                                    };
                                }
                                else
                                {
                                    var result = db.SavePurchaseInvoice(dto);
                                    long purchaseId = result.purchaseId;
                                    string invoiceNo = result.invoiceNo;

                                    var response = new
                                    {
                                        action = "SavePurchaseInvoiceResponse",
                                        success = true,
                                        purchaseId = purchaseId,   // <-- FIXED
                                        invoiceNo = invoiceNo,
                                        message = "Purchase invoice saved successfully."
                                    };

                                    webView.CoreWebView2.PostWebMessageAsJson(
                                        JsonConvert.SerializeObject(response)
                                    );

                                }

                                   
                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "SavePurchaseInvoiceResponse",
                                    success = false,
                                    message = ex.Message
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }

                            break;
                        }
                    case "GetPurchaseInvoice":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                    break;

                                long purchaseId = payload["PurchaseId"]?.ToObject<long>() ?? 0;

                                var invoice = db.GetPurchaseInvoice(purchaseId);

                                var response = new
                                {
                                    action = "GetPurchaseInvoiceResponse",
                                    success = invoice != null,
                                    data = invoice
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "GetPurchaseInvoiceResponse",
                                    success = false,
                                    message = ex.Message
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            break;
                        }
                    //case "GetNextBatchNumForItem":
                    //    {
                    //        try
                    //        {
                    //            var payload = req.Payload as JObject;
                    //            if (payload == null)
                    //                break;

                    //            long itemId = payload["ItemId"]?.ToObject<long>() ?? 0;

                    //            int nextBatchNum = db.GetNextBatchNumForItem(itemId);

                    //            var response = new
                    //            {
                    //                action = "GetNextBatchNumForItemResponse",
                    //                success = true,
                    //                itemId = itemId,
                    //                nextBatchNum = nextBatchNum
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(
                    //                JsonConvert.SerializeObject(response)
                    //            );
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var response = new
                    //            {
                    //                action = "GetNextBatchNumForItemResponse",
                    //                success = false,
                    //                message = ex.Message
                    //            };

                    //            webView.CoreWebView2.PostWebMessageAsJson(
                    //                JsonConvert.SerializeObject(response)
                    //            );
                    //        }

                    //        break;
                    //    }
                    case "GetNextBatchNum":
                        {
                            var payload = req.Payload as JObject;

                            long itemId = payload["ItemId"]?.ToObject<long>() ?? 0;
                            int lineIndex = payload["LineIndex"]?.ToObject<int>() ?? 0;

                            int nextBatch = db.GetNextBatchNumForItem(itemId);

                            var response = new
                            {
                                action = "GetNextBatchNumResponse",
                                success = true,
                                itemId = itemId,
                                lineIndex = lineIndex,
                                batchNum = nextBatch
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "GetSupplierById":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                long supplierId = payload["SupplierId"]?.ToObject<long>() ?? 0;

                                var supplier = db.GetSupplierById(supplierId);

                                var response = new
                                {
                                    action = "GetSupplierByIdResponse",
                                    success = true,
                                    data = supplier
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "GetSupplierByIdResponse",
                                    success = false,
                                    message = ex.Message
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            break;
                        }
                    case "GetPurchaseInvoiceNumbersByDate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string date = payload["Date"]?.ToObject<string>();

                            var list = db.GetPurchaseInvoiceNumbersByDate(date);

                            var response = new
                            {
                                action = "GetPurchaseInvoiceNumbersByDateResponse",
                                data = list
                            };

                            webView.CoreWebView2
                                .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                            break;
                        }
                    case "GetSalesInvoiceNumbersByDate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string date = payload["Date"]?.ToString() ?? "";

                            // Call DB
                            var list = db.GetSalesInvoiceNumbersByDate(date);
                            var response = new
                            {
                                action = "GetSalesInvoiceNumbersByDateResponse",
                                data = list
                            };

                            webView.CoreWebView2
                                .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                            break;
                            
                        }

                    case "CanEditPurchaseInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long purchaseId = payload["PurchaseId"].ToObject<long>();

                            //long purchaseId = msg.Payload?["PurchaseId"]?.ToObject<long>() ?? 0;

                            bool editable = db.CanEditPurchaseInvoice(purchaseId);

                            var response = new
                            {
                                action = "CanEditPurchaseInvoiceResponse",
                                PurchaseId = purchaseId,
                                Editable = editable
                            };

                            webView.CoreWebView2
                               .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                            break;
                        }

                    case "LoadPurchaseInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long purchaseId = payload["PurchaseId"].ToObject<long>();

                            var dto = db.LoadPurchaseInvoice(purchaseId);

                            var response = new
                            {
                                action = "LoadPurchaseInvoiceResponse",
                                success = dto != null,
                                data = dto
                            };

                            webView.CoreWebView2
                               .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                            break;

                           
                        }

                    case "UpdatePurchaseInvoice":
                        {
                            var payload = req.Payload as JObject;

                            if (payload == null)
                            {
                                var response = new
                                {
                                    action = "UpdatePurchaseInvoiceResponse",
                                    success = false,
                                    message = "Invalid update payload"
                                };
                                webView.CoreWebView2
                              .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                                break;
                            }
                            else
                            {


                                // Deserialize into PurchaseInvoiceDto
                                var dto = payload.ToObject<PurchaseInvoiceDto>();
                                var result = db.UpdatePurchaseInvoice(dto);

                                var response = new
                                {
                                    action = "UpdatePurchaseInvoiceResponse",
                                    success = result.Success,
                                    message = result.Message,
                                    newPurchaseId = result.NewPurchaseId,
                                    invoiceNo = dto.InvoiceNo
                                };
                                webView.CoreWebView2
                              .PostWebMessageAsJson(JsonConvert.SerializeObject(response));

                                break;

                            }
                        }


                    case "PrintPurchaseInvoice":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long PurchaseId = payload["PurchaseId"]?.ToObject<long>() ?? 0;

                            // 1) Load invoice (contains SupplierId)
                            var invoice = db.GetPurchaseInvoiceDto(PurchaseId);

                            // 2) Load supplier details from DB
                            var supplier = db.GetSupplierById(invoice.SupplierId);

                            // 3) Load company
                            var company = db.GetCompanyProfile();

                            // 4) Build PDF DTO
                            var pdfInvoice = new PurchaseInvoicePdfDto
                            {
                                PurchaseId = invoice.PurchaseId,
                                InvoiceNo = invoice.InvoiceNo,
                                InvoiceNum = invoice.InvoiceNum,
                                InvoiceDate = invoice.InvoiceDate,

                                SupplierId = invoice.SupplierId,
                                SupplierName = supplier?.SupplierName,
                                SupplierGSTIN = supplier?.GSTIN,
                                SupplierAddress = supplier?.Address,
                                SupplierPhone = supplier?.Mobile,
                                SupplierState = supplier?.State,
                                SubTotalAmount=invoice.SubTotal,
                                TotalAmount = invoice.TotalAmount,
                                TotalTax = invoice.TotalTax,
                                RoundOff = invoice.RoundOff,
                                Notes = invoice.Notes,

                                Items = invoice.Items.Select(x => new PurchaseInvoiceItemPdfDto
                                {
                                    ItemId = x.ItemId,
                                    ItemName=x.ItemName,
                                    Qty = x.Qty,
                                    Rate = x.Rate,
                                    DiscountPercent = x.DiscountPercent,
                                    NetRate = x.NetRate,

                                    GstPercent = x.GstPercent,
                                    GstValue = x.GstValue,

                                    CgstPercent = x.CgstPercent,
                                    CgstValue = x.CgstValue,
                                    SgstPercent = x.SgstPercent,
                                    SgstValue = x.SgstValue,
                                    IgstPercent = x.IgstPercent,
                                    IgstValue = x.IgstValue,

                                    LineSubTotal = x.LineSubTotal,
                                    LineTotal = x.LineTotal,
                                    BatchNo = x.BatchNo
                                }).ToList()
                            };

                            var pdfCompany = new CompanyProfilePdfDto(company);

                            // 5) Generate PDF document
                            var doc = new PurchaseInvoiceDocument(pdfInvoice, pdfCompany);

                            // 6) Create unique filename
                            string invoicesDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "Invoices"
);
                            Directory.CreateDirectory(invoicesDir);

                            // sanitize invoice number for filename
                            string safeInvoiceNo = string.Concat(
                                invoice.InvoiceNo.Select(ch =>
                                    Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch
                                )
                            );

                            // create base filename
                            string baseFileName = $"invoice-{safeInvoiceNo}.pdf";
                            string pdfPath = Path.Combine(invoicesDir, baseFileName);

                            // generate copy filenames
                            int copyIndex = 1;
                            while (File.Exists(pdfPath))
                            {
                                string copyName = $"invoice-{safeInvoiceNo} (copy {copyIndex}).pdf";
                                pdfPath = Path.Combine(invoicesDir, copyName);
                                copyIndex++;
                            }

                            // generate PDF
                            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                            try
                            {
                                var bytes = doc.GeneratePdf();
                                File.WriteAllBytes(pdfPath, bytes);
                            }
                            catch (Exception ex)
                            {
                                File.WriteAllText("C:\\Users\\User\\Documents\\Invoices\\debug-error.txt", ex.ToString());
                                throw;
                            }



                            // 6) Return to frontend
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
                            {
                                action = "PrintPurchaseInvoiceResponse",
                                success = true,
                                pdfPath
                            }));

                            break;
                        }
                    case "SaveSalesPayment":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                    throw new Exception("Invalid payload");

                                var dto = payload.ToObject<SalesPaymentDto>();

                                
                                decimal updatedPaid = db.SaveSalesPayment(dto);

                                var response = new
                                {
                                    action = "SaveSalesPaymentResponse",
                                    success = true,
                                     newPaidAmount = updatedPaid   // ← IMPORTANT

                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        action = "SaveSalesPaymentResponse",
                                        success = false,
                                        message = ex.Message
                                    })
                                );
                            }
                            break;
                        }

                    case "GetInvoiceForEdit":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null)
                                    throw new Exception("Invalid payload");

                                long invoiceId = payload.Value<long>("InvoiceId");

                                var invoice = db.GetInvoiceForEdit(invoiceId);

                                var response = new
                                {
                                    action = "GetInvoiceForEditResponse",
                                    success = true,
                                    data = invoice
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "GetInvoiceForEditResponse",
                                    success = false,
                                    message = ex.Message
                                };

                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(response)
                                );
                            }

                            break;
                        }
                    case "getDashboardProfitLoss":
                        {
                            var p = req.Payload as JObject;

                            var from = p.Value<string>("From");
                            var to = p.Value<string>("To");

                            var data = db.GetDashboardProfitLoss(from, to);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "getDashboardProfitLossResult",
                                    data
                                })
                            );
                            break;
                        }

                    case "SavePurchaseReturn":
                        {
                            try
                            {
                                var payload = req.Payload as JObject;
                                if (payload == null) break;

                                var dto = payload.ToObject<PurchaseReturnDto>();
                                var newId = db.SavePurchaseReturn(dto);

                                var response = new
                                {
                                    action = "SavePurchaseReturnResponse",
                                    success = true,
                                    newReturnId = newId
                                };
                                webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                                break;


                            }
                            catch (Exception ex)
                            {
                                var response = new
                                {
                                    action = "SavePurchaseReturnResponse",
                                    success = false,
                                    message = ex.Message
                                };
                                
                            }
                            break;
                        }
                    case "getTrialBalance":
                        {
                            var data = db.GetTrialBalance();

                            var response = new
                            {
                                action = "getTrialBalanceResult",
                                success = true,
                                rows = data
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "getLedgerReport":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long accountId = payload.Value<long>("AccountId");
                            string from = payload.Value<string>("From") ?? DateTime.UtcNow.ToString("yyyy-MM-01");
                            string to = payload.Value<string>("To") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

                            var report = db.GetLedgerReport(accountId, from, to);

                            var response = new
                            {
                                action = "getLedgerReportResult",
                                success = true,
                                report = report
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "Login":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string username = payload.Value<string>("username");
                            string password = payload.Value<string>("password");

                            var result = db.Login(username, password);

                            if (result.Success)
                            {
                                _currentUser = (UserDto)result.Data;   // ✅ STORE LOGGED-IN USER
                            }

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    Type = "Login",
                                    Status = result.Success ? "Success" : "Error",
                                    Message = result.Message,
                                    Data = result.Data
                                })
                            );
                            break;
                        }

                    case "ChangePassword":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            long userId = payload.Value<Int16>("userId");
                            string oldPwd = payload.Value<String>("oldPassword");
                            string newPwd = payload.Value<String>("newPassword");

                            var result = db.ChangePassword(userId, oldPwd, newPwd);

                            var response = new
                            {
                                Type = "ChangePassword",
                                Status = result.Success ? "Success" : "Error",
                                Message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                           
                        }
                    case "CreateUser":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;
                            var username = payload.Value<string>("username");
                            var password = payload.Value<string>("password");
                            string role = payload.Value<string>("role");

                            var result = db.CreateUser(username, password, role);

                            var response = new
                            {
                                Type = "CreateUser",
                                Status = result.Success ? "Success" : "Error",
                                Message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                            
                        }
                    case "GetUsers":
                        {
                            var result = db.GetUsers();

                            var response = new
                            {
                                Type = "GetUsers",
                                Status = result.Success ? "Success" : "Error",
                                Data = result.Data,
                                Message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                           
                        }

                    case "SetUserStatus":
                        {
                            if (_currentUser == null)
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(
                                    JsonConvert.SerializeObject(new
                                    {
                                        Type = "SetUserStatus",
                                        Status = "Error",
                                        Message = "Not authenticated"
                                    })
                                );
                                break;
                            }

                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long userId = payload.Value<long>("userId");
                            bool isActive = payload.Value<bool>("isActive");

                            // ✅ PASS LOGGED-IN USER
                            var result = db.SetUserStatus(userId, isActive, _currentUser);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    Type = "SetUserStatus",
                                    Status = result.Success ? "Success" : "Error",
                                    Message = result.Message
                                })
                            );
                            break;
                        }




                    case "fetchCoA":
                        {
                            var rows = db.FetchAccounts();

                            var response = new
                            {
                                action = "fetchCoAResult",
                                success = true,
                                rows = rows
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "createAccount":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            var dto = payload.ToObject<AccountDto>();

                            OperationResult result = db.CreateAccount(dto);

                            var response = new
                            {
                                action = "createAccountResult",
                                success = result.Success,
                                message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );

                            break;
                        }

                    case "GetDayBook":
                        {
                            var p = req.Payload as JObject;
                            if (p == null) break;

                            var from = p.Value<string>("From");
                            var to = p.Value<string>("To");

                            var data = db.GetDayBook(from, to);

                            var response = new
                            {
                                Status = "Success",
                                Data = data,
                                action= "GetDayBookResponse"
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                            
                        }
                    case "getVoucherReport":
                        {
                            var p = req.Payload as JObject;
                            var from = p.Value<string>("From");
                            var to = p.Value<string>("To");
                            var voucherType = p.Value<string>("VoucherType");

                            var rows = db.GetVoucherReport(from, to, voucherType);
                            var response = new
                            {
                                Status = "Success",
                                rows,
                                action = "getVoucherReportResult"
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;

                            
                        }

                    case "getProfitLoss":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string from = payload.Value<string>("From");
                            string to = payload.Value<string>("To");

                            var pl = db.GetProfitAndLoss(from, to);

                            var response = new
                            {
                                action = "getProfitLossResult",
                                success = true,
                                report = pl
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }
                    case "GetStockSummary":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string asOf = payload.Value<string>("AsOf") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

                            var stockSvc = new StockValuationService();
                            var summary = stockSvc.GetStockSummary(asOf);

                            var response = new
                            {
                                action = "StockSummaryResult",
                                success = true,
                                data = summary
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "getBalanceSheet":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string asOf = payload.Value<string>("AsOf");

                            // -----------------------------------------
                            // STEP 1: Calculate Profit & Loss up to AsOf
                            // -----------------------------------------
                            // NOTE: fromDate should be your financial year start
                            string financialYearStart = "2025-04-01"; // adjust as needed

                            var pl = db.GetProfitAndLoss(financialYearStart, asOf);

                            // Net profit as signed value
                            decimal netProfit =
                                pl.NetProfit > 0
                                    ? pl.NetProfit
                                    : -pl.NetLoss;

                            // -----------------------------------------
                            // STEP 2: Generate Balance Sheet USING P&L RESULT
                            // -----------------------------------------
                            var bs = db.GetBalanceSheet(asOf, netProfit);

                            // -----------------------------------------
                            // STEP 3: Send response
                            // -----------------------------------------
                            var response = new
                            {
                                action = "getBalanceSheetResult",
                                success = true,
                                report = bs
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );

                            break;
                        }

                    case "getFIFOValuation":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string asOf = payload.Value<string>("AsOf") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
                            string from = payload.Value<string>("From"); // optional
                            string to = payload.Value<string>("To") ?? asOf;

                            var svc = new StockValuationService();
                            var rows = svc.CalculateStockValuationFIFO(asOf, from, to);

                            var response = new
                            {
                                action = "getFIFOValuationResult",
                                success = true,
                                data = rows
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "getFIFOTotals":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string from = payload.Value<string>("From");
                            string to = payload.Value<string>("To") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

                            var svc = new StockValuationService();
                            var totals = svc.ComputeTotalsFIFO(from, to);

                            var response = new
                            {
                                action = "getFIFOTotalsResult",
                                success = true,
                                closingStock = totals.ClosingStockTotal,
                                cogs = totals.PeriodCogsTotal
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
                            break;
                        }

                    case "updateAccount":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            var dto = payload.ToObject<AccountDto>();
                            var result = db.UpdateAccount(dto);

                            var response = new
                            {
                                action = "updateAccountResult",
                                success = result.Success,
                                message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                    case "deleteAccount":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long id = payload.Value<long>("AccountId");

                            var result = db.DeleteAccount(id);

                            var response = new
                            {
                                action = "deleteAccountResult",
                                success = result.Success,
                                message = result.Message
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                    case "GetCustomerById":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            long customerId = payload.Value<long>("CustomerId");

                            var customer = db.GetCustomerById(customerId);

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "GetCustomerByIdResult",
                                    data = customer
                                })
                            );
                            break;
                        }
                    case "getVoucherTypes":
                        {
                            var types = db.GetVoucherTypes();

                            var response = new
                            {
                                action = "getVoucherTypesResult",
                                rows = types
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;


                           
                        }
                    case "getCashBook":
                        {
                            var p = req.Payload as JObject;
                            var from = p.Value<string>("From");
                            var to = p.Value<string>("To");

                            CashBookDto dto = db.GetCashBook(from, to);

                            var response = new
                            {
                                action = "getCashBookResult",
                                OpeningBalance = dto.OpeningBalance,
                                ClosingBalance = dto.ClosingBalance,
                                Rows = dto.Rows
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                    case "getBankBook":
                        {
                            var p = req.Payload as JObject;
                            var from = p.Value<string>("From");
                            var to = p.Value<string>("To");

                            CashBookDto dto = db.GetBankBook(from, to);

                            var response = new
                            {
                                action = "getBankBookResult",
                                OpeningBalance = dto.OpeningBalance,
                                ClosingBalance = dto.ClosingBalance,
                                Rows = dto.Rows
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                    case "getOutstandingReport":
                        {
                            var p = req.Payload as JObject;
                            var balanceType = p.Value<string>("BalanceType") ?? "ALL";

                            var rows = db.GetOutstandingReport(balanceType);

                            var response = new
                            {
                                action = "getOutstandingReportResult",
                                rows
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }
                    case "getDashboardSummary":
                        {
                            var data = db.GetDashboardSummary();

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(new
                                {
                                    action = "getDashboardSummaryResult",
                                    data
                                })
                            );
                            break;
                        }


                    case "GetInvoiceNumbersByDate":
                        {
                            var payload = req.Payload as JObject;
                            if (payload == null) break;

                            string date = payload.Value<string>("date");

                            var list = db.GetSalesInvoiceNumbersByDate(date);

                            var response = new
                            {
                                action = "invoiceNumbersByDateResult",
                                data = list
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(
                                JsonConvert.SerializeObject(response)
                            );
                            break;
                        }

                        //case "GetNextPurchaseInvoiceNum":
                        //    {
                        //        try
                        //        {
                        //            var next = db.GetNextPurchaseInvoiceNum();

                        //            var res = new
                        //            {
                        //                action = "GetNextPurchaseInvoiceNumResponse",
                        //                next = next
                        //            };

                        //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(res));
                        //        }
                        //        catch (Exception ex)
                        //        {
                        //            var res = new
                        //            {
                        //                action = "GetNextPurchaseInvoiceNumResponse",
                        //                next = 0,
                        //                error = ex.Message
                        //            };

                        //            webView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(res));
                        //        }

                        //        break;
                        //    }



                }
            }

            catch (Exception ex)
            {
                webView.CoreWebView2.PostWebMessageAsString("Error: " + ex.Message);
            }
        }
       
        public string GetFinancialYear()
        {
            DateTime today = DateTime.Now;

            int year = today.Year;
            int month = today.Month;

            int startYear;
            int endYear;

            // Financial Year starts in APRIL
            if (month >= 4)
            {
                startYear = year;
                endYear = year + 1;
            }
            else
            {
                startYear = year - 1;
                endYear = year;
            }

            return $"{startYear % 100:D2}-{endYear % 100:D2}";
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
