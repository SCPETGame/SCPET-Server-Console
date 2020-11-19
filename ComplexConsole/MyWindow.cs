using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Terminal.Gui;
using static Terminal.Gui.View;

namespace ComplexConsole
{
    public class MyWindow
    {
        public Window window;
        public ScrollView scroll;
        public TextField input;
        private Thread tcpThread;
        private TcpClient client;
        private string conn;
        private StreamReader reader;
        private StreamWriter writer;
        private NetworkStream stream;
        public static MyWindow singleton;

        public MyWindow(string conn)
        {
            singleton = this;
            this.conn = conn;
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
                new MenuBarItem("_Exit", "Exit", () => { Environment.Exit(0); }),
                new MenuBarItem("_Console", "Console related functions", ServerTab),
                new MenuBarItem("_Game", "Game Server related functions", GameTab),
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
                }
            };
            input = new TextField()
            {
                X = 0,
                Y = Pos.AnchorEnd() - 2,
                Width = Dim.Fill(),
                Height = 2
            };
            input.Leave += delegate(FocusEventArgs args)
            {
                SendMsg(input.Text.ToString());
            };
            window.Add(scroll, input);
            Application.Run();
        }

        public void MyThread()
        {
            string[] split = conn.Split(':');
            client = new TcpClient(split[split.Length - 2], int.TryParse(split[split.Length - 1], out int port) ? port : 8701);
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                try
                {
                    string serverMessage = reader.ReadLine();
                    Dictionary<string, string> response = JsonSerializer.Deserialize<Dictionary<string, string>>(serverMessage);
                    response["color"] = response["color"].Replace("RGBA(", "").Replace(")", "");
                    string[] color = response["color"].Split(',');
                    // System.Drawing.Color clr = System.Drawing.Color.FromArgb(Convert.ToInt32(color[3]), Convert.ToInt32(color[0]), Convert.ToInt32(color[1]), Convert.ToInt32(color[2]));
                    // ConsoleColor oldcol = Console.ForegroundColor;
                    // Console.ForegroundColor = Program.FromHex(clr.Name);
                    Log($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] " + response["message"]);
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
                Thread.Sleep(100);
            }
        }

        public void SendMsg(string message)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("color", "RGBA(0.0,1.0,0.0,1.0)");
            data.Add("message", message);
            Log(">" + message);
            writer.WriteLine(message);
            // writer.Close();
            // stream.Close();
        }

        public void Log(string message)
        {
            scroll.Add(new Label(message)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 2,
            });
            File.AppendAllText("Log", message);
        }

        public void ServerTab()
        {

        }

        public void GameTab()
        {

        }
    }
}