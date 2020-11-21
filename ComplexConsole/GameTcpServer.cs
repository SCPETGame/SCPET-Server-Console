using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ComplexConsole
{
    public class GameTcpServer
    {
        public TcpListener server;
        private Thread serverReceiveThread;
        private int port;
        public List<GameConsoleClient> clients = new List<GameConsoleClient>();

        public GameTcpServer(int port)
        {
            this.port = port;
            serverReceiveThread = new Thread(Listen)
            {
                Name = "ListenMaster"
            };
            serverReceiveThread.IsBackground = true;
            serverReceiveThread.Start();
        }

        public void Listen()
        {
            while (true)
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                try
                {
                    while (true)
                    {
                        if (server.Pending())
                        {
                            TcpClient client = server.AcceptTcpClient();
                            GameConsoleClient client2 = new GameConsoleClient()
                            {
                                client = client
                            };
                            clients.Add(client2);
                            client2.RunThreads();
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        public void SendToAll(string data)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Write(data);
            }
        }

        public static byte[] RSAEncrypt(byte[] data, RSAParameters keyinfo)
        {
            try
            {
                byte[] encrypted;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(keyinfo);
                    encrypted = rsa.Encrypt(data, false);
                }
                return encrypted;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static byte[] RSADecrypt(byte[] data, RSAParameters keyinfo)
        {
            try
            {
                byte[] unencrypted;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(keyinfo);
                    unencrypted = rsa.Decrypt(data, false);
                }
                return unencrypted;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}