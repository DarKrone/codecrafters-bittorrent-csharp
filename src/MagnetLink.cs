using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent.src
{
    public class MagnetLink
    {
        public static MagnetLinkInfo ParseLink(string link)
        {
            string tempLink = link;
            tempLink = tempLink[(tempLink.IndexOf("urn:btih:") + 9)..];
            string urn = tempLink[..tempLink.IndexOf("&")];

            tempLink = tempLink[(tempLink.IndexOf("&") + 1)..];
            string dn = tempLink[3..tempLink.IndexOf("&")];

            tempLink = tempLink[(tempLink.IndexOf("&") + 1)..];
            string tr = tempLink[3..];
            tr = HttpUtility.UrlDecode(tr);

            return new MagnetLinkInfo(urn, dn, tr);
        }
    }

    public class MagnetLinkInfo
    {
        public string Hash { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public MagnetLinkInfo(string hash, string name, string url)
        {
            Hash = hash;
            Name = name;
            Url = url;
        }
    }

    public class MagnetLinkMetadata
    {
        public string Url { get; set; }

        public string Length { get; set; }

        public string InfoHash { get; set; }

        public string PieceLength { get; set; }

        public string[] PiecesHashes { get; set; }

        public MagnetLinkMetadata(string url, string length, string infoHash, string peceLength, string[] piecesHashes)
        {
            Url = url;
            Length = length;
            InfoHash = infoHash;
            PieceLength = peceLength;
            PiecesHashes = piecesHashes;
        }
    }
}
