namespace RouteLab.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new Font("Microsoft YaHei UI", 9.5f));
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "Route Lab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Application.Run(new MainForm(args));
    }
}
