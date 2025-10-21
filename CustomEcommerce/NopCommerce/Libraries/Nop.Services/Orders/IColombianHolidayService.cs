namespace Nop.Services.Orders;

/// <summary>
/// Colombian holiday service interface
/// </summary>
public partial interface IColombianHolidayService
{
    /// <summary>
    /// Checks if checkout should be disabled based on current date
    /// (weekends and Colombian holidays) in Colombia timezone (UTC-5)
    /// </summary>
    /// <returns>True if checkout should be disabled, false otherwise</returns>
    Task<bool> IsCheckoutDisabledByDateAsync();

    /// <summary>
    /// Checks if a specific date is a Colombian holiday
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>True if the date is a Colombian holiday, false otherwise</returns>
    bool IsColombianHoliday(DateTime date);

    /// <summary>
    /// Checks if a specific date is a weekend (Saturday or Sunday)
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>True if the date is a weekend, false otherwise</returns>
    bool IsWeekend(DateTime date);
}
