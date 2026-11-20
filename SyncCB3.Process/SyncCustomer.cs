using SyncCB3.DataAceess;
using SyncCB3.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncCB3.Process
{
    
    public class SyncCustomer
    {
        DAOSYNC dataaccessSYNC = new DAOSYNC();
        public List<customer> GetCustomersSyncCB3(string CveCiudad)
        {
            List<customer> result = new List<customer>();
            dataaccessSYNC = new DAOSYNC();
            result = dataaccessSYNC.GetCustomersSyncCB3(CveCiudad);

            return result;
        }
    }
}
