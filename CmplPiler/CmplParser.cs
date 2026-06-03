using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CmplPiler
{
    public static class CmplParser
    {
        public static CmplProject LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"The file '{filePath}' was not found.");

            string yamlContent = File.ReadAllText(filePath);

            // Configure the deserializer to ignore extra fields to prevent crashes
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<CmplProject>(yamlContent);
        }
    }
}
