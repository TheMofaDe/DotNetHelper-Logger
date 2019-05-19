using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetHelper_Logger.Helper
{
    public static class ExceptionHelper
    {
        public static string GetAllExceptionInfo(Exception ex)
        {
            var sbexception = new StringBuilder();
            var e = ex;
            var i = 1;
            sbexception.Append(GetExceptionInfo(e, i));

            while (e.InnerException != null)
            {
                i++;
                e = e.InnerException;
                sbexception.Append(GetExceptionInfo(e, i));
            }

            return sbexception.ToString();
        }

        private static string GetExceptionInfo(Exception ex, int count)
        {
            var sbException = new StringBuilder();
            sbException.AppendLine();

            sbException.AppendLine("************************************************");
            sbException.AppendLine($" Timestamp : {DateTime.Now:MM/dd/yyyy HH:mm:ss}");
            sbException.AppendLine($" Inner Exception : No.{count} ");
            sbException.AppendLine($" Exception Type : {ex.GetType().FullName} ");
            sbException.AppendLine($" Error Message : {ex.Message} ");
            if (ex.Data.Count > 0)
            {
                sbException.AppendLine($" Data parameters Count at Source :{ex.Data.Count}");
                try
                {
                    if (ex.Data.Keys.Count > 0)
                    {
                        sbException.AppendLine("==================================================");
                        foreach (var key in ex.Data.Keys)
                        {
                            try
                            {
                                if (key != null)
                                {
                                    var skey = Convert.ToString(key);
                                    sbException.AppendLine($" Key :{skey} , Value:{Convert.ToString(ex.Data[key])}");
                                }
                                else
                                {
                                    sbException.AppendLine(" Key is null");
                                }
                            }
                            catch (Exception e1)
                            {
                                sbException.AppendLine(
                                    $"**  Exception occurred when writing log *** [{e1.Message}] ");
                            }
                        }
                        sbException.AppendLine("==================================================");
                    }
                }
                catch (Exception ex1)
                {
                    sbException.AppendLine($"**  Exception occurred when writing log *** [{ex1.Message}] ");
                }
            }

            sbException.AppendLine($" Source : {ex.Source} ");
            sbException.AppendLine($" StackTrace : {ex.StackTrace} ");
            sbException.AppendLine($" TargetSite : {ex.TargetSite} ");
            sbException.AppendLine($" Finished Writing Exception info :{count} ");
            sbException.AppendLine("************************************************");
            sbException.AppendLine();

            return sbException.ToString();
        }


        public static string GetStackTrace(Exception e)
        {
            try
            {
                var stacksOnDeck = e.StackTrace.Split(Convert.ToChar(" ")).ToList();
                var i = 0;
                var fileName = "";
                var methodName = "";
                var lineNumber = "";

                foreach (var stack in stacksOnDeck)
                {
                    if (stack.Replace(" ", "").ToLower() == "at")
                    {
                        if (stacksOnDeck[i + 1].Contains("Xamarin.Forms") || stacksOnDeck[i + 1].Contains(".."))
                        {

                        }
                        else
                        {
                            methodName = stacksOnDeck[i + 1];
                        }
                    }
                    i++;
                }
                var t = stacksOnDeck[stacksOnDeck.Count - 2].Split(Convert.ToChar(@"\"));
                var obj = t[t.Length - 1];
                fileName = obj.Split(':')[0];
                lineNumber = obj.Split(':')[1];
                if (!int.TryParse(lineNumber, out var r))
                {
                    lineNumber = stacksOnDeck[stacksOnDeck.Count - 1];
                }
                var message = $" In File --- {fileName} Line #  {lineNumber} --- Method Name --- {methodName} ";
                return message;
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(error.Message);
            }
            return "";
        }

    }
}
