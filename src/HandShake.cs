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
        public static async Task<string> DoHandShake(string path, string address, NetworkStream tcpStream)
        {
            var bytesFile = File.ReadAllBytes(path);
            string text = Encoding.ASCII.GetString(bytesFile);
            byte[] hashInfo = Bencode.GetInfoHashBytes(bytesFile, text);
            string urlEncoded = HttpUtility.UrlEncode(hashInfo);

            byte[] peerId = new byte[20];
            Random rnd = new Random();
            rnd.NextBytes(peerId);

            var pstrLenght = 19;
            var pstr = "BitTorrent protocol";
            var reserved = new byte[8];

            var handShakeMsg = new List<byte>();

            handShakeMsg.Add((byte)pstrLenght);
            handShakeMsg.AddRange(Encoding.ASCII.GetBytes(pstr));
            handShakeMsg.AddRange(reserved);
            handShakeMsg.AddRange(hashInfo);
            handShakeMsg.AddRange(peerId);

            await tcpStream.WriteAsync(handShakeMsg.ToArray());

            var buffer = new byte[68];

            var response = await tcpStream.ReadAsync(buffer);

            return Convert.ToHexString(buffer[(buffer.Length - 20)..]).ToLower();
        }
    }
}
