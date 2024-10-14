using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using codecrafters_bittorrent.src;
using System.Reflection.Metadata;


internal class Program
{
    public static TcpClient? tcpClient;

    private static async Task Main(string[] args)
    {
        var command = args[0];

        // Parse command and act accordingly
        switch (command)
        {
            case "decode":
                ShowDecode(args[1]);
                break;
            case "info":
                ShowInfo(args[1]);
                break;
            case "peers":
                ShowPeers(args[1]);
                break;
            case "handshake":
                await ShowPeerId(args[1], args[2]);
                tcpClient?.Close();
                break;
            case "download_piece":
                await DownloadFile(args[1], args[2], args[3], int.Parse(args[4]));
                tcpClient?.Close();
                break;
            case "download":
                await DownloadFile(args[1], args[2], args[3], -1); // -1 for download all pieces
                tcpClient?.Close();
                break;
            case "magnet_parse":
                ShowMagnetLinkInfo(MagnetLink.ParseLink(args[1]));
                break;
            case "magnet_handshake":
                var metadataId = await GetMagnetLinkPeerId(MagnetLink.ParseLink(args[1]));
                tcpClient?.Close();
                Console.WriteLine($"Peer Metadata Extension ID: {metadataId}");
                break;
            case "magnet_info":
                var info = await GetMagnetInfo(MagnetLink.ParseLink(args[1]));
                Console.WriteLine($"Tracker URL: {info.Url}\nLength: {info.Length}\nInfo Hash: {info.InfoHash}\nPiece Length: {info.PieceLength}" +
                          $"\n{string.Join("\n", info.PiecesHashes)}");
                tcpClient?.Close();
                break;
            case "magnet_download_piece":
                var magnetInfo = await GetMagnetInfo(MagnetLink.ParseLink(args[3]));
                await DownloadFileMagnet(args[2], magnetInfo, int.Parse(args[4]));
                tcpClient?.Close();
                break;
            case "magnet_download": // if i dont get lazy -- need to refactor this all
                var magnetInfo2 = await GetMagnetInfo(MagnetLink.ParseLink(args[3]));
                await DownloadFileMagnet(args[2], magnetInfo2, -1);
                tcpClient?.Close();
                break;
            default:
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
        var hashInfo = Bencode.GetInfoHashString(torrentFileName);

        var pieceHashes = Bencode.GetPieceHashes(torrentFileName);

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

    private static async Task ShowPeerId(string torrentFileName, string address)
    {
        tcpClient = new TcpClient();
        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();
        var handshakeMsgBytes = HandShake.DoHandShake(stream, Bencode.GetInfoHashBytes(torrentFileName)).Result;
        var handshakeMsgString = BitConverter.ToString(handshakeMsgBytes);
        handshakeMsgString = handshakeMsgString.Replace("-", "").ToLower();

        Console.WriteLine($"Peer ID: {handshakeMsgString[(handshakeMsgString.Length - 40)..]}");
    }

    private static async Task DownloadFile(string outputFlag, string saveFileLocation, string torrentFileName, int pieceIndex) // if need download all pieces: pieceIndex = -1
    {
        var pieceHashes = Bencode.GetPieceHashes(torrentFileName);
        MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
        tcpClient = new TcpClient();

        var peers = Peers.GetPeers(torrentFileName);
        var address = peers.Result[0];

        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);

        var stream = tcpClient.GetStream();
        var handshakeMsg = HandShake.DoHandShake(stream, Bencode.GetInfoHashBytes(torrentFileName)).Result;
        await Download.GetReadyToDownload(stream);
        if (pieceIndex == -1)
        {
            List<byte> combinedPieces = new List<byte>();
            for (int i = 0; i < pieceHashes.Length; i++)
            {
                var filePiece = await Download.DownloadPiece(stream, i, metaInfo, pieceHashes[i]);
                combinedPieces.AddRange(filePiece);
            }
            ReadWriteFile.WriteBytesToFile(saveFileLocation, combinedPieces.ToArray());
        }
        else
        {
            var filePiece = await Download.DownloadPiece(stream, pieceIndex, metaInfo, pieceHashes[pieceIndex]);
            ReadWriteFile.WriteBytesToFile(saveFileLocation, filePiece);
        }
    }

    public static void ShowMagnetLinkInfo(MagnetLinkInfo linkInfo)
    {
        Console.WriteLine($"Tracker URL: {linkInfo.Url}\nInfo Hash: {linkInfo.Hash}");
    }

    public static async Task<string> GetMagnetLinkPeerId(MagnetLinkInfo linkInfo)
    {
        //Establish a TCP connection with a peer
        var peers = Peers.GetPeers(Convert.FromHexString(linkInfo.Hash), "999", linkInfo.Url).Result;

        tcpClient = new TcpClient();
        var addressAndPort = Address.GetAddressFromIPv4(peers[0]);
        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();

        //Send the base handshake message -- Receive the base handshake message
        var reservedBytes = new byte[8];
        reservedBytes[5] = 16;
        byte[] handshakeMsgBytes = await HandShake.DoHandShake(stream, Convert.FromHexString(linkInfo.Hash), reservedBytes);
        string handshakeMsg = Convert.ToHexString(handshakeMsgBytes).ToLower();

        string extensionsString = handshakeMsg[40..56];
        bool supportsExtensions = extensionsString[10] == '1';
        Console.WriteLine($"Peer ID: {handshakeMsg[(handshakeMsg.Length - 40)..]}");

        //Send the bitfield message (safe to ignore in this challenge) -- Receive the bitfield message
        if (!await Download.GetBitfield(stream))
        {
            return "-1";
        }

        //If the peer supports extensions (based on the reserved bit in the base handshake):
        if (supportsExtensions)
        {
            //Send the extension handshake message
            var extHandshakeMsgBytes = await HandShake.DoExtensionsHandShake(stream);
            var extHandshakePayload = Bencode.Decode(Encoding.UTF8.GetString(extHandshakeMsgBytes));

            Dictionary<string, object> payloadDict = (Dictionary<string, object>)extHandshakePayload;
            Dictionary<string, object> payloadInnerDict = (Dictionary<string, object>)payloadDict["m"];
            string metadataId = payloadInnerDict["ut_metadata"].ToString()!;
            return metadataId;
        }
        return "-1";
    }

    public static async Task<MagnetLinkMetadata> GetMagnetInfo(MagnetLinkInfo linkInfo) //This method is so bad (((
    {
        var peerId = await GetMagnetLinkPeerId(linkInfo);
        var magnetInfoBytes = await Download.SendMetadataRequest(tcpClient?.GetStream()!, peerId);
        var magnetInfoString = Encoding.UTF8.GetString(magnetInfoBytes);

        var piecesLength = magnetInfoString.Skip(magnetInfoString.IndexOf("6:pieces") + 8).Take(magnetInfoString.IndexOf(":")).ToArray(); //This part
        var dataWithoutPieces = magnetInfoBytes.Take(magnetInfoString.IndexOf("6:pieces")).ToList();

        var extHandshakePayload = Bencode.Decode(Encoding.UTF8.GetString(dataWithoutPieces.ToArray()) + "e"); // especially this part
        Dictionary<string, object> payloadDict = (Dictionary<string, object>)extHandshakePayload;

        var pieceHashes = Bencode.GetPieceHashes(magnetInfoBytes, magnetInfoString, int.Parse(payloadDict["length"].ToString()!), 
                                                 int.Parse(payloadDict["piece length"].ToString()!));

        return new MagnetLinkMetadata(linkInfo.Url, payloadDict["length"].ToString()!, linkInfo.Hash, payloadDict["piece length"].ToString()!, pieceHashes);
    }

    public static async Task DownloadFileMagnet(string saveFileLocation, MagnetLinkMetadata info, int pieceIndex) //This is also so shity code
    {
        tcpClient = new TcpClient();

        var peers = Peers.GetPeers(Convert.FromHexString(info.InfoHash), "999", info.Url).Result;
        var address = peers[1];
        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();

        var handshakeMsg = HandShake.DoHandShake(stream, Convert.FromHexString(info.InfoHash)).Result;

        var metaInfo = new MetaInfo();
        metaInfo.Info.Length = int.Parse(info.Length);
        metaInfo.Info.PieceLength = int.Parse(info.PieceLength);
        await Download.GetReadyToDownload(stream);
        if (pieceIndex == -1)
        {
            List<byte> combinedPieces = new List<byte>();
            for (int i = 0; i < info.PiecesHashes.Length; i++)
            {
                var filePiece = await Download.DownloadPiece(stream, i, metaInfo, info.PiecesHashes[i]);
                combinedPieces.AddRange(filePiece);
            }
            ReadWriteFile.WriteBytesToFile(saveFileLocation, combinedPieces.ToArray());
        }
        else
        {
            var filePiece = await Download.DownloadPiece(stream, pieceIndex, metaInfo, info.PiecesHashes[pieceIndex]);
            ReadWriteFile.WriteBytesToFile(saveFileLocation, filePiece);
        }
    }
}
