using RouteLab.Core;

namespace RouteLab.App;

internal sealed class MainForm : Form
{
    private readonly RouteCanvas _canvas = new();
    private readonly ConvergenceChart _chart = new();
    private readonly ResultPanel _results = new();
    private readonly ComboBox _algorithm = Combo(["遗传算法  GA", "模拟退火  SA"]);
    private readonly ComboBox _layout = Combo(["三岛城市群", "环形城市", "随机散点"]);
    private readonly NumericUpDown _count = Numeric(3, 200, 48);
    private readonly NumericUpDown _seed = Numeric(0, int.MaxValue, 2026);
    private readonly NumericUpDown _iterations = Numeric(1, 100000, 1800);
    private readonly NumericUpDown _population = Numeric(4, 1000, 100);
    private readonly NumericUpDown _mutation = Numeric(0, 1, .22m, 2, .01m);
    private readonly NumericUpDown _elites = Numeric(1, 999, 3);
    private readonly NumericUpDown _temperature = Numeric(0, 100000, 0, 1, 10);
    private readonly NumericUpDown _cooling = Numeric(.0001m, .9999m, .995m, 4, .0005m);
    private readonly Button _run = Theme.Button("运行所选算法", true);
    private readonly Button _compare = Theme.Button("对照运行");
    private readonly Button _stop = Theme.Button("停止搜索");
    private readonly Button _generate = Theme.Button("生成城市");
    private readonly Button _clear = Theme.Button("清空");
    private readonly Button _undo = Theme.Button("撤销一城");
    private readonly Button _export = Theme.Button("导出画布");
    private readonly Label _status = new() { Text = "就绪 · 固定种子使城市与搜索均可复现。", ForeColor = Theme.Muted, AutoEllipsis = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _explanation = new() { ForeColor = Theme.Muted, AutoEllipsis = true };
    private readonly Label _details = new() { ForeColor = Theme.Muted, AutoEllipsis = true };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Right, Width = 190, Maximum = 1000, Style = ProgressBarStyle.Continuous };
    private readonly List<City> _cities = [];
    private readonly Dictionary<AlgorithmKind, SolverSnapshot> _snapshots = [];
    private readonly Dictionary<AlgorithmKind, List<SolverSnapshot>> _history = [];
    private CancellationTokenSource? _cancellation;
    private bool _busy;
    private int _runId;
    private readonly string[] _args;
    private readonly Control _geneticParameters;
    private readonly Control _annealingParameters;
    private readonly Control _iterationParameters;

