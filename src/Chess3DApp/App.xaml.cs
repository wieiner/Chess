using System.Windows;

namespace ChessApp;

public partial class Chess3DApp : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new Chess3DWindow();
        MainWindow = window;
        window.Show();
        await window.ApplyStartupArgumentsAsync(e.Args);
    }
}
