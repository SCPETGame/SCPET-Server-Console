using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;

namespace SCPET_Server
{
    public class TcpConsoleClient
    {
        #region private members

        private TcpClient socketConnection;
        private Thread clientReceiveThread;

        #endregion

        public void ConnectToTcpServer()
        {
            try
            {
                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("On client connect exception " + e);
            }
        }

        /// <summary> 	
        /// Runs in background clientReceiveThread; Listens for incomming data. 	
        /// </summary>     
        private void ListenForData()
        {
            try
            {
                socketConnection = new TcpClient("localhost", Program.port);
                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    // Get a stream object for reading 				
                    using (NetworkStream stream = socketConnection.GetStream())
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
                            Color clr = Color.FromArgb(Convert.ToInt32(color[3]), Convert.ToInt32(color[0]), Convert.ToInt32(color[1]), Convert.ToInt32(color[2]));
                            ConsoleColor oldcol = Console.ForegroundColor;
                            Console.ForegroundColor = Program.FromHex(clr.Name);
                            Console.WriteLine(response["message"]);
                            Console.ForegroundColor = oldcol;
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    //retry
                    ConsoleColor oldcol = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Not connected to server yet retrying...");
                    Console.ForegroundColor = oldcol;
                    Thread.Sleep(1000);
                    ListenForData();
                }
                else
                {
                    Console.WriteLine("Socket exception: " + socketException);
                }
            }
        }

        /// <summary> 	
        /// Send message to server using socket connection. 	
        /// </summary> 	
        public void SendMessage(string message)
        {
            if (socketConnection == null)
            {
                return;
            }

            try
            {
                // Get a stream object for writing. 			
                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    // Convert string message to byte array.                 
                    byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                    // Write byte array to socketConnection stream.                 
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                }
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("Socket exception: " + socketException);
            }
        }
    }
}