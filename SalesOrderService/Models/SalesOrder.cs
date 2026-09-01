using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesOrderService.Models
{
    [Table("SALES_SO")]
    public class SalesOrder
    {
        [Key]
        [Column("SALES_SO_ID")]
        public int SALES_SO_ID { get; set; }

        [Column("SO_NO")]
        public string SO_NO { get; set; } = string.Empty;

        [Column("ORDER_DATE")]
        public DateTime ORDER_DATE { get; set; }

        [Column("COM_CUSTOMER_ID")]
        public int COM_CUSTOMER_ID { get; set; }

        [Column("ADDRESS")]
        public string ADDRESS { get; set; } = string.Empty;

        public List<SalesOrderLineItem> Items { get; set; } = new();
    }
}