    internal MainForm(string[] args)
    {
        _args = args;
        Text = "Route Lab · 旅行商实验室 — Liss";
        BackColor = Theme.Background; ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(1440, 940);
        MinimumSize = new Size(1150, 820);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        Icon = SystemIcons.Application;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(22, 12, 22, 8), BackColor = Theme.Background };
        root.RowStyles.Add(new(SizeType.Absolute, 66)); root.RowStyles.Add(new(SizeType.Absolute, 48));
        root.RowStyles.Add(new(SizeType.Percent, 100)); root.RowStyles.Add(new(SizeType.Absolute, 31));
        Controls.Add(root);
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        body.ColumnStyles.Add(new(SizeType.Percent, 100)); body.ColumnStyles.Add(new(SizeType.Absolute, 330));
        root.Controls.Add(body, 0, 2);
        var visualization = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 18, 0) };
        visualization.RowStyles.Add(new(SizeType.Percent, 70)); visualization.RowStyles.Add(new(SizeType.Percent, 30));
        _canvas.Dock = DockStyle.Fill; _canvas.Margin = new Padding(0, 0, 0, 14);
        _chart.Dock = DockStyle.Fill; _chart.Margin = new Padding(0);
        visualization.Controls.Add(_canvas, 0, 0); visualization.Controls.Add(_chart, 0, 1); body.Controls.Add(visualization, 0, 0);
        var sidebar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, Margin = new Padding(0), Padding = new Padding(0), BackColor = Theme.Background };
        body.Controls.Add(sidebar, 1, 0);
        void Add(Control c, int height, int gap = 8) { c.Width = 306; c.Height = height; c.Margin = new Padding(0, 0, 0, gap); sidebar.Controls.Add(c); }
        Add(Heading("实验设置"), 26, 5);
        Add(_layout, 30);
        Add(Pair("生成城市数", _count, "随机种子", _seed), 48, 12);
        Add(_algorithm, 30);
        _iterationParameters = Pair("迭代轮数", _iterations, "种群大小 · GA", _population);
        Add(_iterationParameters, 48);
        _geneticParameters = Pair("变异概率", _mutation, "保留精英数", _elites);
        _annealingParameters = Pair("初温 · 0 为自动", _temperature, "每轮降温系数", _cooling);
        Add(_geneticParameters, 48); Add(_annealingParameters, 48);
        var actionRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        actionRow.ColumnStyles.Add(new(SizeType.Percent, 59)); actionRow.ColumnStyles.Add(new(SizeType.Percent, 41));
        _run.Dock = DockStyle.Fill; _run.Margin = new Padding(0, 0, 6, 0); _compare.Dock = DockStyle.Fill;
        actionRow.Controls.Add(_run, 0, 0); actionRow.Controls.Add(_compare, 1, 0);
        Add(actionRow, 36, 6); Add(_stop, 28, 9);
        _explanation.Font = Theme.Font(8.5f); Add(_explanation, 50, 8);
        Add(Heading("最佳路线长度"), 26, 5); Add(_results, 218, 7);
        _details.Font = Theme.Font(8); Add(_details, 46, 0);
        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 7, 0, 0), Margin = new Padding(0) };
        footer.Controls.Add(_status); footer.Controls.Add(_progress); root.Controls.Add(footer, 0, 3);
        _canvas.Cities = _cities; _canvas.Snapshots = _snapshots; _chart.History = _history; _results.Snapshots = _snapshots;
        _run.Click += async (_, _) => await RunAsync(false);
        _compare.Click += async (_, _) => await RunAsync(true);
        _stop.Click += (_, _) => { _cancellation?.Cancel(); _stop.Enabled = false; _status.Text = "正在停止，已找到的最佳路线会保留。"; };
        _generate.Click += (_, _) => Generate();
        _clear.Click += (_, _) => { _cities.Clear(); ResetResults(); _status.Text = "画布已清空。左键添加城市，或重新生成样例。"; };
        _undo.Click += (_, _) => { if (_cities.Count > 0) { _cities.RemoveAt(_cities.Count - 1); ResetResults(); } };
        _export.Click += (_, _) => Export();
        _algorithm.SelectedIndexChanged += (_, _) => UpdateAlgorithmControls();
        _population.ValueChanged += (_, _) => _elites.Maximum = _population.Value - 1;
        _elites.Maximum = _population.Value - 1;
        _canvas.CityAdded += city => { if (_cities.Count >= Problem.MaximumCities) { _status.Text = "最多支持 200 座城市，请先删除一座。"; return; } _cities.Add(city); ResetResults(); };
        _canvas.CityMoved += (index, city) => { if (index >= 0 && index < _cities.Count) { _cities[index] = city; ResetResults(); } };
        _canvas.CityRemoved += index => { _cities.RemoveAt(index); ResetResults(); };
        FormClosing += (_, _) => _cancellation?.Cancel();
        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && _busy) { _cancellation?.Cancel(); e.Handled = true; }
            if (e.KeyCode == Keys.F5 && !_busy) { e.Handled = true; await RunAsync(e.Shift); }
        };
        Shown += async (_, _) => { ApplyArguments(); Generate(); if (args.Contains("--autorun") || args.Contains("--compare")) await RunAsync(args.Contains("--compare")); };
        UpdateAlgorithmControls(); SetBusy(false);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        var name = new Label { Text = "Route Lab", Font = Theme.Number(26), ForeColor = Theme.Text, Location = new(0, -3), Size = new(260, 45) };
        var chinese = new Label { Text = "旅行商实验室", Font = Theme.Font(12), ForeColor = Theme.Text, Location = new(230, 12), Size = new(280, 29) };
        var subtitle = new Label { Text = "让一条路线，在搜索与选择中变得更短。", ForeColor = Theme.Muted, Location = new(1, 43), Size = new(650, 22), Font = Theme.Font(8.5f) };
        var credit = new Label { Text = "Liss  /  v1.0", ForeColor = Theme.Muted, Dock = DockStyle.Right, Width = 180, TextAlign = ContentAlignment.MiddleRight, Font = Theme.Font(9) };
        panel.Controls.AddRange([name, chinese, subtitle, credit]); return panel;
    }

    private Control BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0, 5, 0, 0) };
        foreach (var button in new[] { _generate, _clear, _undo, _export }) { button.Width = button == _generate ? 110 : button == _undo ? 100 : 88; button.Height = 30; button.Margin = new Padding(0, 0, 9, 0); toolbar.Controls.Add(button); }
        var check = new CheckBox { Text = "显示城市顺序参考线", ForeColor = Theme.Muted, AutoSize = true, Margin = new Padding(18, 6, 0, 0), BackColor = Theme.Background, Font = Theme.Font(8.5f) };
        check.CheckedChanged += (_, _) => { _canvas.ShowInitialRoute = check.Checked; _canvas.Invalidate(); }; toolbar.Controls.Add(check); return toolbar;
    }

    private static Label Heading(string text) => new() { Text = text, ForeColor = Theme.Text, Font = Theme.Font(10.5f, FontStyle.Bold), Margin = new Padding(0), TextAlign = ContentAlignment.MiddleLeft };
    private static ComboBox Combo(string[] entries)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Elevated, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat, Font = Theme.Font(9), Margin = new Padding(0) };
        combo.Items.AddRange(entries); combo.SelectedIndex = 0; return combo;
    }
    private static NumericUpDown Numeric(decimal min, decimal max, decimal value, int decimals = 0, decimal increment = 1) => new()
    {
        Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, Increment = increment, BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.Elevated, ForeColor = Theme.Text, Font = Theme.Font(9), TextAlign = HorizontalAlignment.Left, ThousandsSeparator = true,
        Dock = DockStyle.Fill, Margin = new Padding(0), AccessibleName = "算法参数"
    };
    private static Control Pair(string leftLabel, Control left, string rightLabel, Control right)
    {
        var panel = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0) };
        panel.ColumnStyles.Add(new(SizeType.Percent, 50)); panel.ColumnStyles.Add(new(SizeType.Percent, 50));
        panel.RowStyles.Add(new(SizeType.Absolute, 19)); panel.RowStyles.Add(new(SizeType.Percent, 100));
        var l = new Label { Text = leftLabel, Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.Font(8), Margin = new Padding(0) };
        var r = new Label { Text = rightLabel, Dock = DockStyle.Fill, ForeColor = Theme.Muted, Font = Theme.Font(8), Margin = new Padding(0, 0, 0, 0) };
        left.Margin = new Padding(0, 0, 8, 0); right.Margin = new Padding(0);
        left.AccessibleName = leftLabel; right.AccessibleName = rightLabel;
        panel.Controls.Add(l, 0, 0); panel.Controls.Add(r, 1, 0); panel.Controls.Add(left, 0, 1); panel.Controls.Add(right, 1, 1); return panel;
    }

    private AlgorithmKind SelectedAlgorithm => _algorithm.SelectedIndex == 0 ? AlgorithmKind.Genetic : AlgorithmKind.SimulatedAnnealing;
    private CityLayout SelectedLayout => (CityLayout)_layout.SelectedIndex;
    private void UpdateAlgorithmControls()
    {
        bool ga = SelectedAlgorithm == AlgorithmKind.Genetic;
        _geneticParameters.Visible = ga; _annealingParameters.Visible = !ga;
        _population.Enabled = ga && !_busy;
        _explanation.Text = ga
            ? "用一群路线共同进化：锦标赛选择、保序交叉、逆转变异与精英保留。最佳解持续保留。"
            : "用温度控制探索：2-opt 候选按概率接受，降温后逐渐收敛。拒绝候选不会改变当前解。";
    }

    private void ApplyArguments()
    {
        string? Value(string key) { int index = Array.IndexOf(_args, key); return index >= 0 && index + 1 < _args.Length ? _args[index + 1] : null; }
        void Number(string key, NumericUpDown input) { if (decimal.TryParse(Value(key), out decimal value)) input.Value = Math.Clamp(value, input.Minimum, input.Maximum); }
        Number("--seed", _seed); Number("--cities", _count); Number("--iterations", _iterations); Number("--population", _population);
        _layout.SelectedIndex = Value("--sample")?.ToLowerInvariant() switch { "ring" => 1, "random" => 2, _ => 0 };
        _algorithm.SelectedIndex = Value("--algorithm")?.ToLowerInvariant() == "sa" ? 1 : 0;
        if (Value("--size") is string size && size.Split('x') is [string w, string h] && int.TryParse(w, out int width) && int.TryParse(h, out int height))
            ClientSize = new Size(Math.Clamp(width, 1150, 2400), Math.Clamp(height, 820, 1600));
    }

    private void Generate()
    {
        if (_busy) return;
        _cities.Clear(); _cities.AddRange(CitySamples.Generate((int)_count.Value, (int)_seed.Value, SelectedLayout)); ResetResults();
        _status.Text = $"已生成 {_cities.Count} 座城市 · seed {(int)_seed.Value}。可以移动或增删城市。";
    }
    private void ResetResults()
    {
        _runId++; _snapshots.Clear(); _history.Clear(); _details.Text = "搜索结果来自实际运行。距离为画布坐标单位。";
        _status.Text = $"当前 {_cities.Count} 座城市 · 已清除旧结果，可以重新运行。";
        _progress.Value = 0; _canvas.Invalidate(); _chart.Invalidate(); _results.Invalidate();
    }
    private SolverOptions Options() => new()
    {
        Seed = (int)_seed.Value, Iterations = (int)_iterations.Value, Population = (int)_population.Value,
        EliteCount = (int)_elites.Value, MutationProbability = (double)_mutation.Value,
        InitialTemperature = (double)_temperature.Value, CoolingRate = (double)_cooling.Value
    };
    private void SetBusy(bool busy)
    {
        _busy = busy;
        foreach (var c in new Control[] { _run, _compare, _generate, _clear, _undo, _layout, _seed, _count, _iterations, _population, _elites, _mutation, _temperature, _cooling, _algorithm }) c.Enabled = !busy;
        _stop.Enabled = busy; _canvas.EditingEnabled = !busy; UpdateAlgorithmControls(); _canvas.Invalidate();
    }

    private async Task RunAsync(bool compare)
    {
        if (_busy) return;
        Problem problem; SolverOptions options;
        try { problem = new Problem(_cities); options = Options(); options.Validate(); }
        catch (ArgumentException ex) { _status.Text = ex.ParamName is "EliteCount" ? "精英数必须小于种群大小，请调整参数。" : $"参数不合适：{ex.Message}"; return; }
        ResetResults(); int runId = _runId;
        AlgorithmKind[] algorithms = compare ? [AlgorithmKind.Genetic, AlgorithmKind.SimulatedAnnealing] : [SelectedAlgorithm];
        using var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        SetBusy(true); _status.Text = compare ? "对照运行中：同一组城市与随机种子，分别计算 GA 和 SA。" : $"正在运行{Theme.AlgorithmName(algorithms[0])}…";
        void Receive(SolverSnapshot snapshot)
        {
            if (IsDisposed || Disposing || runId != _runId) return;
            _snapshots[snapshot.Algorithm] = snapshot;
            if (!_history.TryGetValue(snapshot.Algorithm, out var list)) { list = []; _history[snapshot.Algorithm] = list; }
            if (list.Count == 0 || list[^1].Iteration != snapshot.Iteration) list.Add(snapshot);
            double fraction = algorithms.Sum(kind => _snapshots.TryGetValue(kind, out var s) ? (double)s.Iteration / s.IterationLimit : 0) / algorithms.Length;
            _progress.Value = Math.Clamp((int)(fraction * 1000), 0, 1000);
            _details.Text = $"{Theme.AlgorithmName(snapshot.Algorithm)}  {snapshot.Iteration:N0}/{snapshot.IterationLimit:N0} 轮\n当前 {snapshot.CurrentLength:N1}  ·  {snapshot.Evaluations:N0} 次评估" + (snapshot.Algorithm == AlgorithmKind.SimulatedAnnealing ? $"  ·  T {snapshot.Temperature:G3}" : "");
            _canvas.Invalidate(); _chart.Invalidate(); _results.Invalidate();
        }
        IProgress<SolverSnapshot> progress = new Progress<SolverSnapshot>(Receive);
        try
        {
            SolverSnapshot[] final = await Task.Run(() => algorithms.Select(kind => Solver.Run(problem, kind, options, progress.Report, cancellation.Token)).ToArray(), cancellation.Token);
            if (IsDisposed) return;
            foreach (var snapshot in final) Receive(snapshot);
            _status.Text = compare ? "对照完成。实线为遗传算法，虚线为模拟退火；两者均显示全程最佳路线。" : "搜索完成，已显示全程最佳路线。可以继续调整城市或切换算法。";
        }
        catch (OperationCanceledException) { if (!IsDisposed) _status.Text = "搜索已停止，保留停止前的最佳路线。可重新运行或调整城市。"; }
        catch (Exception ex) { if (!IsDisposed) _status.Text = $"运行中断：{ex.Message}"; }
        finally { _cancellation = null; if (!IsDisposed) SetBusy(false); }
    }

    private void Export()
    {
        using var dialog = new SaveFileDialog { Title = "导出当前路线画布", Filter = "PNG 图片|*.png", FileName = $"route-lab-seed-{(int)_seed.Value}.png" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { using var image = new Bitmap(_canvas.Width, _canvas.Height); _canvas.DrawToBitmap(image, _canvas.ClientRectangle); image.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png); _status.Text = $"画布已导出至 {dialog.FileName}"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException) { _status.Text = $"无法保存图片：{ex.Message}"; }
    }
}
