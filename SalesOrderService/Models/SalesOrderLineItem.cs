using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SalesOrderService.Models
{
    [Table("SALES_SO_LITEM")]
    public class SalesOrderLineItem
    {
        [Key]
        [Column("SALES_SO_LITEM_ID")]
        public int SALES_SO_LITEM_ID { get; set; }

        [Column("SALES_SO_ID")]
        public int SALES_SO_ID { get; set; }

        [Column("ITEM_NAME")]
        public string ITEM_NAME { get; set; } = string.Empty;

        [Column("QUANTITY")]
        public int QUANTITY { get; set; }

        [Column("PRICE")]
        public decimal PRICE { get; set; }

        [JsonIgnore]
        public SalesOrder? SalesOrder { get; set; }
    }
}