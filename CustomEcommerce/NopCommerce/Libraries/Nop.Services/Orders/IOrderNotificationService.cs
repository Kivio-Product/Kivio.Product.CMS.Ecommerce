using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

/// <summary>
/// Order notification service interface
/// </summary>
public interface IOrderNotificationService
{
    /// <summary>
    /// Send "order placed" notifications and save order notes
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SendOrderPlacedNotificationsAsync(Order order);
}
