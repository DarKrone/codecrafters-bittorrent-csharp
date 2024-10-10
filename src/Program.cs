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
    private static async Task Main(string[] args)
    {
        var command = args[0];

        // Parse command and act accordingly
        if (command == "decode")
        {
            var torrentFileName = args[1];
            var encodedValue = torrentFileName;
            Console.WriteLine(JsonSerializer.Serialize(Bencode.Decode(encodedValue)));
        }
        else if (command == "info")
        {
            var torrentFileName = args[1];

            MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
            var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
            var text = ReadWriteFile.ReadStringFromFile(torrentFileName);
            var hashInfo = Bencode.GetInfoHashString(bytes, text);

            var pieceHashes = Bencode.GetPieceHashes(metaInfo.Info.Length, metaInfo.Info.PieceLength, bytes, text);

            Console.WriteLine($"Tracker URL: {metaInfo.Announce}\nLength: {metaInfo?.Info?.Length}\nInfo Hash: {hashInfo}" +
                $"\nPiece Length: {metaInfo?.Info.PieceLength}\nPiece Hashes:\n" +
                $"{string.Join("\n", pieceHashes)}");
        }
        else if (command == "peers")
        {
            var torrentFileName = args[1];

            var peers = Peers.GetPeers(torrentFileName);

            foreach (var peer in peers.Result)
            {
                Console.WriteLine(peer);
            }
        }
        else if (command == "handshake")
        {
            var torrentFileName = args[1];
            var address = args[2];

            var tcpClient = new TcpClient();
            var addressAndPort = Address.GetAddressFromIPv4(address!);

            await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
            var stream = tcpClient.GetStream();
            var peerID = HandShake.DoHandShake(torrentFileName, address!, stream).Result;

            tcpClient.Close();
            Console.WriteLine($"Peer ID: {peerID}");
        }
        else if (command == "download_piece")
        {
            var outputFlag = args[1];
            var pieceLocation = args[2];
            var torrentFileName = args[3];
            var pieceIndex = int.Parse(args[4]);

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
            await Download.GetReadyToDownload(stream);
            var filePiece = await Download.DownloadPiece(stream, pieceIndex, metaInfo, pieceHashes[pieceIndex]);
            ReadWriteFile.WriteBytesToFile(pieceLocation, filePiece);

            tcpClient.Close();
        }
        else if (command == "download")
        {
            var outputFlag = args[1];
            var fileLocation = args[2];
            var torrentFileName = args[3];

            MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
            var bytes = ReadWriteFile.ReadBytesFromFile(torrentFileName);
            var text = ReadWriteFile.ReadStringFromFile(torrentFileName);
            var pieceHashes = Bencode.GetPieceHashes(metaInfo.Info.Length, metaInfo.Info.PieceLength, bytes, text);

            var tcpClient = new TcpClient();

            var peers = Peers.GetPeers(torrentFileName);
            var address = peers.Result[0];

            var addressAndPort = Address.GetAddressFromIPv4(address!);

            await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);


            List<byte> combinedPieces = new List<byte>();
            var stream = tcpClient.GetStream();
            var peerID = HandShake.DoHandShake(torrentFileName, address!, stream).Result;
            await Download.GetReadyToDownload(stream);
            for (int i = 0; i < pieceHashes.Length; i++)
            {
                var filePiece = await Download.DownloadPiece(stream, i, metaInfo, pieceHashes[i]);
                combinedPieces.AddRange(filePiece);
            }
            ReadWriteFile.WriteBytesToFile(fileLocation, combinedPieces.ToArray());

            tcpClient.Close();
        }
        else
        {
            throw new InvalidOperationException($"Invalid command: {command}");
        }
    }
}