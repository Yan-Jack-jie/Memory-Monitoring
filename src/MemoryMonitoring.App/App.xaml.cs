using System.Configuration;
using System.Data;
using System.Windows;

namespace MemoryMonitoring.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    static App()
    {
        EnsureWindowsDirectoryEnvironment();
    }

    private static void EnsureWindowsDirectoryEnvironment()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WINDIR")))
        {
            return;
        }

        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        Environment.SetEnvironmentVariable(
            "WINDIR",
            string.IsNullOrWhiteSpace(systemRoot) ? @"C:\Windows" : systemRoot);
    }
}

