using System.Security.Cryptography;
using System.Text.Json;

namespace codecrafters_bittorrent.src
{
    public class Bencode
    {
        public static string GetInfoHashString(string torrentFileName)
        {
            var hash = GetInfoHashBytes(torrentFileName);
            return Convert.ToHexString(hash).ToLower();
        }

        public static byte[] GetInfoHashBytes(string torrentFileName)
        {
            var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
            var stream = ReadWriteFile.ReadStringFromFile(torrentFileName);
            const string infoHashMark = "4:infod";
            var infoHashStart = stream.IndexOf(infoHashMark) + infoHashMark.Length - 1;
            var chunk = bytes[infoHashStart..^1];
            var hash = SHA1.HashData(chunk);
            return hash;
        }

        public static string[] GetPieceHashes(string torrentFileName)
        {
            MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
            var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
            var stream = ReadWriteFile.ReadStringFromFile(torrentFileName);

            return GetPieceHashes(bytes, stream, metaInfo.Info.Length, metaInfo.Info.PieceLength);
        }

        public static string[] GetPieceHashes(byte[] bytes, string stream, int length, int pieceLength)
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
}
