using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO.Ports;

namespace LegoTurbine
{
    class Program
    {
        static int lastRotorSpeed = 0;
        static int lastRoom1Light = 100;
        static int lastRoom2Light = 100;

        static SerialPort comPort;

        static void Main(string[] args)
        {
            string comPortName = "";
            WebServer ws = new WebServer(SendResponse, "http://localhost:2019/");
            ws.Run();
            Console.WriteLine("Windrad-Webinterface läuft auf Port 2019.");
            
            try
            {
                if (0 == args.Length)
                {
                    // use first available serial port
                    if (0 != SerialPort.GetPortNames().Length)
                    {
                        comPortName = SerialPort.GetPortNames()[0];
                    }
                    else
                    {
                        // BOOOM: we don't have any serial ports attached to the computer
                        Console.WriteLine("Es gibt keine seriellen Schnittstellen an diesem Computer. (exit)");
                        return;
                    }
                }
                else
                {
                    comPortName = args[0];
                }
                comPort = new SerialPort(comPortName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Whoops: " + ex.Message);
            }
            comPort.BaudRate = 9600;

            try
            {
                comPort.Open();
                Console.WriteLine("Serielle Kommunikation über " + comPort.PortName + ", " + comPort.BaudRate + " baud.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oopsie: " + ex.Message);
            }

            Console.WriteLine("beliebige Taste drücken zum Beenden\n");
            Console.ReadKey();
            ws.Stop();

            comPort.Close();
        }

        public static void SendSerial(List<char> command)
        {
            command.Add('\n');
            char[] cmd = command.ToArray<char>();
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }
                // TODO: blockiert hier
                comPort.Write(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Booom: " + ex.Message);
            }
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            // check for valid url
            Regex validPattern = new Regex(@"^/(rotor|room1|room2)/(\d{1,3})$", RegexOptions.IgnoreCase);
            string requestUrl = request.Url.PathAndQuery;

            if (validPattern.IsMatch(requestUrl))
            {
                Console.WriteLine("\nvalid request: " + request.HttpMethod.ToString() + " " + requestUrl);
                string[] components = validPattern.Split(requestUrl);

                int newSpeed = Convert.ToInt32(components[2]);
                List<char> command = new List<char>();
                // convert speed into two decimals
                List<char> speed = new List<char>();
                newSpeed--;
                if (0 >= newSpeed)
                {
                    speed = new List<char>{'0','0'};
                }
                else
                {
                    if (100 <= newSpeed)
                    {
                        speed = new List<char>{'9','9'};
                    }
                    else
                    {
                        speed = newSpeed.ToString("00").ToCharArray().ToList<char>();
                    }
                }
                
                switch (components[1])
                {
                    case "rotor":
                        if ((newSpeed + 1) != lastRotorSpeed) // why +1? 'cause we did newSpeed-- in line 106
                        {
                            lastRotorSpeed = newSpeed;
                            
                            command.Add('A');
                            command.AddRange(speed);
                            SendSerial(command);

                            Console.WriteLine("Setting " + components[1] + " to " + components[2] + "%.");
                        }
                        else
                        {
                            Console.WriteLine("Rotor speed has not changed.");
                        }
                        break;
                    case "room1":
                        if ((newSpeed + 1) != lastRoom1Light)
                        {
                            lastRoom1Light = newSpeed;

                            command.Add('B');
                            command.AddRange(speed);
                            SendSerial(command);

                            Console.WriteLine("Setting " + components[1] + " to " + components[2] + "%.");
                        }
                        else
                        {
                            Console.WriteLine("Room1 has not changed.");
                        }
                        break;
                    case "room2":
                        if ((newSpeed + 1) != lastRoom2Light)
                        {
                            lastRoom2Light = newSpeed;

                            command.Add('C');
                            command.AddRange(speed);
                            SendSerial(command);

                            Console.WriteLine("Setting " + components[1] + " to " + components[2] + "%.");
                        }
                        else
                        {
                            Console.WriteLine("Room2 has not changed.");
                        }
                        break;
                    default:
                        Console.WriteLine("Wow. Something has really gone bad (Program.cs:80)");
                        break;
                }
            }
            else
            {
                Console.WriteLine("\ninvalid request: " + requestUrl);
            }

            return string.Format("Affirmative.", DateTime.Now);
        }
    }
}
