using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.Win32.SafeHandles;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

HttpClient client = new HttpClient();

// Parse command and act accordingly
if (command == "decode")
{
    var encodedValue = param;
    Console.WriteLine(JsonSerializer.Serialize(Bencode.Decode(encodedValue)));
}
else if (command == "info")
{
    string path = $"{param}";

    var bytes = File.ReadAllBytes(path);
    if (bytes != null)
    {
        string text = Encoding.ASCII.GetString(bytes);

        var output = JsonSerializer.Serialize(Bencode.Decode(text));

        MetaInfo metaInfo = JsonSerializer.Deserialize<MetaInfo>(output)!;

        var hashInfo = Bencode.GetInfoHashString(bytes, text);

        var pieceHashes = Bencode.GetPieceHashes(metaInfo.info.length, metaInfo.info.piecelength, bytes, text);

        Console.WriteLine($"Tracker URL: {metaInfo.announce}\nLength: {metaInfo?.info?.length}\nInfo Hash: {hashInfo}" +
            $"\nPiece Length: {metaInfo?.info.piecelength}\nPiece Hashes:\n" +
            $"{String.Join("\n", pieceHashes)}"); 
    }
}
else if(command == "peers")
{
    string path = $"{param}";

    var bytes = File.ReadAllBytes(path);
    if (bytes != null)
    {
        string text = Encoding.ASCII.GetString(bytes);

        var output = JsonSerializer.Serialize(Bencode.Decode(text));

        MetaInfo metaInfo = JsonSerializer.Deserialize<MetaInfo>(output)!;
        var hashInfo = Bencode.GetInfoHashBytes(bytes, text);

        var urlEncoded = HttpUtility.UrlEncode(hashInfo);
        var queryParameters = new Dictionary<string, string>
        {
            {"info_hash", urlEncoded},
            {"peer_id", "12345678912345678900"},
            {"port", "6881"},
            {"uploaded", "0"},
            {"downloaded", "0"},
            {"left", metaInfo.info.length.ToString()},
            {"compact", "1"},
        };

        var queryString = string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));
        var url = $"{metaInfo.announce}?{queryString}";
        var response = await client.GetAsync(url);
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var contentString = await response.Content.ReadAsStringAsync();

        client.CancelPendingRequests();
        client.Dispose();
        //var temp = string.Join(", ", contentBytes);
        //Console.WriteLine(temp);
        //Console.WriteLine(temp.IndexOf("178, 62, 82"));
        Console.WriteLine(contentString);
        var peersStart = "5:peers";
        contentBytes = contentBytes[(contentString.IndexOf(peersStart) + peersStart.Length)..];
        contentString = contentString[(contentString.IndexOf(peersStart) + peersStart.Length)..];

        var peersLength = int.Parse(contentString.Substring(0, contentString.IndexOf(":")));

        var peersBytes = contentBytes[(contentString.IndexOf(":") + 1)..(contentString.IndexOf(":") + 1 + peersLength)];

        Console.WriteLine(peersBytes.Length);
        foreach ( var peer in peersBytes)
        {
            Console.Write(peer + ", ");
        }
        Console.WriteLine();

        Console.WriteLine();

        for(int i = 0; i < peersBytes.Length / 6 ; i++)
        {
            Console.WriteLine(GetIpFromBytes(peersBytes[(6 * i)..(6 * (i + 1))]));
        }
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}

string GetIpFromBytes(byte[] bytes)
{
    string result = "";

    result = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}:{BitConverter.ToUInt16(new byte[2] { bytes[5], bytes[4] })}";

    return result;
}

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
}

public class Info
{
    public int length { get; set; }
    public string? name { get; set; }

    [JsonPropertyName("piece length")]
    public int piecelength { get; set; }
    public string? pieces { get; set; }
}


public class Bencode()
{
    public static string GetInfoHashString(byte[] bytes, string stream)
    {
        var hash = GetInfoHashBytes(bytes, stream);
        return Convert.ToHexString(hash).ToLower();
    }

