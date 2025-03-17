using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.OrderService.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderId { get; set; } 

        [Required]
        public string CustomerName { get; set; } 

        [Required]
        public List<int> ProductIds { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } 

        [Required]
        public string Status { get; set; } = "pending"; // "pending", "blocked", "completed", "cancelled"
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
