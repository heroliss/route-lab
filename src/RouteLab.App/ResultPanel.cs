using RouteLab.Core;
using System.ComponentModel;

namespace RouteLab.App;

internal sealed class ResultPanel : BufferedPanel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlyDictionary<AlgorithmKind, SolverSnapshot> Snapshots { get; set; } = new Dictionary<AlgorithmKind, SolverSnapshot>();
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        using var heading = Theme.Font(10, FontStyle.Bold);
        using var small = Theme.Font(8.2f);
        using var number = Theme.Number(23);
        int top = 13;
        foreach (AlgorithmKind kind in Enum.GetValues<AlgorithmKind>())
        {
            Color color = Theme.AlgorithmColor(kind);
            Theme.TextAt(g, Theme.AlgorithmName(kind), heading, color, new(15, top, 180, 24));
            if (Snapshots.TryGetValue(kind, out var s))
            {
                Theme.TextAt(g, s.BestLength.ToString("N1"), number, Theme.Text, new(13, top + 23, Width - 30, 37));
                double improvement = s.InitialLength > 0 ? (1 - s.BestLength / s.InitialLength) * 100 : 0;
                Theme.TextAt(g, $"改善 {improvement:F1}%   ·   {s.Elapsed.TotalMilliseconds:N0} ms", small, Theme.Muted, new(15, top + 65, Width - 30, 22));
            }
            else
                Theme.TextAt(g, "等待运行", small, Theme.Muted, new(15, top + 35, Width - 30, 30));
            if (top == 13) { using var pen = new Pen(Theme.Border); g.DrawLine(pen, 15, top + 96, Width - 15, top + 96); }
            top += 110;
        }
    }
}
