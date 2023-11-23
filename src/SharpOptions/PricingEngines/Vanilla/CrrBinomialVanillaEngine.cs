using CommunityToolkit.Diagnostics;
using SharpOptions.Options;

namespace SharpOptions.PricingEngines;

public class CrrBinomialVanillaEngine(VanillaOption option, int numSteps) : PricingEngine
{
    public int NumSteps { get; set; } = numSteps;

    public override double ValueAt(double s)
    {
        var k = option.Strike;
        var sigma = option.Volatility;
        var z = option.OptionType == OptionType.Call ? 1 : -1;
        var dt = YearsToMaturity(option.MaturityDate) / NumSteps;
        var u = Math.Exp(sigma * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp((option.RiskFreeRate - option.DividendYield) * dt) - d) / (u - d);
        var df = Math.Exp(-option.RiskFreeRate * dt);

        var values = new double[NumSteps + 1];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Math.Max(0, z * (s * Math.Pow(u, i) * Math.Pow(d, NumSteps - i) - k));
        }

        switch (option.ExerciseType)
        {
            case ExerciseType.European:
                for (var j = NumSteps - 1; j >= 0; j--)
                {
                    for (var i = 0; i <= j; i++)
                    {
                        values[i] = (p * values[i + 1] + (1 - p) * values[i]) * df;
                    }
                }

                break;

            case ExerciseType.American:
                for (var j = NumSteps - 1; j >= 0; j--)
                {
                    for (var i = 0; i <= j; i++)
                    {
                        values[i] = Math.Max(z * (s * Math.Pow(u, i) * Math.Pow(d, j - 1) - k), (p * values[i + 1] + (1 - p) * values[i]) * df);
                    }
                }

                break;

            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }

        return values[0];
    }

    public override double DeltaAt(double s)
    {
        var k = option.Strike;
        var sigma = option.Volatility;
        var z = option.OptionType == OptionType.Call ? 1 : -1;
        var dt = YearsToMaturity(option.MaturityDate) / NumSteps;
        var u = Math.Exp(sigma * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp((option.RiskFreeRate - option.DividendYield) * dt) - d) / (u - d);
        var df = Math.Exp(-option.RiskFreeRate * dt);

        double delta = 0;
        var values = new double[NumSteps + 1];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Math.Max(0, z * (s * Math.Pow(u, i) * Math.Pow(d, NumSteps - i) - k));
        }

        switch (option.ExerciseType)
        {
            case ExerciseType.European:
            {
                for (var j = NumSteps - 1; j >= 0; j--)
                {
                    for (var i = 0; i <= j; i++)
                    {
                        values[i] = (p * values[i + 1] + (1 - p) * values[i]) * df;
                    }

                    if (j == 1)
                    {
                        delta = (values[1] - values[0]) / (s * u - s * d);
                    }
                }

                break;
            }

            case ExerciseType.American:
            {
                for (var j = NumSteps - 1; j >= 0; j--)
                {
                    for (var i = 0; i <= j; i++)
                    {
                        values[i] = Math.Max(z * (s * Math.Pow(u, i) * Math.Pow(d, j - 1) - k), (p * values[i + 1] + (1 - p) * values[i]) * df);
                    }

                    if (j == 1)
                    {
                        delta = (values[1] - values[0]) / (s * u - s * d);
                    }
                }

                break;
            }

            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }

        return delta;
    }

    public override double GammaAt(double s)
    {
        var k = option.Strike;
        var sigma = option.Volatility;
        var z = option.OptionType == OptionType.Call ? 1 : -1;
        var dt = YearsToMaturity(option.MaturityDate) / NumSteps;
        var u = Math.Exp(sigma * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp((option.RiskFreeRate - option.DividendYield) * dt) - d) / (u - d);
        var df = Math.Exp(-option.RiskFreeRate * dt);

        double gamma = 0;
        var values = new double[NumSteps + 1];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Math.Max(0, z * (s * Math.Pow(u, i) * Math.Pow(d, NumSteps - i) - k));
        }

        for (var j = NumSteps - 1; j >= 0; j--)
        {
            for (var i = 0; i <= j; i++)
            {
                values[i] = (p * values[i + 1] + (1 - p) * values[i]) * df;
            }

            if (j == 2)
            {
                gamma = ((values[2] - values[1]) / (s * u * u - s) - (values[1] - values[0]) / (s - s * d * d)) / (0.5 * (s * u * u - s * d * d));
            }
        }

        return gamma;
    }

    public override double ThetaAt(double s)
    {
        var k = option.Strike;
        var sigma = option.Volatility;
        var z = option.OptionType == OptionType.Call ? 1 : -1;
        var dt = YearsToMaturity(option.MaturityDate) / NumSteps;
        var u = Math.Exp(sigma * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp((option.RiskFreeRate - option.DividendYield) * dt) - d) / (u - d);
        var df = Math.Exp(-option.RiskFreeRate * dt);

        double theta = 0;
        var values = new double[NumSteps + 1];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Math.Max(0, z * (s * Math.Pow(u, i) * Math.Pow(d, NumSteps - i) - k));
        }

        for (var j = NumSteps - 1; j >= 0; j--)
        {
            for (var i = 0; i <= j; i++)
            {
                values[i] = (p * values[i + 1] + (1 - p) * values[i]) * df;
            }

            if (j == 2)
            {
                theta = values[1];
            }
        }

        theta = (theta - values[0]) / (2 * dt);
        return theta;
    }

    public override double VegaAt(double s)
    {
        const double dv = 0.0001;
        var v = option.Volatility;

        option.Volatility = v + dv;
        var u = ValueAt(s);

        option.Volatility = v - dv;
        var d = ValueAt(s);

        option.Volatility = v;
        return (u - d) / (2 * dv);
    }

    public override double RhoAt(double s)
    {
        const double dr = 0.0001;
        var r = option.RiskFreeRate;

        option.RiskFreeRate = r + dr;
        var u = ValueAt(s);

        option.RiskFreeRate = r - dr;
        var d = ValueAt(s);

        option.RiskFreeRate = r;
        return (u - d) / (2 * dr);
    }
}