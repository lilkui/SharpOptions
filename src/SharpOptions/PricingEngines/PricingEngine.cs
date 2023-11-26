using SharpOptions.Calendars;
using SharpOptions.Utils;

namespace SharpOptions.PricingEngines;

public abstract class PricingEngine(ITradingCalendar calendar, DateTime valuationDate)
{
    protected PricingEngine()
        : this(SseTradingCalendar.Instance, DateTime.Today)
    {
    }

    public ITradingCalendar Calendar { get; set; } = calendar;

    public DateTime ValuationDate { get; set; } = valuationDate;

    public virtual double ValueAt(double s)
    {
        return double.NaN;
    }

    public virtual double DeltaAt(double s)
    {
        return double.NaN;
    }

    public virtual double GammaAt(double s)
    {
        return double.NaN;
    }

    public virtual double ThetaAt(double s)
    {
        return double.NaN;
    }

    public virtual double VegaAt(double s)
    {
        return double.NaN;
    }

    public virtual double RhoAt(double s)
    {
        return double.NaN;
    }

    public virtual double VannaAt(double s)
    {
        return double.NaN;
    }

    public virtual double CharmAt(double s)
    {
        return double.NaN;
    }

    protected double YearsToMaturity(DateTime maturityDate)
    {
        var days = DateTimeUtils.CountTradingDays(ValuationDate, maturityDate, Calendar);
        return (double)days / Calendar.AnnualTradingDays;
    }
}