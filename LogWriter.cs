using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Formula_1_Media_Handler;

internal class LogWriter
{
    internal static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    internal static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        Logger.Fatal(ex);
    }

    internal enum Type
    {
        FATAL,
        ERROR,
        WARNING,
        INFO,
        DEBUG,
        TRACE
    }

    /// <summary>
    /// Log writer. Simultaneously writes to console and to a log file in the executing assembly directory. Includes date and time fields. Parses out all newlines.
    /// </summary>
    /// <param name="logMessage">Descriptive message for the log entry.</param>
    /// <param name="logType">Logging level: FATAL, ERROR, WARNING, INFO, DEBUG, TRACE</param>
    /// <param name="logException">(Optional) Exception object.</param>
    /// <param name="openLogFile">(Optional) Automatically opens the log file in Notepad when set to true. Only use with critical errors, such as FATAL and ERROR.</param>
    /// <param name="consoleEndString">(Optional) Default value is two newlines. Helps separate the log entries from the selection menu.</param>
    internal static void Write(string logMessage, Type logType = Type.INFO, Exception? logException = null, bool openLogFile = false, string consoleEndString = "\n\n")
    {
        var logOutput = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}   ::   [{Enum.GetName(typeof(Type), logType)}] {logMessage.Replace(Environment.NewLine, " ")}";
        if (logException != null) { logOutput += $"   ::   {logException.Message}"; }

        using (StreamWriter sw = File.AppendText(Globals.LOG_PATH))
        {
            sw.WriteLine(logOutput);
            Console.WriteLine(logOutput + consoleEndString);
        }

        if (openLogFile) { Process.Start("notepad.exe", Globals.LOG_PATH); }
    }
}

internal class UnhandledException
{
    public static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    internal static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        Logger.Fatal(ex);
    }
}
