using CommunityToolkit.Diagnostics;
using SharpOptions.Options;
using static System.Math;
using static MathNet.Numerics.Distributions.Normal;

namespace SharpOptions.PricingEngines;

public class AnalyticEuropeanEngine : PricingEngine
{
    private readonly VanillaOption _option;

    public AnalyticEuropeanEngine(VanillaOption option)
    {
        if (option.ExerciseType != ExerciseType.European)
        {
            ThrowHelper.ThrowArgumentException("AnalyticEuropeanEngine supports European options only.");
        }

        _option = option;
    }

    public override double ValueAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var k = _option.Strike;
        var r = _option.RiskFreeRate;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, d2) = D(s);
        return _option.OptionType switch
        {
            OptionType.Call => s * Exp((b - r) * t) * CDF(0, 1, d1) - k * Exp(-r * t) * CDF(0, 1, d2),
            OptionType.Put => k * Exp(-r * t) * CDF(0, 1, -d2) - s * Exp((b - r) * t) * CDF(0, 1, -d1),
            _ => ThrowHelper.ThrowInvalidOperationException<double>(),
        };
    }

    public override double DeltaAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var r = _option.RiskFreeRate;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, _) = D(s);
        return _option.OptionType switch
        {
            OptionType.Call => Exp((b - r) * t) * CDF(0, 1, d1),
            OptionType.Put => Exp((b - r) * t) * (CDF(0, 1, d1) - 1),
            _ => ThrowHelper.ThrowInvalidOperationException<double>(),
        };
    }

    public override double GammaAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var r = _option.RiskFreeRate;
        var v = _option.Volatility;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, _) = D(s);
        return PDF(0, 1, d1) * Exp((b - r) * t) / (s * v * Sqrt(t));
    }

    public override double ThetaAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var k = _option.Strike;
        var r = _option.RiskFreeRate;
        var v = _option.Volatility;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, d2) = D(s);
        return _option.OptionType switch
        {
            OptionType.Call => -s * PDF(0, 1, d1) * v * Exp((b - r) * t) / (2 * Sqrt(t)) - (b - r) * s * CDF(0, 1, d1) * Exp((b - r) * t) -
                               r * k * Exp(-r * t) * CDF(0, 1, d2),
            OptionType.Put => -s * PDF(0, 1, d1) * v * Exp((b - r) * t) / (2 * Sqrt(t)) + (b - r) * s * CDF(0, 1, -d1) * Exp((b - r) * t) +
                              r * k * Exp(-r * t) * CDF(0, 1, -d2),
            _ => ThrowHelper.ThrowInvalidOperationException<double>(),
        };
    }

    public override double VegaAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var r = _option.RiskFreeRate;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, _) = D(s);
        return s * Sqrt(t) * PDF(0, 1, d1) * Exp((b - r) * t);
    }

    public override double RhoAt(double s)
    {
        var k = _option.Strike;
        var r = _option.RiskFreeRate;
        var t = YearsToMaturity(_option.MaturityDate);

        var (_, d2) = D(s);
        return _option.OptionType switch
        {
            OptionType.Call => k * t * Exp(-r * t) * CDF(0, 1, d2),
            OptionType.Put => -k * t * Exp(-r * t) * CDF(0, 1, -d2),
            _ => ThrowHelper.ThrowInvalidOperationException<double>(),
        };
    }

    public override double VannaAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var r = _option.RiskFreeRate;
        var v = _option.Volatility;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, d2) = D(s);
        return -Exp((b - r) * t) * d2 / v * PDF(0, 1, d1);
    }

    public override double CharmAt(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var r = _option.RiskFreeRate;
        var v = _option.Volatility;
        var t = YearsToMaturity(_option.MaturityDate);

        var (d1, d2) = D(s);

        return _option.OptionType switch
        {
            OptionType.Call => -Exp((b - r) * t) * (PDF(0, 1, d1) * (b / (v * Sqrt(t)) - d2 / (2 * t)) + (b - r) * CDF(0, 1, d1)),
            OptionType.Put => -Exp((b - r) * t) * (PDF(0, 1, d1) * (b / (v * Sqrt(t)) - d2 / (2 * t)) - (b - r) * CDF(0, 1, -d1)),
            _ => ThrowHelper.ThrowInvalidOperationException<double>(),
        };
    }

    private (double D1, double D2) D(double s)
    {
        var b = _option.RiskFreeRate - _option.DividendYield;
        var k = _option.Strike;
        var v = _option.Volatility;
        var t = YearsToMaturity(_option.MaturityDate);

        var d1 = (Log(s / k) + (b + v * v / 2) * t) / (v * Sqrt(t));
        var d2 = d1 - v * Sqrt(t);
        return (d1, d2);
    }
}