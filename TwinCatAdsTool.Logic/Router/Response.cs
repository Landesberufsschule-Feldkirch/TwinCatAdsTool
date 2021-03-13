using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TwinCatAdsTool.Logic.Router
{
    internal class Response
    {
        private readonly UdpClient _client;
        private readonly int _timeout;

        public Response(UdpClient client, int timeout = 10000)
        {
            _client = client;
            _timeout = timeout;
        }

        public async Task<List<ResponseResult>> ReceiveMultipleAsync()
        {
            var results = new List<ResponseResult>();
            var stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Reset();
                stopwatch.Start();
                
                var worker = _client.ReceiveAsync();
                var task = await Task.WhenAny(worker, Task.Delay(_timeout));

                if (stopwatch.ElapsedMilliseconds < _timeout && task == worker)
                {
                    var udpResult = worker.Result;
                    results.Add(new ResponseResult(udpResult));
                }
                else
                {
                    _client.Close();
                    break;
                }
            }

            return results;
        }
    }
}

