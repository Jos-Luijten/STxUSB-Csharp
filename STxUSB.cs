/*
STxUSB version 1.0
The purpose of this script:
    A console application to communicate and operate a:
    Spectrum Techniques radiation counter (STx) over a USB com port.
    As described by 'STX_Programming_Guide.pdf' the 'STX Design and Programming Manual'

This script was, created for, and tested on:
    Devise:         ST365 radiation counter
    Manufacturer:   Spectrum techniques

This script was created by:
    Jos Luijten (j.luijten87@gmail.com)
    A version of this script was written in C# and in Python
    C# in Git-repository:       https://github.com/Jos-Luijten/STxUSB-Csharp
    Python in Git-repository:   https://github.com/Jos-Luijten/STxUSB-Python

This scripts goals to achieve:
    - autodetection and confirmation of device on port, on Windows and Linux Debian
    - application works from command line, so that it can be called and used by other programs and users
    - application contains "STxRadiationCounter" class with public functions so that the code can implemented in other projects

Noteworthy dependencies:
    - System.IO.Ports (dotnet add package System.IO.Ports --version 7.0.0)

*/



using System.IO.Ports; //unnessisary, indicating this is used by stxradiationcounter class (not in this file)
using Microsoft.Win32; //unnessisary, indicating this is used by stxradiationcounter class (not in this file)
using System.Text;     //unnessisary, indicating this is used by stxradiationcounter class (not in this file)



namespace STxUSB
{
    class Program
    {
        static bool verbose = false;                                                   //used to deside wich information is printed out into the console
        static List<string> commands = new List<string>();                             //List of known commands to execute in given order
        static List<string> unknown_arguments = new List<string>();                    //List of not recognised arguments that ware given in order to give feedback when verbose is set to true
        static private STxRadiationCounter device;

        static void Main(string[] args)
        {
            if (args.Length == 0)           // Check for arguments
            {
                ShowHelp();                 // No arguments, show help and information
                return;
            }
            else
            {
                foreach (string argument in args)   // prosess arguments
                {
                    switch (argument)
                    {
                        case "-verbose": case "-v":
                            verbose = true;
                            break;
                        case "-help": case "/help": case "help": case "-info": case "-h": case "/h": case "-?": case "/?":
                            ShowHelp();
                            return;
                        case "-00": case "-reset":
                            commands.Add("-00");
                            break;
                        case "-01": case "-start":
                            commands.Add("-01");
                            break;
                        case "-02": case "-stop":
                            commands.Add("-02");
                            break;
                        case "-03": case "-status":
                            commands.Add("-03");
                            break;
                        case "-04": case "-counts":
                            commands.Add("-04");
                            break;
                        case "-05": case "-param":
                            commands.Add("-05");
                            break;
                        case "-06": case "-system":
                            commands.Add("-06");
                            break;
                        case "-07": case "-store":
                            commands.Add("-07");
                            break;
                        case "-08": case "-demo":
                            commands.Add("-08");
                            break;
                        case "-12": case "-hvon":
                            commands.Add("-12");
                            break;
                        case "-13": case "-hvoff":
                            commands.Add("-13");
                            break;
                        case "-14":
                            commands.Add("-14");
                            break;
                        case "-15":
                            commands.Add("-15");
                            break;
                        case "-16": case "-hvstatus":
                            commands.Add("-16");
                            break;
                        case "-17": case "-hvdata":
                            commands.Add("-17");
                            break;
                        case "-e": case "-exit":
                            commands.Add("exit");
                            break;
                        default:
                            unknown_arguments.Add(argument);
                            break;
                    }
                }
            }

            if (verbose &&  unknown_arguments.Count > 0) {
                Console.WriteLine("Unknown commands given:");
                foreach (string argument in unknown_arguments)
                {
                    Console.WriteLine($"  {argument}");
                }
            }


            if (args.Length > 0){commands.Add("exit");}


            if (verbose){Console.WriteLine("Start initializing com port connection with device");}

            try {
                device = new STxRadiationCounter();
            }
            catch (Exception e) {
                if (verbose){Console.WriteLine($"Error on initiating connection: {e.Message}");}
                Console.WriteLine("Failed USB Connection and/or initialisation of STxRadiationcounter class");
                return;
            }
            
            if (verbose){Console.WriteLine($"COM-port found, name: {device.PortName}");}


            Console.WriteLine();
            foreach (string command in commands)
            {
                string result = "";
                if (command == "exit"){return;}
                if (command == "-00"){result = device.reset_device_00();}
                if (command == "-01"){result = device.start_counter_01();}
                if (command == "-02"){result = device.stop_counter_02();}
                if (command == "-03"){result = device.request_status_03();}
                if (command == "-04"){result = device.request_counts_04();}
                if (command == "-05"){result = device.request_parameters_05();}
                if (command == "-06"){result = device.request_system_parameters_06();}
                if (command == "-07"){result = device.store_current_parameters_07();}
                if (command == "-08"){result = device.start_demo_counter_08();}
                if (command == "-12"){result = device.high_voltage_on_12();}
                if (command == "-13"){result = device.high_voltage_off_13();}
                if (command == "-14"){result = device.high_voltage_onewire_on_14();}
                if (command == "-15"){result = device.high_voltage_onewire_off_15();}
                if (command == "-16"){result = device.request_high_voltage_status_16();}
                if (command == "-17"){result = device.read_high_voltage_data_17();}

                Console.WriteLine($"{command}: {result}");
                Console.WriteLine();
            }

        }


        static void ShowHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("STx USB console aplication");
            Console.WriteLine("==============================================================");
            Console.WriteLine("");
            Console.WriteLine("USAGE:>STxUSB -option1 -option2 -option3 .....");
            Console.WriteLine("");
            Console.WriteLine("Where options are:");
            Console.WriteLine("     -? or -h or -help       Display this help message");
            Console.WriteLine("     -00 or -reset           Reset device");
            Console.WriteLine("     -01 or -start           Start counter");
            Console.WriteLine("     -02 or -stop            Stop counter");
            Console.WriteLine("     -03 or -status          Request Status");
            Console.WriteLine("     -04 or -counts          Request Counts");
            Console.WriteLine("     -05 or -param           Request parameters");
            Console.WriteLine("     -06 or -system          Request system parameters");
            Console.WriteLine("     -07 or -store           Store current parameters to eeprom");
            Console.WriteLine("     -08 or -demo            Start demo counter");
            Console.WriteLine("     -12 or -hvon            High voltage on command");
            Console.WriteLine("     -13 or -hvoff           High voltage off command");
            Console.WriteLine("     -14                     High voltage one-wire on command");
            Console.WriteLine("     -15                     High voltage one-wire off command");
            Console.WriteLine("     -16 or -hvstatus        High voltage status request");
            Console.WriteLine("     -17 or -hvdata          Read high voltage data");
            Console.WriteLine("     -e or -exit             exit / terminate this program");
            Console.WriteLine("     -v or -verbose          turn on verbose, show more output,");
            Console.WriteLine("");
            Console.WriteLine("If options are given, the program will execute those and terminate / exit");
            Console.WriteLine("The default behaviour (without options) is:");
            Console.WriteLine("     the program will ask and wait for an command (input by user)");
            Console.WriteLine("     after a response, the program will ask for the next command ");
            Console.WriteLine("");
            Console.WriteLine("Returns:");
            Console.WriteLine("     On possible return values and interpretation read:");
            Console.WriteLine("     'STX_Programming_Guid.pdf'");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("     -01                   start counter");
            Console.WriteLine("     -04 -v                Show detailed information and ask for status");
        }


        

    }
}
