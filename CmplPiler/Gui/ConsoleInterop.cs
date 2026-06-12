using System.Runtime.InteropServices;

namespace CmplPiler
{
    internal static class ConsoleInterop
    {
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// Attaches to the parent process's console. No-op (returns false)
        /// when launched outside a console, e.g. from Explorer; redirected
        /// stdout/stderr handles keep working either way.
        /// </summary>
        public static void AttachToParentConsole() => AttachConsole(AttachParentProcess);
    }
}
