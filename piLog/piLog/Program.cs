/*
 * Hery A Mwenegoha (C) 2019
 * piLog - logs incoming ubx messages at 9600 bps
 * Configure UBX to output :
 * RAWX
 * SFRBX
 * MEASX
 *      and
 * PVT
 * SOL
 * STATUS
 * any-other-messages
 * 
 * first 3 messages are required for the creation of a Multi-GNSS Rinex 3 file.
 * Circle through comports on startup to determine whether we have any stuff connected
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

using System.ComponentModel;

namespace piLog
{
    class Program
    {
        // initialise 5 comPorts for 4 GNSS modules and 1 master moodule for other use
        static SerialPort[] Ports    = new SerialPort[5];
        bool[] _port_assigned        = new bool[] {false, false, false,false, false};
        static DateTime _to_serial   = DateTime.Now.AddMilliseconds(5000);
        static volatile object readlock     = new object();


        static string filepath0 = "";
        static string filepath1 = "";
        static string filepath2 = "";
        static string filepath3 = "";

        static BackgroundWorker bw = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };

        static void Main(string[] args)
        {
            // create a dateTimeObject and local ubx file
            string time_ = DateTime.Now.Date.Day.ToString() + "_" + DateTime.Now.Date.Month.ToString() + "_" + DateTime.Now.Date.Year.ToString() + "_pilog_" + DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString();
            string folder_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + System.IO.Path.DirectorySeparatorChar + "UBX";
            if (!Directory.Exists(folder_path))
            {
                Directory.CreateDirectory(folder_path);
            }
            filepath0 = folder_path + Path.DirectorySeparatorChar + "UBX_" + time_ + "R0" + ".txt";
            filepath1 = folder_path + Path.DirectorySeparatorChar + "UBX_" + time_ + "R1" + ".txt";
            filepath2 = folder_path + Path.DirectorySeparatorChar + "UBX_" + time_ + "R2" + ".txt";
            filepath3 = folder_path + Path.DirectorySeparatorChar + "UBX_" + time_ + "R3" + ".txt";
            if (!File.Exists(filepath0))
            {
                File.Create(filepath0).Close();
                Console.WriteLine(filepath0);
            }

            // check and assign serial port name
            checkNames();

            // start our background worker
            bw.DoWork += Bw_DoWork;
            if (bw.IsBusy == false)
            {
                bw.RunWorkerAsync();
            }
            
            while (true)
            {
               // do other nonblocking functions here
            }
            
        }

        private static void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // read open port
            while (e.Cancel == false)
            {
                if (bw == null)
                {
                    e.Cancel = true;
                    break;
                }

                if (bw.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // read stuff here
                   Serial1_Read(Ports[0], filepath0);
                   Serial1_Read(Ports[1], filepath1);
                }
            }
        }

        //static string assigned_names = new string();
        static List<string> assigned_names = new List<string>();
        private static void checkNames()
        {
            // check every 5 seconds for any new connected ports
            //if (DateTime.Now > _to_serial)
            {
                _to_serial =DateTime.Now.AddMilliseconds(5000);

                string[] PortNames = SerialPort.GetPortNames();
                string _text = "";
                int N = PortNames.Length;


                if (N > 1)
                {
                    foreach (string s in PortNames)
                    {
                        _text += s + "\n";
                    }
                }
                else
                {
                    _text = PortNames[0];
                }

                int len = Ports.Length;
                for(int i=0; i<len; i++)
                {
                    // check for an unsigned port
                    if(Ports[i] == null)
                    {
                        foreach (string s in PortNames)
                        {
                            if(assigned_names.Contains(s) == true)
                            {
                                // this name has already been assigned
                            }
                            else
                            {
                                // assign to list and port
                                if (String.IsNullOrEmpty(s) == false)
                                {
                                    assigned_names.Add(s);
                                    Ports[i] = new SerialPort();
                                    Ports[i].PortName = s;
                                    if (Ports[i].IsOpen == false)
                                    {
                                        Ports[i].Open();

                                        Console.WriteLine(Ports[i].PortName +"\t" +i+" OPEN");

                                        break;
                                    }
                                }
                            }
                        }


                    }
                }
            }
        }


        private static void Serial1_Read(SerialPort _sp, string _path)
        {
            if (_sp != null)
                _sp.ReadTimeout = 10000;
            else
            {
                //Console.WriteLine("error::Serial object is null, please assign global serial object..");
                return;
            }

            if (!_sp.IsOpen)
            {
                return;
            }

            lock (readlock)
            {
                //while (_sp.IsOpen)
                {
                    try
                    {

                        if (_sp.IsOpen && _sp.BytesToRead > 0)
                        {
                            byte _byte = (byte)_sp.ReadByte();

                            
                            using (var sm = new FileStream(_path, FileMode.Append))
                            {
                                sm.WriteByte(_byte);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
        }


    }
}
