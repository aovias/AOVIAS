using System.Collections.ObjectModel;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Logging;

public interface ILoggingService
{
    ObservableCollection<LogEntryModel> Entries { get; }

    void Log(LogLevel level, string source, string message);

    void Info(string source, string message);

    void Warning(string source, string message);

    void Error(string source, string message);

    void Build(string source, string message);

    void Process(string source, string message);
}
