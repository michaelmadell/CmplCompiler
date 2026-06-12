using YamlDotNet.Serialization;

namespace CmplPiler.Core
{
    public class CmplProject
    {
        public string? ProjectName { get; set; }
        public string? CmplVersion { get; set; }
        public Dictionary<string, string>? Environment { get; set; }
        public List<CmplProfile> Profiles { get; set; } = new();

        /// <summary>
        /// Directory containing the .cmpl file. Relative paths in profiles are
        /// resolved against this. Not part of the YAML document.
        /// </summary>
        [YamlIgnore]
        public string? BaseDirectory { get; set; }
    }

    public class CmplProfile
    {
        public string? Name { get; set; }
        public string? BuildSystem { get; set; }
        public bool DotnetPublish { get; set; }
        public string? BuildType { get; set; }
        public string? Toolchain { get; set; }

        /// <summary>Target architecture for MSVC tooling (x86, x64, arm64). Defaults to the host OS architecture.</summary>
        public string? Arch { get; set; }
        public string? SourceDir { get; set; }
        public string? OutputDir { get; set; }
        public List<string>? IncludeDirs { get; set; }
        public List<string>? Defines { get; set; }
        public List<string>? Flags { get; set; }
        public List<string>? PreBuild { get; set; }
        public List<string>? PostBuild { get; set; }
    }

    public class BuildTask
    {
        public string Command { get; set; } = "";

        /// <summary>Raw argument string. Ignored when <see cref="ArgumentList"/> is set.</summary>
        public string Arguments { get; set; } = "";

        /// <summary>
        /// Individual arguments passed without shell re-parsing. Preferred on
        /// Unix where quoting rules differ from Windows.
        /// </summary>
        public List<string>? ArgumentList { get; set; }

        public string? WorkingDirectory { get; set; }

        public override string ToString() =>
            ArgumentList != null
                ? $"{Command} {string.Join(" ", ArgumentList)}"
                : $"{Command} {Arguments}".TrimEnd();
    }

    public class CmplValidationException : Exception
    {
        public CmplValidationException(string message) : base(message) { }
    }
}
