namespace MainPCDoctor.Desktop.Background;

// Returns the current sampling interval based on 3-state machine: Eco / Normal / Incident
internal static class SamplingScheduler
{
    public static TimeSpan GetInterval(SamplingState state) => state switch
    {
        SamplingState.Eco      => TimeSpan.FromSeconds(30),
        SamplingState.Normal   => TimeSpan.FromSeconds(10),
        SamplingState.Incident => TimeSpan.FromSeconds(3),
        _                      => TimeSpan.FromSeconds(10),
    };
}

internal enum SamplingState { Eco, Normal, Incident }
