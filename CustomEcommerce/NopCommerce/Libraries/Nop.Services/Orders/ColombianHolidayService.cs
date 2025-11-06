using System.Text.Json;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;

namespace Nop.Services.Orders;

/// <summary>
/// Colombian holiday service implementation
/// Handles Colombian holidays calculation including "Ley de Emiliani" (movable holidays)
/// Centralized service that reads holidays from JSON configuration file
/// </summary>
public partial class ColombianHolidayService : IColombianHolidayService
{
    private readonly ISettingService _settingService;
    private readonly INopFileProvider _fileProvider;
    private ColombianHolidayData? _holidayData;
    private readonly object _lock = new();

    public ColombianHolidayService(
        ISettingService settingService,
        INopFileProvider fileProvider)
    {
        _settingService = settingService;
        _fileProvider = fileProvider;
    }

    /// <summary>
    /// Loads holiday data from JSON file (singleton pattern with lazy loading)
    /// </summary>
    private ColombianHolidayData GetHolidayData()
    {
        if (_holidayData != null)
            return _holidayData;

        lock (_lock)
        {
            if (_holidayData != null)
                return _holidayData;

            try
            {
                var filePath = _fileProvider.MapPath("~/App_Data/colombian-holidays.json");
                
                if (!_fileProvider.FileExists(filePath))
                {
                    // If file doesn't exist, use default configuration
                    _holidayData = GetDefaultHolidayData();
                    return _holidayData;
                }

                var jsonContent = _fileProvider.ReadAllText(filePath, System.Text.Encoding.UTF8);
                _holidayData = JsonSerializer.Deserialize<ColombianHolidayData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? GetDefaultHolidayData();
            }
            catch
            {
                // If there's any error reading the file, use default configuration
                _holidayData = GetDefaultHolidayData();
            }

            return _holidayData;
        }
    }

    /// <summary>
    /// Gets default holiday configuration (fallback if JSON file is not available)
    /// </summary>
    private ColombianHolidayData GetDefaultHolidayData()
    {
        return new ColombianHolidayData
        {
            FixedHolidays = new List<FixedHoliday>
            {
                new() { Month = 1, Day = 1, Name = "Año Nuevo", Description = "New Year's Day" },
                new() { Month = 5, Day = 1, Name = "Día del Trabajo", Description = "Labor Day" },
                new() { Month = 7, Day = 20, Name = "Día de la Independencia", Description = "Independence Day" },
                new() { Month = 8, Day = 7, Name = "Batalla de Boyacá", Description = "Battle of Boyacá" },
                new() { Month = 12, Day = 8, Name = "Inmaculada Concepción", Description = "Immaculate Conception" },
                new() { Month = 12, Day = 25, Name = "Navidad", Description = "Christmas Day" }
            },
            MovableHolidays = new List<MovableHoliday>
            {
                new() { Month = 1, Day = 6, Name = "Reyes Magos", Description = "Epiphany" },
                new() { Month = 3, Day = 19, Name = "San José", Description = "Saint Joseph's Day" },
                new() { Month = 6, Day = 29, Name = "San Pedro y San Pablo", Description = "Saint Peter and Saint Paul" },
                new() { Month = 8, Day = 15, Name = "Asunción de la Virgen", Description = "Assumption of Mary" },
                new() { Month = 10, Day = 12, Name = "Día de la Raza", Description = "Columbus Day" },
                new() { Month = 11, Day = 1, Name = "Todos los Santos", Description = "All Saints' Day" },
                new() { Month = 11, Day = 11, Name = "Independencia de Cartagena", Description = "Independence of Cartagena" }
            },
            EasterBasedHolidays = new List<EasterBasedHoliday>
            {
                new() { DaysOffset = -3, Name = "Jueves Santo", Description = "Maundy Thursday", MoveToMonday = false },
                new() { DaysOffset = -2, Name = "Viernes Santo", Description = "Good Friday", MoveToMonday = false },
                new() { DaysOffset = 43, Name = "Ascensión del Señor", Description = "Ascension of Jesus", MoveToMonday = true },
                new() { DaysOffset = 64, Name = "Corpus Christi", Description = "Corpus Christi", MoveToMonday = true },
                new() { DaysOffset = 71, Name = "Sagrado Corazón", Description = "Sacred Heart", MoveToMonday = true }
            }
        };
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
        var data = GetHolidayData();

        // Add fixed holidays
        foreach (var holiday in data.FixedHolidays)
        {
            holidays.Add(new DateTime(year, holiday.Month, holiday.Day));
        }

        // Add movable holidays (Ley de Emiliani - moved to next Monday)
        foreach (var holiday in data.MovableHolidays)
        {
            var originalDate = new DateTime(year, holiday.Month, holiday.Day);
            holidays.Add(MoveToNextMonday(originalDate));
        }

        // Add Easter-based holidays
        var easter = CalculateEaster(year);
        foreach (var holiday in data.EasterBasedHolidays)
        {
            var holidayDate = easter.AddDays(holiday.DaysOffset);
            if (holiday.MoveToMonday)
            {
                holidayDate = MoveToNextMonday(holidayDate);
            }
            holidays.Add(holidayDate);
        }

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

    /// <summary>
    /// Checks if a specific date is a working day (not weekend and not holiday)
    /// </summary>
    public bool IsWorkingDay(DateTime date)
    {
        return !IsWeekend(date) && !IsColombianHoliday(date);
    }

    /// <summary>
    /// Gets all Colombian holidays for a specific year with their names
    /// </summary>
    public List<(DateTime Date, string Name)> GetHolidaysForYear(int year)
    {
        var result = new List<(DateTime Date, string Name)>();
        var data = GetHolidayData();

        // Add fixed holidays
        foreach (var holiday in data.FixedHolidays)
        {
            result.Add((new DateTime(year, holiday.Month, holiday.Day), holiday.Name));
        }

        // Add movable holidays
        foreach (var holiday in data.MovableHolidays)
        {
            var originalDate = new DateTime(year, holiday.Month, holiday.Day);
            var movedDate = MoveToNextMonday(originalDate);
            result.Add((movedDate, holiday.Name));
        }

        // Add Easter-based holidays
        var easter = CalculateEaster(year);
        foreach (var holiday in data.EasterBasedHolidays)
        {
            var holidayDate = easter.AddDays(holiday.DaysOffset);
            if (holiday.MoveToMonday)
            {
                holidayDate = MoveToNextMonday(holidayDate);
            }
            result.Add((holidayDate, holiday.Name));
        }

        return result.OrderBy(h => h.Date).ToList();
    }

    /// <summary>
    /// Gets the next working day from a given date
    /// </summary>
    public DateTime GetNextWorkingDay(DateTime fromDate)
    {
        var currentDate = fromDate.AddDays(1).Date;
        
        while (!IsWorkingDay(currentDate))
        {
            currentDate = currentDate.AddDays(1);
        }

        return currentDate;
    }

    /// <summary>
    /// Gets all working days between two dates (inclusive)
    /// </summary>
    public List<DateTime> GetWorkingDaysBetween(DateTime startDate, DateTime endDate)
    {
        var workingDays = new List<DateTime>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            if (IsWorkingDay(currentDate))
            {
                workingDays.Add(currentDate);
            }
            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    /// <summary>
    /// Checks if a specific date range contains any holidays or weekends
    /// </summary>
    public bool HasNonWorkingDays(DateTime startDate, DateTime endDate)
    {
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            if (!IsWorkingDay(currentDate))
            {
                return true;
            }
            currentDate = currentDate.AddDays(1);
        }

        return false;
    }
}
