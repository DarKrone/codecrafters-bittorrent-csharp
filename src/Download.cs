using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace codecrafters_bittorrent.src
{
    internal class Download
    {
        public static async Task GetReadyToDownload(NetworkStream tcpStream)
        {
            await GetBitfield(tcpStream);
            await GetUnchoke(tcpStream);
        }

        public static async Task<bool> GetBitfield(NetworkStream tcpStream)
        {
            var buffer = new byte[4];
            var response = await tcpStream.ReadAsync(buffer);
            var temp = buffer[..10];

            Console.WriteLine("Birfield received: ");
            foreach (var item in temp)
            {
                Console.Write(item + " ");
            }
            Console.WriteLine();

            if (buffer[4] == Convert.ToByte("5"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async void SendBitfield(NetworkStream tcpStream)
        {
            var bitfieldMsg = new List<byte>();
            bitfieldMsg.AddRange(new byte[] {0,0,0,5});
            bitfieldMsg.Add(5);
            bitfieldMsg.AddRange(new byte[] { 0, 0, 0, 0, 0, 0 });
            await tcpStream.WriteAsync(bitfieldMsg.ToArray());
        }

        public static async Task<bool> GetUnchoke(NetworkStream tcpStream)
        {
            List<byte> sendResponse = new List<byte>();

            sendResponse.AddRange(BitConverter.GetBytes(5).ToArray());
            sendResponse.Add(Convert.ToByte("2"));

            await tcpStream.WriteAsync(sendResponse.ToArray());
            sendResponse.Clear();

            var buffer = new byte[4096];
            var response = await tcpStream.ReadAsync(buffer);

            if (buffer[4] == Convert.ToByte("1"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<byte[]> DownloadPiece(NetworkStream tcpStream, int pieceIndex, MetaInfo metaInfo, string neededHash)
        {
            List<byte> sendResponse = new List<byte>();

            const int MAX_BLOCK_LENGTH = 16384;

            int piecesCount = (int)Math.Ceiling((double)metaInfo.Info.Length / metaInfo.Info.PieceLength);

            int blockLength = MAX_BLOCK_LENGTH;

            int pieceLength = (pieceIndex == piecesCount - 1) 
                            ? metaInfo.Info.Length - (metaInfo.Info.PieceLength * (piecesCount - 1))
                            : metaInfo.Info.PieceLength;

            int blocksCount = (int)Math.Ceiling((double)pieceLength / MAX_BLOCK_LENGTH);

            if (blocksCount == 0)
            {
                throw new Exception("No downloadable blocks (16384) in piece");
            }
            var buffer = new byte[4096];
            Console.WriteLine($"Start downloading {blocksCount} blocks, total blocks length - {pieceLength}");

            List<byte> receivedBlocks = new List<byte>();
            for (int i = 0; i < blocksCount; i++)
            {
                blockLength = pieceLength > MAX_BLOCK_LENGTH ? MAX_BLOCK_LENGTH : pieceLength;
                pieceLength -= blockLength;

                sendResponse.AddRange(BitConverter.GetBytes(17).Reverse());
                sendResponse.Add(Convert.ToByte("6"));
                sendResponse.AddRange(BitConverter.GetBytes(pieceIndex).Reverse());
                sendResponse.AddRange(BitConverter.GetBytes(MAX_BLOCK_LENGTH * i).Reverse());
                sendResponse.AddRange(BitConverter.GetBytes(blockLength).Reverse());
                await tcpStream.WriteAsync(sendResponse.ToArray());

                sendResponse.Clear();
                buffer = new byte[blockLength + 13];
                await tcpStream.ReadExactlyAsync(buffer, 0, blockLength + 13);
                receivedBlocks.AddRange(buffer.Skip(13));
                Console.WriteLine($"Downloaded {i + 1} block. Length of block - {blockLength}");
            }
            Console.WriteLine("All blocks downloaded and combined");


            var resultHash = Convert.ToHexString(SHA1.HashData(receivedBlocks.ToArray())).ToLower();
            Console.WriteLine("Needed hash : " + neededHash);
            Console.WriteLine("Control Hash : " + resultHash);
            if (resultHash == neededHash)
            {
                Console.WriteLine("Hashes matches");
            }
            else 
            {
                Console.WriteLine("Hashes didnt match");
            }
            Console.ForegroundColor = ConsoleColor.White;

            return receivedBlocks.ToArray();
        }
    }
}
