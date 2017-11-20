using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderMessageReceiver
{
    public class Order
    {
        public readonly string OrderID = Guid.NewGuid().ToString("N");
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ItemName { get; set; }
        public string CustomerName { get; set; }
    }
}
