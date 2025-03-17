using ECommerce.OrderService.Interfaces;
using ECommerce.OrderService.Models;
using Polly;
using Polly.Wrap;
using System.Text;
using System.Text.Json;

namespace Ecommerce.OrderService.Services;

public class BalanceService : IBalanceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BalanceService> _logger;
    private readonly AsyncPolicyWrap<bool> _preorderPolicy;
    private readonly AsyncPolicyWrap<bool> _completePolicy;
    private readonly AsyncPolicyWrap<bool> _cancelPolicy;
    private const string BalanceApiUrl = "https://balance-management-pi44.onrender.com/api/balance";

    public BalanceService(HttpClient httpClient, ILogger<BalanceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Retry Policy: Try 3 times, wait 2 seconds
        var retryPolicy = Policy<bool>.Handle<HttpRequestException>()
            .OrResult(r => !r) // If it returns false, retry.
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2),
                (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retrying ({retryCount}) after {timeSpan.TotalSeconds} seconds...");
                });

        // Circuit Breaker: Wait 30 seconds after 2 unsuccessful calls
        var circuitBreakerPolicy = Policy<bool>.Handle<HttpRequestException>()
            .OrResult(r => !r) // If it returns false, trigger circuit breaker
            .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30),
                (ex, duration) =>
                {
                    _logger.LogError($"Circuit broken! Waiting {duration.TotalSeconds} seconds...");
                },
                () => _logger.LogInformation("Circuit reset!"));

        // Fallback: Return false on error
        var fallbackPolicy = Policy<bool>.Handle<HttpRequestException>()
            .OrResult(r => !r)
            .FallbackAsync(false, (result, context) =>
            {
                _logger.LogError("Balance service failed! Returning fallback value...");
                return Task.CompletedTask;
            });

        // Combine policies (Fallback -> Retry -> Circuit Breaker)
        _preorderPolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
        _completePolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
        _cancelPolicy = Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
    }

    public async Task<bool> PreorderPaymentAsync(PreorderRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            return await _preorderPolicy.ExecuteAsync(async () =>
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Preorder failed with exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CompletePaymentAsync(CompletePaymentRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            return await _completePolicy.ExecuteAsync(async () =>
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Complete payment failed with exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelPaymentAsync(CancelPaymentRequest request)
    {
        var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        try
        {
            return await _cancelPolicy.ExecuteAsync(async () =>
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Cancel payment failed with exception: {ex.Message}");
            return false;
        }
    }
}
