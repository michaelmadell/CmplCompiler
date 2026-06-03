using System;
using System.IO;
using System.Windows.Forms;

namespace CmplPiler
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("=== PROGRAM STARTED ===");
            string logFile = Path.Combine(Path.GetTempPath(), "CmplPiler_Debug.log");

            try
            {
                Console.WriteLine($"Writing to log: {logFile}");
                File.WriteAllText(logFile, $"[{DateTime.Now}] Application starting...\n");

                Console.WriteLine("Initializing application...");
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                File.AppendAllText(logFile, $"[{DateTime.Now}] Calling ApplicationConfiguration.Initialize()...\n");
                ApplicationConfiguration.Initialize();

                Console.WriteLine("Creating Form1...");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Creating Form1...\n");
                var form = new Form1();

                Console.WriteLine("Running application...");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Calling Application.Run()...\n");
                Application.Run(form);

                File.AppendAllText(logFile, $"[{DateTime.Now}] Application.Run() completed normally.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n");
                MessageBox.Show($"Application failed to start: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nLog file: {logFile}", 
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Console.WriteLine("=== PROGRAM ENDING ===");
        }
    }
}