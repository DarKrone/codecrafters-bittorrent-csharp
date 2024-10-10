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
        public static async Task<string[]> GetPeers(byte[] hashInfo, string leftLength, string url)
        {
            HttpClient client = new HttpClient();

            var hashEncoded = HttpUtility.UrlEncode(hashInfo);
            var queryParameters = new Dictionary<string, string>
            {
                {"info_hash", hashEncoded},
                {"peer_id", "12345678912345678900"},
                {"port", "6881"},
                {"uploaded", "0"},
                {"downloaded", "0"},
                {"left", leftLength},
                {"compact", "1"},
            };

            var queryString = string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));
            var response = await client.GetAsync($"{url}?{queryString}");
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

        public static Task<string[]> GetPeers(string torrentFileName)
        {
            MetaInfo metaInfo = MetaInfo.GetInfo(torrentFileName);
            var hashInfo = Bencode.GetInfoHashBytes(torrentFileName);

            return GetPeers(hashInfo, metaInfo.Info.Length.ToString(), metaInfo.Announce!);
        }

        public static string GetIpFromBytes(byte[] bytes)
        {
            string result = "";

            result = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}:{BitConverter.ToUInt16(new byte[2] { bytes[5], bytes[4] })}";

            return result;
        }
    }
}
