using System.Text;
using System.Text.Json;
using System.Web;
using System.Net.Sockets;
using System.Net;
using codecrafters_bittorrent.src;
using System.Net.Http;
using System.Security.Cryptography;

internal class Program
{
    private static void Main(string[] args)
    {
        var command = args[0];

        // Parse command and act accordingly
        if (command == "decode")
        {
            ShowDecode(args[1]);
        }
        else if (command == "info")
        {
            ShowInfo(args[1]);
        }
        else if (command == "peers")
        {
            ShowPeers(args[1]);
        }
        else if (command == "handshake")
        {
            ShowPeerId(args[1], args[2]);
        }
        else if (command == "download_piece")
        {
            Download(args[1], args[2], args[3], int.Parse(args[4]));
        }
        else if (command == "download")
        {
            Download(args[1], args[2], args[3], -1); // -1 for download all pieces
        }
        else
        {
            throw new InvalidOperationException($"Invalid command: {command}");
        }
    }

    private static void ShowDecode(string torrentFileName)
    {
        var encodedValue = torrentFileName;
        Console.WriteLine(JsonSerializer.Serialize(Bencode.Decode(encodedValue)));
    }

    private static void ShowInfo(string torrentFileName)
    {
        MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
        var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
        var text = ReadWriteFile.ReadStringFromFile(torrentFileName);
        var hashInfo = Bencode.GetInfoHashString(bytes, text);

        var pieceHashes = Bencode.GetPieceHashes(metaInfo.Info.Length, metaInfo.Info.PieceLength, bytes, text);

        Console.WriteLine($"Tracker URL: {metaInfo.Announce}\nLength: {metaInfo?.Info?.Length}\nInfo Hash: {hashInfo}" +
            $"\nPiece Length: {metaInfo?.Info.PieceLength}\nPiece Hashes:\n" +
            $"{string.Join("\n", pieceHashes)}");
    }

    private static void ShowPeers(string torrentFileName)
    {
        var peers = Peers.GetPeers(torrentFileName);

        foreach (var peer in peers.Result)
        {
            Console.WriteLine(peer);
        }
    }

    private static async void ShowPeerId(string torrentFileName, string address)
    {
        var tcpClient = new TcpClient();
        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();
        var peerID = HandShake.DoHandShake(torrentFileName, address!, stream).Result;

        tcpClient.Close();
        Console.WriteLine($"Peer ID: {peerID}");
    }

    private static async void Download(string outputFlag, string fileLocation, string torrentFileName, int pieceIndex) // if need download all pieces: pieceIndex = -1
    {
        MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
        var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
        var text = ReadWriteFile.ReadStringFromFile(torrentFileName);
        var pieceHashes = Bencode.GetPieceHashes(metaInfo.Info.Length, metaInfo.Info.PieceLength, bytes, text);

        var tcpClient = new TcpClient();

        var peers = Peers.GetPeers(torrentFileName);
        var address = peers.Result[0];

        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);

        var stream = tcpClient.GetStream();
        var peerID = HandShake.DoHandShake(torrentFileName, address!, stream).Result;
        await codecrafters_bittorrent.src.Download.GetReadyToDownload(stream);
        if (pieceIndex == -1)
        {
            List<byte> combinedPieces = new List<byte>();
            for (int i = 0; i < pieceHashes.Length; i++)
            {
                var filePiece = await codecrafters_bittorrent.src.Download.DownloadPiece(stream, i, metaInfo, pieceHashes[i]);
                combinedPieces.AddRange(filePiece);
            }
            ReadWriteFile.WriteBytesToFile(fileLocation, combinedPieces.ToArray());
        }
        else
        {
            var filePiece = await codecrafters_bittorrent.src.Download.DownloadPiece(stream, pieceIndex, metaInfo, pieceHashes[pieceIndex]);
            ReadWriteFile.WriteBytesToFile(fileLocation, filePiece);
        }

        tcpClient.Close();
    }
}
