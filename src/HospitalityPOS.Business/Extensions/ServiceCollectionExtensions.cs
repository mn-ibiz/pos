using Microsoft.Extensions.DependencyInjection;

namespace HospitalityPOS.Business.Extensions;

/// <summary>
/// Extension methods for registering business services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all business layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Services will be registered here as they are implemented
        // Example:
        // services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<IReceiptService, ReceiptService>();
        // services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
