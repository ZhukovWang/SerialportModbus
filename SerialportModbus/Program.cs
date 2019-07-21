using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace SerialportModbus
{
    internal class Program
    {
        private static string sendMessage = "010310000001";
        private static string mode = "ASCII"; //ASCII OR RTU

        private static void Main(string[] args)
        {
            SerialPort serialPort = new SerialPort();

            if (sendMessage.Length % 2 == 1)
            {
                Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + "Input Error!");
            }

            //init
            serialPort.PortName = "COM1";
            serialPort.BaudRate = 9600;
            serialPort.Parity = Parity.Even;
            serialPort.DataBits = 7;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            //open
            serialPort.Open();

            //deal data
            string sendData;
            List<Byte> dataBytes = new List<byte>();

            for (int i = 0; i < sendMessage.Length - 1; i += 2)
            {
                dataBytes.Add(Convert.ToByte((sendMessage[i].ToString() + sendMessage[i + 1].ToString()), 16));
            }

            if (mode == "ASCII")
            {
                byte lrc = CalLrc(ref dataBytes);
                string lrcString = Convert.ToString(lrc, 16).ToUpper();
                sendData = ":" + sendMessage + lrcString + "\r\n";
                Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + sendData);
                serialPort.Write(sendData);

                string receiveData;

                try
                {
                    receiveData = serialPort.ReadLine();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + "Open failed\n");
                    return;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + "Timeout\n");
                    serialPort.Close();
                    return;
                }
                Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + receiveData);
            }
            else if (mode == "RTU")
            {
                ushort crc = CalCrc(ref dataBytes);
                string crcString = Convert.ToString(crc, 16).ToUpper();
                sendData = sendMessage + crcString;
                Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + sendData);

                byte[] sendDataBytes = new byte[sendData.Length / 2];
                for (int i = 0; i < sendData.Length; i = i + 2)
                {
                    sendDataBytes[i / 2] = Convert.ToByte(sendData.Substring(i, 2), 16);
                }
                serialPort.Write(sendDataBytes, 0, sendDataBytes.Length);
                Thread.Sleep(50);
                string receiveData = null;
                byte[] receiveDataBytes = new byte[200];
                int readByteCount;
                try
                {
                    readByteCount = serialPort.Read(receiveDataBytes, 0, 200);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + "Open failed\n");
                    return;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + "Timeout\n");
                    serialPort.Close();
                    return;
                }

                //for (int i = 199; i > -1; i--)
                //{
                //    if (receiveDataBytes[i] != 0)
                //    {
                //        readByteCount = i;
                //        break;
                //    }
                //}
                for (int i = 0; i <= readByteCount; i++)
                {
                    if (receiveDataBytes[i] < 0x10)
                    {
                        receiveData += "0" + Convert.ToString(receiveDataBytes[i], 16);
                    }
                    else
                    {
                        receiveData += Convert.ToString(receiveDataBytes[i], 16);
                    }
                }
                Console.WriteLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString() + "-" + receiveData);
            }

            serialPort.Close();
        }

        private static byte CalLrc(ref List<byte> dataBytes)
        {
            byte lrcRes = 0;

            foreach (var b in dataBytes)
            {
                lrcRes = (byte)(((byte)(lrcRes + b)) & 0xFF);
            }
            lrcRes = (byte)(((lrcRes ^ 0xff) + 1) & 0xff);
            return lrcRes;
        }

        private static ushort CalCrc(ref List<byte> dataBytes)
        {
            ushort crcRes = 0xffff;
            foreach (var b in dataBytes)
            {
                crcRes ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crcRes & 0x01) != 0)
                    {
                        crcRes = (ushort)((crcRes >> 1) ^ 0xa001);
                    }
                    else
                    {
                        crcRes = (ushort)(crcRes >> 1);
                    }
                }
            }
            ReverseWord(ref crcRes);
            return crcRes;
        }

        private static void ReverseWord(ref ushort data)
        {
            byte[] p = BitConverter.GetBytes(data);
            byte t;

            t = p[0];
            p[0] = p[1];
            p[1] = t;

            data = BitConverter.ToUInt16(p, 0);
        }
    }
}