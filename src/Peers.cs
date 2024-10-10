using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent.src
{
    internal class Peers
    {
        public static async Task<string[]> GetPeers(string path)
        {
            HttpClient client = new HttpClient();
            var bytes = ReadWriteFile.ReadBytesFromFile(path);
            string text = ReadWriteFile.ReadStringFromFile(path);
            var hashInfo = Bencode.GetInfoHashBytes(bytes, text);

            MetaInfo metaInfo = MetaInfo.GetInfo(path);

            var urlEncoded = HttpUtility.UrlEncode(hashInfo);
            var queryParameters = new Dictionary<string, string>
            {
                {"info_hash", urlEncoded},
                {"peer_id", "12345678912345678900"},
                {"port", "6881"},
                {"uploaded", "0"},
                {"downloaded", "0"},
                {"left", metaInfo.Info.Length.ToString()},
                {"compact", "1"},
            };

            var queryString = string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));
            var url = $"{metaInfo.Announce}?{queryString}";
            var response = await client.GetAsync(url);
            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var contentString = await response.Content.ReadAsStringAsync();

            var peersStart = "5:peers";
            contentBytes = contentBytes[(contentString.IndexOf(peersStart) + peersStart.Length)..];
            contentString = contentString[(contentString.IndexOf(peersStart) + peersStart.Length)..];

            var peersLength = int.Parse(contentString.Substring(0, contentString.IndexOf(":")));

            var peersBytes = contentBytes[(contentString.IndexOf(":") + 1)..(contentString.IndexOf(":") + 1 + peersLength)];

            string[] peers = new string[peersBytes.Length / 6];

            for (int i = 0; i < peersBytes.Length / 6; i++)
            {
                peers[i] = GetIpFromBytes(peersBytes[(6 * i)..(6 * (i + 1))]);
            }
            return peers;
        }

        public static string GetIpFromBytes(byte[] bytes)
        {
            string result = "";

            result = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}:{BitConverter.ToUInt16(new byte[2] { bytes[5], bytes[4] })}";

            return result;
        }
    }
}
