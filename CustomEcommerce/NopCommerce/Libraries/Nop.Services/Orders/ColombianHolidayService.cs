using Nop.Services.Configuration;

namespace Nop.Services.Orders;

/// <summary>
/// Colombian holiday service implementation
/// Handles Colombian holidays calculation including "Ley de Emiliani" (movable holidays)
/// </summary>
public partial class ColombianHolidayService : IColombianHolidayService
{
    private readonly ISettingService _settingService;

    public ColombianHolidayService(ISettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Checks if checkout should be disabled based on current date in Colombia timezone (UTC-5)
    /// </summary>
    public async Task<bool> IsCheckoutDisabledByDateAsync()
    {
        // Check if the feature is enabled
        var isEnabled = await _settingService.GetSettingByKeyAsync<bool>("Orders.EnableColombianHolidaysValidation", false);
        if (!isEnabled)
            return false;

        // Get current time in Colombia (UTC-5)
        var colombiaTime = DateTime.UtcNow.AddHours(-5);
        
        return IsWeekend(colombiaTime) || IsColombianHoliday(colombiaTime);
    }

    /// <summary>
    /// Checks if a specific date is a weekend
    /// </summary>
    public bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Checks if a specific date is a Colombian holiday
    /// </summary>
    public bool IsColombianHoliday(DateTime date)
    {
        var holidays = GetColombianHolidays(date.Year);
        return holidays.Any(h => h.Date == date.Date);
    }

    /// <summary>
    /// Gets all Colombian holidays for a specific year
    /// Includes fixed holidays and movable holidays (Ley de Emiliani)
    /// </summary>
    private List<DateTime> GetColombianHolidays(int year)
    {
        var holidays = new List<DateTime>();

        // Fixed holidays (do not move to Monday)
        holidays.Add(new DateTime(year, 1, 1));   // Año Nuevo
        holidays.Add(new DateTime(year, 5, 1));   // Día del Trabajo
        holidays.Add(new DateTime(year, 7, 20));  // Día de la Independencia
        holidays.Add(new DateTime(year, 8, 7));   // Batalla de Boyacá
        holidays.Add(new DateTime(year, 12, 8));  // Inmaculada Concepción
        holidays.Add(new DateTime(year, 12, 25)); // Navidad

        // Movable holidays based on Easter (Holy Week)
        var easter = CalculateEaster(year);
        holidays.Add(easter.AddDays(-3));  // Jueves Santo
        holidays.Add(easter.AddDays(-2));  // Viernes Santo
        
        // Other movable holidays (moved to next Monday if not on Monday - Ley de Emiliani)
        holidays.Add(MoveToNextMonday(new DateTime(year, 1, 6)));   // Reyes Magos
        holidays.Add(MoveToNextMonday(new DateTime(year, 3, 19)));  // San José
        holidays.Add(MoveToNextMonday(easter.AddDays(43)));         // Ascensión del Señor
        holidays.Add(MoveToNextMonday(easter.AddDays(64)));         // Corpus Christi
        holidays.Add(MoveToNextMonday(easter.AddDays(71)));         // Sagrado Corazón
        holidays.Add(MoveToNextMonday(new DateTime(year, 6, 29)));  // San Pedro y San Pablo
        holidays.Add(MoveToNextMonday(new DateTime(year, 8, 15)));  // Asunción de la Virgen
        holidays.Add(MoveToNextMonday(new DateTime(year, 10, 12))); // Día de la Raza
        holidays.Add(MoveToNextMonday(new DateTime(year, 11, 1)));  // Todos los Santos
        holidays.Add(MoveToNextMonday(new DateTime(year, 11, 11))); // Independencia de Cartagena

        return holidays;
    }

    /// <summary>
    /// Calculates Easter Sunday for a given year using Meeus/Jones/Butcher algorithm
    /// </summary>
    private DateTime CalculateEaster(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateTime(year, month, day);
    }

    /// <summary>
    /// Moves a date to the next Monday if it's not already a Monday (Ley de Emiliani)
    /// </summary>
    private DateTime MoveToNextMonday(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Monday)
            return date;

        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0)
            daysUntilMonday = 7;

        return date.AddDays(daysUntilMonday);
    }
}
