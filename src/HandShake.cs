using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent.src
{
    internal class HandShake
    {
        public static async Task<string> DoHandShake(NetworkStream tcpStream, byte[] hashInfo, byte[] reservedBytes = null!)
        {
            byte[] peerId = new byte[20];
            Random rnd = new Random();
            rnd.NextBytes(peerId);

            foreach (byte b in peerId)
            {
                Console.Write(b);
            }
            Console.WriteLine();
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

            Dictionary<string, object> payload = new Dictionary<string, object>();
            Dictionary<string, object> extensions = new Dictionary<string, object>();
            extensions.Add("ut_metadata", 16);


            payload.Add("m", extensions);
            var bencodedDict = Bencode.Encode(payload);
            var byteDict = Encoding.UTF8.GetBytes(bencodedDict);
            Console.WriteLine(byteDict.Length + 1);
            var msgLengthPrefix = BitConverter.GetBytes(byteDict.Length + 1).Reverse();
            foreach( var key in msgLengthPrefix)
            {
                Console.Write(key);
            }
            Console.WriteLine();

            Console.WriteLine(bencodedDict);
            handShakeMsg.AddRange(msgLengthPrefix);
            handShakeMsg.Add((byte)msgId);
            handShakeMsg.AddRange(Encoding.UTF8.GetBytes(bencodedDict));

            await tcpStream.WriteAsync(handShakeMsg.ToArray());
            Console.WriteLine("Msg sended");

            return Convert.ToHexString(new byte[]{ 1,1,1}).ToLower();
        }
    }
}
