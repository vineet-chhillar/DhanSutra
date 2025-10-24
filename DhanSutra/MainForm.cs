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

                    case "DeleteItem":
                        var itemData = JsonConvert.DeserializeObject<Item>(req.Payload.ToString());
                        db.DeleteItem(itemData.Id);
                        webView.CoreWebView2.PostWebMessageAsString("ItemDeleted");
                        break;

                    default:
                         webView.CoreWebView2.PostWebMessageAsString("Unknown action");
                        break;
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
