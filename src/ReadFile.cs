using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src
{
    internal class ReadFile
    {
        public static byte[] ReadBytesFromFile(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static string ReadStringFromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
