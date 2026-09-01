namespace SalesOrderService.Models
{
    public class UpdateSalesOrderRequest
    {
        public string SO_NO { get; set; } = string.Empty;

        public DateTime? ORDER_DATE { get; set; }

        public int? COM_CUSTOMER_ID { get; set; }

        public string ADDRESS { get; set; } = string.Empty;

        public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
    }
}