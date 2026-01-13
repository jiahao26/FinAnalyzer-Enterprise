using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace FinAnalyzer.Engine
{
    /// <summary>
    /// Centralized logging utility for debugging the RAG pipeline.
    /// All logs are written to debug.log in the working directory.
    /// </summary>
    public static class CentralLogger
    {
        private static readonly object _lock = new();
        private static readonly string LogFilePath;
        private static bool _initialized = false;

        static CentralLogger()
        {
            LogFilePath = Path.Combine(AppContext.BaseDirectory, "debug.log");
        }

        /// <summary>
        /// Initialize the logger, clearing any existing log file.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    File.WriteAllText(LogFilePath, $"=== FinAnalyzer Debug Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
                    _initialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CentralLogger] Failed to initialize: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log an informational message.
        /// </summary>
        public static void Info(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
        {
            Log("INFO", message, caller, file);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
        {
            Log("WARN", message, caller, file);
        }

        /// <summary>
        /// Log an error message with optional exception details.
        /// </summary>
        public static void Error(string message, Exception? ex = null, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
        {
            var errorMessage = ex != null 
                ? $"{message}\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace: {ex.StackTrace}"
                : message;
            Log("ERROR", errorMessage, caller, file);
        }

        /// <summary>
        /// Log a debug/trace message.
        /// </summary>
        public static void Debug(string message, [CallerMemberName] string caller = "", [CallerFilePath] string file = "")
        {
            Log("DEBUG", message, caller, file);
        }

        /// <summary>
        /// Log a step in the pipeline with timing information.
        /// </summary>
        public static void Step(string stepName, string details = "")
        {
            var msg = string.IsNullOrEmpty(details) 
                ? $"[STEP] {stepName}"
                : $"[STEP] {stepName}: {details}";
            Log("STEP", msg, "", "");
        }

        private static void Log(string level, string message, string caller, string file)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    Initialize();
                }

                try
                {
                    var className = Path.GetFileNameWithoutExtension(file);
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logLine = string.IsNullOrEmpty(caller) 
                        ? $"[{timestamp}] [{level}] {message}\n"
                        : $"[{timestamp}] [{level}] {className}.{caller}: {message}\n";
                    
                    File.AppendAllText(LogFilePath, logLine);
                    
                    // Also write to console for immediate visibility
                    Console.WriteLine(logLine.TrimEnd());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CentralLogger] Write failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the path to the log file.
        /// </summary>
        public static string GetLogPath() => LogFilePath;
    }
}
