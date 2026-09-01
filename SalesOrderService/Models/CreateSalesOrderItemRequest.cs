using System.Text.Json.Serialization;

namespace SalesOrderService.Models
{
    public class CreateSalesOrderItemRequest
    {
        [JsonPropertyName("itemName")]
        public string ITEM_NAME { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int QUANTITY { get; set; }

        [JsonPropertyName("price")]
        public decimal PRICE { get; set; }
    }
}