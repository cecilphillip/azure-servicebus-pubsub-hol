using System;

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
