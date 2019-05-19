using System;
using System.Net.Mail;
using System.Text;
using DotNetHelper_IO;


namespace DotNetHelper_Logger.Interface
{
    public interface IFileLogger : ILogger 
    {
        Encoding Encoding { get; set; }
        TimeSpan? MaxLifespan { get; set; }
        long MaxFileSize { get; set; }
        event EventHandler<FileObject> MaxSizeReached;
        event EventHandler<FileObject> MaxLifespanReached;
        FileObject LogFile { get; set; } 
        FileObject ObjectLogFile { get; set; } 
        FileObject ErrorsOnlyLogFile { get; set; } 
    }



    public interface ILogger 
    {
        event EventHandler<Exception> OnFailedLogAttempt;
        bool IsLoggingEnable { get; set; }
        void LogError(Exception error, LogSeverity logSeverity = LogSeverity.Error);
        void Log(string content, LogSeverity logSeverity = LogSeverity.Information);
        void ConsoleAndLog(string content, LogSeverity logSeverity = LogSeverity.Information);
        void ConsoleAndLog(Exception error, LogSeverity logSeverity = LogSeverity.Error);
        void ConsoleWrite(string message, LogSeverity logSeverity = LogSeverity.Information);
        void LogError(string notes, Exception ex, LogSeverity logSeverity = LogSeverity.Error);

        event EventHandler<UnhandledExceptionEventArgs>  OnUnHandledException;
        // Occurs when the default application domain's parent process exits.
        event EventHandler<EventArgs> OnProcessExit;

    
    }




   
}
