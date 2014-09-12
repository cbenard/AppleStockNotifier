using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace StockNotifier
{
    public class PushalotNotifier : INotifier
    {
        public void NotifyAvailability(string apiKey, string productName, string storeName)
        {
            using (var client = new WebClient())
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    AuthorizationToken = apiKey,
                    Body = String.Format("Available at {0}.", storeName),
                    Source = "Apple Stock Notifier",
                    Title = String.Format("{0} Available", productName)
                });

                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.UploadString("https://pushalot.com/api/sendmessage", payload);
            }
        }

        public string Name
        {
            get { return "Pushalot"; }
        }
    }
}