    public static byte[] GetInfoHashBytes(byte[] bytes, string stream)
    {
        const string infoHashMark = "4:infod";
        var infoHashStart = stream.IndexOf(infoHashMark) + infoHashMark.Length - 1;
        var chunk = bytes[infoHashStart..^1];
        var hash = SHA1.HashData(chunk);
        return hash;
    }

    public static string[] GetPieceHashes(long length, long pieceLength, byte[] bytes, string stream)
    {
        string[] pieceHashes = new string[(int)Math.Ceiling((double)length / pieceLength)];

        const string piecesMark = "6:pieces";
        var piecesBytesStart = bytes[(stream.IndexOf(piecesMark) + piecesMark.Length - 1)..];
        var piecesStreamStart = stream[(stream.IndexOf(piecesMark) + piecesMark.Length - 1)..];
        var chunk = piecesBytesStart[(piecesStreamStart.IndexOf(":") + 1)..^1];
        var pieceChunk = 20;

        for (int i = 0; i < pieceHashes.Length; i++)
        {
            var tempChunk = chunk[..pieceChunk];
            pieceHashes[i] = Convert.ToHexString(tempChunk).ToLower();
            chunk = chunk[pieceChunk..];
        }

        return pieceHashes;
    }

    public static object Decode(string input)
    {
        if (Char.IsDigit(input[0]))
        {
            return StringDecode(input);
        }
        else if (input[0] == 'i')
        {
            return IntegerDecode(input);
        }
        else if (input[0] == 'l')
        {
            return ArrayDecode(input);
        }
        else if (input[0] == 'd')
        {
            return DictionaryDecode(input);
        }
        else
        {
            throw new InvalidOperationException("Unhandled encoded value: " + input);
        }
    }

    public static string Encode(object input)
    {
        if (input == null)
        {
            throw new Exception("Input in encode was null");
        }
        if (input is System.Text.Json.JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String: input = jsonElement.GetString()!; break;
                case JsonValueKind.Number: input = jsonElement.GetInt64(); break;
            }
        }
        return Type.GetTypeCode(input.GetType()) switch
        {
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => $"i{input}e",
            TypeCode.String => $"{((string)input).Length}:{input}",
            TypeCode.Object => input is object[] inputArray ? $"l{string.Join("", inputArray.Select(x => Encode(x)))}e"
                                                            : input is Dictionary<string, object> inputDict ? $"d{string.Join("", inputDict.Select(x => Encode(x.Key) + Encode(x.Value)))}e"
                                                            : throw new Exception($"Unknown type: {input.GetType().FullName}"),
            
            _ => throw new Exception($"Unknown type: {input.GetType().FullName}")
        };
    }

    public static string StringDecode(string input)
    {
        var colonIndex = input.IndexOf(':');
        if (colonIndex != -1)
        {
            var strLength = int.Parse(input[..colonIndex]);
            var strValue = input.Substring(colonIndex + 1, strLength);
            return strValue;
        }
        else
        {
            throw new InvalidOperationException("Invalid encoded value: " + input);
        }
    }

    public static long IntegerDecode(string input)
    {
        var endIndex = input.IndexOf('e');
        if (endIndex != -1)
        {
            if (long.TryParse(input.Substring(1, endIndex - 1), out var number))
            {
                return number;
            }
            else
            {
                throw new InvalidOperationException("Invalid number was given: " + number);
            }
        }
        else
        {
            throw new InvalidOperationException("Missing end of output (e)");
        }
    }

    public static object[] ArrayDecode(string input)
    {
        input = input[1..];
        var result = new List<object>();
        while (input.Length > 0 && input[0] != 'e')
        {
            var element = Decode(input);
            result.Add(element);
            input = input[Encode(element).Length..];
        }
        return result.ToArray();
    }

    public static Dictionary<string, object> DictionaryDecode(string input)
    {
        input = input[1..];
        var result = new Dictionary<string, object>();
        while (input.Length > 0 && input[0] != 'e')
        {
            var key = StringDecode(input);
            input = input[Encode(key).Length..];

            var value = Decode(input);
            input = input[Encode(value).Length..];

            result.Add(key, value);
        }
        return result;
    }
}