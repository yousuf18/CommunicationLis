using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;

namespace Maglumi2000N
{
    public static class Maglumi2000C
    {
      
         private static StringBuilder sb = new StringBuilder();
        public const string MachineName = "Maglumi2000";
        private const int wait = 3000;
        public static SerialPort sp;
        private static EventWaitHandle wh;
        public static int flag = 0;
        public static void StartReceiver(string comPort, int baudRate = 9600)
        {
            Maglumi2000C.sp = new SerialPort(comPort);
            Maglumi2000C.sp.BaudRate = baudRate;
            Maglumi2000C.sp.Parity = Parity.None;
            Maglumi2000C.sp.Handshake = Handshake.None;
            Maglumi2000C.sp.DataBits = 8;
            Maglumi2000C.sp.StopBits = StopBits.One;
            Maglumi2000C.sp.DataReceived += new SerialDataReceivedEventHandler(Maglumi2000C.sp_DataReceived);
            Maglumi2000C.sp.Open();
        }
        private static void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Maglumi2000C.Eval(Maglumi2000C.sp.ReadExisting());
        }
        public static void Eval(string data)
        {
            Console.WriteLine(data);
            if(data==Constants.enq||data.StartsWith(Constants.enq))
            {
                Console.WriteLine("Found enq");
                Maglumi2000C.sb.Clear();
                Maglumi2000C.sp.Write(Constants.ack);
            }
            else if(data==Constants.ack)
            {
                Maglumi2000C.flag = 1;
            }
            else if(data==Constants.nack)
            {
                Console.WriteLine("NegeTive Acknowledge");
            }
            else if(data==Constants.stx||data.StartsWith(Constants.stx)||data.Contains(Constants.stx))
            {
                Maglumi2000C.sp.Write(Constants.ack);
                Console.WriteLine("Start Of text Found");
                Maglumi2000C.sb.Append(data);
               
              
            }
            else if(data==Constants.eot||data.StartsWith(Constants.eot)||data.EndsWith(Constants.eot))
            {
                Maglumi2000C.sb.Append(data);
               

                Console.WriteLine("End of Transmisssion");
               
                Console.WriteLine("All Data : "+sb.ToString());
                Console.WriteLine("Data length "+sb.Length);
                    try
                    {
                        
                        using (StreamWriter streamWriter = new StreamWriter(Constants.DumpPath + "Maglumi2000_" + DateTime.Now.Ticks.ToString() + ".txt"))
                            streamWriter.Write(Maglumi2000C.sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR (Write Error): " + ex.Message);
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                
            }
            else
            {
               
                Maglumi2000C.sb.Append(data);
            }
        }
    }
    
}
