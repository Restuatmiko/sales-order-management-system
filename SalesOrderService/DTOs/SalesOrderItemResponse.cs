namespace SalesOrderService.DTOs
{
    public class SalesOrderItemResponse
    {
        public int SalesOrderLineItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Total { get; set; }
    }
}