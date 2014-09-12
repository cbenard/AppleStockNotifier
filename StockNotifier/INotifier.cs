using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockNotifier
{
    public interface INotifier
    {
        string Name { get; }
        void NotifyAvailability(string apiKey, string productName, string storeName);
    }
}
