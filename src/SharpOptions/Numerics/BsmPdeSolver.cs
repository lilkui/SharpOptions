using System.Buffers;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using CSparse.Double;
using CSparse.Double.Factorization.MKL;
using CSparse.Interop.MKL.Pardiso;
using CSparse.Storage;

namespace SharpOptions.Numerics;

public class BsmPdeSolver(int nSpace, int nTime)
{
    public void Solve(
        Span2D<double> matV,
        double[] vecS,
        double sMin,
        double sMax,
        double tMax,
        double sigma,
        double r,
        double q,
        FiniteDifferenceScheme scheme)
    {
        switch (scheme)
        {
            case FiniteDifferenceScheme.FullyImplicit:
                SolveImplicit(matV, vecS, sMin, sMax, tMax, sigma, r, q);
                break;
            case FiniteDifferenceScheme.CrankNicolson:
                break;
            default:
                ThrowHelper.ThrowArgumentException(nameof(scheme));
                break;
        }
    }

    private void SolveImplicit(Span2D<double> matV, double[] vecS, double sMin, double sMax, double tMax, double sigma, double r, double q)
    {
        var b = r - q;
        var sigma2 = sigma * sigma;
        var ds = (sMax - sMin) / (nSpace - 1);
        var dt = tMax / (nTime - 1);

        var vecL = vecS[1..^1].Select(s => 0.5 * s / ds * (b - sigma2 * s / ds) * dt).ToArray();
        var vecC = vecS[1..^1].Select(s => 1 + (sigma2 * s * s / ds / ds + r) * dt).ToArray();
        var vecU = vecS[1..^1].Select(s => -0.5 * s / ds * (b + s / ds * sigma2) * dt).ToArray();

        double[] columnMajor = [.. vecL[1..], 0, .. vecC, 0, .. vecU[..^1]];
        var length = nSpace - 2;
        var diagonals = DenseMatrix.OfColumnMajor(length, 3, columnMajor);
        var matA = CompressedColumnStorage<double>.OfDiagonals(diagonals, [-1, 0, 1], length, length);
        var offset = Vector.Create(length, 0);

        var solver = new Pardiso((SparseMatrix)matA, PardisoMatrixType.RealNonsymmetric);
        solver.Options.IterativeRefinement = 0;

        for (var j = nTime - 2; j >= 0; j--)
        {
            offset[0] = vecL[0] * matV[0, j];
            offset[^1] = vecU[^1] * matV[^1, j];

            var rhs = ArrayPool<double>.Shared.Rent(length);
            for (var i = 1; i < nSpace - 1; i++)
            {
                rhs[i - 1] = matV[i, j + 1] - offset[i - 1];
            }

            var x = ArrayPool<double>.Shared.Rent(length);
            solver.Solve(rhs, x);

            for (var i = 1; i < nSpace - 1; i++)
            {
                matV[i, j] = x[i - 1];
            }

            ArrayPool<double>.Shared.Return(rhs);
            ArrayPool<double>.Shared.Return(x);
        }
    }
}