using SharpOptions.Calendars;
using SharpOptions.Utils;

namespace SharpOptions.PricingEngines;

public abstract class PricingEngine
{
    public required ITradingCalendar Calendar { get; set; }

    public required DateTime ValuationDate { get; set; }

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