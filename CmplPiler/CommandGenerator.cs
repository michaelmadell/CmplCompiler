using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

namespace CmplPiler
{
    public class BuildTask
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
    }

    public static class CommandGenerator
    {
        public static List<BuildTask> GenerateTasks(CmplProject project, CmplProfile profile)
        {
            var tasks = new List<BuildTask>();

            if (profile.pre_build != null)
            {
                foreach (var cmd in profile.pre_build)
                {
                    tasks.Add(new BuildTask { Command = "cmd.exe", Arguments = $"/c {cmd}" });
                }
            }

            if (profile.build_system == "direct")
            {
                var args = new List<string>();

                if (profile.flags != null)
                    args.AddRange(profile.flags);

                if (profile.include_dirs != null)
                    args.AddRange(profile.include_dirs.Select(inc => $"-I\"{inc}\""));

                if (profile.defines != null)
                    args.AddRange(profile.defines.Select(def => $"-D{def}"));

                args.Add($"\"{profile.source_dir}/*.cpp\"");
                args.Add($"-o \"{profile.output_dir}/{project.project_name}.exe\"");

                tasks.Add(new BuildTask
                {
                    Command = profile.toolchain,
                    Arguments = string.Join(" ", args)
                });
            }
            #region CMake
            else if (profile.build_system == "cmake")
            {
                var configArgs = new List<string> { $"-B \"{profile.output_dir}\"", $"-S \"{profile.source_dir}\"" };
                if (profile.flags != null)
                    configArgs.AddRange(profile.flags);

                tasks.Add(new BuildTask { Command = "cmake", Arguments = string.Join(" ", configArgs) });

                tasks.Add(new BuildTask { Command = "cmake", Arguments = $"--build \"{profile.output_dir}\"" });
            }
            #endregion
            #region Dotnet
            else if (profile.build_system == "dotnet")
            {
                if (profile.build_type != null)
                {
                    if (profile.build_type == "Release")
                        profile.flags = (profile.flags ?? new List<string>()).Append("-c Release").ToList();
                    else if (profile.build_type == "Debug")
                        profile.flags = (profile.flags ?? new List<string>()).Append("-c Debug").ToList();
                }

                if (profile.dotnet_publish)
                {
                    var publishArgs = new List<string> { "publish", $"\"{profile.source_dir}\"" };
                    if (!string.IsNullOrEmpty(profile.output_dir))
                    {
                        publishArgs.Add($"-o \"{profile.output_dir}\"");
                    }
                    if (profile.flags != null)
                        publishArgs.AddRange(profile.flags);
                    tasks.Add(new BuildTask { Command = "dotnet", Arguments = string.Join(" ", publishArgs) });
                    return tasks; // Skip the build step if we're publishing
                }
                var args = new List<string> { "build", $"\"{profile.source_dir}\"" };

                if (!string.IsNullOrEmpty(profile.output_dir))
                {
                    args.Add($"-o \"{profile.output_dir}\"");
                }

                if (profile.flags != null)
                    args.AddRange(profile.flags);

                tasks.Add(new BuildTask { Command = "dotnet", Arguments = string.Join(" ", args) });
            }
            #endregion
            #region MSBuild
            else if (profile.build_system == "msbuild")
            {
                var msbuildArgs = new List<string> { $"\"{profile.source_dir}\"" };

                if (!string.IsNullOrEmpty(profile.output_dir))
                {
                    msbuildArgs.Add($"/p:OutputPath=\"{profile.output_dir}\\\"");
                }

                if (profile.flags != null) msbuildArgs.AddRange(profile.flags);

                string finalArgsString = string.Join(" ", msbuildArgs);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string devCmdPath = ToolLocator.GetVsDevCmdPath();

                    if (devCmdPath != null)
                    {
                        string wrappedCommand = $"/c \"call \"{devCmdPath}\" && msbuild {finalArgsString}\"";

                        tasks.Add(new BuildTask { Command = "cmd.exe", Arguments = wrappedCommand });
                    }
                    else
                    {
                        tasks.Add(new BuildTask { Command = "msbuild", Arguments = finalArgsString });
                    }
                }
                else
                {
                    tasks.Add(new BuildTask { Command = "dotnet", Arguments = $"msbuild {finalArgsString }" });
                }
            }
            #endregion

            if (profile.post_build != null)
            {
                foreach (var cmd in profile.post_build)
                {
                    tasks.Add(new BuildTask { Command = "cmd.exe", Arguments = $"/c {cmd}" });
                }
            }

            return tasks;
        }
    }
}
