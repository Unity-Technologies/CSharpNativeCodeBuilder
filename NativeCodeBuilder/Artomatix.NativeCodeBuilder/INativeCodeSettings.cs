using System.Xml.Serialization;

namespace Artomatix.NativeCodeBuilder
{
    public interface INativeCodeSettings
    {
        string PathToNativeCodeBase { get; }
        string[] DLLTargets { get; }
        string CMakeArguments { get; }
        string BuildPathBase { get; }
    }

    public class NativeCodeSettings : INativeCodeSettings
    {
        public string PathToNativeCodeBase { get; set; }

        [XmlArray]
        [XmlArrayItem("Target")]
        public string[] DLLTargets { get; set; }

        public string CMakeArguments { get; set; }

        public string BuildPathBase { get; set; }
    }

    public interface INativeSettingsSerializer
    {
        string Serialize(NativeCodeSettings settings);

        INativeCodeSettings Deserialize(string serialized);
    }
}