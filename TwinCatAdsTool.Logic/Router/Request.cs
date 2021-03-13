using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class Request
    {
        public const int DEFAULT_UDP_PORT = 48899;

        public UdpClient Client { get; }

        public int timeout;
        public int Timeout
        {
            get => timeout;

            set
            {
                timeout = value;
                Client.Client.ReceiveTimeout = Client.Client.SendTimeout = Timeout;
            }
        }

        public Request(int timeout = 10000)
        {
            Client = new UdpClient {EnableBroadcast = true};
            Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Timeout = timeout;
        }

        public async Task<Response> SendAsync(IPEndPoint endPoint)
        {
            var data = GetRequestBytes;
            await Client.SendAsync(data, data.Length, endPoint);

            return new Response(Client, Timeout);
        }

        private readonly List<byte[]> _listOfBytes = new List<byte[]>();
        public byte[] GetRequestBytes
        {
            get { return _listOfBytes.SelectMany(a => a).ToArray(); }
        }

        public void Add(byte[] segment)
        {
            _listOfBytes.Add(segment);
        }

        public void Clear()
        {
            _listOfBytes.Clear();
        }
    }
}
