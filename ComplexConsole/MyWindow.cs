using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Terminal.Gui;
using static Terminal.Gui.View;

namespace ComplexConsole
{
    public partial class MyWindow
    {
        public const int maxChars = 128;
        private readonly object clientLock = new object();
        public Window window;
        public ScrollView scroll;
        public Label label;
        public TextField input;
        private Thread tcpThread;
        private TcpClient client;
        private string conn;
        private BinaryReader reader;
        private BinaryWriter writer;
        private NetworkStream stream;
        public static MyWindow singleton;

        public MyWindow(string conn)
        {
            singleton = this;
            // File.CreateText("Log");
            this.conn = conn;
            string[] split = conn.Split(':');
            client = new TcpClient();
            client.Connect(split[split.Length - 2], int.TryParse(split[split.Length - 1], out int port) ? port : 8701);
            stream = client.GetStream();
            reader = new BinaryReader(stream, Encoding.Unicode);
            writer = new BinaryWriter(stream, Encoding.Unicode);

            tcpThread = new Thread(new ThreadStart(MyThread));
            tcpThread.IsBackground = true;
            tcpThread.Start();
            // Application.UseSystemConsole = true; // we don't need mouse support
            Application.Init();
            Toplevel top = Application.Top;
            window = new Window("Server Console")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            top.Add(window);
            MenuBar bar = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("_View", new MenuItem[]
                {
                    new MenuItem("_Console", "Text-based Console", ConsoleTab),
                    new MenuItem("_Exit", "Exit the console", () => { Environment.Exit(0); })
                })
            });
            top.Add(bar);
            scroll = new ScrollView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 2,
                ColorScheme = new ColorScheme()
                {
                    Normal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black)
                },
                ContentSize = new Size(0, 0),
                AutoHideScrollBars = false,
                ShowHorizontalScrollIndicator = true,
                ShowVerticalScrollIndicator = true,
            };
            label = new Label()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            scroll.Add(label);
            /*scroll.DrawContent += (r) =>
            {
                Size s = filler.Frame.Size;
                Rect rect = filler.Bounds;
                rect.Width = s.Width;
                rect.Height = s.Height;
                filler.Bounds = rect;
                scroll.ContentSize = s;
            };*/
            input = new TextField()
            {
                X = 0,
                Y = Pos.AnchorEnd() - 2,
                Width = Dim.Fill(),
                Height = 2
            };
            input.KeyUp += (k) =>
            {
                if (k.KeyEvent.Key == Key.Enter)
                {
                    SendMsg(input.Text.ToString());
                    input.Text = string.Empty;
                }
            };
            window.Add(scroll, input);
            Application.Run();
            client.Close();
        }

        public void MyThread()
        {
            try
            {
                // stream = client.GetStream();
                while (true)
                {
                    Thread.Sleep(250);
                    if (!client.Connected)
                        break;
                    {
                        string serverMessage = reader.ReadString();
                        lock (clientLock)
                        {
                            if (!client.Connected || string.IsNullOrEmpty(serverMessage))
                                continue;
                            // Log(serverMessage);
                            Dictionary<string, string> response = JsonSerializer.Deserialize<Dictionary<string, string>>(serverMessage);
                            response["color"] = response["color"].Replace("RGBA(", "").Replace(")", "");
                            string[] color = response["color"].Split(',');
                            // System.Drawing.Color clr = System.Drawing.Color.FromArgb(Convert.ToInt32(color[3]), Convert.ToInt32(color[0]), Convert.ToInt32(color[1]), Convert.ToInt32(color[2]));
                            // ConsoleColor oldcol = Console.ForegroundColor;
                            // Console.ForegroundColor = Program.FromHex(clr.Name);
                            Log($"[{DateTime.Now.ToString("HH:mm:ss")}] {response["message"]}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
            finally
            {
                Log("Disconnected from remote server.");
            }
        }

        public void SendMsg(string message)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("color", "RGBA(0.0,1.0,0.0,1.0)");
            data.Add("message", message);
            try
            {
                if (client.Connected)
                {
                    lock (clientLock)
                    {
                        writer.Write(JsonSerializer.Serialize(data));
                        writer.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Log($"Error sending message: {e.ToString()}");
            }
            Log(">" + message);
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (label.Text.Length >= maxChars)
            {
                int idx = label.Text.Length % maxChars;
                label.Text = label.Text.ToString().Substring(idx, maxChars - idx);
            }
            label.Bounds = TextFormatter.CalcRect(0, 0, $"{label.Text}\n{message}");
            scroll.ContentSize = TextFormatter.CalcRect(0, 0, $"{label.Text}\n{message}").Size;
            scroll.ContentOffset = new Point(0, scroll.ContentSize.Height - (TextFormatter.CalcRect(0, 0, message).Height * 2 + 5));
            label.Text = $"{label.Text}\n{message}";
            File.AppendAllText("Log", $"\n{message}");
        }

        public void ConsoleTab()
        {

        }

        public void GameTab()
        {

        }
    }
}