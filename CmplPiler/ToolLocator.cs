using System;
using System.Diagnostics;
using System.IO;

namespace CmplPiler
{
    public static class ToolLocator
    {
        public static string GetVsDevCmdPath()
        {
            string vswherePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");

            if (!File.Exists(vswherePath))
                return null;

            ProcessStartInfo startInfo = new()
            {
                FileName = vswherePath,
                Arguments = "-latest -property installationPath", // Get the path to the newest VS version
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string vsPath = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(vsPath))
                {
                    string devCmdPath = Path.Combine(vsPath, @"Common7\Tools\VsDevCmd.bat");
                    return File.Exists(devCmdPath) ? devCmdPath : null;
                }
            }
            return null;
        }
    }
}