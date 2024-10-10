using System.Text.Json;
using System.Text.Json.Serialization;

namespace codecrafters_bittorrent.src
{
    public class MetaInfo
    {
        [JsonPropertyName("announce")]
        public string? Announce { get; set; }

        [JsonPropertyName("created by")]
        public string? CreatedBy { get; set; }

        [JsonPropertyName("info")]
        public Info Info { get; set; }

        public MetaInfo()
        {
            Announce = null;
            CreatedBy = null;
            Info = new Info();
        }

        public static MetaInfo GetInfo(string path)
        {
            string text = ReadWriteFile.ReadStringFromFile(path);

            var output = JsonSerializer.Serialize(Bencode.Decode(text));

            MetaInfo metaInfo = JsonSerializer.Deserialize<MetaInfo>(output)!;

            return metaInfo;
        }
    }
    public class Info
    {
        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("piece length")]
        public int PieceLength { get; set; }

        // Somehow its not parsing with JsonPropertyName
        public string? pieces { get; set; }
    }
}
