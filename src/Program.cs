using System.Text.Json;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

// Parse command and act accordingly
if (command == "decode")
{
    var encodedValue = param;
    Console.WriteLine(JsonSerializer.Serialize(Bencode.Decode(encodedValue)));
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}

public class Bencode()
{
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
        return Type.GetTypeCode(input.GetType()) switch
        {
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => $"i{input}e",
            TypeCode.String => $"{((string)input).Length}:{input}",
            TypeCode.Object => input is object[] inputArray ? $"l{string.Join("", inputArray.Select(x => Encode(x)))}e"
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

    public static Dictionary<object, object> DictionaryDecode(string input)
    {
        input = input[1..];
        var result = new Dictionary<object, object>();
        while (input.Length > 0 && input[0] != 'e')
        {
            Console.WriteLine(input);
            var key = StringDecode(input);
            Console.WriteLine(key);
            input = input[Encode(key).Length..];

            var value = Decode(input);
            Console.WriteLine(value);
            input = input[Encode(value).Length..];

            result.Add(key, value);
        }
        return result;
    }
}