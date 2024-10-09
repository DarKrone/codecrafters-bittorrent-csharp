using System.Text.Json;
using System.Text.Json.Serialization;

namespace codecrafters_bittorrent.src
{
    public class MetaInfo
    {
        public string? announce { get; set; }
        public string? createdby { get; set; }
        public Info info { get; set; }

        public MetaInfo()
        {
            announce = null;
            createdby = null;
            info = new Info();
        }

        public static MetaInfo GetInfo(string path)
        {
            var bytes = ReadFile.ReadBytesFromFile(path);

            string text = ReadFile.ReadStringFromFile(path);

            var output = JsonSerializer.Serialize(Bencode.Decode(text));

            MetaInfo metaInfo = JsonSerializer.Deserialize<MetaInfo>(output)!;

            var hashInfo = Bencode.GetInfoHashString(bytes, text);
            return metaInfo;

        }
    }
    public class Info
    {
        public int length { get; set; }
        public string? name { get; set; }

        [JsonPropertyName("piece length")]
        public int piecelength { get; set; }
        public string? pieces { get; set; }
    }
}
