using CommunityToolkit.Diagnostics;
using TorchSharp;

namespace SharpOptions.PricingEngines;

public class MonteCarloSimulation(int nPaths, int nSteps, bool useCuda)
{
    public torch.Device Device { get; } = useCuda & torch.cuda.is_available() ? torch.CUDA : torch.CPU;

    public int NumPaths { get; } = nPaths % 2 == 1 ? nPaths + 1 : nPaths;

    public torch.Tensor? RandomSamples { get; set; }

    public void GenerateRandomSamples()
    {
        var e = torch.randn(NumPaths / 2, nSteps - 1, device: Device);
        RandomSamples = torch.cat([e, -e]);
    }

    public torch.Tensor GeometricBrownianMotion(double timeToMaturity, double mu, double sigma)
    {
        if (RandomSamples is null)
        {
            ThrowHelper.ThrowInvalidOperationException("Random samples not generated.");
        }

        var dt = timeToMaturity / (nSteps - 1);
        var lns0 = torch.zeros(NumPaths, 1, device: Device);
        var lns = torch.cat([lns0, (mu - sigma * sigma / 2) * dt + sigma * Math.Sqrt(dt) * RandomSamples], 1).cumsum(1);

        return torch.exp(lns);
    }
}