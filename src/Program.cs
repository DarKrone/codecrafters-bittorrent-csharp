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
        Console.WriteLine(JsonSerializer.Serialize(StringBencode(encodedValue)));
    }
    else if (encodedValue[0] == 'i')
    {
        Console.WriteLine(JsonSerializer.Serialize(IntegerBencode(encodedValue)));
    }
    else if (encodedValue[0] == 'l')
    {
        
        Console.WriteLine(JsonSerializer.Serialize(ArrayBencode(encodedValue)));
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

string StringBencode(string encodedValue)
{
    // Example: "5:hello" -> "hello"
    var colonIndex = encodedValue.IndexOf(':');
    if (colonIndex != -1)
    {
        var strLength = int.Parse(encodedValue[..colonIndex]);
        var strValue = encodedValue.Substring(colonIndex + 1, strLength);
        return strValue;
    }
    else
    {
        throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
    }
}

long IntegerBencode(string encodedValue)
{
    var endIndex = encodedValue.IndexOf('e');
    if (endIndex != -1)
    {
        if (long.TryParse(encodedValue.Substring(1, endIndex - 1), out var number))
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

List<object> ArrayBencode(string encodedValue)
{
    List<object> array = new List<object>();
    string tempEncodeValue = encodedValue.Substring(1);
    Console.WriteLine(encodedValue);
    while (tempEncodeValue != "e" || tempEncodeValue.Length == 0)
    {
        if (Char.IsDigit(tempEncodeValue[0]))
        {
            var colonIndex = tempEncodeValue.IndexOf(':');
            var strLength = int.Parse(tempEncodeValue[..colonIndex]);

            string tempString = tempEncodeValue.Substring(0, colonIndex + strLength + 1);
            array.Add(StringBencode(tempString));

            tempEncodeValue = tempEncodeValue.Substring(colonIndex + strLength + 1);
        }
        else if (tempEncodeValue[0] == 'i')
        {
            var endIndex = tempEncodeValue.IndexOf('e');
            var numberLength = int.Parse(tempEncodeValue[1..endIndex]);

            string tempString = tempEncodeValue.Substring(0, endIndex + 1);

            array.Add(IntegerBencode(tempString));

            tempEncodeValue = tempEncodeValue.Substring(endIndex + 1);
        }
        else if (tempEncodeValue[0] == 'l')
        {
            array.Add(ArrayBencode(tempEncodeValue.Substring(0, tempEncodeValue.LastIndexOf('e'))));
            tempEncodeValue = tempEncodeValue.Substring(tempEncodeValue.LastIndexOf('e'));
        }
        Console.WriteLine(tempEncodeValue);
        Console.WriteLine(JsonSerializer.Serialize(array));
    }
    return array;
}