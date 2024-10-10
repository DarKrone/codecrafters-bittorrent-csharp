using System.Text;

namespace codecrafters_bittorrent.src
{
    internal class ReadWriteFile
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
