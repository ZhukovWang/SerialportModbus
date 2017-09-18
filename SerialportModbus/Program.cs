using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;


namespace SerialportModbus
{
    class Program
    {
        private static string sendMessage = "010310000001";
        private static string mode = "ASCII"; //ASCII OR RTU

        static void Main(string[] args)
        {
            SerialPort _serialPort = new SerialPort(); ;

            if (sendMessage.Length % 2 == 1)
            {
                Console.WriteLine(System.DateTime.Now.ToString() + "." + System.DateTime.Now.Millisecond.ToString() + "-" + "Input Error!");
            }

            //init
            _serialPort.PortName = "COM1";
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.Even;
            _serialPort.DataBits = 7;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            //open
            _serialPort.Open();

            //deal data
            string sendData;
            List<Byte> dataBytes = new List<byte>();
            byte lrc = 0;
            UInt16 crc = 0xffff;
            string lrc_string, crc_string;

            for (int i = 0; i < sendMessage.Length - 1; i = i + 2)
            {
                dataBytes.Add(Convert.ToByte((sendMessage[i].ToString() + sendMessage[i+1].ToString()),16));
            }

            if (mode == "ASCII")
            {
                lrc = CalLrc(ref dataBytes);
                lrc_string = Convert.ToString(lrc, 16).ToUpper();
                sendData = ":" + sendMessage + lrc_string + "\r\n";
                Console.WriteLine(System.DateTime.Now.ToString() + "." + System.DateTime.Now.Millisecond.ToString() + "-" + sendData);
                _serialPort.Write(sendData);

                string recvData;

                try
                {
                    recvData = _serialPort.ReadLine();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                      System.DateTime.Now.Millisecond.ToString() + "-" + "Open failed\n");
                    return;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                      System.DateTime.Now.Millisecond.ToString() + "-" + "Timeout\n");
                    _serialPort.Close();
                    return;
                }
                Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                  System.DateTime.Now.Millisecond.ToString() + "-" + recvData);
            }
            else if (mode == "RTU")
            {
                crc = CalCrc(ref dataBytes);
                crc_string = Convert.ToString(crc, 16).ToUpper();
                sendData = sendMessage + crc_string;
                Console.WriteLine(System.DateTime.Now.ToString() + "." + System.DateTime.Now.Millisecond.ToString() + "-" + sendData);

                byte[] sendDataBytes = new byte[sendData.Length / 2];
                for (int i = 0; i < sendData.Length; i=i+2)
                {
                    sendDataBytes[i / 2] = (byte) Convert.ToByte(sendData.Substring(i, 2), 16);

                }
                _serialPort.Write(sendDataBytes,0,sendDataBytes.Length);
                Thread.Sleep(50);
                string recvData = null;
                byte[] recvDataBytes = new byte[200];
                int readByteCount = 0;
                try
                {
                    _serialPort.Read(recvDataBytes, 0, 200);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                      System.DateTime.Now.Millisecond.ToString() + "-" + "Open failed\n");
                    return;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                      System.DateTime.Now.Millisecond.ToString() + "-" + "Timeout\n");
                    _serialPort.Close();
                    return;
                }

                for (int i = 199; i > -1; i--)
                {
                    if (recvDataBytes[i] != 0)
                    {
                        readByteCount = i;
                        break;
                    }
                }
                for (int i = 0; i <= readByteCount ; i++)
                {
                    if (recvDataBytes[i] < 0x10)
                    {
                        recvData = recvData + "0" + Convert.ToString(recvDataBytes[i], 16).ToString();
                    }
                    else
                    {
                        recvData = recvData + Convert.ToString(recvDataBytes[i], 16).ToString();
                    }
                }
                Console.WriteLine(System.DateTime.Now.ToString() + "." +
                                  System.DateTime.Now.Millisecond.ToString() + "-" + recvData);

            }

            _serialPort.Close();

        }

        private static byte CalLrc(ref List<byte> dataBytes)
        {
            byte lrcRes = 0;

            foreach (var b in dataBytes)
            {
                lrcRes = (byte) (((byte) (lrcRes + b)) & 0xFF);
            }
            lrcRes = (byte) (((lrcRes ^ 0xff) + 1) & 0xff);
            return lrcRes;
        }

        private static ushort CalCrc(ref List<byte> dataBytes)
        {
            ushort crcRes = 0xffff;
            int length = dataBytes.Count;
            foreach (var b in dataBytes)
            {
                crcRes ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crcRes & 0x01) != 0)
                    {
                        crcRes = (ushort) ((crcRes >> 1) ^ 0xa001);
                    }
                    else
                    {
                        crcRes = (ushort) (crcRes >> 1);
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
