using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace VirtualSensor
{
    class Program
    {
        private static bool _devTesting = true;
        private static byte[] _buffer = new byte[1024 * 10];
        private static int _portNumber = 9000;
        private static string _sensorName = "A";
        private static int _repeats = 1;
        private static List<Socket> _clientSocketList = new List<Socket>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static void Main(string[] args)
        {
            /*Random rando = new Random();
            int datapoints = rando.Next(20, 60);
            byte[] writeBuffer = new byte[1024 * 10];
            int length = 0;
            using (MemoryStream ms = new MemoryStream(writeBuffer, true))
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((byte)datapoints);
                    bw.Write((byte)_sensorName[0]);
                    bw.Write(DateTime.Now.ToBinary());
                    for (int j = 0; j < datapoints; j++)
                    {
                        bw.Write((byte)j);
                        bw.Write((byte)0);
                        bw.Write((Int32)(rando.NextDouble() * 1000000));
                        bw.Write((byte)1);
                        bw.Write((Int32)(rando.NextDouble() * 1000000));
                        bw.Write((byte)2);
                        bw.Write((Int32)(rando.NextDouble() * 1000000));
                    }
                    bw.Write((byte)0);
                    length = (int)ms.Position;
                }
            }
            byte[] data = new byte[length];
            Array.Copy(writeBuffer, data, length);*/
            Console.Title = "VirtualSensor";
            bool start = false;
            if (args.Length == 3)
            {
                try
                {
                    _sensorName = args[0];
                    _repeats = int.Parse(args[1]);
                    _portNumber = int.Parse(args[2]);
                }
                catch (Exception err)
                {

                }
                start = true;
                SetupServer();
            }
            else if (_devTesting == true)
            {
                start = true;
                SetupServer();
            }
            else
            {
                Console.WriteLine("Error, need 3 arguments, create shortcut and add arguments as follows:");
                Console.WriteLine("1. Name of sensor");
                Console.WriteLine("2. Amount of sensors in name (example Acc152_1 and Acc153_2)");
                Console.WriteLine("3. Port number");
            }
            if (start == true)
            {
                Console.WriteLine("Press any key to close all connections");
                Console.ReadLine();
                CloseAll();
            }
            Console.WriteLine("Press any key to Exit");
            Console.ReadLine();
        }

        private static void CloseAll()
        {
            foreach (var socket in _clientSocketList)
            {
                socket.Close();
            }
            _serverSocket.Close();
            Console.WriteLine("All sockets closed");
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _portNumber));
            _serverSocket.Listen(10);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            Console.Title = string.Format("Virtual Sensor for {0} on port {1}", _sensorName, _portNumber);
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine("Server started on: " + ip.ToString());
                }
            }
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                Socket socket = _serverSocket.EndAccept(AR);
                _clientSocketList.Add(socket);
                Console.WriteLine("Client connect " + socket.Handle.ToString());
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                Task.Run(() => SendMessages(socket));
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception err)
            {
                Console.WriteLine("Socket error:" + err.Message);
            }
        }

        private static void SendMessages(Socket socket)
        {
            while (true)
            {
                Thread.Sleep(5000);
                if (socket.Connected == false)
                {
                    break;
                }
                Console.WriteLine("Sending Message");
                try
                {
                    for (int i = 0; i < _repeats; i++)
                    {
                        Random rando = new Random();
                        int datapoints = rando.Next(20, 60);
                        byte[] writeBuffer = new byte[1024 * 10];
                        int length = 0;
                        using (MemoryStream ms = new MemoryStream(writeBuffer, true))
                        {
                            using (BinaryWriter bw = new BinaryWriter(ms))
                            {
                                bw.Write((byte)datapoints);
                                bw.Write((byte)_sensorName[0]);
                                bw.Write(DateTime.Now.ToBinary());
                                for (int j = 0; j < datapoints; j++)
                                {
                                    bw.Write((byte)j);
                                    bw.Write((byte)0);
                                    bw.Write((Int32)(rando.NextDouble() * 1000000));
                                    bw.Write((byte)1);
                                    bw.Write((Int32)(rando.NextDouble() * 1000000));
                                    bw.Write((byte)2);
                                    bw.Write((Int32)(rando.NextDouble() * 1000000));
                                }
                                bw.Write((byte)0);
                                length = (int)ms.Position;
                            }
                        }
                        byte[] data = new byte[length];
                        Array.Copy(writeBuffer, data, length);
                        socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Message sending failed: " + err.Message);
                }
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            try
            {
                int received = socket.EndReceive(AR);
                byte[] dataBuffer = new byte[received];
                Array.Copy(_buffer, dataBuffer, received);
                string text = Encoding.ASCII.GetString(dataBuffer);
                Console.WriteLine("RECEIVED: " + text);
                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (Exception err)
            {
                Console.WriteLine("ReceiveCallback error: " + err.Message);
                socket.Disconnect(true);
                socket.Close();
                _clientSocketList.Remove(socket);
            }

        }

        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
            Console.WriteLine("Message sent to " + socket.Handle.ToString());
        }
    }
}
