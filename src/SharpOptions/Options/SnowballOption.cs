namespace SharpOptions.Options;

public class SnowballOption : Option
{
    public required double AnnualCouponRate { get; set; }

    public required double AutocallBarrier { get; set; }

    public required double InitialMargin { get; set; }

    public required double KnockInBarrier { get; set; }

    public required double MarginInterestRate { get; set; }

    public required double MinNav { get; set; }

    public required DateTime[] ObservationDates { get; set; }

    public required int SkipMonths { get; set; }
}