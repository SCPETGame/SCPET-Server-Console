using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace ComplexConsole
{
    public class GameTcpServer
    {
        public TcpListener server;
        private Thread serverReceiveThread;
        private int port;

        public GameTcpServer(int port)
        {
            this.port = port;
            serverReceiveThread = new Thread(new ThreadStart(Listen));
            serverReceiveThread.IsBackground = true;
            serverReceiveThread.Start();
        }

        public void ClientThread(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            // string str = reader.ReadToEnd();
            // Debug.WriteLine(str);
            bool isLoggedIn = false;
            bool paused = false;
            string thePassword = Program.GetArg("-password");
            try
            {
                while (true)
                {
                    if (!stream.CanRead || !stream.CanWrite)
                    {
                        break;
                    }
                    {
                        if (paused || isLoggedIn)
                        {
                            string? str = reader.ReadLine();
                            if (str != null)
                            {
                                Debug.WriteLine(str);
                                if (str == thePassword)
                                    isLoggedIn = true;
                                paused = false;
                            }
                            else
                            {
                                Thread.Sleep(100);
                            }
                        }
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        data.Add("color", "RGBA(0.0,1.0,0.0,1.0)");
                        if (!isLoggedIn)
                        {
                            data.Add("message", "Enter Password.");
                            writer.WriteLine(JsonSerializer.Serialize<Dictionary<string, string>>(data));
                            paused = true;
                        }
                        else
                        {
                            data.Add("message", "kekw.");
                            writer.WriteLine(JsonSerializer.Serialize<Dictionary<string, string>>(data));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                stream.Close();
                client.Close();
                Debug.WriteLine("Socket closed.");
            }
        }

        public void Listen()
        {
            // while (true)
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
                            Thread clientThread = new Thread(() =>
                            {
                                Thread.Sleep(100);
                                ClientThread(client);
                            });
                            clientThread.IsBackground = true;
                            clientThread.Start();
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