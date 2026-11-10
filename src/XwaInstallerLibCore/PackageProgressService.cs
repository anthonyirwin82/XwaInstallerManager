using System.Timers;
using Timer = System.Timers.Timer;

namespace XwaInstallerLibCore;

internal class PackageProgressService
{
    private const double TimerInterval = 1000.0;

    private readonly ConsoleWindow _waitConsoleWindow;
    private int _initStep;
    private Timer? _watcherTimer;
    private string? _watcherFilename;

    public PackageProgressService(ConsoleWindow waitConsoleWindow)
    {
        _waitConsoleWindow = waitConsoleWindow;
    }

    public void SetInitStep(int initStep)
    {
        _initStep = initStep;
    }

    public void ShowProgress(string filename)
    {
        _watcherTimer?.Stop();

        if (_watcherTimer is null)
        {
            _watcherTimer = new Timer(TimerInterval)
            {
                AutoReset = true
            };
            _watcherTimer.Elapsed += WatcherTimer_Elapsed;
        }

        _watcherFilename = filename;
        _watcherTimer.Start();
    }

    private void WatcherTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (string.IsNullOrEmpty(_watcherFilename) || !File.Exists(_watcherFilename))
        {
            return;
        }

        var file = new FileInfo(_watcherFilename);
        _waitConsoleWindow.SetBusyStep(_initStep, file.Length);
    }

    public void Stop()
    {
        if (_watcherTimer is not null)
        {
            _watcherTimer.Stop();
            _watcherTimer.Dispose();
            _watcherTimer = null;
        }

        _watcherFilename = null;
    }
}
