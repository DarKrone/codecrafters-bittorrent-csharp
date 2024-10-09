using System.Text;
using System.Text.Json;
using System.Web;
using System.Net.Sockets;
using System.Net;
using codecrafters_bittorrent.src;

// Parse arguments
var (command, path) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <path>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <path>"),
    _ => (args[0], args[1])
};

// Parse command and act accordingly
if (command == "decode")
{
    var encodedValue = path;
    Console.WriteLine(JsonSerializer.Serialize(Bencode.Decode(encodedValue)));
}
else if (command == "info")
{
    MetaInfo metaInfo = MetaInfo.GetInfo(path);
    var bytes = ReadFile.ReadBytesFromFile(path);
    var text = ReadFile.ReadStringFromFile(path);
    var hashInfo = Bencode.GetInfoHashString(bytes, text);

    var pieceHashes = Bencode.GetPieceHashes(metaInfo.info.length, metaInfo.info.piecelength, bytes, text);

    Console.WriteLine($"Tracker URL: {metaInfo.announce}\nLength: {metaInfo?.info?.length}\nInfo Hash: {hashInfo}" +
        $"\nPiece Length: {metaInfo?.info.piecelength}\nPiece Hashes:\n" +
        $"{String.Join("\n", pieceHashes)}"); 
}
else if(command == "peers")
{
    var peers = Peers.GetPeers(path);

    foreach( var peer in peers.Result)
    {
        Console.WriteLine(peer);
    }
}
else if (command == "handshake")
{
    Console.WriteLine(HandShake.DoHandShake(path, args[2]));
}
else if (command == "download_piece")
{

}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}

