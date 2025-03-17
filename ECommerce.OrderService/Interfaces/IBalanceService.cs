using ECommerce.OrderService.Models;
using System.Threading.Tasks;

namespace ECommerce.OrderService.Interfaces
{
    public interface IBalanceService
    {
        Task<bool> PreorderPaymentAsync(PreorderRequest request);

        Task<bool> CompletePaymentAsync(CompletePaymentRequest request);

        Task<bool> CancelPaymentAsync(CancelPaymentRequest request);
    }
}
