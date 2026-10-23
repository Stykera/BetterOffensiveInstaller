using System.Windows.Forms;

namespace BetterOffensiveInstaller;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        SystemTheme.AllowSystemAppMode();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
