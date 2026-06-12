using System.Diagnostics;

namespace CmplPiler.Core
{
    /// <summary>
    /// Executes the task pipeline for a profile, streaming output through
    /// events so both the CLI and the GUI can share one implementation.
    /// </summary>
    public sealed class BuildRunner
    {
        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;

        /// <summary>Runs all tasks for the profile. Returns the first non-zero exit code, or 0.</summary>
        public async Task<int> RunAsync(CmplProject project, CmplProfile profile, CancellationToken cancellationToken = default)
        {
            var tasks = CommandGenerator.GenerateTasks(project, profile);

            EnsureOutputDirectory(project, profile);

            foreach (var task in tasks)
            {
                OutputReceived?.Invoke($"> {task}");

                int exitCode = await RunProcessAsync(project, task, cancellationToken);
                if (exitCode != 0)
                    return exitCode;
            }

            return 0;
        }

        private static void EnsureOutputDirectory(CmplProject project, CmplProfile profile)
        {
            if (string.IsNullOrEmpty(profile.OutputDir))
                return;

            string dir = profile.OutputDir;
            if (project.BaseDirectory != null && !Path.IsPathRooted(dir))
                dir = Path.Combine(project.BaseDirectory, dir);

            Directory.CreateDirectory(dir);
        }

        private async Task<int> RunProcessAsync(CmplProject project, BuildTask task, CancellationToken cancellationToken)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = task.Command,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            if (task.ArgumentList != null)
                foreach (var arg in task.ArgumentList)
                    startInfo.ArgumentList.Add(arg);
            else
                startInfo.Arguments = task.Arguments;

            if (!string.IsNullOrEmpty(task.WorkingDirectory))
                startInfo.WorkingDirectory = task.WorkingDirectory;

            if (project.Environment != null)
                foreach (var (key, value) in project.Environment)
                    startInfo.Environment[key] = value;

            using Process process = new() { StartInfo = startInfo };

            // stderr is streamed as-is: compilers send banners and progress
            // there, so failure is judged by exit code, not by stream.
            process.OutputDataReceived += (_, ev) => { if (ev.Data != null) OutputReceived?.Invoke(ev.Data); };
            process.ErrorDataReceived += (_, ev) => { if (ev.Data != null) ErrorReceived?.Invoke(ev.Data); };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"ERROR: Could not start '{task.Command}': {ex.Message}");
                return -1;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
                throw;
            }

            return process.ExitCode;
        }
    }
}
