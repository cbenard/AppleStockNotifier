using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace StockNotifier
{
    public class FriendlyException : Exception
    {
        public FriendlyException(string message)
            : base(message)
        {
        }
    }
}
