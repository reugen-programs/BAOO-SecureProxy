//Forked from https://github.com/Stormancer/netproxy
//
//
//
//MIT License
//
//Copyright (c) 2021 Stormancer
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace BAOOProxy
{
    class TcpProxyServer
    {
        public static async System.Threading.Tasks.Task Start(string RemoteServerDNS, ushort RemoteServerPort, string LocalIp, ushort LocalPort, AppConfig AppConfig)
        {
            System.Net.Sockets.TcpListener ProxyServer = null;
            try
            {
                System.Net.IPAddress LocalIpAddress = string.IsNullOrEmpty(LocalIp) ? System.Net.IPAddress.Parse("127.0.0.1") : System.Net.IPAddress.Parse(LocalIp);
                ProxyServer = new System.Net.Sockets.TcpListener(new System.Net.IPEndPoint(LocalIpAddress, LocalPort));
                ProxyServer.Server.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.IPv6Only, false);
                ProxyServer.Start();

                var ProxyEndPoint = ProxyServer.Server.LocalEndPoint;
                Preparation.RerouteBAOOToProxy(AppConfig, ProxyEndPoint);
                Console.WriteLine($"TCP proxy started {ProxyEndPoint} -> {RemoteServerDNS}:{RemoteServerPort}");
                Console.WriteLine("You can now start the game.");
                while (true)
                {
                    try
                    {
                        var GameClient = await ProxyServer.AcceptTcpClientAsync();
                        GameClient.NoDelay = true;
                        _ = new TcpProxyClient(GameClient, RemoteServerDNS, RemoteServerPort, AppConfig);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                        Console.ResetColor();
                    }
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine("Error Code: {0}", e.ErrorCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                ProxyServer.Stop();
            }

            Console.WriteLine("Press ENTER to exit.");
            Console.Read();
        }
    }

    class TcpProxyClient
    {
        private System.Net.Sockets.TcpClient _GameClient;
        //private readonly System.Net.IPEndPoint _GameClientEndpoint;
        private readonly string _RemoteServerDNS;
        private readonly ushort _RemoteServerPort;
        public System.Net.Sockets.TcpClient _ProxyClient = new();
        private static readonly object LockObject = new();

        public TcpProxyClient(System.Net.Sockets.TcpClient GameClient, string RemoteServerDNS, ushort RemoteServerPort, AppConfig AppConfig)
        {
            _GameClient = GameClient;
            _RemoteServerDNS = RemoteServerDNS;
            _RemoteServerPort = RemoteServerPort;
            _ProxyClient.NoDelay = true;
            //_GameClientEndpoint = (System.Net.IPEndPoint)_GameClient.Client.RemoteEndPoint;
            //Console.WriteLine($"Established {_GameClientEndpoint} => {_RemoteServerDNS}:{_RemoteServerPort}");

            Run(AppConfig);
        }
        private void Run(AppConfig AppConfig)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using (_GameClient)
                    using (_ProxyClient)
                    {
                        await _ProxyClient.ConnectAsync(_RemoteServerDNS, _RemoteServerPort);
                        using var RemoteServerStream = _ProxyClient.GetStream();

                        if (_RemoteServerPort == 443)
                        {
                            using var SecureRemoteServerStream = new System.Net.Security.SslStream(RemoteServerStream, true, new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate));
                            SecureRemoteServerStream.AuthenticateAsClient(_RemoteServerDNS);

                            using var GameClientStream = _GameClient.GetStream();
                            await System.Threading.Tasks.Task.WhenAny(CopyStreamToStream(GameClientStream, SecureRemoteServerStream, AppConfig), CopyStreamToStream(SecureRemoteServerStream, GameClientStream, AppConfig));
                        }
                        else
                        {
                            using var GameClientStream = _GameClient.GetStream();
                            await System.Threading.Tasks.Task.WhenAny(CopyStreamToStream(GameClientStream, RemoteServerStream, AppConfig), CopyStreamToStream(RemoteServerStream, GameClientStream, AppConfig));
                        }
                    }
                }
                catch (System.Security.Authentication.AuthenticationException)
                {
                    //Console.WriteLine("AuthenticationException: {0}", e);
                    Console.WriteLine("Server certificate rejected.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e);
                }
                finally
                {
                    //Console.WriteLine($"Closed {_GameClientEndpoint} => {_RemoteServerDNS}:{_RemoteServerPort}");
                    _GameClient = null;
                }
            });
        }
        private static async System.Threading.Tasks.Task CopyStreamToStream(System.IO.Stream Input, System.IO.Stream Output, AppConfig AppConfig)
        {
            byte[] Buffer = new byte[1048576];
            int BytesRead/* = -1*/;
            string Data = "";
            do
            {
                try
                {
                    BytesRead = await Input.ReadAsync(Buffer.AsMemory(0, Buffer.Length));
                    byte[] FixedData = System.Text.Encoding.UTF8.GetBytes(DataHandler.FixUp(System.Text.Encoding.UTF8.GetString(Buffer, 0, BytesRead)));
                    await Output.WriteAsync(FixedData.AsMemory(0, FixedData.Length));
                    if (AppConfig.BackupEnabled)
                    {
                        lock (LockObject)
                        {
                            Data += System.Text.Encoding.UTF8.GetString(Buffer, 0, BytesRead);
                            if (DataHandler.AnalyzeAndBackupWhenDone(Data, AppConfig))
                            {
                                Data = "";
                            }
                        }
                    }
                }
                catch (System.IO.IOException) { break; }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            } while (BytesRead > 0);
        }
        private static bool ValidateServerCertificate(
            object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                return true;

            //Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            //WB server cert hash - 9C8FF2E38304552E33E6E08C66323467D182C15F
            if (certificate.GetCertHashString().Equals("9C8FF2E38304552E33E6E08C66323467D182C15F"))
            {
                //Console.WriteLine("Certificate exception allowed");
                return true;
            }

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}
