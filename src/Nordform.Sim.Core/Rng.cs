namespace Nordform.Sim.Core;

/// <summary>
/// Deterministisk slumpgenerator (mulberry32). Samma seed ger samma talföljd oavsett .NET-version,
/// och följden är densamma som i webbprototypen.
/// </summary>
public sealed class Rng
{
    private uint _state;

    public Rng(int seed) => _state = unchecked((uint)seed);

    /// <summary>Nästa tal i intervallet [0, 1).</summary>
    public double NextDouble()
    {
        unchecked
        {
            _state += 0x6D2B79F5;
            var t = (_state ^ (_state >> 15)) * (_state | 1);
            t = (t + (t ^ (t >> 7)) * (t | 61)) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }
}
