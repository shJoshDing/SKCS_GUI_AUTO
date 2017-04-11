using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace rs232_dmm
{
    public class dmm34401a
    {
        public bool IsOpen = false;
        private SerialPort serialPort = new SerialPort( ); 

        public bool InitSerialPort(string portname)
        {
            IsOpen = serialPort.IsOpen;
            if (!IsOpen)
            {
                serialPort.PortName = portname;
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;

                serialPort.DtrEnable = true;                 
                //serialPort.WriteLine("TRIG:SOUR IMM"); 
                try
                {
                    serialPort.Open();
                    IsOpen = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }

                //serialPort.Open();
                serialPort.WriteLine("SYST:REM");
                serialPort.WriteLine("*CLS");  	
            }
            return true;
        }

        public double readVolt()
        {
            serialPort.WriteLine("MEAS:VOLT:DC? AUTO");  
            return double.Parse(serialPort.ReadLine());  
        }




        public double readAmp()
        {
            serialPort.WriteLine("MEAS:CURR:DC? AUTO");
            return double.Parse(serialPort.ReadLine());
        }

    }
}
