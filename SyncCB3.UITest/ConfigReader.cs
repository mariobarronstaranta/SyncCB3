using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace SyncCB3.UITest
{
    public class ConfigReader
    {
        public static string UrlPutCustomers => ConfigurationManager.AppSettings["UrlPutCustomers"];
        public static string UrlPutOrders => ConfigurationManager.AppSettings["UrlPutOrders"];
        public static string UrlGetOrders => ConfigurationManager.AppSettings["UrlGetOrders"];
    }
}
