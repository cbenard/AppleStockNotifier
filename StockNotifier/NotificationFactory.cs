using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StockNotifier
{
    public static class NotificationFactory
    {
        private static Type[] _iNotifierTypes;

        static NotificationFactory()
        {
            _iNotifierTypes = (from t in Assembly.GetExecutingAssembly().GetExportedTypes()
                                 where !t.IsInterface && !t.IsAbstract
                                 where typeof(INotifier).IsAssignableFrom(t)
                                 select t).ToArray();
        }

        internal static INotifier CreateNotifier(string pushMethod)
        {
            if (String.IsNullOrWhiteSpace(pushMethod))
            {
                return null;
            }

            INotifier notifier = _iNotifierTypes
                .Where(t => String.Equals(t.Name, pushMethod, StringComparison.OrdinalIgnoreCase))
                .Select(t => (INotifier)Activator.CreateInstance(t))
                .FirstOrDefault();

            return notifier;
        }
    }
}
