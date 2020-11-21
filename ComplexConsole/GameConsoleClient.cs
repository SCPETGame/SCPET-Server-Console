using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ComplexConsole
{
    public class GameConsoleClient
    {
        private readonly object clientLock = new object();
        string thePassword = Program.GetArg("-password");
        public TcpClient client;
        private EndPoint endPoint;

        NetworkStream stream;
        BinaryReader reader;
        BinaryWriter writer;
        Thread read;
        Thread write;
        bool isLoggedIn = false;
        
        public void ReadThread()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(250);
                    if (!client.Connected || !client.GetStream().CanRead || !client.GetStream().CanWrite)
                    {
                        break;
                    }
                    string? str = reader.ReadString();
                    Console.WriteLine("readline.");
                    lock (clientLock)
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            Dictionary<string, string> response = JsonSerializer.Deserialize<Dictionary<string, string>>(str);
                            response["color"] = response["color"].Replace("RGBA(", "").Replace(")", "");
                            string[] color = response["color"].Split(',');
                            Console.WriteLine($">>{response["message"]}<<");
                            if (!isLoggedIn)
                            {
                                if (response["message"].Equals(thePassword))
                                {
                                    isLoggedIn = true;
                                    Console.WriteLine("Remote logged in");
                                }
                                else
                                {
                                    Write("");
                                    Console.WriteLine("Remote password failure");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // its not that important... right?
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("ReadThread End:");
            }
        }

        public void WriteThread()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(250);
                    lock (clientLock)
                    {
                        if (!client.Connected || !client.GetStream().CanRead || !client.GetStream().CanWrite)
                        {
                            break;
                        }
                        Write("kekw.");
                    }
                }
            }
            catch (Exception e)
            {
                // its not that important... right?
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("WriteThread End:");
            }
        }

        public void LoopThread()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(250);
                    lock (clientLock)
                    {
                        if (!client.Connected || !client.GetStream().CanRead || !client.GetStream().CanWrite)
                        {
                            break;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                // its not that important... right?
                Console.WriteLine(e.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine($"Socket {endPoint} closed.");
                client.Close();
                // read.Interrupt();
                // write.Interrupt();
            }
        }

        public void Write(string message)
        {
            lock (clientLock)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("color", "RGBA(0.0,1.0,0.0,1.0)");
                if (isLoggedIn)
                {
                    data.Add("message", message);
                    writer.Write(JsonSerializer.Serialize<Dictionary<string, string>>(data));
                }
                else
                {
                    data.Add("message", "Enter Password.");
                    writer.Write(JsonSerializer.Serialize<Dictionary<string, string>>(data));
                }
                writer.Flush();
            }
        }

        public void RunThreads()
        {
            Console.WriteLine(thePassword);
            endPoint = client.Client.RemoteEndPoint;
            stream = client.GetStream();
            reader = new BinaryReader(client.GetStream(), Encoding.Unicode);
            writer = new BinaryWriter(client.GetStream(), Encoding.Unicode);
            read = new Thread(ReadThread)
            {
                Name = $"Read{endPoint}"
            };
            write = new Thread(WriteThread)
            {
                Name = $"Write{endPoint}"
            };
            Thread loop = new Thread(LoopThread)
            {
                Name = $"Loop{endPoint}"
            };
            read.Start();
            write.Start();
            loop.Start();
        }
    }
}