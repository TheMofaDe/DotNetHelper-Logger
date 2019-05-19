// ***********************************************************************
// Assembly         : TheMoFaDe:
// Author           : Joseph McNeal Jr 
// Created          : 02-10-2017
//
// Last Modified By : Joseph McNeal Jr
// Last Modified On : 09-04-2017
// ***********************************************************************
// <copyright>
//             Copyright (C) 2017 Joseph McNeal Jr - All Rights Reserved
//             You may use, distribute and modify this code under the
//             terms of the TheMoFaDe: license,
//             You should have received a copy of the TheMoFaDe: license with
//             this file. If not, please write to: josephmcnealjr@mofade.com
//</copyright>
// <summary>   </summary>
// ***********************************************************************

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DotNetHelper_IO;
using DotNetHelper_Logger.Helper;
using DotNetHelper_Logger.Interface;

namespace DotNetHelper_Logger
{
    /// <summary>
    /// Class TheMoFaDeDI.Logger
    /// </summary>
    public class FileLogger : IFileLogger 
    {

  
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        /// <summary>
        /// we use a function because if your prefix include DateTime.Now the value with be static therefore we invoke a function that returns a string
        /// </summary>
        public Func<string> Prefix { get; set; } = () => $"{Environment.NewLine} {DateTime.Now} Application Name - {GetApplicationName()}  Device - {Environment.MachineName} ------------- ";


        /// <summary>
        /// Max Size In Bytes That Log Files Are Allow To Reach. Anything Zero Or Less Will Be Consider Infinite. Defaults To 10 MB
        /// </summary>
        /// <remarks>DEFAULT IS 10000000 which is 10 MB</remarks>
        public long MaxFileSize { get; set; } = 10000000;


        /// <inheritdoc />
        /// <summary>
        /// if set invokes onMaxCreatedInterval
        /// </summary>
        /// <remarks>DEFAULT IS NULL</remarks>
        public TimeSpan? MaxLifespan { get; set; }



        public event EventHandler<FileObject> MaxLifespanReached;
        public event EventHandler<FileObject> MaxSizeReached;
        public event EventHandler<Exception> OnFailedLogAttempt;
        public event EventHandler<UnhandledExceptionEventArgs> OnUnHandledException;
        public event EventHandler<EventArgs> OnProcessExit;



        public bool IsLoggingEnable { get; set; } = true;

        /// <summary>
        /// Name Self Explanatory
        /// </summary>
        public FileObject LogFile { get; set; }

        /// <summary>
        /// Name Self Explanatory
        /// </summary>
        public FileObject ObjectLogFile { get; set; }

        /// <summary>
        /// Name Self Explanatory
        /// </summary>
        public FileObject ErrorsOnlyLogFile { get; set; }




        /// <summary>
        /// If True Any Logged Errors That Is Excuted Will Go To An Additional Log File Containing Only Errors 
        /// </summary>
        /// <remarks>Default is true</remarks>
        public bool ErrorsOnlyFileEnable { get; set; } = true;


        private bool? _TruncateOnAppStart { get; set; } = null;
        /// <summary>
        /// Truncate All Logs On Application Start
        /// </summary>
        public bool TruncateOnAppStart
        {
            get => _TruncateOnAppStart.GetValueOrDefault(false);
            set
            {
                if (_TruncateOnAppStart == null && value == true)
                {
                    LogFile.CreateOrTruncate();
                    if (ErrorsOnlyFileEnable)
                        ErrorsOnlyLogFile.CreateOrTruncate();
                }
                _TruncateOnAppStart = value;
            }
        }


        private string ApplicationName { get; } = GetApplicationName();


        /// <summary>
        /// Gets the application name 
        /// </summary>
        /// <returns></returns>
        private static string GetApplicationName()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var name = assembly?.FullName.Split(',')[0];
            return  name;
    }


