using System.Net;


namespace codecrafters_bittorrent.src
{
    internal class Address
    {
        public static (IPAddress, int) GetAddressFromIPv4(string address)
        {
            var ipString = address.Substring(0, address.IndexOf(':'));
            byte[] ipBytes = new byte[4];
            string[] ipNUmbers = ipString.Split('.');

            for (int i = 0; i < ipNUmbers.Length; i++)
            {
                ipBytes[i] = Convert.ToByte(ipNUmbers[i]);
            }
            IPAddress ip = new IPAddress(ipBytes);
            var port = int.Parse(address.Substring(address.IndexOf(":") + 1));

            return (ip, port);
        }
    }
}
