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
        private DatabaseService db = new DatabaseService();
        public MainForm()
        {
            InitializeComponent();
            InitializeWebViewAsync();
        }
        private async void InitializeWebViewAsync()
        {
            await webView.EnsureCoreWebView2Async(null);

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
                            db.AddItemDetails(details);

                            var response = new
                            {
                                Type = "AddItemDetails",
                                Status = "Success",
                                Message = "Inventory details saved successfully"
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
                            var batchNo = payload["batchNo"]?.ToString();
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
                            bool success = db.UpdateInventoryRecord(
                                itemId,
                                batchNo,
                                hsnCode,
                                date,
                                quantity,
                                purchasePrice,
                                salesPrice,
                                mrp,
                                goodsOrServices,
                                description,
                                mfgDate,
                                expDate,
                                modelno,
                                brand,
                                size,
                                color,
                                weight,
                                dimension,
                                invbatchno
                            );

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
