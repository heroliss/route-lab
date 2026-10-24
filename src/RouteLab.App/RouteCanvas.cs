using System.Drawing.Drawing2D;
using System.ComponentModel;
using RouteLab.Core;

namespace RouteLab.App;

internal sealed class RouteCanvas : BufferedPanel
{
    private int _dragging = -1;
    private Point _mouseDown;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlyList<City> Cities { get; set; } = [];
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlyDictionary<AlgorithmKind, SolverSnapshot> Snapshots { get; set; } = new Dictionary<AlgorithmKind, SolverSnapshot>();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool EditingEnabled { get; set; } = true;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool ShowInitialRoute { get; set; }
    internal event Action<int, City>? CityMoved;
    internal event Action<City>? CityAdded;
    internal event Action<int>? CityRemoved;
    internal RouteCanvas()
    {
        Cursor = Cursors.Cross;
        AccessibleName = "旅行商路线画布";
        AccessibleDescription = "左键空白添加城市，拖动城市移动，右键城市删除。第一座城市是起点。";
    }

    private RectangleF Plot
    {
        get
        {
            float width = Math.Max(1, ClientSize.Width - 84), height = Math.Max(1, ClientSize.Height - 122);
            float scale = Math.Min(width / 1400, height / 850);
            float w = 1400 * scale, h = 850 * scale;
            return new(42 + (width - w) / 2, 82 + (height - h) / 2, w, h);
        }
    }
    private PointF ToScreen(City city) => new(Plot.Left + (float)city.X / 1400 * Plot.Width, Plot.Top + (float)city.Y / 850 * Plot.Height);
    private City ToCity(Point point) => new(Math.Clamp((point.X - Plot.Left) / Plot.Width * 1400, 0, 1400), Math.Clamp((point.Y - Plot.Top) / Plot.Height * 850, 0, 850));
    private int Hit(Point point)
    {
        for (int i = Cities.Count - 1; i >= 0; i--) { PointF p = ToScreen(Cities[i]); if (Math.Pow(point.X - p.X, 2) + Math.Pow(point.Y - p.Y, 2) < 13 * 13) return i; }
        return -1;
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!EditingEnabled || !Plot.Contains(e.Location)) return;
        int hit = Hit(e.Location);
        if (e.Button == MouseButtons.Right && hit >= 0) { CityRemoved?.Invoke(hit); return; }
        if (e.Button == MouseButtons.Left) { _dragging = hit; _mouseDown = e.Location; Capture = hit >= 0; }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (EditingEnabled && _dragging >= 0 && e.Button == MouseButtons.Left) CityMoved?.Invoke(_dragging, ToCity(e.Location));
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (EditingEnabled && e.Button == MouseButtons.Left && _dragging < 0 && Plot.Contains(e.Location)
            && Math.Abs(e.X - _mouseDown.X) + Math.Abs(e.Y - _mouseDown.Y) < 6) CityAdded?.Invoke(ToCity(e.Location));
        _dragging = -1; Capture = false;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        using var heading = Theme.Font(12, FontStyle.Bold);
        using var small = Theme.Font(8.5f);
        Theme.TextAt(g, "路线视图", heading, Theme.Text, new(22, 17, 160, 28));
        Theme.TextAt(g, $"{Cities.Count} 座城市  /  欧氏距离  /  闭环路线", small, Theme.Muted, new(22, 48, 400, 24));
        int legendX = Math.Max(330, Width - 235);
        foreach (var kind in Enum.GetValues<AlgorithmKind>())
        {
            using var brush = new SolidBrush(Theme.AlgorithmColor(kind));
            g.FillEllipse(brush, legendX, 25, 7, 7);
            Theme.TextAt(g, Theme.AlgorithmName(kind), small, Theme.Muted, new(legendX + 14, 18, 83, 24));
            legendX += 106;
        }
        RectangleF plot = Plot;
        using (var grid = new Pen(Color.FromArgb(29, 47, 66)) { DashStyle = DashStyle.Dot })
        {
            for (int i = 0; i <= 8; i++) { float x = plot.Left + plot.Width * i / 8; g.DrawLine(grid, x, plot.Top, x, plot.Bottom); }
            for (int i = 0; i <= 5; i++) { float y = plot.Top + plot.Height * i / 5; g.DrawLine(grid, plot.Left, y, plot.Right, y); }
        }
        if (ShowInitialRoute && Cities.Count >= 3)
            DrawRoute(g, Enumerable.Range(0, Cities.Count).ToArray(), Color.FromArgb(65, Theme.Muted), 1, DashStyle.Dot);
        foreach (var pair in Snapshots.OrderByDescending(pair => pair.Key))
        {
            Color color = Theme.AlgorithmColor(pair.Key);
            if (pair.Value.Iteration < pair.Value.IterationLimit)
                DrawRoute(g, pair.Value.CurrentRoute, Color.FromArgb(45, color), 1, DashStyle.Solid);
            DrawRoute(g, pair.Value.BestRoute, Color.FromArgb(230, color), 2.4f, pair.Key == AlgorithmKind.Genetic ? DashStyle.Solid : DashStyle.Dash);
        }
        for (int i = 0; i < Cities.Count; i++)
        {
            PointF p = ToScreen(Cities[i]);
            using var outer = new SolidBrush(Color.FromArgb(40, i == 0 ? Theme.Genetic : Theme.Text));
            using var inner = new SolidBrush(i == 0 ? Theme.Genetic : Theme.Text);
            g.FillEllipse(outer, p.X - 8, p.Y - 8, 16, 16);
            g.FillEllipse(inner, p.X - 3.6f, p.Y - 3.6f, 7.2f, 7.2f);
            if (i == 0 || Cities.Count <= 55)
                Theme.TextAt(g, i == 0 ? "起点" : (i + 1).ToString(), small, i == 0 ? Theme.Genetic : Theme.Muted, new((int)p.X + 7, (int)p.Y - 18, 42, 20));
        }
        Theme.TextAt(g, EditingEnabled ? "左键添加  ·  拖动移动  ·  右键删除城市" : "搜索进行中，城市编辑暂时锁定；停止后可继续调整。", small, Theme.Muted, new(22, Height - 30, Width - 44, 23));
        if (Cities.Count < 3)
            Theme.TextAt(g, "在画布中放置至少 3 座城市，或生成一组样例。", heading, Theme.Text, new(30, Height / 2, Width - 60, 60), TextFormatFlags.HorizontalCenter);
    }
    private void DrawRoute(Graphics g, int[] route, Color color, float width, DashStyle style)
    {
        if (route.Length != Cities.Count || route.Length < 2) return;
        var points = route.Select(i => ToScreen(Cities[i])).Append(ToScreen(Cities[route[0]])).ToArray();
        using var pen = new Pen(color, width) { DashStyle = style, LineJoin = LineJoin.Round };
        g.DrawLines(pen, points);
    }
}
