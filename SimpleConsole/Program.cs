using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

namespace SCPET_Server
{
    class Program
    {
        public static int port = 0;
        public static string IP = "localhost";
        public static bool portfound = true;
        public static TcpConsoleClient console;
        private static Thread inputthread;

        static void Main(string[] args)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            IP = GetArg("-ip");
            if (string.IsNullOrEmpty(IP))
                IP = "127.0.0.1";
            string tempport = GetArg("-port");
            if (!string.IsNullOrEmpty(tempport) && int.TryParse(tempport, out port))
            {
                portfound = true;
            }
            else
            {
                //look for a port that is not in use
                port = new Random().Next(50000, 60000);
                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        portfound = false;
                        Console.WriteLine("console port was already in use");
                        break;
                    }
                }
            }

            if (portfound)
            {
                Console.WriteLine("Loading");
                List<string> cmdargs = new List<string>();
                string command = string.Empty;


                if (!Directory.Exists("logs"))
                {
                    Console.WriteLine("Logs folder not found, creating");
                    Directory.CreateDirectory("logs");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Platform windows");
                    if (string.IsNullOrEmpty(GetArg("-gamelocation")))
                    {
                        Console.WriteLine("Starting game server...");

                        string file = "SCP_ET.exe";
                        Console.WriteLine(file);
                        command = file;
                        cmdargs.Add("-consoleport");
                        cmdargs.Add(port.ToString());
                        if (GetArg("-gameoutput") != "true")
                        {
                            cmdargs.Add("-logfile");
                            cmdargs.Add("logs/SCP-ETServerLog-" + DateTime.UtcNow.Ticks + ".txt");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Starting game server...");

                        string file = GetArg("-gamelocation") + "/SCP_ET.exe";
                        Console.WriteLine(file);
                        command = file;
                        cmdargs.Add("-consoleport");
                        cmdargs.Add(port.ToString());
                        if (GetArg("-gameoutput") != "true")
                        {
                            cmdargs.Add("-logfile");
                            cmdargs.Add(GetArg("-gamelocation") + "/logs/SCP-ETServerLog-" + DateTime.UtcNow.Ticks + ".txt");
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Console.WriteLine("Platform linux");
                    if (string.IsNullOrEmpty(GetArg("-gamelocation")))
                    {
                        Console.WriteLine("Starting game server...");

                        string file = "scp_et.x86_64";
                        Console.WriteLine(file);
                        command = file;
                        cmdargs.Add("-consoleport");
                        cmdargs.Add(port.ToString());
                        if (GetArg("-gameoutput") != "true")
                        {
                            cmdargs.Add("-logfile");
                            cmdargs.Add("logs/SCP-ETServerLog-" + DateTime.UtcNow.Ticks + ".txt");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Starting game server...");

                        string file = GetArg("-gamelocation") + "/scp_et.x86_64";
                        Console.WriteLine(file);
                        command = file;
                        cmdargs.Add("-consoleport");
                        cmdargs.Add(port.ToString());
                        if (GetArg("-gameoutput") != "true")
                        {
                            cmdargs.Add("-logfile");
                            cmdargs.Add(GetArg("-gamelocation") + "/logs/SCP-ETServerLog-" + DateTime.UtcNow.Ticks + ".txt");
                        }
                    }
                }
                
                ProcessStartInfo info2 = new ProcessStartInfo(Path.GetFullPath(command), string.Join(' ', cmdargs));
                info2.RedirectStandardError = true;
                info2.RedirectStandardOutput = true;
                
                using (Process cmd = Process.Start(info2))
                {
                    console = new TcpConsoleClient();
                    console.ConnectToTcpServer();
                    AppDomain.CurrentDomain.DomainUnload += (s, e) => { cmd.Kill(); cmd.WaitForExit(); };
                    AppDomain.CurrentDomain.ProcessExit += (s, e) => { cmd.Kill(); cmd.WaitForExit(); };
                    AppDomain.CurrentDomain.UnhandledException += (s, e) => { cmd.Kill(); cmd.WaitForExit(); };
                    cmd.ErrorDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(args.Data))
                            return;
                        Console.WriteLine($"[GAME-ERR] {args.Data}");
                    };
                    cmd.OutputDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(args.Data))
                            return;
                        Console.WriteLine($"[GAME-OUT] {args.Data}");
                    };

                    cmd.Exited += (sender, arg) =>
                    {
                        Console.WriteLine("Game process has exited");
                        Thread.Sleep(1000);
                        Environment.Exit(1);
                    };

                    cmd.BeginErrorReadLine();
                    cmd.BeginOutputReadLine();
                    
                    listeninput(cmd);
                }
            }
        }

        public static void listeninput(Process gameprocess)
        {
            while (true)
            {
                string line = Console.ReadLine(); // Get string from user
                if (line == "exit" || line == "stop") // Check string
                {
                    Console.WriteLine("Shutting down, killing server...");
                    Console.WriteLine("stop");
                    Thread.Sleep(1000);
                    gameprocess.Kill();
                    gameprocess.WaitForExit();
                    Environment.Exit(0);
                }

                Console.WriteLine(">" + line);
                console.SendMessage(line);
            }
        }

        public static string GetArg(string argname)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == argname)
                {
                    if (args.Length > i + 1)
                    {
                        if (args[i + 1].StartsWith("-"))
                            return "true";
                        // else if (args[i + 1].EndsWith("\\"))
                        else
                            return args[i + 1];
                    }
                    else
                    {
                        return "true";
                    }
                }
            }

            return string.Empty;
        }

        public static ConsoleColor FromHex(string hex)
        {
            int argb = Int32.Parse(hex.Replace("#", ""), NumberStyles.HexNumber);
            Color c = Color.FromArgb(argb);

            int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0; // Bright bit
            index |= (c.R > 64) ? 4 : 0; // Red bit
            index |= (c.G > 64) ? 2 : 0; // Green bit
            index |= (c.B > 64) ? 1 : 0; // Blue bit

            return (System.ConsoleColor) index;
        }
    }
}