using System;
using System.Reflection;
using DotNetHelper_Logger.Helper;
using DotNetHelper_Logger.Interface;
#if NETFRAMEWORK
using System.Diagnostics;
#endif
namespace DotNetHelper_Logger
{


    public enum EventLogSource
    {
        Application 
       ,Security
       ,Setup
       ,System
    }

   public class EventLogger : ILogger
   {

        public object ThreadSafe { get; } = new object();
        public string Source { get; } = "Application"; // GetApplicationName();
        public event EventHandler<Exception> OnFailedLogAttempt;
        public bool IsLoggingEnable { get; set; } = true;
        public event EventHandler<UnhandledExceptionEventArgs> OnUnHandledException;
        public event EventHandler<EventArgs> OnProcessExit;

        public EventLogger(string source)
        {
            Init();
            Source = source;
        }

        public EventLogger()
        {
           Init();
            //var data = new EventSourceCreationData(Source, "Application")
            //{
            //    MessageResourceFile = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "windowsevents.log"
            //};
            //EventLog.CreateEventSource(data);
        }

       private void Init()
       {
           AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
               var ex = (Exception)args.ExceptionObject;
               Console.WriteLine($"Exception caught by Global Exception Handler. {ex.Message}.");
               LogError($"Exception caught by Global Exception Handler", ex);
               OnUnHandledException?.Invoke(sender, args);
               if (args.IsTerminating)
                   Environment.Exit(0);
           };
           AppDomain.CurrentDomain.ProcessExit += delegate (object sender, EventArgs args)
           {
               OnProcessExit?.Invoke(sender, args);
           };
        }



    
    
        public void LogError(Exception error, LogSeverity logSeverity = LogSeverity.Error)
        {
          LogError("",error,logSeverity);
        }


       public void LogError(string notes, Exception ex, LogSeverity logSeverity = LogSeverity.Error)
       {
           if(IsLoggingEnable)
           try
           {
#if NETFRAMEWORK
                    var eventLog = new EventLog {Source = Source};
               eventLog.WriteEntry(notes + ExceptionHelper.GetAllExceptionInfo(ex), MapToEventLogType(logSeverity));
#endif
           }
           catch (Exception error)
           {
               OnFailedLogAttempt?.Invoke(this,error);
           }
       }


        public void Log(string content, LogSeverity logSeverity = LogSeverity.Information)
        {
            if (IsLoggingEnable)
                try {
#if NETFRAMEWORK
                    var eventLog = new EventLog {Source = Source};
            eventLog.WriteEntry(content,MapToEventLogType(logSeverity));
#endif
                }
            catch (Exception error)
            {
                OnFailedLogAttempt?.Invoke(this, error);
            }
        }

        public void ConsoleAndLog(string content, LogSeverity logSeverity = LogSeverity.Information)
        {
            Log(content, logSeverity);
            Console.WriteLine(content);
        }

        public void ConsoleAndLog(Exception error, LogSeverity logSeverity = LogSeverity.Error)
        {
            LogError(error,logSeverity);
            ConsoleWrite(error.Message);
        }

        public void ConsoleWrite(string message, LogSeverity logSeverity = LogSeverity.Information)
        {
            Console.WriteLine(message);
        }



      

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetApplicationName()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly() ?? Assembly.GetEntryAssembly();
                return assembly?.FullName.Split(',')[0];
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
                return null;
            }
        }

#if NETFRAMEWORK
        public LogSeverity MapToLogSeverity(EventLogEntryType type)
       {
           switch (type)
           {
               case EventLogEntryType.Error:
                   return LogSeverity.Error;

               case EventLogEntryType.Warning:
                   return LogSeverity.Warning;
               case EventLogEntryType.Information:
                   return LogSeverity.Information;

               case EventLogEntryType.SuccessAudit:

                   break;
               case EventLogEntryType.FailureAudit:
                   break;
               default:
                   throw new ArgumentOutOfRangeException(nameof(type), type, null);
           }
           return LogSeverity.Trace;
       }


        private EventLogEntryType MapToEventLogType(LogSeverity type)
       {
           switch (type)
           {
               case LogSeverity.Trace:
                   return EventLogEntryType.Information;
               case LogSeverity.Debug:
                   return EventLogEntryType.Information;
               case LogSeverity.Information:
                   return EventLogEntryType.Information;
               case LogSeverity.Warning:
                   return EventLogEntryType.Warning;
               case LogSeverity.Error:
                   return EventLogEntryType.Error;
               case LogSeverity.Fatal:
                   return EventLogEntryType.Error;
               default:
                   throw new ArgumentOutOfRangeException(nameof(type), type, null);
           }
       }

#endif

    }
}
