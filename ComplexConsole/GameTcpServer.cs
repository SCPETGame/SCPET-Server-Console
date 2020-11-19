using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            // string str = reader.ReadToEnd();
            // Debug.WriteLine(str);
            bool isLoggedIn = false;
            bool paused = false;
            string thePassword = Program.GetArg("-password");
            while (true)
            {
                if (!stream.CanRead || !stream.CanWrite)
                {
                    break;
                }
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    if (paused || isLoggedIn)
                    {
                        string? str = reader.ReadLine();
                        if (str != null)
                        {
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
            stream.Close();
            client.Close();
        }

        public void Listen()
        {
            while (true)
            {
                server = TcpListener.Create(port);
                server.Start();
                try
                {
                    while (true)
                    {
                        TcpClient client = server.AcceptTcpClient();
                        Thread clientThread = new Thread(() =>
                        {
                            ClientThread(client);
                        });
                        clientThread.IsBackground = true;
                        clientThread.Start();
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