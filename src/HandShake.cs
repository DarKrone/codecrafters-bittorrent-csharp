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
        public static async Task<string> DoHandShake(string path, string address)
        {
            var ipString = address.Substring(0, address.IndexOf(':'));
            byte[] ipBytes = new byte[4];
            string[] ipNUmbers = ipString.Split('.');

            for (int i = 0; i < ipNUmbers.Length; i++)
            {
                ipBytes[i] = Convert.ToByte(ipNUmbers[i]);
            }

            var bytesFile = File.ReadAllBytes(path);
            string text = Encoding.ASCII.GetString(bytesFile);
            byte[] hashInfo = Bencode.GetInfoHashBytes(bytesFile, text);
            string urlEncoded = HttpUtility.UrlEncode(hashInfo);


            byte[] peerId = new byte[20];
            Random rnd = new Random();
            rnd.NextBytes(peerId);


            IPAddress ip = new IPAddress(ipBytes);
            Console.WriteLine(ip);
            var port = int.Parse(address.Substring(address.IndexOf(":") + 1));
            Console.WriteLine(port);

            var pstrLenght = 19;
            var pstr = "BitTorrent protocol";
            var reserved = new byte[8];

            var handShakeMsg = new List<byte>();

            handShakeMsg.Add((byte)pstrLenght);
            handShakeMsg.AddRange(Encoding.ASCII.GetBytes(pstr));
            handShakeMsg.AddRange(reserved);
            handShakeMsg.AddRange(hashInfo);
            handShakeMsg.AddRange(peerId);

            using var tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(ip, port);

            var stream = tcpClient.GetStream();

            await stream.WriteAsync(handShakeMsg.ToArray());

            var buffer = new byte[68];

            var response = await stream.ReadAsync(buffer);

            tcpClient.Close();

            return $"Peer ID: {Convert.ToHexString(buffer[(buffer.Length - 20)..]).ToLower()}";
        }
    }
}
