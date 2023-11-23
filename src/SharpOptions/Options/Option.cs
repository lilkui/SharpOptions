using CommunityToolkit.Diagnostics;
using SharpOptions.PricingEngines;

namespace SharpOptions.Options;

public abstract class Option
{
    private PricingEngine? _pricingEngine;

    public required DateTime MaturityDate { get; set; }

    public required double Volatility { get; set; }

    public required double RiskFreeRate { get; set; }

    public required double DividendYield { get; set; }

    public PricingEngine PricingEngine
    {
        get => _pricingEngine ?? ThrowHelper.ThrowInvalidOperationException<PricingEngine>("PricingEngine not set");
        set => _pricingEngine = value;
    }

    public double ValueAt(double s)
    {
        return PricingEngine.ValueAt(s);
    }

    // ∂V/∂S
    public double DeltaAt(double s)
    {
        return PricingEngine.DeltaAt(s);
    }

    // ∂^2V/∂S^2
    public double GammaAt(double s)
    {
        return PricingEngine.GammaAt(s);
    }

    // -∂V/∂T
    public double ThetaAt(double s)
    {
        return PricingEngine.ThetaAt(s);
    }

    // ∂V/∂σ
    public double VegaAt(double s)
    {
        return PricingEngine.VegaAt(s);
    }

    // ∂V/∂r
    public double RhoAt(double s)
    {
        return PricingEngine.RhoAt(s);
    }

    // ∂Δ/∂σ, ∂Vega/∂S, ∂^2V/∂S∂σ
    public double VannaAt(double s)
    {
        return PricingEngine.VannaAt(s);
    }

    // -∂Δ/∂T, -∂^2V/∂S∂T
    public double CharmAt(double s)
    {
        return PricingEngine.CharmAt(s);
    }
}