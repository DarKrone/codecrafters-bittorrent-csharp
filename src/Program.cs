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
    if (Char.IsDigit(encodedValue[0]))
    {
        // Example: "5:hello" -> "hello"
        var colonIndex = encodedValue.IndexOf(':');
        if (colonIndex != -1)
        {
            var strLength = int.Parse(encodedValue[..colonIndex]);
            var strValue = encodedValue.Substring(colonIndex + 1, strLength);
            Console.WriteLine(JsonSerializer.Serialize(strValue));
        }
        else
        {
            throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
        }
    }
    else if (encodedValue[0] == 'i')
    {
        var endIndex = encodedValue.IndexOf('e');
        if (endIndex != -1)
        {
            if (int.TryParse(encodedValue.Substring(1, endIndex - 1), out var number))
            {
                Console.WriteLine(JsonSerializer.Serialize(number));
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
    else
    {
        throw new InvalidOperationException("Unhandled encoded value: " + encodedValue);
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
