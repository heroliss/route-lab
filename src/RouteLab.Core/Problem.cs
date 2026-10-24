namespace RouteLab.Core;

public readonly record struct City(double X, double Y);
public enum AlgorithmKind { Genetic, SimulatedAnnealing }
public enum CityLayout { Archipelago, Ring, Random }

/// <summary>Immutable city coordinates and a cached symmetric distance matrix.</summary>
public sealed class Problem
{
    public const int MaximumCities = 200;
    private readonly City[] _cities;
    private readonly double[,] _distances;
    public int Count => _cities.Length;
    public IReadOnlyList<City> Cities { get; }
    public double MeanEdge { get; }

    public Problem(IEnumerable<City> cities)
    {
        ArgumentNullException.ThrowIfNull(cities);
        _cities = cities.ToArray();
        if (_cities.Length is < 3 or > MaximumCities)
            throw new ArgumentException($"城市数量应在 3—{MaximumCities} 之间。", nameof(cities));
        if (_cities.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y) || Math.Abs(p.X) > 100_000 || Math.Abs(p.Y) > 100_000))
            throw new ArgumentException("城市坐标必须是有限数值，且绝对值不能超过 100000。", nameof(cities));
        Cities = Array.AsReadOnly(_cities);
        _distances = new double[Count, Count];
        double sum = 0;
        for (int i = 0; i < Count; i++)
            for (int j = i + 1; j < Count; j++)
            {
                double dx = _cities[i].X - _cities[j].X, dy = _cities[i].Y - _cities[j].Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                _distances[i, j] = _distances[j, i] = d;
                sum += d;
            }
        MeanEdge = sum / (Count * (Count - 1) / 2);
    }

    public double Distance(int from, int to) => _distances[from, to];

    public bool IsValidTour(IReadOnlyList<int> route)
    {
        if (route.Count != Count || route[0] != 0) return false;
        var visited = new bool[Count];
        foreach (int city in route)
        {
            if (city < 0 || city >= Count || visited[city]) return false;
            visited[city] = true;
        }
        return true;
    }

    public double Length(IReadOnlyList<int> route)
    {
        if (!IsValidTour(route)) throw new ArgumentException("路线必须从 0 开始，且每个城市恰好出现一次。", nameof(route));
        return LengthUnchecked(route);
    }

    internal double LengthUnchecked(IReadOnlyList<int> route)
    {
        double sum = 0;
        for (int i = 0; i < route.Count; i++) sum += _distances[route[i], route[(i + 1) % route.Count]];
        return sum;
    }

    /// <summary>Cost of reversing [first,last], preserving the fixed start city.</summary>
    public double ReversalDelta(IReadOnlyList<int> route, int first, int last)
    {
        if (first < 1 || last >= Count || first >= last)
            throw new ArgumentOutOfRangeException(nameof(first), "逆转区间须满足 1 ≤ first < last < 城市数。");
        int a = route[first - 1], b = route[first], c = route[last], d = route[(last + 1) % Count];
        return _distances[a, c] + _distances[b, d] - _distances[a, b] - _distances[c, d];
    }
}

public sealed record SolverOptions
{
    public int Seed { get; init; } = 2026;
    public int Iterations { get; init; } = 1800;
    public int Population { get; init; } = 100;
    public int EliteCount { get; init; } = 3;
    public double CrossoverProbability { get; init; } = .9;
    public double MutationProbability { get; init; } = .22;
    public double InitialTemperature { get; init; } = 0; // Zero selects a scale-aware temperature.
    public double CoolingRate { get; init; } = .995;

    public void Validate()
    {
        if (Iterations is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(Iterations));
        if (Population is < 4 or > 1000) throw new ArgumentOutOfRangeException(nameof(Population));
        if (EliteCount < 1 || EliteCount >= Population) throw new ArgumentOutOfRangeException(nameof(EliteCount));
        if (!double.IsFinite(CrossoverProbability) || CrossoverProbability < 0 || CrossoverProbability > 1) throw new ArgumentOutOfRangeException(nameof(CrossoverProbability));
        if (!double.IsFinite(MutationProbability) || MutationProbability < 0 || MutationProbability > 1) throw new ArgumentOutOfRangeException(nameof(MutationProbability));
        if (!double.IsFinite(InitialTemperature) || InitialTemperature < 0 || InitialTemperature > 1e9) throw new ArgumentOutOfRangeException(nameof(InitialTemperature));
        if (!double.IsFinite(CoolingRate) || CoolingRate <= 0 || CoolingRate >= 1) throw new ArgumentOutOfRangeException(nameof(CoolingRate));
    }
}

public static class CitySamples
{
    public static City[] Generate(int count, int seed, CityLayout layout)
    {
        if (count is < 3 or > Problem.MaximumCities) throw new ArgumentOutOfRangeException(nameof(count));
        var random = new Random(seed);
        City[] centers = [new(245, 250), new(745, 310), new(525, 760)];
        return Enumerable.Range(0, count).Select(i =>
        {
            double angle = random.NextDouble() * Math.Tau;
            if (layout == CityLayout.Ring)
            {
                angle = Math.Tau * i / count + (random.NextDouble() - .5) * .09;
                double radius = 350 + random.NextDouble() * 70;
                return new City(500 + Math.Cos(angle) * radius, 500 + Math.Sin(angle) * radius);
            }
            if (layout == CityLayout.Archipelago)
            {
                City center = centers[i % centers.Length];
                double radius = 145 * Math.Sqrt(random.NextDouble());
                return new City(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
            }
            return new City(70 + random.NextDouble() * 860, 70 + random.NextDouble() * 860);
        }).Select(p => new City(p.X * 1.4, p.Y * .85)).ToArray();
    }
}
