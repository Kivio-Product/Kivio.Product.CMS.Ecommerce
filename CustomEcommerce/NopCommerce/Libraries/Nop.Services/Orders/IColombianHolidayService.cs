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

    /// <summary>
    /// Checks if a specific date is a working day (not weekend and not holiday)
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <returns>True if the date is a working day, false otherwise</returns>
    bool IsWorkingDay(DateTime date);

    /// <summary>
    /// Gets all Colombian holidays for a specific year
    /// </summary>
    /// <param name="year">Year to get holidays for</param>
    /// <returns>List of holiday dates with their names</returns>
    List<(DateTime Date, string Name)> GetHolidaysForYear(int year);

    /// <summary>
    /// Gets the next working day from a given date
    /// </summary>
    /// <param name="fromDate">Starting date</param>
    /// <returns>Next working day</returns>
    DateTime GetNextWorkingDay(DateTime fromDate);

    /// <summary>
    /// Gets all working days between two dates (inclusive)
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of working days</returns>
    List<DateTime> GetWorkingDaysBetween(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Checks if a specific date range contains any holidays or weekends
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>True if range contains non-working days, false otherwise</returns>
    bool HasNonWorkingDays(DateTime startDate, DateTime endDate);
}
