using System.Text.Json.Serialization;

namespace FrontEnd.Models
{
    public class SalesOrderItemResponse
    {
        [JsonPropertyName("salesOrderItemId")]
        public int SALES_SO_LITEM_ID { get; set; }

        [JsonPropertyName("salesSoId")]
        public int SALES_SO_ID { get; set; }

        [JsonPropertyName("itemName")]
        public string ITEM_NAME { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int QUANTITY { get; set; }

        [JsonPropertyName("price")]
        public decimal PRICE { get; set; }

        [JsonPropertyName("total")]
        public decimal TOTAL { get; set; }
    }
}