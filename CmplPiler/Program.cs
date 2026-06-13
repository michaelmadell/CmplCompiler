using CmplPiler.Cli;

namespace CmplPiler
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point. Windows builds open the GUI when launched
        ///  with no arguments (or with --gui); any other invocation runs the
        ///  CLI. Non-Windows builds are CLI-only.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
#if WINDOWS
            bool wantsGui = args.Length == 0 || args.Contains("--gui");
            if (wantsGui)
            {
                // An optional .cmpl path alongside --gui is preloaded
                string? projectFile = args.FirstOrDefault(a => !a.StartsWith('-'));
                try
                {
                    // To customize application configuration such as set high DPI settings or default font,
                    // see https://aka.ms/applicationconfiguration.
                    ApplicationConfiguration.Initialize();
                    Application.Run(new Form1(projectFile));
                    return 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Application failed to start: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }
            }

            // WinExe processes have no console of their own; reattach to the
            // invoking shell's so CLI output is visible.
            ConsoleInterop.AttachToParentConsole();
#endif
            return CliRunner.RunAsync(args).GetAwaiter().GetResult();
        }
    }
}