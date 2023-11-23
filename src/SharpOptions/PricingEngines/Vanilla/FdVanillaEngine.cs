using System.Buffers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using CSparse.Double;
using CSparse.Double.Factorization.MKL;
using CSparse.Interop.MKL.Pardiso;
using CSparse.Storage;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
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

    public void SolvePde()
    {
        var k = option.Strike;
        var r = option.RiskFreeRate;
        var q = option.DividendYield;
        var sigma = option.Volatility;
        var omega = option.OptionType == OptionType.Call ? 1 : -1;

        // solution domain
        const double sMin = 0;
        var sMax = 6 * k;
        var tMax = YearsToMaturity(option.MaturityDate);

        // spatial and time discretization
        var vecS = Generate.LinearSpaced(nSpace, sMin, sMax);
        var ds = (sMax - sMin) / (nSpace - 1);
        var vecT = Generate.LinearSpaced(nTime, 0, tMax);
        var dt = tMax / (nTime - 1);

        // grid construction
        Span2D<double> grid = new double[nSpace, nTime];

        // set terminal condition
        var payoff = vecS.Select(s => Math.Max(omega * (s - k), 0)).ToArray();
        grid.GetColumn(nTime - 1).TryCopyFrom(payoff);

        // set boundary condition
        switch (option.OptionType)
        {
            case OptionType.Call:
                grid.GetRow(nSpace - 1).TryCopyFrom(vecT.Select(t => sMax * Math.Exp(-q * (tMax - t)) - k * Math.Exp(-r * (tMax - t))).ToArray());
                grid.GetRow(0).Fill(0);
                break;
            case OptionType.Put:
                grid.GetRow(nSpace - 1).Fill(0);
                grid.GetRow(0).TryCopyFrom(vecT.Select(t => k * Math.Exp(-r * (tMax - t)) - sMin * Math.Exp(-q * (tMax - t))).ToArray());
                break;
            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }

        // set coefficients
        var vecL = vecS[1..^1].Select(s => 0.5 * s / ds * (r - q - s / ds * sigma * sigma) * dt).ToArray();
        var vecC = vecS[1..^1].Select(s => 1 + (r + s * s / ds / ds * sigma * sigma) * dt).ToArray();
        var vecU = vecS[1..^1].Select(s => -0.5 * s / ds * (r - q + s / ds * sigma * sigma) * dt).ToArray();

        // set matrix
        double[] columnMajor = [..vecL[1..], 0, ..vecC, 0, ..vecU[..^1]];
        var length = nSpace - 2;
        var diagonals = DenseMatrix.OfColumnMajor(length, 3, columnMajor);
        var matA = CompressedColumnStorage<double>.OfDiagonals(diagonals, [-1, 0, 1], length, length);
        var offset = Vector.Create(length, 0);

        var solver = new Pardiso((SparseMatrix)matA, PardisoMatrixType.RealNonsymmetric);
        solver.Options.IterativeRefinement = 0;

        for (var j = nTime - 2; j >= 0; j--)
        {
            offset[0] = vecL[0] * grid[0, j];
            offset[^1] = vecU[^1] * grid[^1, j];

            var rhs = ArrayPool<double>.Shared.Rent(length);
            for (var i = 1; i < nSpace - 1; i++)
            {
                rhs[i - 1] = grid[i, j + 1] - offset[i - 1];
            }

            var x = ArrayPool<double>.Shared.Rent(length);
            solver.Solve(rhs, x);

            for (var i = 1; i < nSpace - 1; i++)
            {
                grid[i, j] = x[i - 1];
            }

            ArrayPool<double>.Shared.Return(rhs);
            ArrayPool<double>.Shared.Return(x);
        }

        _dS = ds;
        _vecS = vecS;
        _vecV = grid.GetColumn(0).ToArray();
    }
}