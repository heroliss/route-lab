using System.Drawing.Drawing2D;
using System.ComponentModel;
using RouteLab.Core;

namespace RouteLab.App;

internal sealed class ConvergenceChart : BufferedPanel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlyDictionary<AlgorithmKind, List<SolverSnapshot>> History { get; set; } = new Dictionary<AlgorithmKind, List<SolverSnapshot>>();
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        using var title = Theme.Font(11, FontStyle.Bold);
        using var small = Theme.Font(8);
        Theme.TextAt(g, "收敛过程", title, Theme.Text, new(22, 15, 160, 28));
        Theme.TextAt(g, "全程最佳路线长度越低越好；两种算法的每轮工作量不同。", small, Theme.Muted, new(150, 19, Width - 172, 25), TextFormatFlags.Right);
        var samples = History.Values.SelectMany(v => v).ToArray();
        RectangleF plot = new(82, 57, Math.Max(1, Width - 112), Math.Max(1, Height - 99));
        if (samples.Length == 0)
        {
            Theme.TextAt(g, "运行后展示真实搜索记录。可用同一组城市对照两种算法。", small, Theme.Muted, new(30, Height / 2, Width - 60, 30), TextFormatFlags.HorizontalCenter);
            return;
        }
        double min = Math.Max(0, samples.Min(s => s.BestLength) * .92), max = samples.Max(s => s.InitialLength) * 1.02;
        if (max <= min) max = min + 1;
        using var grid = new Pen(Theme.Border) { DashStyle = DashStyle.Dot };
        for (int i = 0; i <= 3; i++)
        {
            float y = plot.Top + plot.Height * i / 3;
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
            Theme.TextAt(g, (max - (max - min) * i / 3).ToString("N0"), small, Theme.Muted, new(10, (int)y - 9, 60, 21), TextFormatFlags.Right);
        }
        for (int i = 0; i <= 4; i++)
            Theme.TextAt(g, $"{i * 25}%", small, Theme.Muted, new((int)(plot.Left + plot.Width * i / 4) - 20, (int)plot.Bottom + 10, 50, 20), TextFormatFlags.HorizontalCenter);
        foreach (var pair in History)
        {
            if (pair.Value.Count < 2) continue;
            PointF[] points = pair.Value.Select(s => new PointF(plot.Left + plot.Width * s.Iteration / s.IterationLimit,
                plot.Bottom - (float)((s.BestLength - min) / (max - min)) * plot.Height)).ToArray();
            using var line = new Pen(Theme.AlgorithmColor(pair.Key), 2.2f);
            g.DrawLines(line, points);
            using var dot = new SolidBrush(Theme.AlgorithmColor(pair.Key));
            g.FillEllipse(dot, points[^1].X - 3, points[^1].Y - 3, 6, 6);
        }
    }
}
