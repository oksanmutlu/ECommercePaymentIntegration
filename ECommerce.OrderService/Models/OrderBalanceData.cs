namespace ECommerce.OrderService.Models
{
    public class OrderBalanceData
    {
        public Order Order { get; set; }
        public UpdatedBalance UpdatedBalance { get; set; }
    }

    public class UpdatedBalance
    {
        public string UserId { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal BlockedBalance { get; set; }
        public string Currency { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
