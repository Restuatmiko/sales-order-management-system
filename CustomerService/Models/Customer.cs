using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerService.Models
{
    [Table("COM_CUSTOMER")]
    public class Customer
    {
        [Key]
        [Column("COM_CUSTOMER_ID")]
        public int COM_CUSTOMER_ID { get; set; }

        [Column("CUSTOMER_NAME")]
        public string CUSTOMER_NAME { get; set; } = string.Empty;
    }
}