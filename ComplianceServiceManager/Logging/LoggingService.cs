using System.Collections.ObjectModel;
using System.Windows.Threading;
using ComplianceServiceManager.Models;
using Serilog;

namespace ComplianceServiceManager.Logging;

public sealed class LoggingService : ILoggingService
{
    private readonly Dispatcher dispatcher;
    private const int MaxEntries = 5000;

    public LoggingService(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public ObservableCollection<LogEntryModel> Entries { get; } = new();

    public void Log(LogLevel level, string source, string message)
    {
        var entry = new LogEntryModel
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Source = source,
            Message = message
        };

        dispatcher.BeginInvoke(() =>
        {
            Entries.Add(entry);
            while (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        });

        LogToSerilog(entry);
    }

    public void Info(string source, string message) => Log(LogLevel.Info, source, message);

    public void Warning(string source, string message) => Log(LogLevel.Warning, source, message);

    public void Error(string source, string message) => Log(LogLevel.Error, source, message);

    public void Build(string source, string message) => Log(LogLevel.Build, source, message);

    public void Process(string source, string message) => Log(LogLevel.Process, source, message);

    private static void LogToSerilog(LogEntryModel entry)
    {
        var text = "[{Source}] {Message}";
        switch (entry.Level)
        {
            case LogLevel.Error:
                Serilog.Log.Error(text, entry.Source, entry.Message);
                break;
            case LogLevel.Warning:
                Serilog.Log.Warning(text, entry.Source, entry.Message);
                break;
            default:
                Serilog.Log.Information(text, entry.Source, entry.Message);
                break;
        }
    }
}
