using System.Net;
using TwinCAT.Ads;

namespace TwinCatAdsTool.Logic.Router
{
    public class RemotePlcInfo
    {
        public string Name { get; set; } = "";

        public IPAddress Address { get; set; } = IPAddress.Any;

        public AmsNetId AmsNetId { get; set; } = new AmsNetId("127.0.0.1.1.1");


        public string OsVersion { get; set; } = "";

        public string Comment { get; set; } = "";

        public AdsVersion TcVersion  = new AdsVersion();


        public bool IsRuntime { get; set; } = false;

        public string TcVersionString => TcVersion.Version + "." + TcVersion.Revision + "." + TcVersion.Build;
    }
}
