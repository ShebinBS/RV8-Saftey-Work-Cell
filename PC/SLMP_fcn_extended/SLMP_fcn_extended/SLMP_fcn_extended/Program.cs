using System;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace SLMP_fcn_extended
{
    class Program
    {
        static TcpClient tcpC = new TcpClient(); // Global TcpClient object
        static void Main(string[] args)
        {
            byte[] byAdres = new byte[4];
            // set IP address of PLC
            byAdres[0] = 192; byAdres[1] = 168; byAdres[2] = 0; byAdres[3] = 250;
            IPAddress ipAdress = new IPAddress(byAdres);
            if (MakePingTest(ipAdress)) // check if you can find divice with set address
            {
                ConnectTCP(ipAdress, 2000); //connection for set IP address and Port No.
                if (tcpC.Connected) // check availability for SLMP connection
                {
                    if (SelfTest())// verify communication function
                    {
                        Console.WriteLine("All tests PASS");
                        Read_D200();
                    }
                    else
                    {
                        Console.WriteLine("Binary set FAIL");
                    }
                }
                else
                {
                    Console.WriteLine("No available SLMP connection");
                }
            }
            else
            {
                Console.WriteLine("Ping Test FAIL");
            }
            Console.ReadKey();
        }
        #region Function for perform Ping test with real PLC
        static bool MakePingTest(IPAddress IPAddressForTest)
        {
            bool pingAns = false;
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(IPAddressForTest);
            if (reply.Status == IPStatus.Success)
            {
                pingAns = true;
            }
            return pingAns;
        }
        #endregion

        #region Part of code for generci TcpClinet perform connection for more info please check C# documnetation
        static void ConnectTCP(IPAddress IPAddressToConnect, int portNumber)
        {
            tcpC.ReceiveTimeout = 5;
            tcpC.SendTimeout = 5;
            try
            {
                tcpC = Connect(IPAddressToConnect, portNumber, 1000);
            }
            catch
            {
                Console.WriteLine("Port Open FAIL");
            }
        }
        static TcpClient Connect(IPAddress hostName, int port, int timeout)
        {
            var client = new TcpClient();
            var state = new State { Client = client, Success = true };
            IAsyncResult ar = client.BeginConnect(hostName, port, EndConnect, state);
            state.Success = ar.AsyncWaitHandle.WaitOne(timeout, false);
            if (!state.Success || !client.Connected)
                throw new Exception("Failed to connect.");
            return client;
        }
        private class State
        {
            public TcpClient Client { get; set; }
            public bool Success { get; set; }
        }
        static void EndConnect(IAsyncResult ar)
        {
            var state = (State)ar.AsyncState;
            TcpClient client = state.Client;
            try
            {
                client.EndConnect(ar);
            }
            catch { }
            if (client.Connected && state.Success)
                return;
            client.Close();
        }
        #endregion

        #region Part of code used to verify whether the communication function operates normally or not
        static bool SelfTest()
        {
            bool loopTestAns = false;

            byte[] loopMessage = new byte[5] { 0x41, 0x42, 0x43, 0x44, 0x45 }; // 5 elements for test - "ABCDE"


            //Request data length
            int needByteMessage = 2 + 4 + 2 + loopMessage.Length;
            byte lowByte = (byte)(needByteMessage & 0xff);
            byte highByte = (byte)(needByteMessage >> 8 & 0xff);


            byte[] payload = new byte[] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, lowByte, highByte, 0x10, 0x00, 
                                          0x19, 0x06,0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };


            //number of loopack data
            lowByte = (byte)(loopMessage.Length & 0xff);
            highByte = (byte)(loopMessage.Length >> 8 & 0xff);
            payload[15] = lowByte; payload[16] = highByte;

            // loopack data 
            for (int i = 0; i < loopMessage.Length; i++)
            {
                payload[17 + i] = loopMessage[i];
            }

            NetworkStream stream = tcpC.GetStream();
            stream.Write(payload, 0, payload.Length);
            byte[] data = new Byte[20];
            stream.ReadTimeout = 1000;
            try
            {
                Int32 bytes = stream.Read(data, 0, data.Length);
                if (data[9] == 0 && data[10] == 0 && data[11] == lowByte && data[12] == highByte)
                {
                    loopTestAns = true;
                    for (int i = 0; i < loopMessage.Length; i++)
                    {
                        if (loopMessage[i] != data[13 + i])
                        {
                            loopTestAns = false;
                        }
                    }
                }
            }
            catch
            {
                loopTestAns = false;
            }
            return loopTestAns;
        }
        #endregion

        #region Part of code for read D200 register
        static void Read_D200()
        {
            byte[] payload = new byte[] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00, 0x00, 0xC8, 0x00, 0x00, 0xA8, 0x01, 0x00 };
            NetworkStream stream = tcpC.GetStream();
            stream.Write(payload, 0, payload.Length);
            byte[] data = new Byte[20];
            stream.ReadTimeout = 1000;
            try
            {
                Int32 bytes = stream.Read(data, 0, data.Length);
                if (data[9] == 0 && data[10] == 0)
                {
                    byte lowbyteResponse = data[11];
                    int hibyteResponse = data[12];
                    int afterConversion = (hibyteResponse << 8) + lowbyteResponse;
                    Console.WriteLine("Read D200 finished correct!");
                    Console.WriteLine("Readed value D200 (HEX): Hi byte[" + hibyteResponse.ToString("X") + "], Low byte [" + lowbyteResponse.ToString("X") + "]");
                    Console.WriteLine("Readed value D200 (DEC + Converted): " + afterConversion.ToString());
                }
                else
                {
                    Console.WriteLine("Error in Answer");
                }
            }
            catch
            {
                Console.WriteLine("Error in interpreter");
            }
        }
        #endregion
    }
}
