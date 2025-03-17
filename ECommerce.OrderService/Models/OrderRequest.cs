namespace ECommerce.OrderService.Models
{
    public class OrderRequest
    {
        public string CustomerName { get; set; }
        public List<int> ProductIds { get; set; }
    }
}
