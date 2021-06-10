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
                        OnDataReceived(dst);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("ContinuousRead(): Lesefehler Bitstream von COM-Port:\r\n" + 
                            ">" + System.Text.Encoding.UTF8.GetString(buffer) + "<" + Environment.NewLine +
                            exception.GetType() + Environment.NewLine + 
                            exception.Message + Environment.NewLine + 
                            exception.InnerException + Environment.NewLine + 
                            exception.Source + Environment.NewLine + 
                            exception.StackTrace);
#if DEBUG
                        throw exception;
#endif
                    }

                    kickoffRead();
                }, null)); kickoffRead();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ContinuousRead(): Lesefehler bei Beginn. COM-Port:\r\n" +  
                    exception.GetType() + Environment.NewLine + 
                    exception.Message + Environment.NewLine + 
                    exception.InnerException + Environment.NewLine + 
                    exception.Source + Environment.NewLine + 
                    exception.StackTrace);
#if DEBUG
                throw exception;
#endif
            }
        }

        public delegate void DataReceivedEventHandler(object sender, DataReceivedArgs e);
        new public event EventHandler<DataReceivedArgs> DataReceived;

        static string recLine = string.Empty;

        /// <summary>
        /// Sammelt die vom Modem empfangenen Daten für die Weiterleitung
        /// </summary>
        /// <param name="data"></param>
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
            string request = RequestQueue.Dequeue();
            base.WriteLine(request);

            if ((Gsm.Debug & (int)Gsm.DebugCategory.GsmRequest) > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(request);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

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
                    Interval = ReadTimeout + WriteTimeout + 300 //min 100 ms nach Empfang von Modem
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
