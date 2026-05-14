using Gesturing.Models;
using Gesturing.Services;

namespace Gesturing;

static class Program
{
    [STAThread]
    static void Main()
    {
        var config = ConfigManager.Load();

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}