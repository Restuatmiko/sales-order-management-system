namespace SalesOrderService.DTOs
{
    public class SalesOrderResponse
    {
        public int SalesSoId { get; set; }

        public string SoNo { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal GrandTotal { get; set; }

        public List<SalesOrderItemResponse> Items { get; set; } = new();
    }
}