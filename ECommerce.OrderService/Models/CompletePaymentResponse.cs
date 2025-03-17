namespace ECommerce.OrderService.Models
{
    public class CompletePaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public OrderBalanceData Data { get; set; }
    }
}
