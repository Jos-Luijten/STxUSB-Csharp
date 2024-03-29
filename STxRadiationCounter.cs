/*
STxRadiationCounter version 1.0
The purpose of this script:
    Provide interfase functionality as a class with functions to communicate with a:
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
    - enforce thread serialization to prevent communication collisions, (The com port wil be used as a non-reentrant resource)
    - Able to send a command and wait for repley, wich needs to be returnd
    - enforce only known commands to be send

Noteworthy dependencies:
    - System.IO.Ports (dotnet add package System.IO.Ports --version 7.0.0)

*/



using System.IO.Ports;
using Microsoft.Win32;
using System.Text;



namespace STxUSB
{
    class STxRadiationCounter
    {
        // Define the VID and PID of the USB device you want to find  0403:6001
        public readonly int vid = 0x12AB;
        public readonly int pid = 0x0001;
        private readonly Mutex mutex = new Mutex(); //to ensure serialization on com port use
        public readonly string PortName;
        private SerialPort serialPort;


        
        public STxRadiationCounter()
        {
            PortName = "";
            // Determine which OS the program is running on
            string os = Environment.OSVersion.Platform.ToString();
            if (os.Contains("Win")) // Call the appropriate search and retrieve function based on the OS
            {
                PortName = GetComPortNameWindows();
            }
            else if (os.Contains("Linux"))
            {
                PortName = GetComPortNameLinux();
            }

            if(PortName == ""){
                throw new Exception("Portname corresponding to device not found");
            }
           
            serialPort = new SerialPort(PortName,115200,Parity.None,8,StopBits.One);
            serialPort.NewLine = "\r";   //(could be \r\n) is the ending the machine is expecting, posably used when using readtline, we will add this to be shure. but it is not needed.
            serialPort.Encoding = System.Text.Encoding.ASCII;
            serialPort.WriteTimeout = 1000; // 1 second, 
            serialPort.ReadTimeout = 1000; // 1 second,
            //SerialPort = serial.Serial(port = aPort.device, baudrate = 115200, bytesize = 8, parity = "N", stopbits = 1,timeout = None, xonxoff = False, rtscts = False, write_timeout = None, dsrdtr = False, inter_byte_timeout = None, exclusive = None)
        }


        
        private string GetComPortNameLinux()
        {
            string comPortName = "";

            // Find all ttyUSB devices in /dev
            string[] deviceNames = Directory.GetFiles("/dev/", "ttyUSB*");

            // Check each device to see if it matches the given VID and PID
            foreach (string deviceName in deviceNames)
            {
                try
                {
                    string deviceNameWithoutPath = Path.GetFileName(deviceName);
                    string devicePath = $"/sys/class/tty/{deviceNameWithoutPath}/device/driver";
                    string driver = Path.GetFileName(Directory.GetParent(devicePath).ToString());

                    if (driver == "usb")
                    {
                        string deviceID = File.ReadAllText($"/sys/class/tty/{deviceNameWithoutPath}/device/idVendor") + File.ReadAllText($"/sys/class/tty/{deviceNameWithoutPath}/device/idProduct");

                        if (deviceID.Trim() == $"{vid:X4}{pid:X4}")
                        {
                            comPortName = deviceName;
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore any errors that occur while reading the device files
                }
            }
            return comPortName;
        }



        private string GetComPortNameWindows()
        {
            string comPortName = "";

            // Get a list of all the available serial ports
            string[] portNames = SerialPort.GetPortNames();

            // Check each port to see if it matches the given VID and PID
            foreach (string portName in portNames)
            {
                try
                {
                    RegistryKey portKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{portName}");

                    foreach (string subKeyName in portKey.GetSubKeyNames())
                    {
                        RegistryKey subKey = portKey.OpenSubKey(subKeyName);
                        string deviceDesc = subKey.GetValue("DeviceDesc")?.ToString();

                        if (deviceDesc != null && deviceDesc.Contains($"VID_{vid:X4}&PID_{pid:X4}"))
                        {
                            comPortName = portName;
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore any errors that occur while reading the registry
                }
            }
            return comPortName;
        }



        private void OpenSerialPort()        // Methode voor het openen van de poort
        {
            serialPort.Open();
            //TODO: Catch exceptions
        }



        private void CloseSerialPort()                 // Methode voor het sluiten van de poort
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }



        private void SendString(string text)
        {
            SendData(Encoding.ASCII.GetBytes(text));
        }



        private void SendData(byte[] data)   // Method for sending byte data over serialport
        {
            mutex.WaitOne(); // wait until no other thread uses serialport.write
            try
            {   OpenSerialPort();
                serialPort.Write(data, 0, data.Length); 
                CloseSerialPort();
            }
            finally
            {
                mutex.ReleaseMutex(); //release hold
            }
        }



        private string ReadLine()
        {
            mutex.WaitOne(); // wait until no other thread uses serialport.write
            try
            {
                OpenSerialPort();
                string data = serialPort.ReadLine();
                CloseSerialPort();
                return data;
            }
            finally
            {
                mutex.ReleaseMutex(); //release hold
            }
        }



        private byte[] ReceiveData(int bytesToRead)  // Methode voor het ontvangen van data
        {
            mutex.WaitOne(); // wait until no other thread uses serialport.write
            try
            {
                byte[] data = new byte[bytesToRead];
                OpenSerialPort();
                serialPort.Read(data, 0, bytesToRead);
                CloseSerialPort();
                return data;
            }
            finally
            {
                mutex.ReleaseMutex(); //release hold
            }
        }


        // combine sendstring and readline functions, to enforce waiting on return value. 
        // note from documentation: commands should not be stacked or sent without waiting for the appropriate response. otherwise the system will experience communication collisions. 
        private string send_command(string command)
        {
            SendString(">"+command+"\r");
            string result;
            try{
                result = ReadLine();
            }catch(Exception e){ //most likeley timeout error, timout amount is set in initialisation of this stx object..
                result = "ERROR on "+command+": "+e.Message;
            }
            return result;
        }



        public string reset_device_00()
        {
            return send_command("00");
        }


        public string start_counter_01()
        {
            return send_command("01");
        }

        public string stop_counter_02()
        {
            return send_command("02");
        }

        public string request_status_03()
        {
            return send_command("03");
        }

        public string request_counts_04()
        {
            return send_command("04");
        }

        public string request_parameters_05()
        {
            return send_command("05");
        }

        public string request_system_parameters_06()
        {
            return send_command("06");
        }

        public string store_current_parameters_07()
        {
            return send_command("07");
        }

        public string start_demo_counter_08()
        {
            return send_command("08");
        }

        public string high_voltage_on_12()
        {
            return send_command("12");
        }

        public string high_voltage_off_13()
        {
            return send_command("13");
        }

        public string high_voltage_onewire_on_14()
        {
            return send_command("14");
        }

        public string high_voltage_onewire_off_15()
        {
            return send_command("15");
        }

        public string request_high_voltage_status_16()
        {
            return send_command("16");
        }

        public string read_high_voltage_data_17()
        {
            return send_command("17");
        }

    }
}
