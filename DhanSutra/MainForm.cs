using DhanSutra;
using DhanSutra.Models;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

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

                            bool updated = db.UpdateItem(itemId, name, itemCode, categoryId, date, description, unitId, gstId);

                            var result = new
                            {
                                action = "updateItem",
                                success = updated,
                                message = updated ? "✅ Item updated successfully." : "⚠️ Item update failed."
                            };

                            webView.CoreWebView2.PostWebMessageAsJson(Newtonsoft.Json.JsonConvert.SerializeObject(result));
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

                            var id = payload["id"]?.ToString();
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
                                        success = db.UpdateInventoryRecord(itemId, batchNo, refno, hsnCode, date, quantity, purchasePrice, discountPercent, netPurchasePrice, amount,
                                salesPrice, mrp, goodsOrServices, description, mfgDate, expDate, modelno, brand, size, color, weight, dimension, invbatchno);

                                        bool success_ledger = db.UpdateItemLedger(itemId, batchNo, refno, date, quantity, purchasePrice, discountPercent, netPurchasePrice, amount,
                                            description, invbatchno);

                                        bool success_itembalance_batchno = db.UpdateItemBalanceForBatchNo(itemId, batchNo, invbatchno);

                                        //there should be change in itembalance also if quantity changes
                                        bool success_itembalance_forquantity = db.UpdateItemBalance_ForChangeInQuantity(stritemId, batchNo, invbatchno, quantity);
                                        


                                        // ✅ If both succeeded, commit
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
                                        // ❌ Rollback on any error
                                        transaction.Rollback();
                                        Console.WriteLine("Error updating inventory & ledger: " + ex.Message);
                                        success = false;
                                    }
                                }

                                conn.Close();
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
    }
    public class WebRequestMessage
    {
        public string Action { get; set; }
        public object Payload { get; set; }
    }
}
