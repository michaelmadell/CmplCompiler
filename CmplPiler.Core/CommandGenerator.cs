namespace CmplPiler.Core
{
    public static class CommandGenerator
    {
        public static List<BuildTask> GenerateTasks(CmplProject project, CmplProfile profile)
        {
            var tasks = new List<BuildTask>();
            string? baseDir = project.BaseDirectory;

            if (profile.PreBuild != null)
                tasks.AddRange(profile.PreBuild.Select(cmd => ShellTask(cmd, baseDir)));

            switch (profile.BuildSystem)
            {
                case "direct":
                    tasks.Add(GenerateDirectTask(project, profile));
                    break;
                case "cmake":
                    tasks.AddRange(GenerateCmakeTasks(profile, baseDir));
                    break;
                case "dotnet":
                    tasks.Add(GenerateDotnetTask(profile, baseDir));
                    break;
                case "msbuild":
                    tasks.Add(GenerateMsbuildTask(profile, baseDir));
                    break;
                default:
                    throw new CmplValidationException(
                        $"Profile '{profile.Name}': unknown build_system '{profile.BuildSystem}'.");
            }

            if (profile.PostBuild != null)
                tasks.AddRange(profile.PostBuild.Select(cmd => ShellTask(cmd, baseDir)));

            return tasks;
        }

        /// <summary>
        /// Wraps a command line in the platform shell so that pipes, globs and
        /// builtins behave the way users expect from a build script.
        /// </summary>
        public static BuildTask ShellTask(string commandLine, string? workingDirectory)
        {
            if (OperatingSystem.IsWindows())
            {
                return new BuildTask
                {
                    Command = "cmd.exe",
                    Arguments = $"/c {commandLine}",
                    WorkingDirectory = workingDirectory
                };
            }

            return new BuildTask
            {
                Command = "/bin/sh",
                ArgumentList = new List<string> { "-c", commandLine },
                WorkingDirectory = workingDirectory
            };
        }

        private static string Resolve(string path, string? baseDir) =>
            baseDir != null && !Path.IsPathRooted(path)
                ? Path.GetFullPath(Path.Combine(baseDir, path))
                : path;

        private static BuildTask GenerateDirectTask(CmplProject project, CmplProfile profile)
        {
            string? baseDir = project.BaseDirectory;
            bool msvc = profile.Toolchain == "msvc";

            string compiler = profile.Toolchain switch
            {
                "msvc" => "cl",
                "gcc" => "g++",
                "clang" => "clang++",
                _ => profile.Toolchain!  // allow a custom compiler path
            };

            var args = new List<string>();

            if (profile.Flags != null)
                args.AddRange(profile.Flags);

            if (profile.IncludeDirs != null)
                args.AddRange(profile.IncludeDirs.Select(inc =>
                    $"{(msvc ? "/I" : "-I")}\"{Resolve(inc, baseDir)}\""));

            if (profile.Defines != null)
                args.AddRange(profile.Defines.Select(def => $"{(msvc ? "/D" : "-D")}{def}"));

            string sourceDir = Resolve(profile.SourceDir!, baseDir);
            string outputDir = Resolve(profile.OutputDir!, baseDir);
            string exeSuffix = OperatingSystem.IsWindows() ? ".exe" : "";
            string outputFile = Path.Combine(outputDir, $"{project.ProjectName}{exeSuffix}");

            // Keep the glob outside the quotes so the shell expands it
            args.Add($"\"{sourceDir}\"/*.cpp");
            args.Add(msvc ? $"/Fe:\"{outputFile}\"" : $"-o \"{outputFile}\"");

            string commandLine = $"{compiler} {string.Join(" ", args)}";

            // MSVC's cl is only on PATH inside a developer prompt, so route
            // through VsDevCmd when we can find one.
            if (msvc && OperatingSystem.IsWindows())
            {
                string? devCmdPath = ToolLocator.GetVsDevCmdPath();
                if (devCmdPath != null)
                    return new BuildTask
                    {
                        Command = "cmd.exe",
                        Arguments = $"/c \"call \"{devCmdPath}\" && {commandLine}\"",
                        WorkingDirectory = baseDir
                    };
            }

            // The shell expands the *.cpp glob (MSVC and MinGW expand it
            // themselves, but Unix compilers rely on the shell).
            return ShellTask(commandLine, baseDir);
        }

        private static List<BuildTask> GenerateCmakeTasks(CmplProfile profile, string? baseDir)
        {
            string sourceDir = Resolve(profile.SourceDir!, baseDir);
            string buildDir = Resolve(profile.OutputDir!, baseDir);

            var configArgs = new List<string> { $"-B \"{buildDir}\"", $"-S \"{sourceDir}\"" };
            if (!string.IsNullOrEmpty(profile.BuildType))
                configArgs.Add($"-DCMAKE_BUILD_TYPE={profile.BuildType}");
            if (profile.Defines != null)
                configArgs.AddRange(profile.Defines.Select(def => $"-D{def}"));
            if (profile.Flags != null)
                configArgs.AddRange(profile.Flags);

            var buildArgs = new List<string> { $"--build \"{buildDir}\"" };
            if (!string.IsNullOrEmpty(profile.BuildType))
                buildArgs.Add($"--config {profile.BuildType}");

            return new List<BuildTask>
            {
                new() { Command = "cmake", Arguments = string.Join(" ", configArgs), WorkingDirectory = baseDir },
                new() { Command = "cmake", Arguments = string.Join(" ", buildArgs), WorkingDirectory = baseDir }
            };
        }

        private static BuildTask GenerateDotnetTask(CmplProfile profile, string? baseDir)
        {
            string verb = profile.DotnetPublish ? "publish" : "build";
            var args = new List<string> { verb, $"\"{Resolve(profile.SourceDir!, baseDir)}\"" };

            if (!string.IsNullOrEmpty(profile.BuildType))
                args.Add($"-c {profile.BuildType}");

            if (!string.IsNullOrEmpty(profile.OutputDir))
                args.Add($"-o \"{Resolve(profile.OutputDir, baseDir)}\"");

            if (profile.Flags != null)
                args.AddRange(profile.Flags);

            return new BuildTask
            {
                Command = "dotnet",
                Arguments = string.Join(" ", args),
                WorkingDirectory = baseDir
            };
        }

        private static BuildTask GenerateMsbuildTask(CmplProfile profile, string? baseDir)
        {
            var args = new List<string> { $"\"{Resolve(profile.SourceDir!, baseDir)}\"" };

            if (!string.IsNullOrEmpty(profile.BuildType))
                args.Add($"/p:Configuration={profile.BuildType}");

            if (!string.IsNullOrEmpty(profile.OutputDir))
                args.Add($"/p:OutputPath=\"{Resolve(profile.OutputDir, baseDir)}\\\"");

            if (profile.Flags != null)
                args.AddRange(profile.Flags);

            string finalArgs = string.Join(" ", args);

            if (OperatingSystem.IsWindows())
            {
                string? devCmdPath = ToolLocator.GetVsDevCmdPath();
                if (devCmdPath != null)
                    return new BuildTask
                    {
                        Command = "cmd.exe",
                        Arguments = $"/c \"call \"{devCmdPath}\" && msbuild {finalArgs}\"",
                        WorkingDirectory = baseDir
                    };

                return new BuildTask { Command = "msbuild", Arguments = finalArgs, WorkingDirectory = baseDir };
            }

            // On Linux/macOS msbuild ships inside the .NET SDK
            return new BuildTask { Command = "dotnet", Arguments = $"msbuild {finalArgs}", WorkingDirectory = baseDir };
        }
    }
}