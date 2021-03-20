namespace BAOOProxy
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(/*string[] args*/)
        {
            var Config = Preparation.FindAndSaveBAOPAth();
            await TcpProxyServer.Start("ozzypc-wbid.live.ws.fireteam.net", 443, "127.0.0.1", 0, Config);
        }
    }
}
