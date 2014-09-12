using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace StockNotifier
{
    public class BoxcarNotifier : INotifier
    {
        public void NotifyAvailability(string apiKey, string productName, string storeName)
        {
            var nvc = new NameValueCollection
            {
                { "user_credentials", apiKey },
                { "notification[title]", String.Format("{0} Available!", productName) },
                { "notification[long_message]", String.Format("Available at {0}.", storeName) },
                { "notification[source_name]", String.Format("Apple Stock Notifier", storeName) },
                { "notification[icon_url]", "http://www.mobiflip.de/wp-content/uploads/2011/09/apple-logo9.jpg" },
                { "notification[open_url]", "http://store.apple.com" },
            };

            var uri = new Uri("https://new.boxcar.io/api/notifications");
            string payload = string.Join("&", nvc.AllKeys.Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
            
            using (var client = new WebClient())
            {
                client.UploadString(uri, payload);
            }
        }

        public string Name
        {
            get { return "Boxcar"; }
        }
    }
}
