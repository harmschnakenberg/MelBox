using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

namespace MelBoxGsm
{
    /// <summary>
    /// Klasse bietet grundlegende Verbindung, Schreib- und Lesevorgänge über COM-Port.
    /// </summary>
    public class ReliableSerialPort : SerialPort
    {

        #region Connection
        public ReliableSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            #region COM-Port verifizieren
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            if (ports.Length == 0)
            {
                throw new Exception("Es sind keine COM-Ports vorhanden.");
            }

            if (!Array.Exists(ports, x => x == portName))
            {
                int pos = ports.Length - 1;
                portName = ports[pos];
            }
            #endregion

            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            Handshake = Handshake.None;
            DtrEnable = true;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
            WriteTimeout = 300;
            ReadTimeout = 500;

        }

        new public void Open()
        {
            int Try = 10;

            do
            {
                try
                {
                    base.Open();
                    ContinuousRead();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    Console.WriteLine(base.PortName + " verbleibende Verbindungsversuche: " + Try);
                    System.Threading.Thread.Sleep(2000);
                }
#pragma warning restore CA1031 // Do not catch general exception types

            } while (!base.IsOpen && --Try > 0);
        }

        #endregion

        #region Read
        public const string Terminator = "\r\nOK\r\n";

        private void ContinuousRead()
        {
            try
            {
                byte[] buffer = new byte[4096];
                Action kickoffRead = null;
                kickoffRead = (Action)(() => BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
                {
                    try
                    {
                        if (!IsOpen) return; //Wenn beim Lesen die Verbindung abbricht.

                        int count = BaseStream.EndRead(ar);
                        byte[] dst = new byte[count];
                        Buffer.BlockCopy(buffer, 0, dst, 0, count);

                        //Console.ForegroundColor = ConsoleColor.DarkGray;
                        //Console.WriteLine( System.Text.Encoding.UTF8.GetString(dst) );
                        //Console.ForegroundColor = ConsoleColor.Gray;

                        OnDataReceived(dst);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("ContinuousRead(): Lesefehler2 COM-Port:\r\n" + exception.GetType() + Environment.NewLine + exception.Message + Environment.NewLine + exception.InnerException + Environment.NewLine + exception.Source + Environment.NewLine + exception.StackTrace);
                        throw exception;
                    }

                    kickoffRead();
                }, null)); kickoffRead();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ContinuousRead(): Lesefehler1 COM-Port:\r\n" + exception.GetType() + Environment.NewLine + exception.Message + Environment.NewLine + exception.InnerException + Environment.NewLine + exception.Source + Environment.NewLine + exception.StackTrace);
                throw exception;
            }
        }

        public delegate void DataReceivedEventHandler(object sender, DataReceivedArgs e);
        new public event EventHandler<DataReceivedArgs> DataReceived;

        static string recLine = string.Empty;

        public virtual void OnDataReceived(byte[] data)
        {
            string rec = System.Text.Encoding.UTF8.GetString(data);
            recLine += rec;

            //Melde empfangne Daten, wenn...
            if (recLine.Contains(Terminator) || recLine.EndsWith("\r\n") || recLine.Contains("ERROR")) //
            {
                var handler = DataReceived;
                if (handler != null)
                {
                    handler(this, new DataReceivedArgs { Data = recLine });
                    recLine = string.Empty;
                }
            }
        }

        #endregion

        #region alternative Read Serial Port

        //private async void ContinuousReadAsync()
        //{
        //    byte[] buffer = await ReceiveData(); //Blockiert, bis Antwort kommt!
        //    OnDataReceived(buffer);
        //    ContinuousReadAsync();
        //}

        //public async Task<byte[]> ReceiveData()
        //{
        //    //Quelle: https://stackoverflow.com/questions/33226414/correct-implementation-of-async-serialport-read
        //    var buffer = new byte[4096];
        //    int readBytes = 0;
        //    //SerialPort port = new SerialPort(/* ... */);
        //    if (!base.IsOpen) return null;
        //    using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
        //    {
        //        while ((readBytes = await base.BaseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        //        {
        //            memoryStream.Write(buffer, 0, readBytes);
        //        }

        //        return memoryStream.ToArray();
        //    }
        //}
        #endregion


        #region Write

        private static readonly Queue<string> RequestQueue = new Queue<string>();

        static Timer sendTimer = null;

        public static void SetPortWriteIntervall(int millisceonds)
        {
            if (sendTimer != null)
            {
                sendTimer.Interval = millisceonds;
            }
        }

        private void WriteQueue(object sender, ElapsedEventArgs e)
        {
            base.WriteLine(RequestQueue.Dequeue());

            if (RequestQueue.Count == 0)
            {
                sendTimer.Enabled = false;
            }
        }

        new public void WriteLine(string message)
        {
            RequestQueue.Enqueue(message);

            if (sendTimer == null)
            {
                sendTimer = new Timer
                {
                    Interval = ReadTimeout + WriteTimeout
                };
                sendTimer.Elapsed += new ElapsedEventHandler(WriteQueue);
                sendTimer.AutoReset = true;
                sendTimer.Start();
            }

            sendTimer.Enabled = true;
        }

        #endregion
    }

    public class DataReceivedArgs : EventArgs
    {
        public string Data { get; set; }
    }
}
