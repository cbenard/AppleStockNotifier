using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Web;

namespace StockNotifier
{
    public class Program
    {
        private static string _modelNumber;
        private static string _zipCode;
        private static bool _hasAcknowledgedExit = false;
        private static int? _intervalSeconds;
        private static int? _numberToSearch;
        private static string _pushApiKey;
        private static INotifier _notifier;
        private static string _productName;

        public static int Main(string[] args)
        {

            try
            {
                Console.WriteLine("Stock Notifier");
                Console.WriteLine("----------------------");

                SetStartupParameters();

                RunTimer();

                return 0;
            }
            catch (FriendlyException friendlyEx)
            {
                Console.WriteLine(friendlyEx.Message);

                return 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled Exception: " + ex);
                return 1;
            }
            finally
            {
                if (!_hasAcknowledgedExit)
                {
                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void RunTimer()
        {
            Console.WriteLine();
            Console.WriteLine("Running check every {0} seconds. Press enter to quit.", _intervalSeconds);

            Timer timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = TimeSpan.FromSeconds(_intervalSeconds.Value).TotalMilliseconds;
            timer.Elapsed += HandleTimer;
            timer.Start();

            System.Threading.Thread async = new System.Threading.Thread(() => HandleTimer(null, null));
            async.IsBackground = true;
            async.Start();

            Console.ReadLine();
            _hasAcknowledgedExit = true;
        }

        private static void HandleTimer(object sender, ElapsedEventArgs e)
        {
            Timer timer = null;

            try
            {
                timer = sender as Timer;

                RunCheck();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in timer elapsed: " + ex);
            }
            finally
            {
                if (timer != null)
                {
                    timer.Start();
                }
            }
        }

        private static void RunCheck()
        {
            using (var appleClient = new WebClient())
            {
                var builder = new UriBuilder("http://store.apple.com/us/retailStore/availabilitySearch");
                builder.Query = String.Format("parts.0={0}&zip={1}", HttpUtility.UrlEncode(_modelNumber), HttpUtility.UrlEncode(_zipCode));
                Uri uri = builder.Uri;

                string jsonPayload = appleClient.DownloadString(uri);

                ProcessPayload(jsonPayload);
            }
        }

        private static void ProcessPayload(string jsonPayload)
        {
            dynamic result = JsonConvert.DeserializeObject(jsonPayload);

            if (result.head.status != "200")
            {
                Console.WriteLine("Non-OK error code: {0}", result.head.status);
            }
            else if (result.body.success != true)
            {
                Console.WriteLine("Non-true success: {0}", result.body.success);
            }
            else if (result.body.stores[0].partsAvailability[_modelNumber] == null)
            {
                Console.Write("Couldn't find model number {0} in the JSON payload.", _modelNumber);
            }
            else
            {
                int counter = 0;
                bool foundProduct = false;

                foreach (var store in result.body.stores)
                {
                    counter++;
                    if (counter > _numberToSearch)
                    {
                        break;
                    }

                    dynamic availability = result.body.stores[counter - 1].partsAvailability[_modelNumber];
                    if (availability.storeSelectionEnabled.Value || availability.pickupSearchQuote != "Unavailable for Pickup")
                    {
                        string storeName = store.address.address;
                        if (_notifier != null)
                        {
                            _notifier.NotifyAvailability(_pushApiKey, _productName, storeName);
                        }
                        Console.WriteLine("[{0}] {1} Available at {2}!!", DateTime.Now.ToString("g", CultureInfo.CreateSpecificCulture("en-us")), _productName, storeName);
                        foundProduct = true;
                    }
                }

                if (!foundProduct)
                {
                    Console.WriteLine("[{0}] No {1} found this time.", DateTime.Now.ToString("g", CultureInfo.CreateSpecificCulture("en-us")), _productName);
                }
            }
        }

        private static void SetStartupParameters()
        {
            _productName = ConfigurationManager.AppSettings["ProductName"];
            _modelNumber = ConfigurationManager.AppSettings["ModelNumber"];
            _zipCode = ConfigurationManager.AppSettings["ZipCode"];
            _pushApiKey = ConfigurationManager.AppSettings["PushApiKey"];
            string pushMethod = ConfigurationManager.AppSettings["PushMethod"];
            string intervalSecondsText = ConfigurationManager.AppSettings["IntervalSeconds"];
            int intervalSeconds;
            if (Int32.TryParse(intervalSecondsText, out intervalSeconds))
            {
                _intervalSeconds = intervalSeconds;
            }
            string numberToSearchText = ConfigurationManager.AppSettings["NumberToSearch"];
            int numberToSearch;
            if (Int32.TryParse(numberToSearchText, out numberToSearch))
            {
                _numberToSearch = numberToSearch;
            }

            if (String.IsNullOrWhiteSpace(_zipCode)
                || String.IsNullOrWhiteSpace(_modelNumber)
                || !_intervalSeconds.HasValue)
            {
                throw new FriendlyException("You must specify the zip code, model number, number of stores to search, and interval seconds in the application configuration file.");
            }

            _notifier = NotificationFactory.CreateNotifier(pushMethod);

            Console.WriteLine("Product Name: {0}", _productName);
            Console.WriteLine("Model #: {0}", _modelNumber);
            Console.WriteLine("Zip Code: {0}", _zipCode);
            Console.WriteLine("Push Method: {0}", pushMethod ?? "Not Present");
            Console.WriteLine("Notifier from Push Method: {0}", _notifier != null ? _notifier.Name : "Not Present");
        }
    }
}
