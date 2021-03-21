using System;

namespace BAOOProxy
{
    class Program
    {
        static void Main(/*string[] args*/)
        {
            Console.Title = "BAOO-SecureProxy";
            var Config = Preparation.FindAndSaveBAOPAth();
            var Proxy = new TcpProxyServer();
            Proxy.Start("ozzypc-wbid.live.ws.fireteam.net", 443, "127.0.0.1", 0, Config).Wait();
        }
    }
}
