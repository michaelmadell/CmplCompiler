using CmplPiler.Core;

namespace CmplPiler.Cli
{
    internal static class Program
    {
        private const string Usage = """
            cmpl - build orchestrator for .cmpl project files

            Usage:
              cmpl <file.cmpl> [options]

            Options:
              -p, --profile <name>   Build the named profile (default: first profile)
              -l, --list             List the profiles in the file and exit
              -n, --dry-run          Print the commands without running them
              -h, --help             Show this help
            """;

        private static async Task<int> Main(string[] args)
        {
            string? file = null;
            string? profileName = null;
            bool list = false;
            bool dryRun = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h" or "--help":
                        Console.WriteLine(Usage);
                        return 0;
                    case "-l" or "--list":
                        list = true;
                        break;
                    case "-n" or "--dry-run":
                        dryRun = true;
                        break;
                    case "-p" or "--profile":
                        if (++i >= args.Length)
                            return Fail("--profile requires a value.");
                        profileName = args[i];
                        break;
                    default:
                        if (args[i].StartsWith('-'))
                            return Fail($"Unknown option '{args[i]}'.");
                        if (file != null)
                            return Fail("Only one .cmpl file may be given.");
                        file = args[i];
                        break;
                }
            }

            if (file == null)
            {
                Console.WriteLine(Usage);
                return 1;
            }

            CmplProject project;
            try
            {
                project = CmplParser.LoadFile(file);
            }
            catch (Exception ex) when (ex is CmplValidationException or FileNotFoundException or YamlDotNet.Core.YamlException)
            {
                return Fail(ex.Message);
            }

            if (list)
            {
                Console.WriteLine($"{project.ProjectName} ({file})");
                foreach (var p in project.Profiles)
                    Console.WriteLine($"  {p.Name}  [{p.BuildSystem}]");
                return 0;
            }

            CmplProfile? profile = profileName == null
                ? project.Profiles.FirstOrDefault()
                : project.Profiles.FirstOrDefault(p => p.Name == profileName);

            if (profile == null)
                return Fail(profileName == null
                    ? "The file contains no profiles."
                    : $"Profile '{profileName}' not found. Use --list to see available profiles.");

            if (dryRun)
            {
                foreach (var task in CommandGenerator.GenerateTasks(project, profile))
                    Console.WriteLine(task);
                return 0;
            }

            Console.WriteLine($"--- Building {project.ProjectName} [{profile.Name}] ---");

            var runner = new BuildRunner();
            runner.OutputReceived += Console.WriteLine;
            runner.ErrorReceived += Console.Error.WriteLine;

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                int exitCode = await runner.RunAsync(project, profile, cts.Token);
                Console.WriteLine(exitCode == 0
                    ? "--- Build succeeded ---"
                    : $"--- Build failed (exit code {exitCode}) ---");
                return exitCode == 0 ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Build cancelled.");
                return 130;
            }
            catch (CmplValidationException ex)
            {
                return Fail(ex.Message);
            }
        }

        private static int Fail(string message)
        {
            Console.Error.WriteLine($"cmpl: {message}");
            return 1;
        }
    }
}
