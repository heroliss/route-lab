using System.Diagnostics;

namespace RouteLab.Core;

/// <summary>Each snapshot owns its arrays; solvers never modify a published route.</summary>
public sealed record SolverSnapshot(AlgorithmKind Algorithm, int Iteration, int IterationLimit,
    int[] CurrentRoute, double CurrentLength, int[] BestRoute, double BestLength,
    double InitialLength, double Temperature, long Evaluations, TimeSpan Elapsed);

public static class Solver
{
    public static SolverSnapshot Run(Problem problem, AlgorithmKind algorithm, SolverOptions options,
        Action<SolverSnapshot>? report = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        return algorithm switch
        {
            AlgorithmKind.Genetic => Genetic(problem, options, report, cancellationToken),
            AlgorithmKind.SimulatedAnnealing => Anneal(problem, options, report, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    private static int[] RandomTour(int count, Random random)
    {
        int[] route = Enumerable.Range(0, count).ToArray();
        for (int i = count - 1; i > 1; i--)
        {
            int j = random.Next(1, i + 1);
            (route[i], route[j]) = (route[j], route[i]);
        }
        return route;
    }

    private static SolverSnapshot Genetic(Problem p, SolverOptions o, Action<SolverSnapshot>? report, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var random = new Random(o.Seed);
        int[][] population = new int[o.Population][];
        double[] scores = new double[o.Population];
        long evaluations = 0;
        for (int i = 0; i < population.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            population[i] = RandomTour(p.Count, random);
            scores[i] = p.LengthUnchecked(population[i]);
            evaluations++;
        }
        double initial = scores[0];
        Array.Sort(scores, population);
        int[] best = (int[])population[0].Clone();
        double bestLength = scores[0];
        SolverSnapshot Snapshot(int iteration) => new(AlgorithmKind.Genetic, iteration, o.Iterations,
            (int[])population[0].Clone(), scores[0], (int[])best.Clone(), bestLength, initial, 0, evaluations, watch.Elapsed);
        report?.Invoke(Snapshot(0));
        int interval = Math.Max(1, o.Iterations / 160);
        for (int generation = 1; generation <= o.Iterations; generation++)
        {
            ct.ThrowIfCancellationRequested();
            var next = new int[population.Length][];
            for (int i = 0; i < o.EliteCount; i++) next[i] = (int[])population[i].Clone();
            for (int i = o.EliteCount; i < next.Length; i++)
            {
                if ((i & 31) == 0) ct.ThrowIfCancellationRequested();
                int[] parent = population[Tournament(scores, random)];
                int[] child = random.NextDouble() < o.CrossoverProbability
                    ? OrderedCrossover(parent, population[Tournament(scores, random)], random)
                    : (int[])parent.Clone();
                if (random.NextDouble() < o.MutationProbability)
                {
                    int first = random.Next(1, p.Count - 1), last = random.Next(first + 1, p.Count);
                    Array.Reverse(child, first, last - first + 1);
                }
                next[i] = child;
            }
            population = next;
            for (int i = 0; i < scores.Length; i++) { scores[i] = p.LengthUnchecked(population[i]); evaluations++; }
            Array.Sort(scores, population);
            if (scores[0] < bestLength) { bestLength = scores[0]; best = (int[])population[0].Clone(); }
            if (generation % interval == 0 || generation == o.Iterations) report?.Invoke(Snapshot(generation));
        }
        return Snapshot(o.Iterations);
    }

    private static int Tournament(double[] scores, Random random)
    {
        int winner = random.Next(scores.Length);
        for (int i = 0; i < 3; i++) { int candidate = random.Next(scores.Length); if (scores[candidate] < scores[winner]) winner = candidate; }
        return winner;
    }

    private static int[] OrderedCrossover(int[] a, int[] b, Random random)
    {
        int count = a.Length;
        int left = random.Next(1, count - 1), right = random.Next(left + 1, count);
        var child = new int[count];
        Array.Fill(child, -1);
        child[0] = 0;
        var used = new bool[count]; used[0] = true;
        for (int i = left; i <= right; i++) { child[i] = a[i]; used[a[i]] = true; }
        int destination = right + 1;
        if (destination == count) destination = 1;
        for (int offset = 0; offset < count - 1; offset++)
        {
            int gene = b[1 + (right + offset) % (count - 1)];
            if (used[gene]) continue;
            while (child[destination] != -1) { destination++; if (destination == count) destination = 1; }
            child[destination] = gene; used[gene] = true;
        }
        return child;
    }

    private static SolverSnapshot Anneal(Problem p, SolverOptions o, Action<SolverSnapshot>? report, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var random = new Random(o.Seed);
        int[] current = RandomTour(p.Count, random), best = (int[])current.Clone();
        double currentLength = p.LengthUnchecked(current), bestLength = currentLength, initial = currentLength;
        double temperature = o.InitialTemperature > 0 ? o.InitialTemperature : Math.Max(p.MeanEdge, 1e-9);
        long evaluations = 1;
        SolverSnapshot Snapshot(int iteration) => new(AlgorithmKind.SimulatedAnnealing, iteration, o.Iterations,
            (int[])current.Clone(), currentLength, (int[])best.Clone(), bestLength, initial, temperature, evaluations, watch.Elapsed);
        report?.Invoke(Snapshot(0));
        int interval = Math.Max(1, o.Iterations / 160);
        int proposals = p.Count * 4;
        for (int iteration = 1; iteration <= o.Iterations; iteration++)
        {
            ct.ThrowIfCancellationRequested();
            for (int j = 0; j < proposals; j++)
            {
                if ((j & 63) == 0) ct.ThrowIfCancellationRequested();
                int first = random.Next(1, p.Count - 1), last = random.Next(first + 1, p.Count);
                double delta = p.ReversalDelta(current, first, last);
                evaluations++;
                // Rejected proposals never mutate current. Keep the incumbent separate from the best-ever tour.
                if (delta <= 0 || random.NextDouble() < Math.Exp(-delta / Math.Max(temperature, 1e-300)))
                {
                    Array.Reverse(current, first, last - first + 1);
                    currentLength = p.LengthUnchecked(current); // Avoid accumulated floating-point drift.
                    if (currentLength < bestLength) { bestLength = currentLength; best = (int[])current.Clone(); }
                }
            }
            temperature = Math.Max(temperature * o.CoolingRate, 1e-300);
            if (iteration % interval == 0 || iteration == o.Iterations) report?.Invoke(Snapshot(iteration));
        }
        return Snapshot(o.Iterations);
    }
}
