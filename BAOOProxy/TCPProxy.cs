//Forked from https://github.com/ngbrown/netproxy/tree/branch1
//which is based on https://github.com/Stormancer/netproxy
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
    internal class TcpProxyServer
    {
        /// <summary>
        /// Milliseconds
        /// </summary>
        public int ConnectionTimeout { get; set; } = (4 * 60 * 1000);

        public async System.Threading.Tasks.Task Start(string RemoteServerDNS, ushort RemoteServerPort, string LocalIp, ushort LocalPort, AppConfig AppConfig)
        {
            System.Net.Sockets.TcpListener ProxyServer = null;
            var Connections = new System.Collections.Concurrent.ConcurrentBag<TcpProxyClient>();

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

                var _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    while (true)
                    {
                        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                        var tempConnections = new System.Collections.Generic.List<TcpProxyClient>(Connections.Count);
                        while (Connections.TryTake(out var connection))
                        {
                            tempConnections.Add(connection);
                        }

                        foreach (var tcpConnection in tempConnections)
                        {
                            if (tcpConnection.LastActivity + ConnectionTimeout < Environment.TickCount64)
                            {
                                tcpConnection.Stop();
                            }
                            else
                            {
                                Connections.Add(tcpConnection);
                            }
                        }
                    }
                });

                while (true)
                {
                    try
                    {
                        var GameConnection = await TcpProxyClient.AcceptTcpClientAsync(ProxyServer, RemoteServerDNS, RemoteServerPort, AppConfig).ConfigureAwait(false);
                        GameConnection.Run();
                        Connections.Add(GameConnection);
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

    internal class TcpProxyClient
    {
        //Unused fileds are commented out
        private readonly System.Net.Sockets.TcpClient _GameClient;
        //private readonly System.Net.EndPoint _GameClientRemoteEndpoint;
        private readonly string _RemoteServerDNS;
        private readonly ushort _RemoteServerPort;
        private readonly System.Net.Sockets.TcpClient _ProxyClient;
        private readonly System.Threading.CancellationTokenSource _cancellationTokenSource = new();
        //private readonly System.Net.EndPoint _GameClientLocalEndpoint;
        //private System.Net.EndPoint _ProxyClientLocalEndpoint;
        //private long _totalBytesForwarded;
        //private long _totalBytesResponded;

        private readonly AppConfig _AppConfig;
        private static readonly object _LockObject = new();

        public long LastActivity { get; private set; } = Environment.TickCount64;

        public static async System.Threading.Tasks.Task<TcpProxyClient> AcceptTcpClientAsync(System.Net.Sockets.TcpListener ProxyServer, string RemoteServerDNS, ushort RemoteServerPort, AppConfig AppConfig)
        {
            var localServerConnection = await ProxyServer.AcceptTcpClientAsync().ConfigureAwait(false);
            localServerConnection.NoDelay = true;
            return new TcpProxyClient(localServerConnection, RemoteServerDNS, RemoteServerPort, AppConfig);
        }

        private TcpProxyClient(System.Net.Sockets.TcpClient GameClient, string RemoteServerDNS, ushort RemoteServerPort, AppConfig AppConfig)
        {
            _GameClient = GameClient;
            _RemoteServerDNS = RemoteServerDNS;
            _RemoteServerPort = RemoteServerPort;

            _ProxyClient = new System.Net.Sockets.TcpClient { NoDelay = true };

            //_GameClientRemoteEndpoint = _GameClient.Client.RemoteEndPoint;
            //_GameClientLocalEndpoint = _GameClient.Client.LocalEndPoint;

            _AppConfig = AppConfig;
        }

        public void Run()
        {
            RunInternal(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while closing TcpConnection : {ex}");
            }
        }

        private void RunInternal(System.Threading.CancellationToken cancellationToken)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using (_GameClient)
                    using (_ProxyClient)
                    {
                        await _ProxyClient.ConnectAsync(_RemoteServerDNS, _RemoteServerPort, cancellationToken).ConfigureAwait(false);
                        //_ProxyClientLocalEndpoint = _ProxyClient.Client.LocalEndPoint;

                        //Console.WriteLine($"Established TCP {_GameClientRemoteEndpoint} => {_GameClientLocalEndpoint} => {_ProxyClientLocalEndpoint} => {_RemoteServerDNS}:{_RemoteServerPort}");

                        using (var serverStream = _ProxyClient.GetStream())
                        using (var clientStream = _GameClient.GetStream())
                        using (cancellationToken.Register(() =>
                        {
                            serverStream.Close();
                            clientStream.Close();
                        }, true))
                        {
                            if (_RemoteServerPort == 443)
                            {
                                using var SecureRemoteServerStream = new System.Net.Security.SslStream(serverStream, true, new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate));
                                SecureRemoteServerStream.AuthenticateAsClient(_RemoteServerDNS);

                                await System.Threading.Tasks.Task.WhenAny(
                                   CopyToAsync(clientStream, SecureRemoteServerStream, 81920, Direction.Forward, cancellationToken),
                                   CopyToAsync(SecureRemoteServerStream, clientStream, 81920, Direction.Responding, cancellationToken)
                               ).ConfigureAwait(false);
                            }
                            else
                            {
                                await System.Threading.Tasks.Task.WhenAny(
                                    CopyToAsync(clientStream, serverStream, 81920, Direction.Forward, cancellationToken),
                                    CopyToAsync(serverStream, clientStream, 81920, Direction.Responding, cancellationToken)
                                ).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (System.Security.Authentication.AuthenticationException)
                {
                    //Console.WriteLine("AuthenticationException: {0}", e);
                    Console.WriteLine("Server certificate rejected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception occurred during TCP stream : {ex}");
                }
                finally
                {
                    //Console.WriteLine($"Closed TCP {_GameClientRemoteEndpoint} => {_GameClientLocalEndpoint} => {_ProxyClientLocalEndpoint} => {_RemoteServerDNS}:{_RemoteServerPort}. {_totalBytesForwarded} bytes forwarded, {_totalBytesResponded} bytes responded.");
                }
            }, System.Threading.CancellationToken.None);
        }

        private async System.Threading.Tasks.Task CopyToAsync(System.IO.Stream Source, System.IO.Stream Destination, int BufferSize = 81920, Direction Direction = Direction.Unknown, System.Threading.CancellationToken cancellationToken = default)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(BufferSize);
            string Data = "";

            try
            {
                while (true)
                {
                    int bytesRead = await Source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0) break;
                    LastActivity = Environment.TickCount64;
                    byte[] FixedData = System.Text.Encoding.UTF8.GetBytes(DataHandler.FixUp(System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead), Direction));
                    await Destination.WriteAsync(new ReadOnlyMemory<byte>(FixedData, 0, FixedData.Length), cancellationToken).ConfigureAwait(false);

                    if (_AppConfig.BackupEnabled)
                    {
                        lock (_LockObject)
                        {
                            Data += System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            if (DataHandler.AnalyzeAndBackupWhenDone(Data, Direction, _AppConfig))
                            {
                                Data = "";
                            }
                        }
                    }

                    /*
                    switch (Direction)
                    {
                        case Direction.Forward:
                            System.Threading.Interlocked.Add(ref _totalBytesForwarded, bytesRead);
                            break;
                        case Direction.Responding:
                            System.Threading.Interlocked.Add(ref _totalBytesResponded, bytesRead);
                            break;
                    }
                    */
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
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

    internal enum Direction
    {
        Unknown = 0,
        Forward,
        Responding,
    }
}