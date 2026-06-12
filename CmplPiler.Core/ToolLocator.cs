using System.Diagnostics;

namespace CmplPiler.Core
{
    public static class ToolLocator
    {
        /// <summary>
        /// Locates VsDevCmd.bat for the newest Visual Studio install via
        /// vswhere. Returns null on non-Windows hosts or when VS is absent.
        /// </summary>
        public static string? GetVsDevCmdPath()
        {
            if (!OperatingSystem.IsWindows())
                return null;

            string vswherePath = Environment.ExpandEnvironmentVariables(
                @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");

            if (!File.Exists(vswherePath))
                return null;

            ProcessStartInfo startInfo = new()
            {
                FileName = vswherePath,
                Arguments = "-latest -property installationPath",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process == null)
                return null;

            string vsPath = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (string.IsNullOrEmpty(vsPath))
                return null;

            string devCmdPath = Path.Combine(vsPath, @"Common7\Tools\VsDevCmd.bat");
            return File.Exists(devCmdPath) ? devCmdPath : null;
        }
    }
}
