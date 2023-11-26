using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using SharpOptions.Numerics;
using SharpOptions.Options;

namespace SharpOptions.PricingEngines;

public class FdVanillaEngine(VanillaOption option, int nSpace, int nTime) : PricingEngine
{
    private double _dS;
    private double[]? _vecS;
    private double[]? _vecV;

    public override double ValueAt(double s)
    {
        return LinearSpline.InterpolateSorted(_vecS, _vecV).Interpolate(s);
    }

    public override double DeltaAt(double s)
    {
        var spline = LinearSpline.InterpolateSorted(_vecS, _vecV);
        var vu = spline.Interpolate(s + _dS);
        var vd = spline.Interpolate(s - _dS);
        return (vu - vd) / (2 * _dS);
    }

    public override double GammaAt(double s)
    {
        var spline = LinearSpline.InterpolateSorted(_vecS, _vecV);
        var vm = spline.Interpolate(s);
        var vu = spline.Interpolate(s + _dS);
        var vd = spline.Interpolate(s - _dS);
        return (vu - 2 * vm + vd) / (_dS * _dS);
    }

    public void Calculate()
    {
        var k = option.Strike;
        var r = option.RiskFreeRate;
        var q = option.DividendYield;
        var sigma = option.Volatility;
        var omega = option.OptionType == OptionType.Call ? 1 : -1;

        const double sMin = 0;
        var sMax = 4 * k;
        var tMax = YearsToMaturity(option.MaturityDate);

        var vecS = Generate.LinearSpaced(nSpace, sMin, sMax);
        var vecT = Generate.LinearSpaced(nTime, 0, tMax);

        Span2D<double> matV = new double[nSpace, nTime];

        // set terminal condition
        var payoff = vecS.Select(s => Math.Max(omega * (s - k), 0)).ToArray();
        matV.GetColumn(nTime - 1).TryCopyFrom(payoff);

        // set boundary condition
        switch (option.OptionType)
        {
            case OptionType.Call:
                matV.GetRow(nSpace - 1).TryCopyFrom(vecT.Select(t => sMax * Math.Exp(-q * (tMax - t)) - k * Math.Exp(-r * (tMax - t))).ToArray());
                matV.GetRow(0).Fill(0);
                break;
            case OptionType.Put:
                matV.GetRow(nSpace - 1).Fill(0);
                matV.GetRow(0).TryCopyFrom(vecT.Select(t => k * Math.Exp(-r * (tMax - t)) - sMin * Math.Exp(-q * (tMax - t))).ToArray());
                break;
            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }

        var solver = new BsmPdeSolver(nSpace, nTime);
        solver.Solve(matV, vecS, sMin, sMax, tMax, sigma, r, q, FiniteDifferenceScheme.FullyImplicit);

        _dS = (sMax - sMin) / (nSpace - 1);
        _vecS = vecS;
        _vecV = matV.GetColumn(0).ToArray();
    }
}