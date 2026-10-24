using System.Text.Json;
using RouteLab.Core;

var results = new List<object>();
var scenarios = new List<object>();
int passed = 0;
void Check(bool condition, string detail) { if (!condition) throw new Exception(detail); }
void Test(string name, Action run) { run(); passed++; results.Add(new { name, passed = true }); Console.WriteLine($"PASS {name}"); }
void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
var square = new Problem([new(0, 0), new(1, 0), new(1, 1), new(0, 1)]);
var cities = CitySamples.Generate(48, 2026, CityLayout.Archipelago);
var problem = new Problem(cities);
var options = new SolverOptions { Iterations = 450, Population = 64, Seed = 1024 };
foreach (AlgorithmKind algorithm in Enum.GetValues<AlgorithmKind>())
{
    Test($"{algorithm}: square optimum", () =>
    {
        var r = Solver.Run(square, algorithm, options);
        Check(Math.Abs(r.BestLength - 4) < 1e-9, "Square length must be 4");
    });
    Test($"{algorithm}: permutations, exact costs, monotone best", () =>
    {
        double previousBest = double.PositiveInfinity;
        var result = Solver.Run(problem, algorithm, options, s =>
        {
            Check(problem.IsValidTour(s.CurrentRoute) && problem.IsValidTour(s.BestRoute), "Invalid permutation");
            Check(s.BestLength <= previousBest + 1e-8, "Best regressed");
            Check(s.BestLength <= s.CurrentLength + 1e-8, "Best is worse than current");
            Check(Math.Abs(problem.Length(s.CurrentRoute) - s.CurrentLength) < 1e-8, "Current cost mismatch");
            Check(Math.Abs(problem.Length(s.BestRoute) - s.BestLength) < 1e-8, "Best cost mismatch");
            previousBest = s.BestLength;
        });
        Check(result.BestLength < result.InitialLength, "Expected actual improvement for fixed sample");
        scenarios.Add(new { algorithm = algorithm.ToString(), seed = options.Seed, cities = problem.Count, iterations = options.Iterations,
            population = options.Population, result.InitialLength, result.BestLength, result.Evaluations, elapsedMs = result.Elapsed.TotalMilliseconds });
        Console.WriteLine($"  cities=48 initial={result.InitialLength:F3} best={result.BestLength:F3} evaluations={result.Evaluations}");
    });
    Test($"{algorithm}: deterministic seed", () =>
    {
        var first = Solver.Run(problem, algorithm, options);
        var second = Solver.Run(problem, algorithm, options);
        Check(first.BestRoute.SequenceEqual(second.BestRoute) && first.BestLength == second.BestLength, "Seed is not reproducible");
    });
    Test($"{algorithm}: cancellation", () =>
    {
        using var cts = new CancellationTokenSource();
        Throws<OperationCanceledException>(() => Solver.Run(problem, algorithm, options, s => { if (s.Iteration > 3) cts.Cancel(); }, cts.Token));
    });
    Test($"{algorithm}: coincident and three cities", () =>
    {
        var p = new Problem([new(0, 0), new(0, 0), new(0, 0)]);
        var r = Solver.Run(p, algorithm, options with { Iterations = 3 });
        Check(r.BestLength == 0 && p.IsValidTour(r.BestRoute), "Zero-distance case failed");
    });
    Test($"{algorithm}: maximum city boundary", () =>
    {
        var p = new Problem(CitySamples.Generate(200, 10, CityLayout.Random));
        var r = Solver.Run(p, algorithm, options with { Iterations = 2 });
        Check(p.IsValidTour(r.BestRoute), "Maximum-size route invalid");
    });
}
Test("SA: rejected uphill proposals leave current unchanged", () =>
{
    double previous = double.PositiveInfinity;
    Solver.Run(problem, AlgorithmKind.SimulatedAnnealing, options with { InitialTemperature = 1e-200 }, s =>
    {
        Check(s.CurrentLength <= previous + 1e-8, "Cold annealing accepted uphill or mutated rejected route");
        previous = s.CurrentLength;
    });
});
Test("Reversal delta equals complete closed-route recomputation", () =>
{
    var route = Enumerable.Range(0, problem.Count).ToArray();
    var random = new Random(18);
    for (int i = 0; i < 1000; i++)
    {
        int first = random.Next(1, route.Length - 1), last = random.Next(first + 1, route.Length);
        double before = problem.Length(route), delta = problem.ReversalDelta(route, first, last);
        Array.Reverse(route, first, last - first + 1);
        Check(Math.Abs((problem.Length(route) - before) - delta) < 1e-8, "Delta mismatch");
    }
});
Test("Invalid problem/options are rejected", () =>
{
    Throws<ArgumentException>(() => new Problem([new(0, 0), new(1, 0)]));
    Throws<ArgumentException>(() => new Problem([new(0, 0), new(double.NaN, 0), new(0, 1)]));
    Throws<ArgumentOutOfRangeException>(() => Solver.Run(problem, AlgorithmKind.Genetic, options with { MutationProbability = -1 }));
    Throws<ArgumentOutOfRangeException>(() => Solver.Run(problem, AlgorithmKind.Genetic, options with { EliteCount = 100 }));
    Throws<ArgumentOutOfRangeException>(() => Solver.Run(problem, AlgorithmKind.Genetic, options with { CoolingRate = 1 }));
});
Test("Published snapshots cannot be changed by later solver iterations", () =>
{
    SolverSnapshot? earliest = null; int[]? clone = null;
    Solver.Run(problem, AlgorithmKind.Genetic, options, s => { if (earliest is null) { earliest = s; clone = (int[])s.BestRoute.Clone(); } });
    Check(earliest!.BestRoute.SequenceEqual(clone!), "Snapshot mutated after publication");
});
if (args.Length == 2 && args[0] == "--report")
    File.WriteAllText(Path.GetFullPath(args[1]), JsonSerializer.Serialize(new { passed, failed = 0, checks = results, scenarios }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{passed} passed, 0 failed");
