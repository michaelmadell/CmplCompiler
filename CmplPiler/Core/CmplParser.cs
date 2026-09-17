using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmplPiler.Core
{
    public static class CmplParser
    {
        private static readonly Regex VariablePattern = new(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

        private static readonly string[] KnownBuildSystems = { "direct", "cmake", "dotnet", "msbuild" };

        public static CmplProject LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"The file '{filePath}' was not found.");

            var project = Parse(File.ReadAllText(filePath));
            project.BaseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            Validate(project);
            ExpandVariables(project);
            return project;
        }

        public static CmplProject Parse(string yamlContent)
        {
            // Ignore extra fields so newer .cmpl files still load on older builds
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var project = deserializer.Deserialize<CmplProject>(yamlContent);
            if (project == null)
                throw new CmplValidationException("The file is empty or not a valid .cmpl document.");
            return project;
        }

        public static void Validate(CmplProject project)
        {
            if (string.IsNullOrWhiteSpace(project.ProjectName))
                throw new CmplValidationException("'project_name' is required.");

            if (project.Profiles == null || project.Profiles.Count == 0)
                throw new CmplValidationException("At least one entry under 'profiles' is required.");

            foreach (var profile in project.Profiles)
            {
                string label = string.IsNullOrWhiteSpace(profile.Name) ? "<unnamed>" : profile.Name;

                if (string.IsNullOrWhiteSpace(profile.Name))
                    throw new CmplValidationException("Every profile requires a 'name'.");

                if (string.IsNullOrWhiteSpace(profile.BuildSystem))
                    throw new CmplValidationException($"Profile '{label}': 'build_system' is required.");

                if (!KnownBuildSystems.Contains(profile.BuildSystem))
                    throw new CmplValidationException(
                        $"Profile '{label}': unknown build_system '{profile.BuildSystem}'. " +
                        $"Expected one of: {string.Join(", ", KnownBuildSystems)}.");

                if (string.IsNullOrWhiteSpace(profile.SourceDir))
                    throw new CmplValidationException($"Profile '{label}': 'source_dir' is required.");

                if (profile.BuildSystem == "direct")
                {
                    if (string.IsNullOrWhiteSpace(profile.Toolchain))
                        throw new CmplValidationException($"Profile '{label}': 'toolchain' is required for direct builds.");
                    if (string.IsNullOrWhiteSpace(profile.OutputDir))
                        throw new CmplValidationException($"Profile '{label}': 'output_dir' is required for direct builds.");
                }

                if (profile.BuildSystem == "cmake" && string.IsNullOrWhiteSpace(profile.OutputDir))
                    throw new CmplValidationException($"Profile '{label}': 'output_dir' is required for cmake builds (used as the binary dir).");
            }
        }

        /// <summary>
        /// Expands ${VAR} tokens in all profile strings. Lookup order:
        /// built-ins (project_name, base_dir), the profile's 'environment' map,
        /// the project's 'environment' map, then OS environment variables.
        /// Unknown tokens are left as-is.
        /// </summary>
        public static void ExpandVariables(CmplProject project)
        {
            string? ProjectLookup(string name) => name switch
            {
                "project_name" => project.ProjectName,
                "base_dir" => project.BaseDirectory,
                _ => (project.Environment != null && project.Environment.TryGetValue(name, out var v))
                        ? v
                        : Environment.GetEnvironmentVariable(name)
            };

            string? ExpandWith(string? value, Func<string, string?> lookup) =>
                value == null ? null : VariablePattern.Replace(value, m => lookup(m.Groups[1].Value) ?? m.Value);

            // Expand values within the project environment map
            if (project.Environment != null)
            {
                foreach (var key in project.Environment.Keys.ToList())
                {
                    project.Environment[key] = ExpandWith(project.Environment[key], ProjectLookup)!;
                }
            }

            foreach (var profile in project.Profiles)
            {
                // Expand values within the profile environment map using project lookup first
                if (profile.Environment != null)
                {
                    foreach (var key in profile.Environment.Keys.ToList())
                    {
                        profile.Environment[key] = ExpandWith(profile.Environment[key], ProjectLookup)!;
                    }
                }

                string? ProfileLookup(string name) => name switch
                {
                    "project_name" => project.ProjectName,
                    "base_dir" => project.BaseDirectory,
                    _ => (profile.Environment != null && profile.Environment.TryGetValue(name, out var pv))
                            ? pv
                            : (project.Environment != null && project.Environment.TryGetValue(name, out var v))
                                ? v
                                : Environment.GetEnvironmentVariable(name)
                };

                string? Expand(string? value) => ExpandWith(value, ProfileLookup);
                List<string>? ExpandList(List<string>? values) => values?.Select(v => Expand(v)!).ToList();

                profile.Toolchain = Expand(profile.Toolchain);
                profile.Arch = Expand(profile.Arch);
                profile.BuildType = Expand(profile.BuildType);
                profile.SourceDir = Expand(profile.SourceDir);
                profile.OutputDir = Expand(profile.OutputDir);
                profile.IncludeDirs = ExpandList(profile.IncludeDirs);
                profile.Defines = ExpandList(profile.Defines);
                profile.Flags = ExpandList(profile.Flags);
                profile.PreBuild = ExpandList(profile.PreBuild);
                profile.PostBuild = ExpandList(profile.PostBuild);
            }
        }
    }
}