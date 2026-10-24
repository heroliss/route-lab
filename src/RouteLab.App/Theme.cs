using System.Drawing.Drawing2D;

namespace RouteLab.App;

internal static class Theme
{
    internal static readonly Color Background = Color.FromArgb(12, 19, 32);
    internal static readonly Color Surface = Color.FromArgb(17, 29, 47);
    internal static readonly Color Elevated = Color.FromArgb(25, 42, 61);
    internal static readonly Color Border = Color.FromArgb(39, 57, 78);
    internal static readonly Color Text = Color.FromArgb(229, 238, 248);
    internal static readonly Color Muted = Color.FromArgb(141, 161, 185);
    internal static readonly Color Genetic = Color.FromArgb(126, 231, 196);
    internal static readonly Color Annealing = Color.FromArgb(250, 178, 122);
    internal static Font Font(float size = 9.5f, FontStyle style = FontStyle.Regular) => new("Microsoft YaHei UI", size, style);
    internal static Font Number(float size = 22, FontStyle style = FontStyle.Bold) => new("Segoe UI Variable", size, style);
    internal static Color AlgorithmColor(RouteLab.Core.AlgorithmKind kind) => kind == Core.AlgorithmKind.Genetic ? Genetic : Annealing;
    internal static string AlgorithmName(RouteLab.Core.AlgorithmKind kind) => kind == Core.AlgorithmKind.Genetic ? "遗传算法" : "模拟退火";
    internal static void TextAt(Graphics graphics, string text, Font font, Color color, Rectangle rect, TextFormatFlags extra = 0)
        => TextRenderer.DrawText(graphics, text, font, rect, color, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | extra);

    internal static Button Button(string text, bool primary = false)
    {
        var button = new Button { Text = text, Height = 36, AutoSize = false, FlatStyle = FlatStyle.Flat,
            ForeColor = primary ? Background : Text, BackColor = primary ? Genetic : Elevated,
            Font = Font(9.5f, primary ? FontStyle.Bold : FontStyle.Regular), Cursor = Cursors.Hand, Margin = new Padding(0) };
        button.FlatAppearance.BorderColor = primary ? Genetic : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(161, 242, 215) : Color.FromArgb(39, 61, 82);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(96, 206, 172) : Color.FromArgb(45, 68, 92);
        return button;
    }
}

internal class BufferedPanel : Panel
{
    internal BufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; BackColor = Theme.Surface; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        base.OnPaint(e);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}
