using SharpOptions.Calendars;

namespace SharpOptions.Utils;

public static class DateTimeUtils
{
    public static bool IsTradingDay(this DateTime value, ITradingCalendar calendar)
    {
        return value.DayOfWeek != DayOfWeek.Saturday && value.DayOfWeek != DayOfWeek.Sunday && !calendar.Holidays.Contains(value);
    }

    public static int CountTradingDays(DateTime startDate, DateTime endDate, ITradingCalendar calendar)
    {
        var days = ((endDate - startDate).Days * 5 - (startDate.DayOfWeek - endDate.DayOfWeek) * 2) / 7;

        if (endDate.DayOfWeek == DayOfWeek.Saturday)
        {
            days--;
        }

        if (startDate.DayOfWeek == DayOfWeek.Sunday)
        {
            days--;
        }

        days -= calendar.Holidays.Count(date => date >= startDate && date <= endDate);

        return days;
    }

    public static DateTime[] CreateArrayOfTradingDays(DateTime startDate, DateTime endDate, ITradingCalendar calendar)
    {
        var dates = new List<DateTime>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.IsTradingDay(calendar))
            {
                dates.Add(date);
            }
        }

        return dates.ToArray();
    }
}