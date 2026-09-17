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

        /// <summary>
        /// Builds the "call VsDevCmd.bat ..." prefix. VsDevCmd defaults to
        /// x86 tools, so the target/host architecture is passed explicitly
        /// (profile 'arch' if set, otherwise the host OS architecture).
        /// </summary>
        private static string? GetVsDevCmdCall(CmplProfile profile)
        {
            string? devCmdPath = ToolLocator.GetVsDevCmdPath();
            if (devCmdPath == null)
                return null;

            string hostArch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
                System.Runtime.InteropServices.Architecture.X86 => "x86",
                _ => "x64"
            };
            string targetArch = profile.Arch ?? hostArch;

            return $"call \"{devCmdPath}\" -arch={targetArch} -host_arch={hostArch}";
        }

        private static BuildTask GenerateDirectTask(CmplProject project, CmplProfile profile)
        {
            string? baseDir = project.BaseDirectory;
            bool msvc = profile.Toolchain == "msvc";

            string compiler = profile.Toolchain switch
            {
                "msvc" => "cl",
                "gcc" => "g++",
                "clang" => "clang++",
                _ => profile.Toolchain!.Contains(' ') && !profile.Toolchain!.StartsWith('"')
                    ? $"\"{profile.Toolchain}\""
                    : profile.Toolchain!  // allow a custom compiler path
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

            // Add source files or glob patterns
            args.AddRange(ResolveSourceArguments(sourceDir, profile.Sources, baseDir));
            if (msvc)
            {
                // Keep cl's .obj intermediates out of the working directory.
                // The forward slash avoids a trailing backslash escaping the
                // closing quote under cmd's argument rules.
                args.Add($"/Fo\"{outputDir}/\"");
                args.Add($"/Fe:\"{outputFile}\"");
            }
            else
            {
                args.Add($"-o \"{outputFile}\"");
            }

            string commandLine = $"{compiler} {string.Join(" ", args)}";

            // MSVC's cl is only on PATH inside a developer prompt, so route
            // through VsDevCmd when we can find one.
            if (msvc && OperatingSystem.IsWindows())
            {
                string? devCmdCall = GetVsDevCmdCall(profile);
                if (devCmdCall != null)
                    return new BuildTask
                    {
                        Command = "cmd.exe",
                        Arguments = $"/c \"{devCmdCall} && {commandLine}\"",
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
                string? devCmdCall = GetVsDevCmdCall(profile);
                if (devCmdCall != null)
                    return new BuildTask
                    {
                        Command = "cmd.exe",
                        Arguments = $"/c \"{devCmdCall} && msbuild {finalArgs}\"",
                        WorkingDirectory = baseDir
                    };

                return new BuildTask { Command = "msbuild", Arguments = finalArgs, WorkingDirectory = baseDir };
            }

            // On Linux/macOS msbuild ships inside the .NET SDK
            return new BuildTask { Command = "dotnet", Arguments = $"msbuild {finalArgs}", WorkingDirectory = baseDir };
        }

        private static List<string> ResolveSourceArguments(string sourceDir, List<string>? sources, string? baseDir)
        {
            var patterns = (sources != null && sources.Count > 0)
                ? sources
                : new List<string> { "*.cpp" };

            var result = new List<string>();

            foreach (var rawPattern in patterns)
            {
                string pattern = rawPattern.Replace('\\', '/');
                bool hasWildcard = pattern.Contains('*') || pattern.Contains('?');

                if (!hasWildcard)
                {
                    string fullPath = Resolve(Path.IsPathRooted(pattern) ? pattern : Path.Combine(sourceDir, pattern), baseDir);
                    result.Add($"\"{fullPath}\"");
                    continue;
                }

                bool resolvedAny = false;
                if (Directory.Exists(sourceDir))
                {
                    try
                    {
                        string searchDir = sourceDir;
                        string filePattern = pattern;
                        SearchOption searchOption = SearchOption.TopDirectoryOnly;

                        int globIndex = pattern.IndexOf("/**/", StringComparison.Ordinal);
                        if (globIndex >= 0)
                        {
                            string sub = pattern[..globIndex];
                            searchDir = Path.Combine(sourceDir, sub);
                            filePattern = pattern[(globIndex + 4)..];
                            searchOption = SearchOption.AllDirectories;
                        }
                        else if (pattern.StartsWith("**/", StringComparison.Ordinal))
                        {
                            filePattern = pattern[3..];
                            searchOption = SearchOption.AllDirectories;
                        }
                        else if (pattern.Contains('/'))
                        {
                            int lastSlash = pattern.LastIndexOf('/');
                            searchDir = Path.Combine(sourceDir, pattern[..lastSlash]);
                            filePattern = pattern[(lastSlash + 1)..];
                        }

                        if (Directory.Exists(searchDir))
                        {
                            var files = Directory.GetFiles(searchDir, filePattern, searchOption);
                            if (files.Length > 0)
                            {
                                foreach (var file in files.OrderBy(f => f))
                                {
                                    result.Add($"\"{Path.GetFullPath(file)}\"");
                                }
                                resolvedAny = true;
                            }
                        }
                    }
                    catch
                    {
                        // Fall back to pattern string on IO errors
                    }
                }

                if (!resolvedAny)
                {
                    // Fall back to shell / compiler glob syntax
                    if (pattern == "*.cpp")
                    {
                        result.Add($"\"{sourceDir}\"/*.cpp");
                    }
                    else
                    {
                        string fallback = Path.IsPathRooted(pattern) ? pattern : $"{sourceDir.TrimEnd('\\', '/')}/{pattern}";
                        result.Add($"\"{fallback}\"");
                    }
                }
            }

            return result;
        }
    }
}