        public FileLogger()
        {


            var logsFolder = GetDefaultLogFolder() + "Logs" + Path.DirectorySeparatorChar;

            LogFile = new FileObject($"{logsFolder}{ApplicationName}_Logs.txt");
            ObjectLogFile = new FileObject($"{logsFolder}{ApplicationName}_ObjectLogs.txt");
            ErrorsOnlyLogFile = new FileObject($"{logsFolder}{ApplicationName}_Errors.txt");

            // CHECKPOINT
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                var ex = (Exception)args.ExceptionObject;
                Console.WriteLine($"Exception caught by Global Exception Handler. {ex.Message}.");
                LogError($"Exception caught by Global Exception Handler", ex);
                OnUnHandledException?.Invoke(sender, args);
                if (args.IsTerminating)
                    Environment.Exit(-1);
            };
            AppDomain.CurrentDomain.ProcessExit += delegate (object sender, EventArgs args)
            {
                OnProcessExit?.Invoke(sender, args);
            };

         

            if (_TruncateOnAppStart == null && TruncateOnAppStart == true)
            {
                LogFile.CreateOrTruncate();
                if (ErrorsOnlyFileEnable)
                    ErrorsOnlyLogFile.CreateOrTruncate();
            }
            
        }



        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <returns>System.String.</returns>
        private  string GetDefaultLogFolder()
        {

            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var appName = assembly?.FullName.Split(',')[0];
            var repoName = $"{appName}_AppRepo";
            try
            {

                var path = AppDomain.CurrentDomain.BaseDirectory;
                if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {

                }
                else
                {
                    path = path + Path.DirectorySeparatorChar;
                }
              
                return $"{path}{repoName}{Path.DirectorySeparatorChar}";
           
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
            
                    var assembly2 = Assembly.GetExecutingAssembly();
                    var tempPath = Path.GetDirectoryName(assembly2.Location) + Path.DirectorySeparatorChar;
                
               return $"{tempPath}{repoName}{Path.DirectorySeparatorChar}";
             
            }


         
        }



        /// <summary>
        /// Checks if loggings is enabled and whether or not the file should be deleted or purge based on configurations 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool CheckRequirements(FileObject file)
        {
            if (!IsLoggingEnable) return false;
            file.RefreshObject();

            if (file.Exist == false)
            {

                file.CreateOrTruncate();

            }

            if (MaxLifespan != null && DateTime.Now - file.CreationTime.GetValueOrDefault(DateTime.Now) > MaxLifespan.Value)
            {
                OnMaxLifespanReached(file);
                // Log($"Previous Logs has been delete due to the business logic being set to truncate every {DeleteOnTimeSpan.Value.Days} Days {DeleteOnTimeSpan.Value.Hours} Hours  & {DeleteOnTimeSpan.Value.Minutes}  Minutes");
            }
            if (MaxFileSize <= 0 || !File.Exists(file.FullFilePath))
            {
                return true;
            }

            if (MaxFileSize <= file.FileSize)
            {
                OnMaxSizeReached(file);
                //if (DeleteOnMaxSizeReach)
                //{
                //    file.CreateOrTruncate();
                //}
                //else
                //{
                // //   Console.WriteLine($"Warning :: Log File {file.FullFilePath} Has Reach Max Capacity. Could Not Log Content To File Please Take Action Or Configure Your Application Source To Handle This Senario");
                //} 
                return false;
            }
            else
            {

            }
            return true;
        }



        /// <summary>
        /// Logs the content to the log file 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="logSeverity"></param>
        public void Log(string content, LogSeverity logSeverity = LogSeverity.Information)
        {
            try
            {
                if (CheckRequirements(LogFile))
                {
                    LogFile.WriteContentToFile(GetDefaultVerbiage(content),Encoding);
                }
            }
            catch (Exception error)
            {
                OnFailedLogAttempt?.Invoke(this, error);
            }
        }

        /// <summary>
        /// Logs the exception to the log file
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="logSeverity"></param>
        public void LogError(Exception ex, LogSeverity logSeverity = LogSeverity.Error)
        {
            try
            {
                if (CheckRequirements(LogFile))
                {
                    var content = GetDefaultVerbiage(ex) + ExceptionHelper.GetStackTrace(ex);
                    LogFile.WriteContentToFile(content,Encoding);
                }
                if (CheckRequirements(ErrorsOnlyLogFile))
                {
                    ErrorsOnlyLogFile.WriteContentToFile(ExceptionHelper.GetAllExceptionInfo(ex),Encoding);
                }
            }
            catch (Exception error)
            {
                OnFailedLogAttempt?.Invoke(this, error);
            }

        }

        /// <summary>
        /// Logs the exception to the log file 
        /// </summary>
        /// <param name="notes">notes that will be logged prior to logging the exception </param>
        /// <param name="ex"></param>
        /// <param name="logSeverity"></param>
        public void LogError(string notes, Exception ex, LogSeverity logSeverity = LogSeverity.Error)
        {

            var currentError = ex;
            var i = 0;

            try
            {
                while (currentError.InnerException != null)
                {
                    var subNotes = "";
                    if (i > 0)
                    {
                        subNotes += $" The Following Exception Was Buried {i} Levels Deep ";
                    }
                    i++;
                    if (string.IsNullOrEmpty(notes)) notes = "";

                    if (CheckRequirements(LogFile))
                    {
                        var content = GetDefaultVerbiage(GetDefaultVerbiage(ex) + subNotes + ExceptionHelper.GetStackTrace(ex));
                        LogFile.WriteContentToFile(content,Encoding);
                    }
                    currentError = currentError.InnerException;
                }
                if (CheckRequirements(ErrorsOnlyLogFile))
                {
                    ErrorsOnlyLogFile.WriteContentToFile(ExceptionHelper.GetAllExceptionInfo(ex),Encoding);
                }
            }
            catch (Exception error)
            {
                OnFailedLogAttempt?.Invoke(this, error);
            }


        }



        /// <summary>
        /// Writes the message to the Diagnostic output
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logSeverity"></param>
        public void DebugWrite(string message, LogSeverity logSeverity = LogSeverity.Information)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }


        /// <summary>
        /// Writes the message to the console 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logSeverity"></param>
        public void ConsoleWrite(string message, LogSeverity logSeverity = LogSeverity.Information)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Writes the message to the console and log file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logSeverity"></param>
        public void ConsoleAndLog(string message, LogSeverity logSeverity = LogSeverity.Information)
        {
            ConsoleWrite(message);
            Log(message);
        }

        public void ConsoleAndLog(Exception error, LogSeverity logSeverity = LogSeverity.Error)
        {
            ConsoleWrite(error.Message);
            LogError(error);
        }



        private string GetDefaultVerbiage(Exception ex)
        {

            // return $"{DateTime.Now}   Application Name - {ApplicationName} AppVersion - {App.ApplicationVersion}  Device - {DeviceInformation.Model} {DeviceInformation.MachineName}  OS - {DeviceInformation.Platform}  Version # {DeviceInformation.Version}   -------------  An Error Has Occur Error Message : {ex.Message}";

            return$"{Prefix?.Invoke()} An Error Has Occur Error Message : {ex.Message}";

        }
        private string GetDefaultVerbiage(string a)
        {
            return !string.IsNullOrEmpty(a)
                ? $"{Prefix?.Invoke()} {a}"
                : $"{Prefix?.Invoke()} NULL";
            //return !string.IsNullOrEmpty(a)
            //	? $"{DateTime.Now}   Application Name - {ApplicationName} AppVersion - {App.ApplicationVersion}  Device - {DeviceInformation.Model} {DeviceInformation.MachineName}  OS - {DeviceInformation.Version}   -------------  A Developer Left Logging : {a}"
            //	: $"{DateTime.Now}   Application Name - {ApplicationName} AppVersion - {App.ApplicationVersion}  Device - {DeviceInformation.Model} {DeviceInformation.MachineName}  OS - {DeviceInformation.Version}   -------------   A Developer Decided To Log A NULL/EMPTY Object SMH";
        }



        

        protected virtual void OnMaxSizeReached(FileObject file)
        {
            MaxSizeReached?.Invoke(this, file);
        }

        protected virtual void OnMaxLifespanReached(FileObject file)
        {
            MaxLifespanReached?.Invoke(this, file);
        }
    }







}
