using SharpOptions.Calendars;
using SharpOptions.Options;
using SharpOptions.Utils;
using TorchSharp;

namespace SharpOptions.PricingEngines;

public class McSnowballEngine : PricingEngine
{
    private readonly int _nPaths;
    private readonly SnowballOption _option;
    private readonly bool _useCuda;
    private MonteCarloSimulation? _mc;
    private DateTime[]? _tradingDates;

    public McSnowballEngine(SnowballOption option, int nPaths, bool useCuda)
    {
        _option = option;
        _nPaths = nPaths;
        _useCuda = useCuda;
        Device = useCuda & torch.cuda.is_available() ? torch.CUDA : torch.CPU;
    }

    public torch.Device Device { get; }

    public override double ValueAt(double s)
    {
        Initialize();

        using var d = torch.NewDisposeScope();

        var ttm = YearsToMaturity(_option.MaturityDate);
        var paths = s * _mc!.GeometricBrownianMotion(ttm, _option.RiskFreeRate - _option.DividendYield, _option.Volatility);
        var marginCost = _option.MarginInterestRate * _option.InitialMargin * ttm;
        var coupons = GetCoupons();
        var discountFactors = GetDiscountFactors();

        // autocall triggered
        var obsDateIndex = torch.TensorIndex.Tensor(
            _option.ObservationDates
                .Select(obsDate => (long)Array.IndexOf(_tradingDates, obsDate))
                .ToArray()); // the index of each element of ObservationDates within tradingDates
        var acPoints = paths[torch.TensorIndex.Colon, obsDateIndex] > _option.AutocallBarrier; // all points that breach the autocall barrier
        var acPaths = acPoints.any(1); // the paths where autocall triggered
        var acDatesIndex = acPoints[acPaths].@int().argmax(1); // the indices of the autocall dates
        var acValues = torch.eye(_option.ObservationDates.Length, device: _mc.Device)[acDatesIndex] * coupons * discountFactors;

        // knocks in, autocall never triggered
        var kiPaths = (paths < _option.KnockInBarrier).any(1);
        var kiValues = ((paths[kiPaths & ~acPaths, -1] - 1).clip(_option.MinNav - 1, 0) - marginCost) * discountFactors[-1];

        // naturally expires
        var expireValue = ((~kiPaths & ~acPaths).sum() - marginCost) * coupons[-1] * discountFactors[-1];

        return (acValues.sum() + kiValues.sum() + expireValue).ToDouble() / _mc.NumPaths;
    }

    public override double DeltaAt(double s)
    {
        const double ds = 0.001;
        var vu = ValueAt(s + ds);
        var vd = ValueAt(s - ds);
        return (vu - vd) / (2 * ds);
    }

    public override double GammaAt(double s)
    {
        const double ds = 0.001;
        var vm = ValueAt(s);
        var vu = ValueAt(s + ds);
        var vd = ValueAt(s - ds);
        return (vu - 2 * vm + vd) / (ds * ds);
    }

    private torch.Tensor GetCoupons()
    {
        var marginInterestRate = _option.MarginInterestRate * _option.InitialMargin;
        var timeToMaturity = YearsToMaturity(_option.MaturityDate);
        return torch.linspace(
            (_option.AnnualCouponRate - marginInterestRate) / 12,
            (_option.AnnualCouponRate - marginInterestRate) * timeToMaturity,
            (long)Math.Round(12 * timeToMaturity),
            device: Device)[torch.TensorIndex.Slice(_option.SkipMonths)];
    }

    private torch.Tensor GetDiscountFactors()
    {
        var discountFactors = new List<double>(_option.ObservationDates.Length);
        foreach (var date in _option.ObservationDates)
        {
            var dt = (double)(date - ValuationDate).Days;
            discountFactors.Add(Math.Exp(-1 * _option.RiskFreeRate * dt / 365));
        }

        return torch.tensor(discountFactors, device: Device);
    }

    private void Initialize()
    {
        if (_mc is null)
        {
            _tradingDates = DateTimeUtils.CreateArrayOfTradingDays(ValuationDate, _option.MaturityDate, SseTradingCalendar.Instance);
            _mc = new MonteCarloSimulation(_nPaths, _tradingDates.Length, _useCuda);
            _mc.GenerateRandomSamples();
        }
    }
}