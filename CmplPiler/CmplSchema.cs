using System;
using System.Collections.Generic;
using System.Text;

namespace CmplPiler
{
    public class CmplProject
    {
        public string project_name { get; set; }
        public string cmpl_version { get; set; }
        public Dictionary<string, string> environment { get; set; }
        public List<CmplProfile> profiles { get; set; }
    }

    public class CmplProfile
    {
        public string name { get; set; }
        public string build_system { get; set; }
        public bool dotnet_publish { get; set; } = false;
        public string build_type { get; set; }
        public string toolchain { get; set; }
        public string source_dir { get; set; }
        public string output_dir { get; set; }
        public List<string> include_dirs { get; set; }
        public List<string> defines { get; set; }
        public List<string> flags { get; set; }
        public List<string> pre_build { get; set; }
        public List<string> post_build { get; set; }
    }
}
