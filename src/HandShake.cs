﻿using System;
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
            extensions.Add("ut_metadata", 1);

            var msgLengthPrefix = new byte[4] { 1, 1, 1, 1 };

            payload.Add("m", extensions);
            var bencodedDict = Bencode.Encode(payload);
            Console.WriteLine(bencodedDict);
            handShakeMsg.AddRange(msgLengthPrefix);
            handShakeMsg.Add((byte)msgId);
            handShakeMsg.AddRange(Convert.FromBase64String(bencodedDict));

            await tcpStream.WriteAsync(handShakeMsg.ToArray());

            var buffer = new byte[68];

            var response = await tcpStream.ReadAsync(buffer);

            return Convert.ToHexString(buffer).ToLower();
        }
    }
}
