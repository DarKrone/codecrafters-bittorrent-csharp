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
using System.Text.Unicode;

namespace codecrafters_bittorrent.src
{
    internal class HandShake
    {
        public static async Task<byte[]> DoHandShake(NetworkStream tcpStream, byte[] hashInfo, byte[] reservedBytes = null!)
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

            return buffer;
        }

        public static async Task<byte[]> DoExtensionsHandShake(NetworkStream tcpStream)
        {
            var msgId = 20;

            var handShakeMsg = new List<byte>();

            Dictionary<string, object> extensionMsg = new Dictionary<string, object>();
            Dictionary<string, object> extensions = new Dictionary<string, object>();

            extensions.Add("ut_metadata", 12);
            extensionMsg.Add("m", extensions);

            var bencodedDict = Bencode.Encode(extensionMsg);
            var byteDict = Encoding.UTF8.GetBytes(bencodedDict);

            var msgLengthPrefix = BitConverter.GetBytes(byteDict.Length + 2).Reverse();

            handShakeMsg.AddRange(msgLengthPrefix);
            handShakeMsg.Add((byte)msgId);
            handShakeMsg.Add((byte)0);
            handShakeMsg.AddRange(byteDict);

            await tcpStream.WriteAsync(handShakeMsg.ToArray());

            var msgPrefix = new byte[4];
            await tcpStream.ReadExactlyAsync(msgPrefix, 0, 4);
            Array.Reverse(msgPrefix);
            var msgLength = BitConverter.ToInt32(msgPrefix);

            var ids = new byte[2];
            await tcpStream.ReadExactlyAsync(ids, 0, 2);

            var payloadDict = new byte[msgLength - 2];
            await tcpStream.ReadExactlyAsync(payloadDict, 0, msgLength - 2);

            return payloadDict;
        }
    }
}
