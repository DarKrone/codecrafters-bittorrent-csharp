using System.Text;
using System.Text.Json;
using System.Web;
using System.Net.Sockets;
using System.Net;
using codecrafters_bittorrent.src;
using System.Net.Http;
using System.Security.Cryptography;
using System;

internal class Program
{
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
                break;
            case "download_piece":
                await DownloadFile(args[1], args[2], args[3], int.Parse(args[4]));
                break;
            case "download":
                await DownloadFile(args[1], args[2], args[3], -1); // -1 for download all pieces
                break;
            case "magnet_parse":
                ShowMagnetLinkInfo(MagnetLink.ParseLink(args[1]));
                break;
            case "magnet_handshake":
                await ShowMagnetLinkPeerId(MagnetLink.ParseLink(args[1]));
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
        var tcpClient = new TcpClient();
        var addressAndPort = Address.GetAddressFromIPv4(address!);

        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();
        var handshakeMsg = HandShake.DoHandShake(stream, Bencode.GetInfoHashBytes(torrentFileName)).Result;

        tcpClient.Close();
        Console.WriteLine($"Peer ID: {handshakeMsg[(handshakeMsg.Length - 20)..]}");
    }

    private static async Task DownloadFile(string outputFlag, string saveFileLocation, string torrentFileName, int pieceIndex) // if need download all pieces: pieceIndex = -1
    {
        var pieceHashes = Bencode.GetPieceHashes(torrentFileName);
        MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
        var tcpClient = new TcpClient();

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

        tcpClient.Close();
    }

    public static void ShowMagnetLinkInfo(MagnetLinkInfo linkInfo)
    {
        Console.WriteLine($"Tracker URL: {linkInfo.Url}\nInfo Hash: {linkInfo.Hash}");
    }

    public static async Task ShowMagnetLinkPeerId(MagnetLinkInfo linkInfo)
    {
        //Establish a TCP connection with a peer
        TcpClient tcpClient = new TcpClient();
        var peers = Peers.GetPeers(Convert.FromHexString(linkInfo.Hash), "999", linkInfo.Url).Result;
        var addressAndPort = Address.GetAddressFromIPv4(peers[0]);
        await tcpClient.ConnectAsync(addressAndPort.Item1, addressAndPort.Item2);
        var stream = tcpClient.GetStream();

        //Send the base handshake message -- Receive the base handshake message
        var reservedBytes = new byte[8];
        reservedBytes[5] = 16;
        string handshakeMsg = await HandShake.DoHandShake(stream, Convert.FromHexString(linkInfo.Hash), reservedBytes);
        string extensionsString = handshakeMsg.Skip(20).Take(16).ToString()!;
        bool supportsExtensions = extensionsString[10] == '1';

        //Send the bitfield message (safe to ignore in this challenge) -- Receive the bitfield message
        await Download.GetBitfield(stream);

        //If the peer supports extensions (based on the reserved bit in the base handshake):
        if (supportsExtensions)
        {
            //Send the extension handshake message
            await HandShake.DoExtensionsHandShake(stream);
        }

        Console.WriteLine($"Peer ID: {handshakeMsg[(handshakeMsg.Length - 20)..]}");
        tcpClient.Close();
    }
}
