using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Terminal.Gui;

namespace ComplexConsole
{
    public class MyWindow
    {
        public Window window;
        public ScrollView scroll;
        private Thread tcpThread;
        private TcpClient client;
        private string conn;
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
            scroll = new ScrollView();
            window.Add();
            Application.Run();
        }

        public void MyThread()
        {
            string[] split = conn.Split(':');
            client = new TcpClient(split[split.Length - 2], int.TryParse(split[split.Length - 1], out int port) ? port : 8701);
            NetworkStream stream = client.GetStream();
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                try
                {
                    int length;
                    // Read incomming stream into byte arrary.
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        // Convert byte array to string message.
                        string serverMessage = Encoding.ASCII.GetString(incommingData);
                        Dictionary<string, string> response = JsonSerializer.Deserialize<Dictionary<string, string>>(serverMessage);
                        response["color"] = response["color"].Replace("RGBA(", "").Replace(")", "");
                        string[] color = response["color"].Split(',');
                        // System.Drawing.Color clr = System.Drawing.Color.FromArgb(Convert.ToInt32(color[3]), Convert.ToInt32(color[0]), Convert.ToInt32(color[1]), Convert.ToInt32(color[2]));
                        // ConsoleColor oldcol = Console.ForegroundColor;
                        // Console.ForegroundColor = Program.FromHex(clr.Name);
                        Log($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] " + response["message"]);
                        // Console.ForegroundColor = oldcol;
                    }
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
            }
        }

        public void Log(string message)
        {
            scroll.Add(new Label(message));
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