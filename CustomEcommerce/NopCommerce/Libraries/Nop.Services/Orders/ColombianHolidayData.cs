namespace Nop.Services.Orders;

/// <summary>
/// Represents Colombian holiday configuration data
/// </summary>
public partial class ColombianHolidayData
{
    /// <summary>
    /// Fixed holidays (always on the same date every year)
    /// </summary>
    public List<FixedHoliday> FixedHolidays { get; set; } = new();

    /// <summary>
    /// Movable holidays based on specific dates (Ley de Emiliani - moved to next Monday)
    /// </summary>
    public List<MovableHoliday> MovableHolidays { get; set; } = new();

    /// <summary>
    /// Easter-based holidays (calculated based on Easter Sunday)
    /// </summary>
    public List<EasterBasedHoliday> EasterBasedHolidays { get; set; } = new();
}

/// <summary>
/// Represents a fixed holiday (always on the same date)
/// </summary>
public partial class FixedHoliday
{
    /// <summary>
    /// Month (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Day (1-31)
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Holiday name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a movable holiday (moved to next Monday if not on Monday)
/// </summary>
public partial class MovableHoliday
{
    /// <summary>
    /// Original month (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Original day (1-31)
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Holiday name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an Easter-based holiday (calculated relative to Easter Sunday)
/// </summary>
public partial class EasterBasedHoliday
{
    /// <summary>
    /// Days offset from Easter Sunday (negative for before, positive for after)
    /// </summary>
    public int DaysOffset { get; set; }

    /// <summary>
    /// Holiday name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this holiday should be moved to next Monday (Ley de Emiliani)
    /// </summary>
    public bool MoveToMonday { get; set; }
}
