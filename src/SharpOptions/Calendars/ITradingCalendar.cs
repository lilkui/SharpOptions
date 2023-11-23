namespace SharpOptions.Calendars;

public interface ITradingCalendar
{
    public DateTime[] Holidays { get; }

    public int AnnualTradingDays { get; }
}