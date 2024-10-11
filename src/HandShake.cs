using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace codecrafters_bittorrent.src
{
    internal class HandShake
    {
        public static async Task<string> DoHandShake(NetworkStream tcpStream, byte[] hashInfo, byte[] reservedBytes = null!)
        {
            byte[] peerId = new byte[20];
            Random rnd = new Random();
            rnd.NextBytes(peerId);

            var pstrLenght = 19;
            var pstr = "BitTorrent protocol";

            if (reservedBytes == null)
                reservedBytes = new byte[8];

            var handShakeMsg = new List<byte>();

            handShakeMsg.Add((byte)pstrLenght);
            handShakeMsg.AddRange(Encoding.ASCII.GetBytes(pstr));
            handShakeMsg.AddRange(reservedBytes);
            handShakeMsg.AddRange(hashInfo);
            handShakeMsg.AddRange(peerId);

            await tcpStream.WriteAsync(handShakeMsg.ToArray());

            var buffer = new byte[68];

            var response = await tcpStream.ReadAsync(buffer);

            return Convert.ToHexString(buffer).ToLower();
        }

        public static async Task<string> DoExtensionsHandShake(NetworkStream tcpStream)
        {
            var msgId = 20;

            var handShakeMsg = new List<byte>();

            Dictionary<string, object> extensionMsg = new Dictionary<string, object>();
            Dictionary<string, object> extensions = new Dictionary<string, object>();

            extensions.Add("ut_metadata", 16);
            extensionMsg.Add("m", extensions);
            extensionMsg.Add("metadata_size", 0);
            extensionMsg.Add("v", "innerBittorrent v0.0.9");

            Console.WriteLine(JsonSerializer.Serialize(extensionMsg));

            var bencodedDict = Bencode.Encode(extensionMsg);
            Console.WriteLine(bencodedDict);
            var byteDict = Encoding.UTF8.GetBytes(bencodedDict);

            var msgLengthPrefix = BitConverter.GetBytes(byteDict.Length + 2).Reverse();

            handShakeMsg.AddRange(msgLengthPrefix);
            handShakeMsg.Add((byte)msgId);
            handShakeMsg.Add((byte)0);
            handShakeMsg.AddRange(byteDict);

            foreach (var item in handShakeMsg)
            {
                Console.Write(item + " ");
            }

            await tcpStream.WriteAsync(handShakeMsg.ToArray());
            Console.WriteLine("Handshake sended");


            var buffer = new byte[4096];

            await tcpStream.ReadAsync(buffer);
            Console.WriteLine("Handshake received");


            foreach (var item in buffer)
            {
                Console.Write(item + " ");
            }

            return Convert.ToHexString(buffer).ToLower();
        }
    }
}
