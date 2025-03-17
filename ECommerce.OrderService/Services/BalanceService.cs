using ECommerce.OrderService.Models;
using System.Text;
using System.Text.Json;

namespace Ecommerce.OrderService.Services;

public class BalanceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BalanceService> _logger;
    private const string BalanceApiUrl = "https://balance-management-pi44.onrender.com/api/balance";

    public BalanceService(HttpClient httpClient, ILogger<BalanceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> PreorderPayment(PreorderRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{BalanceApiUrl}/preorder", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Preorder failed: {responseContent}");
                return false;
            }

            var jsonResponse = JsonSerializer.Deserialize<PreorderResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return jsonResponse?.Success == true && jsonResponse.Data?.Order?.Status == "blocked";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Preorder failed with exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CompletePayment(CompletePaymentRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{BalanceApiUrl}/complete", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Complete payment failed: {responseContent}");
                return false;
            }

            var jsonResponse = JsonSerializer.Deserialize<CompletePaymentResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return jsonResponse?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Complete payment failed with exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelPayment(CancelPaymentRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{BalanceApiUrl}/cancel", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Cancel payment failed: {responseContent}");
                return false;
            }

            var jsonResponse = JsonSerializer.Deserialize<CancelPaymentResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (jsonResponse?.Success == true)
            {
                _logger.LogInformation($"Payment cancellation successful for Order ID: {request.OrderId}");
                return true;
            }
            else
            {
                _logger.LogWarning($"Payment cancellation failed: {jsonResponse?.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Cancel payment failed with exception: {ex.Message}");
            return false;
        }
    }
}
