using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TwinCatAdsTool.Logic.Services
{
    public static class IpHelper
    {
        public static IPAddress GetBroadcastAddress(IPAddress localhost)
        {
            var hostMask = GetHostMask(localhost);

            if (hostMask == null || localhost == null)
            {
                return null;
            }

            var complementedMaskBytes = new byte[4];
            var broadcastIpBytes = new byte[4];

            for (var i = 0; i < 4; i++)
            {
                complementedMaskBytes[i] = (byte)
                    ~hostMask.GetAddressBytes().ElementAt(i);

                broadcastIpBytes[i] = (byte)(
                    localhost.GetAddressBytes().ElementAt(i) |
                    complementedMaskBytes[i]);
            }

            return new IPAddress(broadcastIpBytes);
        }

        /// <summary>
        /// Host mask for the given localhost address.
        /// </summary>
        /// <param name="localhost">Address of the localhost.</param>
        /// <returns>May produce exception or return null!</returns>
        public static IPAddress GetHostMask(IPAddress localhost)
        {
            var strLocalAddress = localhost.ToString();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in interfaces)
            {
                var unicastInfos = netInterface.GetIPProperties().UnicastAddresses;

                foreach (var info in unicastInfos)
                {
                    if (info.Address.ToString() == strLocalAddress)
                    {
                        return info.IPv4Mask;
                    }
                }
            }

            return null;
        }

        public static List<IPAddress> Localhosts => FilteredLocalhosts();

        public static List<IPAddress> FilteredLocalhosts()
        {
            return FilteredLocalhosts(null);
        }

        public static List<IPAddress> FilteredLocalhosts(List<NetworkInterfaceType> niTypes)
        {
            if (niTypes == null)
            {
                niTypes =
                    new List<NetworkInterfaceType> {NetworkInterfaceType.Wireless80211, NetworkInterfaceType.Ethernet};
            }

            var localhosts = new List<IPAddress>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (niTypes.Contains(ni.NetworkInterfaceType))
                {
                    foreach (var unicastInfo in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localhosts.Add(unicastInfo.Address);
                        }
                    }
                }
            }

            return localhosts;
        } // FilteredLocalhosts(...)
    } // class
}
