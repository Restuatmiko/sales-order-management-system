using System.Text.Json.Serialization;

namespace FrontEnd.Models
{
    public class CreateSalesOrderRequest
    {
        [JsonPropertyName("soNo")]
        public string SO_NO { get; set; } = string.Empty;

        [JsonPropertyName("orderDate")]
        public DateTime? ORDER_DATE { get; set; }

        [JsonPropertyName("customerId")]
        public int? COM_CUSTOMER_ID { get; set; }

        [JsonPropertyName("address")]
        public string ADDRESS { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
    }
}