using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using ADI.DMY2;
using rs232_dmm;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Management;

namespace CurrentSensorV3
{
    public partial class CurrentSensorConsole : Form
    {
        public CurrentSensorConsole()
        {
            InitializeComponent();
            UserInit();
        }

        public struct ModuleAttribute
        { 
            public double dIQ;
            public double dVoutIPNative;
            public double dVout0ANative;
            public double dVoutIPMiddle;
            public double dVout0AMiddle;
            public double dVoutIPTrimmed;
            public double dVout0ATrimmed;
            public uint iErrorCode;
            public bool bDigitalCommFail;
            public bool bNormalModeFail;
            public bool bReadMarginal;
            public bool bReadSafety;
            public bool bTrimmed;
        }

        public struct SL620Attribute
        {
            public double Iq;
            public double Vip;
            public double Offset;
            public double Gain;
            public double Sensity;
            public int CoarseGainCode;
            public int FineGainCode;
            public int CoarseOffsetCode;
            public int FineOffsetCode;
            public double VipTrimmed;
            public double OffsetTrimmed;
            public int Bin;
            public double ElapsedTime;
            public bool DigReadFail;
            public bool DigCommFail;
            public bool OutputFail;
            public bool Mre;
            public bool LowSensity;
            public bool OffsetFail;
            public bool Short;
        }

        /// <summary>
        /// 枚举win32 api
        /// </summary>
        public enum HardwareEnum
        {
            // 硬件
            Win32_Processor, // CPU 处理器
            Win32_PhysicalMemory, // 物理内存条
            Win32_Keyboard, // 键盘
            Win32_PointingDevice, // 点输入设备，包括鼠标。
            Win32_FloppyDrive, // 软盘驱动器
            Win32_DiskDrive, // 硬盘驱动器
            Win32_CDROMDrive, // 光盘驱动器
            Win32_BaseBoard, // 主板
            Win32_BIOS, // BIOS 芯片
            Win32_ParallelPort, // 并口
            Win32_SerialPort, // 串口
            Win32_SerialPortConfiguration, // 串口配置
            Win32_SoundDevice, // 多媒体设置，一般指声卡。
            Win32_SystemSlot, // 主板插槽 (ISA & PCI & AGP)
            Win32_USBController, // USB 控制器
            Win32_NetworkAdapter, // 网络适配器
            Win32_NetworkAdapterConfiguration, // 网络适配器设置
            Win32_Printer, // 打印机
            Win32_PrinterConfiguration, // 打印机设置
            Win32_PrintJob, // 打印机任务
            Win32_TCPIPPrinterPort, // 打印机端口
            Win32_POTSModem, // MODEM
            Win32_POTSModemToSerialPort, // MODEM 端口
            Win32_DesktopMonitor, // 显示器
            Win32_DisplayConfiguration, // 显卡
            Win32_DisplayControllerConfiguration, // 显卡设置
            Win32_VideoController, // 显卡细节。
            Win32_VideoSettings, // 显卡支持的显示模式。

            // 操作系统
            Win32_TimeZone, // 时区
            Win32_SystemDriver, // 驱动程序
            Win32_DiskPartition, // 磁盘分区
            Win32_LogicalDisk, // 逻辑磁盘
            Win32_LogicalDiskToPartition, // 逻辑磁盘所在分区及始末位置。
            Win32_LogicalMemoryConfiguration, // 逻辑内存配置
            Win32_PageFile, // 系统页文件信息
            Win32_PageFileSetting, // 页文件设置
            Win32_BootConfiguration, // 系统启动配置
            Win32_ComputerSystem, // 计算机信息简要
            Win32_OperatingSystem, // 操作系统信息
            Win32_StartupCommand, // 系统自动启动程序
            Win32_Service, // 系统安装的服务
            Win32_Group, // 系统管理组
            Win32_GroupUser, // 系统组帐号
            Win32_UserAccount, // 用户帐号
            Win32_Process, // 系统进程
            Win32_Thread, // 系统线程
            Win32_Share, // 共享
            Win32_NetworkClient, // 已安装的网络客户端
            Win32_NetworkProtocol, // 已安装的网络协议
            Win32_PnPEntity,//all device
        }

        #region Param Definition

        bool bUsbConnected = false;
        bool bAutoTrimTest = true;          //Debug mode, display engineer tab
        //bool bAutoTrimTest = false;       //Release mode, bon't display engineer tab
        //bool bPretrimOrAuto = false;        //For operator, only auto tab
        //bool bPretrimOrAuto = true;         //For engineer, only PreTrim tab
        uint uTabVisibleCode = 0;
        bool bMRE = false;
        bool bMASK = false;
        bool bSAFEREAD = false;
        bool bFastVersion = false;

        uint DeviceAddress = 0x73;
        uint SampleRateNum = 1024;
        uint SampleRate = 1000;     //KHz
        string SerialNum = "None";

        /// <summary>
        /// Delay Define
        /// </summary>
        int Delay_Power = 100;      //ms
        int Delay_Sync =10;   //ms
        int Delay_Fuse = 300;       //ms
        //int Delay_Sync = 50;        //ms

        double ADCOffset = 0;
        double AadcOffset
        {
            set
            {
                this.ADCOffset = Math.Round(value, 0);
                //Set three adcofset combobox on the GUI
                //this.txt_IP_EngT.Text = this.ip.ToString("F0");
                this.txt_AdcOffset_PreT.Text = this.ADCOffset.ToString("F0");
                this.txt_AdcOffset_AutoT.Text = this.ADCOffset.ToString("F0");
            }
            get { return this.ADCOffset; }
        }

        double VoutIPThreshold = 0.010;
        double ThresholdOfGain = 0.999;
        double RefVoltOffset = -0.007;
        double dCurrentUpLimit = 20;
        double dCurrentDownLimit = 7;

        double Vout_0A = 0;
        double Vout_IP = 0;
        double Mout_0A = 0;
        double Mout_IP = 0;
        double AMPout_0A = 0;
        double AMPout_IP = 0;
        double ip = 20;
        double IP
        {
            set
            {
                this.ip = Math.Round(value,0);
                //Set three ip combobox on the GUI
                this.txt_IP_EngT.Text = this.ip.ToString("F0");
                this.txt_IP_PreT.Text = this.ip.ToString("F0");
                this.txt_IP_AutoT.Text = this.ip.ToString("F0");
            }
            get { return this.ip; }
        }
        double sl910out = 0;

        string StrIPx_Auto = "15A";
        double selectedCurrent_Auto = 20;   //A
        double targetGain_customer = 100;    //mV/A

        double targetOffset = 2.5;
        double TargetOffset
        {
            get { return this.targetOffset; }
            set 
            {
                this.targetOffset = value;
                //this.txt_VoutOffset_AutoT.Text = (string)this.cmb_Voffset_PreT.SelectedItem;

                if (this.targetOffset == 1.65)
                    saturationVout = 4.9;
                else
                    saturationVout = 4.9;

                //Update trim code table 
                FilledRoughTable_Customer();
                FilledPreciseTable_Customer();
            }
        }
        double saturationVout = 4.90;
        double minimumVoutIP = 1.5;
        double bin2accuracy = 1.4;
        double bin3accuracy = 2;

        double targetVoltage_customer = 2;
        double TargetVoltage_customer
        {
            get { return this.targetVoltage_customer; }
            set
            {
                this.targetVoltage_customer = value;

                //Update GUI
                this.txt_targetvoltage_PreT.Text = this.targetVoltage_customer.ToString();
                //this.txt_TargetGain_PreT.Text = this.targetVoltage_customer.ToString();
                this.txt_TargertVoltage_AutoT.Text = this.targetVoltage_customer.ToString();
            }
        }

        double TargetGain_customer
        {
            get { return this.targetGain_customer; }
            set
            {
                this.targetGain_customer = value;

                //Update GUI
                this.txt_TargetGain_EngT.Text = this.targetGain_customer.ToString();
                this.txt_TargetGain_PreT.Text = this.targetGain_customer.ToString();
                this.txt_TargetGain_AutoT.Text = this.targetGain_customer.ToString();
            }
        }

        uint reg80Value = 0;
        uint Reg80Value
        {
            get { return this.reg80Value; }
            set
            {
                this.reg80Value = value;
                //Update GUI
                this.txt_reg80_EngT.Text = "0x" + this.reg80Value.ToString("X2");
                this.txt_Reg80_PreT.Text = "0x" + this.reg80Value.ToString("X2");
            }
        }

        uint reg81Value = 0;
        uint Reg81Value
        {
            get { return this.reg81Value; }
            set
            {
                this.reg81Value = value;
                //Update GUI
                this.txt_reg81_EngT.Text = "0x" + this.reg81Value.ToString("X2");
                this.txt_Reg81_PreT.Text = "0x" + this.reg81Value.ToString("X2");
            }
        }

        uint reg82Value = 0;
        uint Reg82Value
        {
            get { return this.reg82Value; }
            set
            {
                this.reg82Value = value;
                //Update GUI
                this.txt_reg82_EngT.Text = "0x" + this.reg82Value.ToString("X2");
                this.txt_Reg82_PreT.Text = "0x" + this.reg82Value.ToString("X2");
            }
        }

        uint reg83Value = 0;
        uint Reg83Value
        {
            get { return this.reg83Value; }
            set
            {
                this.reg83Value = value;
                //Update GUI
                this.txt_reg83_EngT.Text = "0x" + this.reg83Value.ToString("X2");
                this.txt_Reg83_PreT.Text = "0x" + this.reg83Value.ToString("X2");
            }
        }

        uint Reg84Value = 0;
        uint Reg85Value = 0;
        uint Reg86Value = 0;
        uint Reg87Value = 0;

        //Just used for auto trim, will be updated when auto tirm tabe entering and loading config file
        //uint[] Reg80ToReg83Backup = new uint[4];
        uint[] Reg80ToReg88Backup = new uint[8];

        uint[] tempReadback = new uint[9];

        uint[] uResult = new uint[16];
        double[] multiSiteVout0A = new double[16];
        double[] multiSiteVoutIP = new double[16];

        uint[][] trimData = new uint[16 * 2][];

        int moduleTypeindex = 0;
        int ModuleTypeIndex
        {
            set 
            {
                this.moduleTypeindex = value; 
                //Set both combobox on GUI
                this.cmb_Module_EngT.SelectedIndex = this.moduleTypeindex;
                this.cmb_Module_PreT.SelectedIndex = this.moduleTypeindex;
                this.txt_ModuleType_AutoT.Text = (string)this.cmb_Module_EngT.SelectedItem;

                //Set Voffset
                if (this.moduleTypeindex == 2)
                {
                    TargetOffset = 1.65;
                    //saturationVout = 3.25;
                }
                else if (this.moduleTypeindex == 1)
                {
                    TargetOffset = 2.5;
                    //saturationVout = 4.9;
                }
                else
                {
                    //TargetOffset = 2.5;
                    //saturationVout = 4.9;
                }

                //if (this.moduleTypeindex == 2)
                //{
                //    this.cmb_Voffset_PreT.SelectedItem = (object)(this.TargetOffset + "V");
                //    //this.cmb_Voffset_PreT.Enabled = false;
                //}
                //else if (this.moduleTypeindex == 1)
                //{
                //    this.cmb_Voffset_PreT.SelectedItem = (object)(this.TargetOffset + "V");
                //    //this.cmb_Voffset_PreT.Enabled = false;
                //}
                //else
                //{
                //    //this.cmb_Voffset_PreT.Enabled = true;
                //}
            }
            get { return this.moduleTypeindex; }
        }

        int socketType = 0;
        int SocketType
        {
            set
            {
                this.socketType = value;
                //Set combobox on GUI
                this.cmb_SocketType_AutoT.SelectedIndex = this.socketType;
                //this.cmb_Module_PreT.SelectedIndex = this.moduleTypeindex;
                //this.cmb_SocketType_AutoT.Text = (string)this.cmb_Module_EngT.SelectedItem;
            }
            get { return this.socketType; }
        }

        int programMode = 0;
        int ProgramMode
        {
            set
            {
                this.programMode = value;
                this.cmb_ProgramMode_AutoT.SelectedIndex = this.programMode;
            }
            get { return this.programMode; }
        }

        uint ix_forRoughGainCtrl = 15;
        uint Ix_ForRoughGainCtrlBackup = 15;
        uint Ix_ForRoughGainCtrl
        {
            get { return this.ix_forRoughGainCtrl; }
            set
            {
                this.ix_forRoughGainCtrl = value;
                this.txt_ChosenGain_AutoT.Text = RoughTable_Customer[0][ix_forRoughGainCtrl].ToString("F2");
                this.txt_ChosenGain_PreT.Text = RoughTable_Customer[0][ix_forRoughGainCtrl].ToString("F2");
            }
        }

        int ix_forPrecisonGainCtrl = 0;
        int Ix_ForPrecisonGainCtrl
        {
            get { return this.ix_forPrecisonGainCtrl; }
            set { this.ix_forPrecisonGainCtrl = value; }
        }

        int ix_forOffsetATable = 0;
        int Ix_ForOffsetATable
        {
            get { return this.ix_forOffsetATable; }
            set { this.ix_forOffsetATable = value; }
        }

        int ix_forOffsetBTable = 0;
        int Ix_ForOffsetBTable
        {
            get { return this.ix_forOffsetBTable; }
            set { this.ix_forOffsetBTable = value; }
        }

        double k_slope = 0.5;
        double b_offset = 0;

        double[][] RoughTable = new double[3][];        //3x16: 0x80,0x81,Rough
        double[][] PreciseTable = new double[2][];      //2x32: 0x80,Precise
        double[][] OffsetTableA = new double[3][];      //3x16: 0x81,0x82,OffsetA
        double[][] OffsetTableB = new double[2][];      //2x16: 0x83,OffsetB

        //Trim code for 2.5V offset
        double[][] RoughTable_Customer = new double[3][];        //3x16: Rough,0x80,0x81
        double[][] PreciseTable_Customer = new double[2][];      //2x32: 0x80,Precise
        double[][] OffsetTableA_Customer = new double[3][];      //3x16: 0x81,0x82,OffsetA
        double[][] OffsetTableB_Customer = new double[2][];      //2x16: 0x83,OffsetB

        double[][] sl620CoarseGainTable = new double[2][];
        double[][] sl620FineGainTable = new double[3][];

        //Gain trim code for 1.65V offset
        //double[][] RoughTable_1v65offset = new double[3][];        //3x16: Rough,0x80,0x81
        //double[][] PreciseTable_1v65offset = new double[2][];      //2x32: 0x80,Precise

        uint[] MultiSiteReg0 = new uint[16];
        uint[] MultiSiteReg1 = new uint[16];
        uint[] MultiSiteReg2 = new uint[16];
        uint[] MultiSiteReg3 = new uint[16];
        uint[] MultiSiteReg4 = new uint[16];
        uint[] MultiSiteReg5 = new uint[16];
        uint[] MultiSiteReg6 = new uint[16];
        uint[] MultiSiteReg7 = new uint[16];
        uint[] MultiSiteRoughGainCodeIndex = new uint[16];

        uint[] TunningTabReg = new uint[9];                          //Brake usage
        int Ix_OffsetA_TunningTab = 0;
        int Ix_OffsetB_TunningTab = 0;
        int Ix_GainRough_TunningTab = 0;
        int Ix_GainFine_TunningTab = 0;

        enum PRGMRSULT{
            DUT_BIN_1 = 1,
            DUT_BIN_2 = 2,
            DUT_BIN_3 = 3,
            DUT_BIN_4 = 4,
            DUT_BIN_5 = 5,
            DUT_BIN_6 = 6,
            DUT_BIN_NORMAL = 21,
            DUT_BIN_MARGINAL = 22,
            DUT_VOUT_SHORT = 90,
            DUT_CURRENT_HIGH = 91,
            DUT_TRIMMED_SOMEBITS = 92,
            DUT_TRIMMRD_ALREADY = 97,
            DUT_COMM_FAIL = 98,
            DUT_VOUT_SATURATION = 93,
            DUT_LOW_SENSITIVITY = 94,
            DUT_VOUT_LOW = 95,
            DUT_VOUT_VDD = 96,
            DUT_OFFSET_ABN = 99
        }

        #region Bit Operation Mask
        readonly uint bit0_Mask = Convert.ToUInt32(Math.Pow(2, 0));
        readonly uint bit1_Mask = Convert.ToUInt32(Math.Pow(2, 1));
        readonly uint bit2_Mask = Convert.ToUInt32(Math.Pow(2, 2));
        readonly uint bit3_Mask = Convert.ToUInt32(Math.Pow(2, 3));
        readonly uint bit4_Mask = Convert.ToUInt32(Math.Pow(2, 4));
        readonly uint bit5_Mask = Convert.ToUInt32(Math.Pow(2, 5));
        readonly uint bit6_Mask = Convert.ToUInt32(Math.Pow(2, 6));
        readonly uint bit7_Mask = Convert.ToUInt32(Math.Pow(2, 7));

        uint bit_op_mask;
        #endregion Bit Mask

        #endregion

        #region Device Connection
        OneWireInterface oneWrie_device = new OneWireInterface();

        dmm34401a dmm = new dmm34401a();

        private int WM_DEVICECHANGE = 0x0219;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DEVICECHANGE)
            {
                ConnectDevice();
            }
        }

        private void ConnectDevice()
        {
            bool result = false;
            #region One wire
            if(!bUsbConnected)
                result = oneWrie_device.ConnectDevice();

            if (result)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.YellowGreen;
                this.toolStripStatusLabel_Connection.Text = "Connected";
                btn_GetFW_OneWire_Click(null, null);
                bUsbConnected = true;
            }
            else
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
                this.toolStripStatusLabel_FWInfo.Text = "";
            }
            #endregion
        }
        #endregion Device Connection

        #region Device Setting
        private decimal pilotwidth_ow_value_backup = 80000;
        private void numUD_pilotwidth_ow_ValueChanged(object sender, EventArgs e)
        {
            this.numUD_pilotwidth_ow_EngT.Value = (decimal)((int)Math.Round((double)this.numUD_pilotwidth_ow_EngT.Value / 20d) * 20);
            if (this.numUD_pilotwidth_ow_EngT.Value % 20 == 0 & this.numUD_pilotwidth_ow_EngT.Value != pilotwidth_ow_value_backup)
            {
                this.pilotwidth_ow_value_backup = this.numUD_pilotwidth_ow_EngT.Value;
                oneWrie_device.SetPilotWidth((uint)this.numUD_pilotwidth_ow_EngT.Value);
            }
        }

        private void num_UD_pulsewidth_ow_ValueChanged(object sender, EventArgs e)
        {
            //this.num_UD_pulsewidth_ow_EngT.Value = (decimal)((int)Math.Round((double)this.num_UD_pulsewidth_ow_EngT.Value / 5d) * 5);
            //this.num_UD_pulsewidth_ow_EngT.Value = (double)this.num_UD_pulsewidth_ow_EngT.Value;
        }

        private void btn_fuse_action_ow_Click(object sender, EventArgs e)
        {
            //bool fuseMasterBit = false;
            

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_withCap_Vout.Checked = false;
            rbt_withoutCap_Vout_EngT.Checked = true;
            //rbt_signalPathSeting_Vout.Checked = false;

            //0x03->0x43
            uint _reg_addr = 0x43;
            uint _reg_data = 0x03;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

            //0xAA->0x44
            _reg_addr = 0x44;
            _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            
            //Console.WriteLine("Fuse write result->{0}", oneWrie_device.FuseClockSwitch((double)this.num_UD_pulsewidth_ow_EngT.Value, (double)this.numUD_pulsedurationtime_ow_EngT.Value));
        }

        private void btn_fuse_clock_ow_EngT_Click(object sender, EventArgs e)
        {
            //DialogResult dr = MessageBox.Show("Please Change Power To 6V", "Change Power", MessageBoxButtons.OKCancel);
            //if (dr == DialogResult.Cancel)
            //    return;

            Console.WriteLine("Fuse write result->{0}", oneWrie_device.FuseClockSwitch((double)this.num_UD_pulsewidth_ow_EngT.Value, (double)this.numUD_pulsedurationtime_ow_EngT.Value));
        }

        private void btn_flash_onewire_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Flash result->{0}", oneWrie_device.FlashLED());
        }

        private void btn_GetFW_OneWire_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Enter Get FW Interface");
            byte[] info = oneWrie_device.GetFirmwareInfo();

            if (info == null)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
                this.toolStripStatusLabel_FWInfo.Text = "null";
                return;
            }

            string fwVersion = "v" + info[1].ToString() + "." + info[0].ToString() + " ";

            char[] dataInfo = new char[12];
            Array.Copy(info, 8, dataInfo, 0, 12);

            char[] timeInfo = new char[8];
            Array.Copy(info, 20, timeInfo, 0, 8);

            SerialNum = ((info[29] << 8) + info[28]).ToString();

            string data = new string(dataInfo);
            string time = "Build @ " + new string(timeInfo);

            this.toolStripStatusLabel_FWInfo.Text = fwVersion + time + " " + data;
            //this.lbl_FW_onewire.Text = "FW Version:" + oneWrie_device.GetFWVersion();
        }

        #endregion Device Setting

        #region Methods

        private void UserInit()
        {
            //Connect device first.
            ConnectDevice();

            //init 910 datagrid
            InitSL910TabDataGrid();

            InitSL620TabDataGrid();

            btn_SL620Tab_PowerOn.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn6V.BackColor = Color.Transparent;

            //InitChar910TabDataGrid();

            //Refresh pilot width
            //Console.WriteLine("Set pilot width result->{0}", oneWrie_device.SetPilotWidth(8000));
            //numUD_pilotwidth_ow_ValueChanged(null, null);
            oneWrie_device.SetPilotWidth(80000);

            //Fill all the tables for internal tab
            FilledRoughTable();
            FilledPreciseTable();
            FilledOffsetTableA();
            FilledOffsetTableB();

            //Fill all the tables for customer tab
            FilledRoughTable_Customer();
            FilledPreciseTable_Customer();
            FilledOffsetTableA_Customer();
            FilledOffsetTableB_Customer();

            Fillsl620CoarseGainTable();
            Fillsl620FineGainTable();

            InitTuningTab();

            //Init combobox
            //1. Engineering
            this.cmb_SensingDirection_EngT.SelectedIndex = 0;
            this.cmb_OffsetOption_EngT.SelectedIndex = 0;
            this.cmb_PolaritySelect_EngT.SelectedIndex = 0;
            this.cmb_Module_EngT.SelectedIndex = 0;
            //2. PreTrim
            this.cmb_SensitivityAdapt_PreT.SelectedIndex = 0;
            //this.cmb_TempCmp_PreT.SelectedIndex = 0;
            //-350ppm
            this.cmb_TempCmp_PreT.SelectedIndex = 1;

            this.cmb_IPRange_PreT.SelectedIndex = 1;
            this.cmb_Module_PreT.SelectedIndex = 0;
            this.cmb_Voffset_PreT.SelectedIndex = 0;
            this.cmb_SocketType_AutoT.SelectedIndex = 0;
            this.cmb_ProgramMode_AutoT.SelectedIndex = 0;

            this.cb_ChannelSelect.SelectedIndex = 0;

            //Serial Num
            this.txt_SerialNum_EngT.Text = SerialNum;
            this.txt_SerialNum_PreT.Text = SerialNum;

            //load config
            btn_loadconfig_AutoT_Click(null, null);

            //this.tabControl1.Controls.Remove(BrakeTab);

            //Display Tab
            if (uTabVisibleCode == 1)
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                this.tabControl1.Controls.Remove(PriTrimTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 2)
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(AutoTrimTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 3)
            {
                this.tabControl1.Controls.Remove(BrakeTab);
                this.tabControl1.Controls.Remove(EngineeringTab);
                DisplayOperateMes("Load config profile success!");
            }
            else if (uTabVisibleCode == 4)
            {
                DisplayOperateMes("Load config profile success!");
            }
            else
            {
                DisplayOperateMes("Invalid config profile!", Color.DarkRed);
                //MessageBox.Show("Invalid config profile!");
                MessageBox.Show("Invalid config profile!", "Change Current", MessageBoxButtons.OKCancel);
            }

            //DisplayOperateMes("AadcOffset = " + AadcOffset.ToString());
            DisplayOperateMes("MRE = "+ bMRE.ToString());
            DisplayOperateMes("MASK = " + bMASK .ToString());
            DisplayOperateMes("SAFETY = " + bSAFEREAD .ToString());
            DisplayOperateMes("<------- " + DateTime.Now.ToString() + " ------->");
        }

        private double AverageVout()
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));
            Delay(Delay_Sync);
            result += oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));
            Delay(Delay_Sync);
            result += oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));

            result /= 3d;

            result = RefVoltOffset - AadcOffset/1000d + (result * 5d / 4096d);
            return result;
        }

        private double AverageVout_Customer(uint sampleNum)
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, sampleNum));

            result = RefVoltOffset - AadcOffset/1000d + (result * 5d / 4096d);
            return result;
        }

        private double GetModuleCurrent()
        {
            double result = oneWrie_device.AverageADCSamples(oneWrie_device.ADCSampleTransfer(SampleRate, SampleRateNum));

            result = 1000d * (RefVoltOffset + (result * 5d / 4096d)) / 100d;
            return result;
        }

        private void SaveMultiSiteRegData(uint indexDut)
        {
            MultiSiteReg0[indexDut] = Reg80Value;
            MultiSiteReg1[indexDut] = Reg81Value;
            MultiSiteReg2[indexDut] = Reg82Value;
            MultiSiteReg3[indexDut] = Reg83Value;
        }

        /// <summary>
        /// 根据采集的Vout@0A，Vout@IP计算出Gain
        /// </summary>
        /// <returns>计算出的Gain供查表用,单位mV/A</returns>
        private double GainCalculate()
        {
            double result = 0;

            result = 1000d * ((Vout_IP - Vout_0A) / IP);

            return result;
        }

        /// <summary>
        /// 根据采集的Vout@0A，Vout@IP计算出Gain
        /// </summary>
        /// <returns>计算出的Gain供查表用,单位mV/A</returns>
        private double GainCalculate(double v_0A, double v_ip)
        {
            return 1000d * ((v_ip - v_0A) / IP);
        }

        /// <summary>
        /// 根据第二次计算的IP0计算，公式：2.5/IP0
        /// </summary>
        /// <returns>计算出的Offset供查表用</returns>
        private double OffsetTuningCalc_Customer()
        {
            //return 2.5 / Vout_0A;
            return TargetOffset / Vout_0A;
        }

        private double GainTuningCalc_Customer(double testValue, double targetValue)
        {
            return targetValue / testValue;
        }

        private void FilledRoughTable()
        {
            for (int i = 0; i < RoughTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Rough
                        RoughTable[i] = new double[]{
                            -87.75,
                            -85.91,
                            -83.76,
                            -81.26,
                            -78.44,
                            -75.19,
                            -71.27,
                            -67.16,
                            -62.28,
                            -56.52,
                            -50.05,
                            -42.45,
                            -33.83,
                            -24.01,
                            -12.47,
                            0.00
                            };
                        break;
                    case 2: //0x81
                        RoughTable[i] = new double[]{
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0,
                            1,
                            0
                        };
                        break;
                    case 1: //0x80
                        RoughTable[i] = new double[]{
                        0xE0,
                        0xE0,
                        0x60,
                        0x60,
                        0xA0,
                        0xA0,
                        0x20,
                        0x20,
                        0xC0,
                        0xC0,
                        0x40,
                        0x40,
                        0x80,
                        0x80,
                        0x0,
                        0x0
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledPreciseTable()
        {
            for (int i = 0; i < PreciseTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        PreciseTable[i] = new double[]{
                            0.00,
                            -0.45,
                            -0.90,
                            -1.35,
                            -1.80,
                            -2.25,
                            -2.69,
                            -3.14,
                            -3.59,
                            -4.04,
                            -4.49,
                            -4.94,
                            -5.38,
                            -5.83,
                            -6.28,
                            -6.73,
                            -7.18,
                            -7.63,
                            -8.08,
                            -8.52,
                            -8.97,
                            -9.42,
                            -9.87,
                            -10.32,
                            -10.77,
                            -11.21,
                            -11.66,
                            -12.11,
                            -12.56,
                            -13.01,
                            -13.46,
                            -13.90
                        };
                        break;
                    case 1: //0x80
                        PreciseTable[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F        
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableA()
        {
            for (int i = 0; i < OffsetTableA.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableA[i] = new double[]{
                            0,
                            -1.08,
                            -2.160,
                            -3.240,
                            -4.320,
                            -5.400,
                            -6.480,
                            -7.560,
                            8.28,
                            7.2450,
                            6.2100,
                            5.1750,
                            4.1400,
                            3.1050,
                            2.0700,
                            1.0350
                            };
                        break;
                    case 1: //0x81
                        OffsetTableA[i] = new double[]{
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80    
                        };
                        break;
                    case 2: //0x82
                        OffsetTableA[i] = new double[]{
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7,
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7   
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableB()
        {
            for (int i = 0; i < OffsetTableB.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableB[i] = new double[]{
                            0,
                            -0.29,
                            -0.58,
                            -0.87,
                            -1.16,
                            -1.45,
                            -1.74,
                            -2.03,
                            2.32,
                            2.03,
                            1.74,
                            1.45,
                            1.16,
                            0.87,
                            0.58,
                            0.29   
                        };
                        break;
                    case 1: //0x83
                        OffsetTableB[i] = new double[]{
                            0x0,
                            0x20,
                            0x10,
                            0x30,
                            0x8,
                            0x28,
                            0x18,
                            0x38,
                            0x4,
                            0x24,
                            0x14,
                            0x34,
                            0xC,
                            0x2C,
                            0x1C,
                            0x3C  
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledRoughTable_Customer()
        {
            if (TargetOffset == 2.5)
            {
                for (int i = 0; i < RoughTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Rough
                            RoughTable_Customer[i] = new double[]{
                                12.3565,
                                14.1316,
                                16.2615,
                                18.7339,
                                21.5248,
                                24.7433,
                                28.6430,
                                32.7433,
                                37.7594,
                                43.5103,
                                49.9655,
                                57.5622,
                                66.1880,
                                75.9998,
                                87.5380,
                                100.0000
                            };
                            break;
                        case 2: //0x81
                            RoughTable_Customer[i] = new double[]{
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0
                            };
                            break;
                        case 1: //0x80
                            RoughTable_Customer[i] = new double[]{
                            0xE0,
                            0xE0,
                            0x60,
                            0x60,
                            0xA0,
                            0xA0,
                            0x20,
                            0x20,
                            0xC0,
                            0xC0,
                            0x40,
                            0x40,
                            0x80,
                            0x80,
                            0x0,
                            0x0
                            };
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (TargetOffset == 1.65)
            {
                for (int i = 0; i < RoughTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Rough
                            RoughTable_Customer[i] = new double[]{
                                12.5545,
                                14.4698,
                                16.6670,
                                19.1870,
                                22.0822,
                                25.4006,
                                29.1830,
                                33.5584,
                                38.7607,
                                44.6210,
                                51.2550,
                                58.9272,
                                67.5783,
                                77.3808,
                                88.4811,
                                100.0000

                            };
                            break;
                        case 2: //0x81
                            RoughTable_Customer[i] = new double[]{
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0,
                                1,
                                0
                            };
                            break;
                        case 1: //0x80
                            RoughTable_Customer[i] = new double[]{
                            0xE0,
                            0xE0,
                            0x60,
                            0x60,
                            0xA0,
                            0xA0,
                            0x20,
                            0x20,
                            0xC0,
                            0xC0,
                            0x40,
                            0x40,
                            0x80,
                            0x80,
                            0x0,
                            0x0
                            };
                            break;
                        default:
                            break;
                    }
                }
            }
            else
                DisplayOperateMes("Offset Selection Error!",Color.DarkRed);
        }

        private void FilledPreciseTable_Customer()
        {
            if (TargetOffset == 2.5)
            {
                for (int i = 0; i < PreciseTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Precise
                            PreciseTable_Customer[i] = new double[]{
                            100.0000,
                            99.5107,
                            99.0909,
                            98.6436,
                            98.1915,
                            97.7258,
                            97.2238,
                            96.7933,
                            96.3252,
                            95.9057,
                            95.4774,
                            94.9961,
                            94.5857,
                            94.2226,
                            93.6917,
                            93.2525,
                            92.8312,
                            92.3754,
                            91.8996,
                            91.5016,
                            91.0280,
                            90.5525,
                            90.0948,
                            89.6563,
                            89.2102,
                            88.7558,
                            88.2891,
                            87.8519,
                            87.3960,
                            86.9635,
                            86.4919,
                            86.0669
                        };
                            break;
                        case 1: //0x80
                            PreciseTable_Customer[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F        
                        };
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (TargetOffset == 1.65)
            {
                for (int i = 0; i < PreciseTable.Length; i++)
                {
                    switch (i)
                    {
                        case 0: //Precise
                            PreciseTable_Customer[i] = new double[]{
                            100.0000,
                            99.5621,
                            99.0883,
                            98.6571,
                            98.2420,
                            97.8018,
                            97.3733,
                            96.9106,
                            96.5204,
                            96.0668,
                            95.6047,
                            95.1692,
                            94.7649,
                            94.3190,
                            93.8573,
                            93.4373,
                            93.0262,
                            92.5910,
                            92.1260,
                            91.7141,
                            91.2899,
                            90.8342,
                            90.4010,
                            89.9252,
                            89.4676,
                            89.0324,
                            88.5949,
                            88.1567,
                            87.6997,
                            87.2675,
                            86.8323,
                            86.3821

                        };
                            break;
                        case 1: //0x80
                            PreciseTable_Customer[i] = new double[]{
                            0x0,
                            0x8,
                            0x4,
                            0xC,
                            0x2,
                            0xA,
                            0x6,
                            0xE,
                            0x1,
                            0x9,
                            0x5,
                            0xD,
                            0x3,
                            0xB,
                            0x7,
                            0xF,
                            0x10,
                            0x18,
                            0x14,
                            0x1C,
                            0x12,
                            0x1A,
                            0x16,
                            0x1E,
                            0x11,
                            0x19,
                            0x15,
                            0x1D,
                            0x13,
                            0x1B,
                            0x17,
                            0x1F       
                        };
                            break;
                        default:
                            break;
                    }
                }
            }
            else
                DisplayOperateMes("Offset Selection Error!", Color.DarkRed);
        }

        private void FilledOffsetTableA_Customer()
        {
            for (int i = 0; i < OffsetTableA_Customer.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableA_Customer[i] = new double[]{
                            100.00,
                            98.94,
                            97.87,
                            96.78,
                            95.68,
                            94.60,
                            93.50,
                            92.39,
                            108.27,
                            107.27,
                            106.26,
                            105.23,
                            104.20,
                            103.16,
                            102.12,
                            101.07
                        };
                        break;
                    case 1: //0x81
                        OffsetTableA_Customer[i] = new double[]{
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x0,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80,
                            0x80    
                        };
                        break;
                    case 2: //0x82
                        OffsetTableA_Customer[i] = new double[]{
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7,
                            0x0,
                            0x4,
                            0x2,
                            0x6,
                            0x1,
                            0x5,
                            0x3,
                            0x7   
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void FilledOffsetTableB_Customer()
        {
            for (int i = 0; i < OffsetTableB_Customer.Length; i++)
            {
                switch (i)
                {
                    case 0: //Offset
                        OffsetTableB_Customer[i] = new double[]{
                            100.00,
                            99.72,
                            99.43,
                            99.14,
                            98.85,
                            98.56,
                            98.28,
                            98.00,
                            102.39,
                            102.10,
                            101.80,
                            101.48,
                            101.19,
                            100.89,
                            100.60,
                            100.31
                        };
                        break;
                    case 1: //0x83
                        OffsetTableB_Customer[i] = new double[]{
                            0x0,
                            0x20,
                            0x10,
                            0x30,
                            0x8,
                            0x28,
                            0x18,
                            0x38,
                            0x4,
                            0x24,
                            0x14,
                            0x34,
                            0xC,
                            0x2C,
                            0x1C,
                            0x3C  
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620CoarseGainTable()
        {
            for (int i = 0; i < sl620CoarseGainTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620CoarseGainTable[i] = new double[]{
                            100,
                            86.61852399,
                            75.09163521,
                            65.39197987,
                            57.22960775,
                            49.93161552,
                            43.50894469,
                            37.96159527,
                            33.17468133,
                            28.91843099,
                            25.16549045,
                            21.9596258,
                            19.16953882,
                            16.69675584,
                            14.54674763,
                            12.70310192
                        };
                        break;
                    case 1: //0x80
                        sl620CoarseGainTable[i] = new double[]{
                            0,
                            16,
                            32,
                            48,
                            64,
                            80,
                            96,
                            112,
                            128,
                            144,
                            160,
                            176,
                            192,
                            208,
                            224,
                            240
      
                        };
                        break;
                    default:
                        break;
                }
            }
        }

        private void Fillsl620FineGainTable()
        {
            for (int i = 0; i < sl620FineGainTable.Length; i++)
            {
                switch (i)
                {
                    case 0: //Precise
                        sl620FineGainTable[i] = new double[]{
                            100,
                            99.84154737,
                            99.54103377,
                            99.34433395,
                            99.0929953,
                            98.90175937,
                            98.75423451,
                            98.51382363,
                            98.27887663,
                            98.07671293,
                            97.86362146,
                            97.60135504,
                            97.47568572,
                            97.2462026,
                            96.99486395,
                            96.75991695,
                            96.51404218,
                            96.3064146,
                            96.06053983,
                            95.9020872,
                            95.67806797,
                            95.46497651,
                            95.24095727,
                            94.98961862,
                            94.72188832,
                            94.62353841,
                            94.36127199,
                            94.10993334,
                            93.95148071,
                            93.67282264,
                            93.53622555,
                            93.24117583,
                            93.06086766,
                            92.89148727,
                            92.6128292,
                            92.4106655,
                            92.24674899,
                            91.92437985,
                            91.79871052,
                            91.49273303,
                            91.31242487,
                            91.06108622,
                            90.88077806,
                            90.61304775,
                            90.42727571,
                            90.19779259,
                            89.98470113,
                            89.71150694,
                            89.51480712,
                            89.27986012,
                            89.01759371,
                            88.87006884,
                            88.63512184,
                            88.36192766,
                            88.17615561,
                            88.00131133,
                            87.71172549,
                            87.51502568,
                            87.31832587,
                            87.1052344,
                            86.83204021,
                            86.58070156,
                            86.38946563,
                            86.18730193
                        };
                        break;
                    case 1: //0x86
                        sl620FineGainTable[i] = new double[]{
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224,
                            0,
                            32,
                            64,
                            96,
                            128,
                            160,
                            192,
                            224

                        };
                        break;
                    case 2: //0x87
                        sl620FineGainTable[i] = new double[]{
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            32,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            64,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            96,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            128,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            160,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            192,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224,
                            224

                        };
                        break;
                    default:
                        break;
                }
            }
        }

        //private void FilledRoughTable_1v65offset()
        //{
        //    for (int i = 0; i < RoughTable.Length; i++)
        //    {
        //        switch (i)
        //        {
        //            case 0: //Rough
        //                RoughTable_Customer[i] = new double[]{
        //                    12.216,
        //                    13.862,
        //                    16.147,
        //                    18.348,
        //                    21.401,
        //                    24.653,
        //                    29.975,
        //                    34.020,
        //                    38.312,
        //                    44.412,
        //                    52.052,
        //                    59.549,
        //                    68.440,
        //                    77.870,
        //                    88.259,
        //                    100.000

        //                };
        //                break;
        //            case 2: //0x81
        //                RoughTable_Customer[i] = new double[]{
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0,
        //                    1,
        //                    0
        //                };
        //                break;
        //            case 1: //0x80
        //                RoughTable_Customer[i] = new double[]{
        //                0xE0,
        //                0xE0,
        //                0x60,
        //                0x60,
        //                0xA0,
        //                0xA0,
        //                0x20,
        //                0x20,
        //                0xC0,
        //                0xC0,
        //                0x40,
        //                0x40,
        //                0x80,
        //                0x80,
        //                0x0,
        //                0x0
        //                };
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        
        //}

        //private void FilledPreciseTable_1v65offse()
        //{
        //    for (int i = 0; i < PreciseTable.Length; i++)
        //    {
        //        switch (i)
        //        {
        //            case 0: //Precise
        //                PreciseTable_Customer[i] = new double[]{
        //                    100,
        //                    100.9133872,
        //                    100.6252553,
        //                    98.38610861,
        //                    97.60862141,
        //                    96.76134507,
        //                    96.72656851,
        //                    96.58239657,
        //                    95.42803881,
        //                    95.39503088,
        //                    94.97995235,
        //                    93.9553052,
        //                    93.75533338,
        //                    93.14090708,
        //                    92.67175348,
        //                    92.1915706,
        //                    91.78149903,
        //                    91.73470358,
        //                    91.59799441,
        //                    91.51023103,
        //                    90.70553856,
        //                    90.51250858,
        //                    90.15082225,
        //                    90.03869214,
        //                    89.96089341,
        //                    89.84972078,
        //                    89.7525681,
        //                    89.3287556,
        //                    88.18534057,
        //                    87.14040232,
        //                    86.86542726,
        //                    85.50573369

        //                };
        //                break;
        //            case 1: //0x80
        //                PreciseTable_Customer[i] = new double[]{
        //                    0x0,
        //                    0x8,
        //                    0x4,
        //                    0x2,
        //                    0x1,
        //                    0xC,
        //                    0xE,
        //                    0x9,
        //                    0x5,
        //                    0x3,
        //                    0x6,
        //                    0xA,
        //                    0xB,
        //                    0x13,
        //                    0x18,
        //                    0x1D,
        //                    0xD ,
        //                    0x15,
        //                    0x1B,
        //                    0x10,
        //                    0x19,
        //                    0x12,
        //                    0x16,
        //                    0x1A,
        //                    0x14,
        //                    0x11,
        //                    0x1C,
        //                    0x17,
        //                    0x7,
        //                    0x1F,
        //                    0x1E,
        //                    0xF
        
        //                };
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

        //Abs(Value) decreased table

        private int LookupRoughGain(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                    return i;
            }
            return gainTable[0].Length - 1;
        }

        //Abs(Value) increased table
        private int LookupPreciseGain(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                }
            }
            return 0;
        }

        private int LookupOffset(ref double offset, double[][] offsetTable)
        {
            double temp = offset - offsetTable[0][0];
            int ix = 0;
            for (int i = 1; i < offsetTable[0].Length; i++)
            {
                if (Math.Abs(temp) > Math.Abs(offset - offsetTable[0][i]))
                {
                    temp = offset - offsetTable[0][i];
                    ix = i;
                }
            }
            //offset = temp;
            offset = 100 * offset / offsetTable[0][ix];
            return ix;
        }

        private int LookupOffsetIndex(uint regData, double[][] offsetTable)
        {
            for (int i = 0; i < offsetTable[0].Length; i++)
            {
                if (regData == offsetTable[1][i])
                    return i;
            }

            return 0;
        }

        //Abs(Value) increased table
        private int LookupRoughGain_Customer(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) <= 0)
                    return i;
            }
            return gainTable[0].Length - 1;
        }

        //Abs(Value) decreased table
        private int LookupPreciseGain_Customer(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                    else
                        return (gainTable[0].Length - 1);
                }
            }
            return 0;
        }

        private int LookupOffset_Customer(ref double offset, double[][] offsetTable)
        {
            //Offset = 2.5/IP0_Auto
            double temp = offset - offsetTable[0][0];
            int ix = 0;
            for (int i = 1; i < offsetTable[0].Length; i++)
            {
                if (Math.Abs(temp) > Math.Abs(offset - offsetTable[0][i]))
                {
                    temp = offset - offsetTable[0][i];
                    ix = i;
                }
            }

            offset = 100 * offset / offsetTable[0][ix];  //Return (2.5/IP0_Auto)/offsetTable[ix] which will used for next lookup table operation
            return ix;
        }

        private bool DiffModeOffsetAlg(uint[] reg_TMS)
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            //Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            reg_TMS[0] += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            reg_TMS[1] += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x81 Value = 0x" + reg_TMS[0].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg0x82 Value = 0x" + reg_TMS[1].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            reg_TMS[2] &= ~bit_op_mask;
            reg_TMS[2] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x83 Value = 0x" + reg_TMS[2].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
            return true;
        }

        public void DisplayOperateMes(string strError, Color fontColor)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            //txt_OutputLogInfo.ForeColor = Color.Chartreuse;
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        public void DisplayOperateMes(string strError)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            //txt_OutputLogInfo.ForeColor = Color.Chartreuse;
            txt_OutputLogInfo.Select(beginIndex, length);
            //txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        public void DisplayOperateMesClear( )
        {
            txt_OutputLogInfo.Clear();
        }

        private void DisplayAutoTrimOperateMes(string strMes, bool ifSucceeded, int step)
        {
            if (bAutoTrimTest)
            {
                if (step == 0)
                {
                    if (ifSucceeded)
                        DisplayOperateMes("-------------------Automatica Trim Start(Debug Mode)-------------------\r\n");
                    else
                        DisplayOperateMes("-------------------Automatica Trim Finished(Debug Mode)-------------------\r\n");

                    return;
                }

                //DisplayOperateMes("Step " + step + ":");
                strMes = "Step" + step.ToString() + ":" + strMes;
                if (ifSucceeded)
                {
                    strMes += " succeeded!";
                    DisplayOperateMes(strMes);
                }
                else
                {
                    strMes += " Failed!";
                    DisplayOperateMes(strMes, Color.Red);
                }
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes, bool ifSucceeded)
        {
            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Step " + step + ":");
                if (ifSucceeded)
                {
                    strMes += " succeeded!";
                    DisplayOperateMes(strMes);
                }
                else
                {
                    strMes += " Failed!";
                    DisplayOperateMes(strMes, Color.Red);
                }
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes, int step)
        {
            if (bAutoTrimTest)
            {
                strMes = "Step" + step.ToString() + ":" + strMes;
                DisplayOperateMes(strMes);
            }
        }

        private void DisplayAutoTrimOperateMes(string strMes)
        {
            if (bAutoTrimTest)
            {
                DisplayOperateMes(strMes);
            }
        }

        private void DisplayAutoTrimResult(bool ifPass)
        {
            if (ifPass)
            {
                this.lbl_passOrFailed.ForeColor = Color.DarkGreen;
                this.lbl_passOrFailed.Text = "PASS!";
            }
            else
            {
                this.lbl_passOrFailed.ForeColor = Color.Red;
                this.lbl_passOrFailed.Text = "FAIL!";
            }
        }

        private void DisplayAutoTrimResult( UInt16 errorCode)
        {
            switch ( errorCode & 0x000F )
            {
                case 0x0000:
                    this.lbl_passOrFailed.ForeColor = Color.DarkGreen;
                    this.lbl_passOrFailed.Text = "PASS!";
                    break;

                case 0x0001:
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "S.N.E";
                    break;

                case 0x0002:
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "M.R.E";
                    break;

                case 0x0003:
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "O.P.E";
                    break;

                case 0x0004:
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "M.T.E";
                    break;

                case 0x0005:
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    break;

                default:
                    break;

            
            }
        }

        private void DisplayAutoTrimResult(bool ifPass, UInt16 errorCode,string strResult)
        {
            if (ifPass)
            {
                this.lbl_passOrFailed.ForeColor = Color.DarkGreen;
                this.lbl_passOrFailed.Text = "PASS!";

                autoTrimResultIndicator.Clear();
                autoTrimResultIndicator.AppendText( "PASS!\t\t" + strResult);
                autoTrimResultIndicator.Refresh();

            }
            else
            {
                switch (errorCode & 0x000F)
                {
                    case 0x0001:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "S.N.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Sentisivity NOT Enough!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0002:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "M.R.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Marginal Read Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0003:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "O.P.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Output Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0004:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "M.T.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Master Bits Trim Error!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0005:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "H.W.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("No Hardware!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0006:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "I2C.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("I2C Comunication Error\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0007:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "O.P.C";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Operation Canceled!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x0008:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "T.M.E";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("Trim Master Bits Again!\t\t" + strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    case 0x000F:
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";

                        autoTrimResultIndicator.Clear();
                        autoTrimResultIndicator.SelectionColor = Color.DarkRed;
                        autoTrimResultIndicator.AppendText("FAIL!\t\t"+ strResult);
                        autoTrimResultIndicator.Refresh();
                        break;

                    default:
                        break;


                }           
            }
        }

        private void DisplayLogInfo(string strError, Color fontColor)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.SelectionColor = fontColor;
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        private void DisplayLogInfo(string strError)
        {
            int length = strError.Length;
            int beginIndex = txt_OutputLogInfo.Text.Length;
            txt_OutputLogInfo.AppendText(strError + "\r\n");
            txt_OutputLogInfo.Select(beginIndex, length);
            txt_OutputLogInfo.Select(txt_OutputLogInfo.Text.Length, 0);//.SelectedText = "";
            txt_OutputLogInfo.ScrollToCaret();
            txt_OutputLogInfo.Refresh();
        }

        private void MultiSiteDisplayResult(uint[] uResult)
        {
            //bool FF = false;
            autoTrimResultIndicator.Clear();
            autoTrimResultIndicator.SelectionColor = Color.Black;
            autoTrimResultIndicator.AppendText("\r\n");
            autoTrimResultIndicator.AppendText("--00--\t--01--\t--02--\t--03--\t--04--\t--05--\t--06--\t--07--\t--08--\t--09--\t--10--\t--11--\t--12--\t--13--\t--14--\t--15--\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                if ( uResult[idut] < 4 && uResult[idut] >0 )
                {
                    autoTrimResultIndicator.SelectionColor = Color.Green;
                    autoTrimResultIndicator.AppendText("PASS\t");
                }
                else if (uResult[idut] < 7 && uResult[idut] > 3 )
                {
                    if (bMRE)
                    {
                        autoTrimResultIndicator.SelectionColor = Color.Green;
                        autoTrimResultIndicator.AppendText("MRE!\t");
                    }
                    else
                    {
                        autoTrimResultIndicator.SelectionColor = Color.Green;
                        autoTrimResultIndicator.AppendText("PASS\t");
                    }
                }
                else
                {
                    autoTrimResultIndicator.SelectionColor = Color.Red;
                    autoTrimResultIndicator.AppendText("**" + uResult[idut].ToString() + "**\t");
                }
            }
        }

        private void MultiSiteDisplayResultNew(uint[] uResult)
        {
            //bool FF = false;
            autoTrimResultIndicator.Clear();
            autoTrimResultIndicator.SelectionColor = Color.Black;
            autoTrimResultIndicator.AppendText("\r\n");
            autoTrimResultIndicator.AppendText("--00--\t--01--\t--02--\t--03--\t--04--\t--05--\t--06--\t--07--\t--08--\t--09--\t--10--\t--11--\t--12--\t--13--\t--14--\t--15--\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                if (uResult[idut] == 0x00)
                {
                    autoTrimResultIndicator.SelectionColor = Color.Green;
                    autoTrimResultIndicator.AppendText("PASS*\t");
                }
                else if (uResult[idut] == 0xFF)
                {
                    autoTrimResultIndicator.SelectionColor = Color.Red;
                    autoTrimResultIndicator.AppendText("FAIL!\t");
                }
                else if (uResult[idut] == 0xFE)
                {
                    autoTrimResultIndicator.SelectionColor = Color.Red;
                    autoTrimResultIndicator.AppendText("NULL!\t");
                }
                else if (uResult[idut] == 0x01)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("STUAT\t");
                }
                else if (uResult[idut] == 0x02)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("SHORT\t");
                }
                else if (uResult[idut] == 0x03)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("COMMU\t");
                }
                else if (uResult[idut] == 0x04)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("BLOWN\t");
                }
                else if (uResult[idut] == 0x05)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("L0SEN\t");
                }
                else if (uResult[idut] == 0x06)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("OUTER\t");
                }
                else if (uResult[idut] == 0x07)
                {
                    autoTrimResultIndicator.SelectionColor = Color.DarkSlateGray;
                    autoTrimResultIndicator.AppendText("CURRE\t");
                }
                else if (uResult[idut] == 0x08)
                {
                    autoTrimResultIndicator.SelectionColor = Color.YellowGreen;
                    autoTrimResultIndicator.AppendText("MIX!!\t");
                }
            }
            autoTrimResultIndicator.AppendText("\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                    autoTrimResultIndicator.SelectionColor = Color.Green;
                    autoTrimResultIndicator.AppendText( multiSiteVout0A[idut].ToString("F3") + "\t");
                    //autoTrimResultIndicator.AppendText("\r\n");
            }
            autoTrimResultIndicator.AppendText("\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                autoTrimResultIndicator.SelectionColor = Color.Green;
                autoTrimResultIndicator.AppendText(multiSiteVoutIP[idut].ToString("F3") + "\t");
                //autoTrimResultIndicator.AppendText("\r\n");
            }
        }

        private void MultiSiteDisplayVout(double[] uResult)
        {
            //bool FF = false;
            autoTrimResultIndicator.Clear();
            autoTrimResultIndicator.SelectionColor = Color.Black;
            autoTrimResultIndicator.AppendText("\r\n");
            autoTrimResultIndicator.AppendText("--00--\t--01--\t--02--\t--03--\t--04--\t--05--\t--06--\t--07--\t--08--\t--09--\t--10--\t--11--\t--12--\t--13--\t--14--\t--15--\r\n\r\n");
            for (uint idut = 0; idut < 16; idut++)
            {
                autoTrimResultIndicator.SelectionColor = Color.Green;
                autoTrimResultIndicator.AppendText( uResult[idut].ToString("F3") + "\t");             
            }
        }

        private void MultiSiteSocketSelect(UInt32 uDut)
        {
            Delay(Delay_Sync);
            if( uDut < 8)
                oneWrie_device.SDPSignalPathGroupSel(OneWireInterface.SPControlCommand.SP_MULTISITTE_GROUP_A);
            else
                oneWrie_device.SDPSignalPathGroupSel(OneWireInterface.SPControlCommand.SP_MULTISITTE_GROUP_B);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSocketSel(uDut);
            Delay(Delay_Sync);
        }

        private string CreateSingleLogInfo(int index)
        {
            return string.Format("{0}\t{1}\t", "DUT" + index, DateTime.Now.ToString());
        }

        private uint[] ReadBackReg1ToReg4(uint DevAddr)
        {
            uint _dev_addr = DevAddr;

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            //Read Back 0x80~0x84
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[4];

            if (oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr_start, 4, _readBack_data) != 0)
            {
                DisplayAutoTrimOperateMes("Burst Read Back failed!");
                return null;
            }
            else
            {
                DisplayAutoTrimOperateMes("Reg1 = 0x" + _readBack_data[0].ToString("X") +
                    "\r\nReg2 = 0x" + _readBack_data[1].ToString("X") +
                    "\r\nReg3 = 0x" + _readBack_data[2].ToString("X") +
                    "\r\nReg4 = 0x" + _readBack_data[3].ToString("X"));
            }

            return _readBack_data;
        }

        private uint[] ReadBackReg1ToReg5(uint DevAddr)
        {
            uint _dev_addr = DevAddr;

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            //Read Back 0x80~0x85
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[5];

            if (oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr_start, 5, _readBack_data) != 0)
            {
                DisplayAutoTrimOperateMes("Burst Read Back failed!");
                return null;
            }
            else
            {
                DisplayAutoTrimOperateMes("Reg1 = 0x" + _readBack_data[0].ToString("X") +
                    "\r\nReg2 = 0x" + _readBack_data[1].ToString("X") +
                    "\r\nReg3 = 0x" + _readBack_data[2].ToString("X") +
                    "\r\nReg4 = 0x" + _readBack_data[3].ToString("X") +
                    "\r\nReg5 = 0x" + _readBack_data[4].ToString("X"));
            }

            return _readBack_data;
        }

        private bool CheckReg1ToReg4(uint[] readBackData, uint Reg1, uint Reg2, uint Reg3, uint Reg4)
        {
            if (readBackData == null)
                return false;

            if ((readBackData[0] >= Reg1) &&
                (readBackData[1] >= Reg2) &&
                (readBackData[2] >= Reg3) &&
                (readBackData[3] >= Reg4))
                return true;
            else if((readBackData[0] == Reg1) &&
                    (readBackData[1] == Reg2) &&
                    (readBackData[2] == Reg3) &&
                    (readBackData[3] == Reg4))
                return true;
            else
                return false;
        }

        private bool MarginalCheckReg1ToReg4(uint[] readBackData, uint _dev_addr, double testGain_Auto)
        {
            if (readBackData == null)
                return false;

            #region Setup Marginal Read
            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            _reg_addr = 0x43;
            _reg_data = 0x0E;

            bool writeResult = oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
            if (writeResult)
                DisplayOperateMes("Marginal Read succeeded!");
            else
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);

            //Delay 50ms
            Thread.Sleep(50);
            DisplayOperateMes("Delay 50ms");

            _reg_addr = 0x43;
            _reg_data = 0x0;

            writeResult = oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
            //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
            if (writeResult)
                DisplayOperateMes("Reset Reg0x43 succeeded!");
            else
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
            #endregion Setup Marginal Read

            uint[] _MarginalreadBack_data = new uint[4];
            _MarginalreadBack_data = ReadBackReg1ToReg4(_dev_addr);

            if ((readBackData[0] == _MarginalreadBack_data[0]) &&
                (readBackData[1] == _MarginalreadBack_data[1]) &&
                (readBackData[2] == _MarginalreadBack_data[2]) &&
                (readBackData[3] == _MarginalreadBack_data[3]))
                return true;
            else
            {
                //if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x20) == 0x20 )
                //{
                //    return false;
                //}
                //else if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x40) == 0x40 && (readBackData[0] & 0x20) == 0x20 )
                //{
                //    return false;
                //}
                //else if (((readBackData[0] ^ _MarginalreadBack_data[0]) & 0x80) == 0x80 && (readBackData[0] & 0x40) == 0x40 && (readBackData[0] & 0x20) == 0x20)
                //{
                //    return false;
                //}
                //else if (((readBackData[1] ^ _MarginalreadBack_data[1]) & 0x80)==0x80 || ((readBackData[2] ^ _MarginalreadBack_data[2]) & 0x01 )== 0x01 )
                //{
                //    return false;
                //}
                //else
                //{
                //    return true;
                //}
                return false;
            }
        }

        private bool FuseClockOn(uint _dev_addr, double fusePulseWidth, double fuseDurationTime)
        {
            //0x03->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x03;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("I2C Write 1 before Fuse Clock", true);
                }
            }
            else
            {
                return false;
            }

            //Delay(Delay_Operation);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("I2C Write 2 before Fuse Clock", true);
                }
            }
            else
            {
                return false; ;
            }

            Delay(Delay_Sync);

            //Fuse 
            if (oneWrie_device.FuseClockSwitch(fusePulseWidth, fuseDurationTime))
            {
                if (bAutoTrimTest)
                {
                    DisplayAutoTrimOperateMes("Fuse Clock On", true);
                }
            }
            else
            {
                return false;
            }

            Delay(Delay_Fuse);
            return true;
        }

        private bool FuseClockOn(uint _dev_addr, double fusePulseWidth, double fuseDurationTime, int delayTime , int step)
        {
            //0x03->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x03;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("I2C Write 1 before Fuse Clock", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("I2C Write 2 before Fuse Clock", true, step);
            else
            {
                return false; ;
            }

            //Delay 50ms
            Thread.Sleep(delayTime);
            DisplayAutoTrimOperateMes("Delay x00ms", step);

            //Fuse 
            if (oneWrie_device.FuseClockSwitch(fusePulseWidth, fuseDurationTime))
                DisplayAutoTrimOperateMes("Fuse Clock On", true, step);
            else
            {
                return false;
            }

            //Delay 700ms -> changed to 100ms @ 2014-09-04
            Thread.Sleep(100);
            DisplayAutoTrimOperateMes("Delay 100ms", step);
            return true;
        }

        private bool WriteBlankFuseCode(uint _dev_addr, uint _reg1Addr, uint _reg2Addr, uint _reg3Addr, int step)
        {
            uint _reg_Value = 00;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg1Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.1"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg2Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.2"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg3Addr, _reg_Value))
                DisplayAutoTrimOperateMes(string.Format("Write 0 to other 3 Regs:No.3"), true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            return true;
        }

        private bool WriteMasterBit(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x07;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool WriteMasterBit0(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x01;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool WriteMasterBit1(uint _dev_addr, int step)
        {
            if (!WriteBlankFuseCode(_dev_addr, 0x80, 0x81, 0x82, step))
                return false;
            //Reg83 <-- 0x0
            uint _reg_Addr = 0x83;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write 0 to Reg4", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }

            //Reg84, Fuse with master bit
            _reg_Addr = 0x84;
            _reg_Value = 0x02;

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write Reg5(0x" + _reg_Value.ToString("X") + ")", true, step);
            else
            {
                DisplayAutoTrimResult(false);
                return false;
            }
            return true;
        }

        private bool ResetReg43And44(uint _dev_addr, int step)
        {
            //0x00->0x43
            uint _reg_Addr = 0x43;
            uint _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Reset Reg0x43 before new bit Fuse", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);

            //0xAA->0x44
            _reg_Addr = 0x44;
            _reg_Value = 0x0;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Reset Reg0x44 before new bit Fuse", true, step);
            else
            {
                return false;
            }

            //Delay 50ms
            Thread.Sleep(50);
            DisplayAutoTrimOperateMes("Delay 50ms", step);
            return true;
        }

        private void EnterNomalMode()
        {
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

            Delay(Delay_Sync);

            _reg_addr = 0x42;
            _reg_data = 0x04;

            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Enter Nomal Mode!");
                }
            }
            else
                DisplayOperateMes("I2C write failed, Enter Normal Mode Failed!", Color.Red);

            //Thread.Sleep(100);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            //rbt_signalPathSeting_AIn_EngT.Checked = true;

            //Delay(Delay_Sync);
            //rbt_withCap_Vout_EngT.Checked = true;
        }

        private void EnterTestMode()
        {
            Delay(Delay_Sync);
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);
            Delay(Delay_Sync);

            //set CONFIG without cap
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            Delay(Delay_Sync);
            //set CONFIG to VOUT
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            Delay(Delay_Sync);
            //Enter test mode
            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            if (oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data))
            {
                if (bAutoTrimTest)
                {
                    ;
                    //DisplayOperateMes("Enter test mode succeeded!");
                }
            }
            else
                DisplayOperateMes("Enter test mode failed!");
        }

        private void I2CWrite( uint addr, uint data)
        {
            uint _reg_addr = addr;
            uint _reg_data = data;
            if (oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data))
            {
                if (bAutoTrimTest)
                {
                    ;
                    //DisplayOperateMes("Enter test mode succeeded!");
                }
            }
            else
                DisplayOperateMes("Enter test mode failed!");
        }

        private bool RegisterWrite(int wrNum, uint[] data)
        {
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, 0x55, 0xAA);

            bool rt = false;
            if (data.Length < wrNum * 2)
                return false;

            if (bAutoTrimTest)
                DisplayOperateMes("Write In Data is:");

            for (int ix = 0; ix < wrNum; ix++)
            {
                rt = oneWrie_device.I2CWrite_Single(this.DeviceAddress, data[ix * 2], data[ix * 2 + 1]);
            }

            return rt;
        }

        private double CalcTargetXFromDetectiveY(double y)
        {
            /* y = k*x + b -> x = (y - b) / k */
            return (y - b_offset) / k_slope;
        }

        /// <summary>
        /// Use Y = kX +b to calculate the real vout X, and modify the index of precison table to
        /// find the best gain code. 
        /// ** Enter Current is 0A.
        /// ** Exit Current is also 0A.
        /// </summary>
        /// <returns></returns>
        private bool GainCodeCalcWithLoop()
        {
            double vout_0A_Convert;
            double vout_IP_Convert;
            double target_Gain1 = 0; //new
            double target_Gain2 = 0; //older
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            /* 1. Write Reg0x80 and enter normal mode */
            EnterTestMode();
            RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
            EnterNomalMode();

            /* 2.Get Vout@0A and Vout@IP */
            Vout_0A = AverageVout();
            DialogResult dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                return false;
            }
            Vout_IP = AverageVout();

            vout_0A_Convert = CalcTargetXFromDetectiveY(Vout_0A);
            vout_IP_Convert = CalcTargetXFromDetectiveY(Vout_IP);
            target_Gain1 = GainCalculate(vout_0A_Convert, vout_IP_Convert);
            target_Gain2 = target_Gain1;

            if (target_Gain1 == TargetGain_customer)
            {
                /* make sure exit current is 0A */
                dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    return false;
                }
                return true;
            }

            // if (testGain > targetGain) then index++
            bool IncreaseOrDecrease = (target_Gain1 > TargetGain_customer) ? true : false;

            while (true)
            {
                /* get the right value, will record the Ix_ForPrecisonGainCtrl and break loop */
                if ((target_Gain1 - TargetGain_customer) * (target_Gain2 - TargetGain_customer) <= 0)
                {
                    /* Judge which target gain is the best */
                    if (Math.Abs(target_Gain1 - TargetGain_customer) <= Math.Abs(target_Gain2 - TargetGain_customer)) //The new value is needed
                    {
                        break;
                    }
                    else // Back to older gain
                    {
                        /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                        if (!IncreaseOrDecrease)
                        {
                            if (Ix_ForPrecisonGainCtrl < 15)
                                Ix_ForPrecisonGainCtrl++;
                            else
                                break;
                        }
                        else
                        {
                            if (Ix_ForPrecisonGainCtrl > 0)
                                Ix_ForPrecisonGainCtrl--;
                            else
                                break;
                        }

                        /* 1. Write Reg0x80 and enter normal mode */
                        Reg80Value &= ~bit_op_mask;
                        Reg80Value |= Convert.ToUInt32(PreciseTable[1][Ix_ForPrecisonGainCtrl]);
                        EnterTestMode();
                        RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
                        EnterNomalMode();
                        /* 2.Get Vout@IP and Vout@0A */
                        Vout_IP = AverageVout();
                        dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            return false;
                        }
                        Vout_0A = AverageVout();
                        return true;
                    }
                }
                else
                {
                    /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                    if (IncreaseOrDecrease)
                    {
                        if (Ix_ForPrecisonGainCtrl < 15)
                            Ix_ForPrecisonGainCtrl++;
                        else
                            break;
                    }
                    else
                    {
                        if (Ix_ForPrecisonGainCtrl > 0)
                            Ix_ForPrecisonGainCtrl--;
                        else
                            break;
                    }

                    /* 1. Write Reg0x80 and enter normal mode */
                    Reg80Value &= ~bit_op_mask;
                    Reg80Value |= Convert.ToUInt32(PreciseTable[1][Ix_ForPrecisonGainCtrl]);
                    EnterTestMode();
                    RegisterWrite(1, new uint[2] { 0x80, Reg80Value });
                    EnterNomalMode();
                    /* 2.Get Vout@IP and Vout@0A */
                    Vout_IP = AverageVout();
                    dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        return false;
                    }
                    Vout_0A = AverageVout();

                    vout_0A_Convert = CalcTargetXFromDetectiveY(Vout_0A);
                    vout_IP_Convert = CalcTargetXFromDetectiveY(Vout_IP);
                    target_Gain2 = target_Gain1;    //backup history gain
                    target_Gain1 = GainCalculate(vout_0A_Convert, vout_IP_Convert);
                    dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        return false;
                    }
                }
            }

            /* make sure exit current is 0A */
            dr = MessageBox.Show(String.Format("Please Change Current To {0}A", 0), "Change Current", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel)
            {
                DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                return false;
            }
            return true;
        }

        private bool OffsetCalcWithLoop()
        {
            /* 如果小与2.5那么就从最后一行往上索引到102.40%(ix: 15 -> 8),如果大于2.5那么就从第一行向下索引到97.97%(ix:0 -> 7) */
            double delta_offset1 = 0; //new
            double delta_offset2 = 0; //older
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            /* 1. Write Reg0x81, 0x82, 0x83 and enter normal mode */
            EnterTestMode();
            RegisterWrite(3, new uint[6] { 0x81, Reg81Value, 0x82, Reg82Value, 0x83, Reg83Value});
            EnterNomalMode();

            /* 2.Get Vout@0A */
            Vout_0A = AverageVout();

            if (Vout_0A == b_offset)
            {
                return true;
            }

            delta_offset1 = Vout_0A - b_offset;
            delta_offset2 = delta_offset1;
            // if (Vout_0A > b_offset) then index++ ,else index--
            bool IncreaseOrDecrease = (delta_offset1 > 0) ? true : false;
            Ix_ForOffsetBTable = IncreaseOrDecrease ? 0 : 15;
            
            while(true)
            {
                /* get the right offset code */
                if (delta_offset1 * delta_offset2 <= 0)
                {
                    /* the latest one is the right code, do nothing then */
                    if (Math.Abs(delta_offset1) <= Math.Abs(delta_offset2))
                    {
                        break;
                    }
                    /* Back to older one */
                    else
                    {
                        if (!IncreaseOrDecrease)
                        {
                            if (Ix_ForOffsetBTable > 0)
                                Ix_ForOffsetBTable--;
                            else
                                break;
                        }
                        else
                        {
                            if (Ix_ForOffsetBTable < 15)
                                Ix_ForOffsetBTable++;
                            else
                                break;
                        }

                        Reg83Value &= ~bit_op_mask;
                        Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
                        /* 1. Write Reg0x83 and enter normal mode */
                        EnterTestMode();
                        RegisterWrite(1, new uint[2] { 0x83, Reg83Value });
                        EnterNomalMode();
                        /* 2.Get Vout@0A */
                        Vout_0A = AverageVout();
                        break;
                    }
                }
                /* Increase/decrease the Ix_ForPrecisonGainCtrl; update reg80; Get Vaout*/
                else
                {
                    if (IncreaseOrDecrease)
                    {
                        if (Ix_ForOffsetBTable < 7)
                            Ix_ForOffsetBTable++;
                        else
                            break;
                    }
                    else
                    {
                        if (Ix_ForOffsetBTable > 8)
                            Ix_ForOffsetBTable--;
                        else
                            break;
                    }
                    Reg83Value &= ~bit_op_mask;
                    Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
                    /* 1. Write Reg0x83 and enter normal mode */
                    EnterTestMode();
                    RegisterWrite(1, new uint[2] { 0x83, Reg83Value });
                    EnterNomalMode();
                    /* 2.Get Vout@0A */
                    Vout_0A = AverageVout();
                    delta_offset2 = delta_offset1;
                    delta_offset1 = Vout_0A - b_offset;
                }
            }

            return true;
        }

        private void RePower()
        {
            Delay(Delay_Sync);
            //1. Power Off
            PowerOff();

            Delay(Delay_Power);

            //2. Power On
            PowerOn();
        }

        private void PowerOff()
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_OFF))
            {
                if (bAutoTrimTest)
                {
                    //DisplayOperateMes("Power off succeeded!");
                }
            }
            else
                DisplayOperateMes("Power off failed!");
        }

        private void PowerOn()
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_ON))
            {
                if (bAutoTrimTest)
                {
                    //DisplayOperateMes("Power on succeeded!");
                }
            }
            else
                DisplayOperateMes("Power on failed!");
        }

        private void Delay(int time)
        {
            Thread.Sleep(time);
            if (bAutoTrimTest)
            {
                //DisplayOperateMes(String.Format("Delay {0}ms", time));
            }
        }

        private void StoreReg80ToReg83Value()
        {
            Reg80ToReg88Backup[0] = Reg80Value;
            Reg80ToReg88Backup[1] = Reg81Value;
            Reg80ToReg88Backup[2] = Reg82Value;
            Reg80ToReg88Backup[3] = reg83Value;
            Reg80ToReg88Backup[4] = Reg84Value;
            Reg80ToReg88Backup[5] = Reg85Value;
            Reg80ToReg88Backup[6] = Reg86Value;
            Reg80ToReg88Backup[7] = Reg87Value;
            Ix_ForRoughGainCtrlBackup = Ix_ForRoughGainCtrl;
        }

        private void RestoreReg80ToReg83Value()
        {
            Reg80Value = Reg80ToReg88Backup[0];
            Reg81Value = Reg80ToReg88Backup[1];
            Reg82Value = Reg80ToReg88Backup[2];
            Reg83Value = Reg80ToReg88Backup[3];
            Reg84Value = Reg80ToReg88Backup[4];
            Reg85Value = Reg80ToReg88Backup[5];
            Reg86Value = Reg80ToReg88Backup[6];
            Reg87Value = Reg80ToReg88Backup[7];
            Ix_ForRoughGainCtrl = Ix_ForRoughGainCtrlBackup;
        }

        private void MarginalReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x43;
            uint _reg_data = 0x06;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0E;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Marginal Read succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("I2C write failed, Marginal Read Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Marginal Read Setup succeeded!");
                }
            }
            else
                DisplayOperateMes("Marginal Read Setup failed!", Color.Red);
        }

        private void ReloadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x43;
            uint _reg_data = 0x0B;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reload succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("I2C write failed, Relaod Failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reload Setup succeeded!");
                }
            }
            else
                DisplayOperateMes("Reload Setup failed!", Color.Red);
        
        }

        private void SafetyReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x84;
            uint _reg_data = 0xC0;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x84;
            _reg_data = 0x00;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("2nd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x0E;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation); //delay 300ms

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);    //delay 300ms

            //_reg_addr = 0x84;
            //_reg_data = 0x0;
            //writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            //if (writeResult)
            //{
            //    if (bAutoTrimTest)
            //    {
            //        DisplayOperateMes("Safety Read Setup succeeded!\r\n");
            //    }
            //}
            //else
            //    DisplayOperateMes("Safety Read Setup failed!\r\n", Color.Red);
        }

        private void SafetyHighReadPreset()
        {
            EnterTestMode();

            uint _reg_addr = 0x84;
            uint _reg_data = 0xC0;
            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x84;
            _reg_data = 0x00;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("1st I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("2nd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation);

            _reg_addr = 0x43;
            _reg_data = 0x03;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            _reg_addr = 0x43;
            _reg_data = 0x0B;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (!writeResult)
            {
                DisplayOperateMes("3rd I2C write failed, Safety Read Failed!", Color.Red);
                return;
            }

            //Delay(Delay_Operation); //delay 300ms

            _reg_addr = 0x43;
            _reg_data = 0x0;
            writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                }
            }
            else
            {
                DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
                return;
            }

            Delay(Delay_Sync);    //delay 300ms
        }

        private bool BurstRead(uint _reg_addr_start, int num, uint[] _readBack_data)
        {
            Delay(Delay_Sync);
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            if (bAutoTrimTest)
                DisplayOperateMes("Read Out Data is:");

            if (oneWrie_device.I2CRead_Burst(this.DeviceAddress, _reg_addr_start, Convert.ToUInt32(num), _readBack_data) == 0)
            {
                for (int ix = 0; ix < num; ix++)
                {
                    if (bAutoTrimTest)
                        DisplayOperateMes(string.Format("Reg{0} = 0x{1}", ix, _readBack_data[ix].ToString("X2")));
                }
                return true;
            }
            else
            {
                DisplayOperateMes("Read Back Failed!");
                return false;
            }
        }

        private void BurstRead9(uint _reg_addr_start, int num, uint[] _readBack_data)
        {
            Delay(Delay_Sync);
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            if (bAutoTrimTest)
                DisplayOperateMes("Read Out Data is:");

            if (oneWrie_device.I2CRead_Burst(this.DeviceAddress, _reg_addr_start, Convert.ToUInt32(num), _readBack_data) == 0)
            {
                for (int ix = 0; ix < num; ix++)
                {
                    if (bAutoTrimTest)
                        DisplayOperateMes(string.Format("Reg{0} = 0x{1}", ix, _readBack_data[ix].ToString("X2")));
                }
                
            }
            else
            {
                DisplayOperateMes("Read Back Failed!");
                //return false;
            }
        }

        private void TrimFinish()
        {
            //DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            if(ProgramMode == 0)
                oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u);
            Delay(Delay_Sync);
            PowerOff();
            RestoreReg80ToReg83Value();
            DisplayOperateMes("Return!");
            //return;
        }

        private void PrintDutAttribute( ModuleAttribute sDUT)
        {
            DisplayOperateMes("<--------------------------->");
            DisplayOperateMes("IQ = " + sDUT.dIQ.ToString("F3"));
            DisplayOperateMes("dVoutIPNative = " + sDUT.dVoutIPNative.ToString("F3"));
            DisplayOperateMes("dVout0ANative = " + sDUT.dVout0ANative.ToString("F3"));
            DisplayOperateMes("dVoutIPMiddle = " + sDUT.dVoutIPMiddle.ToString("F3"));
            DisplayOperateMes("dVout0AMiddle = " + sDUT.dVout0AMiddle.ToString("F3"));
            DisplayOperateMes("dVoutIPTrimmed = " + sDUT.dVoutIPTrimmed.ToString("F3"));
            DisplayOperateMes("dVout0ATrimmed = " + sDUT.dVout0ATrimmed.ToString("F3"));
            DisplayOperateMes("iErrorCode = " + sDUT.iErrorCode.ToString("D2"));
            DisplayOperateMes("bDigitalCommFail = " + sDUT.bDigitalCommFail.ToString());
            DisplayOperateMes("bNormalModeFail = " + sDUT.bNormalModeFail.ToString());
            DisplayOperateMes("bReadMarginal = " + sDUT.bReadMarginal.ToString());
            DisplayOperateMes("bReadSafety = " + sDUT.bReadSafety.ToString());
            DisplayOperateMes("bTrimmed = " + sDUT.bTrimmed.ToString());
            DisplayOperateMes("<--------------------------->");

            //open file for prodcution record
            string filename = System.Windows.Forms.Application.StartupPath; ;
            filename += @"\Record.dat";

            int iFileLine = 0;

            StreamReader sr = new StreamReader(filename);
            while (sr.ReadLine() != null)
            {
                //sr.ReadLine();
                iFileLine++;
            }
            sr.Close();

            StreamWriter sw;
            if (iFileLine < 65535)
                sw = new StreamWriter(filename, true);
            else
                sw = new StreamWriter(filename, false);

            string msg;

            msg = string.Format("{0} {1}{2} {3} {4} {5} {6} {7}", sDUT.dIQ.ToString("F3"),
                sDUT.dVoutIPNative.ToString("F3"), sDUT.dVout0ANative.ToString("F3"),
                sDUT.dVoutIPMiddle.ToString("F3"), sDUT.dVout0AMiddle.ToString("F3"),
                sDUT.dVoutIPTrimmed.ToString("F3"), sDUT.dVout0ATrimmed.ToString("F3"),
                sDUT.iErrorCode.ToString("D2"));
            sw.WriteLine(msg);

            sw.Close();
        }

        private void InitSL910TabDataGrid()
        {
            DataTable dtable = new DataTable("Rock");
            //set columns names
            dtable.Columns.Add("ID", typeof(System.String));
            dtable.Columns.Add("RegAddr(Hex)", typeof(System.String));
            dtable.Columns.Add("RegValue(Hex)", typeof(System.String));
            dtable.Columns.Add("Discription", typeof(System.String));

            //dtable.Columns.Add("Read", typeof(System.Windows.Forms.Button));

            //DataRow [] row = new dtable.

            //Add Rows
            DataRow drow = dtable.NewRow();
            drow = dtable.NewRow();
            drow["ID"] = "1";
            drow["RegAddr(Hex)"] = "85";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "bits[3:0] - TC1[3:0];  \r\nbits[7:4] - TC2[3:0] ";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "2";
            drow["RegAddr(Hex)"] = "86";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "OFF_A[7:0] ";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "3";
            drow["RegAddr(Hex)"] = "87";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "bit0 - EXT_Hall(C) / 4 Halls(D);   \r\nbit1 - OFF_Trim;  \r\nbits[3:2] - TCTH[1:0];    \r\nbits[7:4] - S1_A[3:0] ";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "4";
            drow["RegAddr(Hex)"] = "88";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "bits[4:0] - RCCSEL[4:0];   \r\nbit5 - Dis_Chop;  \r\nbit6 - INC_1x;    \r\nbit7 - INC_4x ";
            dtable.Rows.Add(drow);

            

            drow = dtable.NewRow();
            drow["ID"] = "5";
            drow["RegAddr(Hex)"] = "45";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "bits[1:0] - fuse_Rsel[1:0], fuse read compare Resistor selection; 0= ? 1=? 2=? 3=?;"
                + "\r\nbit2 - DIG_CLK_dis, After  master bit is blow, 1=disable the digital clock;"
                + "\r\nbit3 - Hall_pdb_force, force Hall power down; 1= force (could use during fuse program);"
                + "\r\nbit4 - LDO_5V_sel_b, use internal LDO 5V (from +15V) as fuse PVDD while fuse_supply_en=1"
                + "\r\nbit5 - Ext_5V_sel, use external 5V (at +15V pin) as fuse PVDD  while fuse_supply_en=1"
                + "\r\nbit6 - Pvdd_sel, use PVDD pin as fuse PVDD "
                + "\r\nbit7 - nrst_fuse_bypass, bypass +/-15V reset for fuse";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "6";
            drow["RegAddr(Hex)"] = "46";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "ana_test[2:0] - bypass internal signal to test pins," 
                + "\r\n    3'd1:VDD_2V5 to ANA_T1;      3'd2:iHall to ANA_T1;       3'd3:IOUT to ANA_T1; " 
                + "\r\n    3'd4:vinp to ANA_T1, vinn to ANA_T2, iHall to ANA_T3;        3'd5:op4 to ANA_T1;" 
                + "\r\n    3'd6:op5 to ANA_T1, on5 to ANA_T2;       3'd7:VSS_2v5 to ANA_T1"
                + "\r\nbit3 - chop_amp_dishpf"
                + "\r\nbits[5:4] - osc output clk trim, 2'b0: 5M/8; 2'b1: 5MHz/6; 2'b2: 5MHz/4"
                + "\r\nbit6 - ldo_5v_m output, 1'b0: 5V, 1'b1: 5.3V"
                + "\r\nbit7 - clk_gen_sel, 1'b0: old clkgen, 1'b1: new clkgen(could use ana_test<5:4> to select freq";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "7";
            drow["RegAddr(Hex)"] = "89";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "bits[3:0] - Post_Trim;\r\nbits[7:4] - Master ";
            dtable.Rows.Add(drow);

            //drow = dtable
            //SL910_Tab_DataGridView.RowHeadersWidth[2] = 200;
            

            SL910_Tab_DataGridView.DataSource = dtable;

            SL910_Tab_DataGridView.Columns[1].Width = 50;
            SL910_Tab_DataGridView.Columns[2].Width = 90;
            SL910_Tab_DataGridView.Columns[3].Width = 90;
            //SL910_Tab_DataGridView.Columns[4].Width = 480;
            SL910_Tab_DataGridView.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            SL910_Tab_DataGridView.Columns[1].ReadOnly = true;
            //SL910_Tab_DataGridView.Columns[2].ReadOnly = true;
            SL910_Tab_DataGridView.Columns[4].ReadOnly = true;

            //SL910_Tab_DataGridView.Columns[0].ReadOnly = true;
            //SL910_Tab_DataGridView.Columns[1].ReadOnly = true;
            //SL910_Tab_DataGridView.Columns[3];

            SL910_Tab_DataGridView.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            SL910_Tab_DataGridView.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            SL910_Tab_DataGridView.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //SL910_Tab_DataGridView.Columns[0].FillWeight = 50;
            SL910_Tab_DataGridView.Columns[1].DefaultCellStyle.BackColor = Color.LightGray;
            SL910_Tab_DataGridView.Columns[2].DefaultCellStyle.BackColor = Color.LightGray;
            SL910_Tab_DataGridView.Columns[4].DefaultCellStyle.BackColor = Color.LightGray;

            //SL910_Tab_DataGridView. = false;
            SL910_Tab_DataGridView.RowsDefaultCellStyle.WrapMode  = DataGridViewTriState.True;
            SL910_Tab_DataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            SL910_Tab_DataGridView.Update();

            //dock datagridview
            SL910_Tab_DataGridView.Dock = DockStyle.Fill;

            //dock richtextbox
            //richTextBox1.Dock = DockStyle.Fill;

        }

        private void InitSL620TabDataGrid()
        {
            DataTable dtable = new DataTable("Rock");
            //set columns names
            dtable.Columns.Add("ID", typeof(System.String));
            dtable.Columns.Add("RegAddr(Hex)", typeof(System.String));
            dtable.Columns.Add("RegValue(Hex)", typeof(System.String));
            dtable.Columns.Add("Discription", typeof(System.String));

            //dtable.Columns.Add("Read", typeof(System.Windows.Forms.Button));

            //DataRow [] row = new dtable.

            //Add Rows
            DataRow drow = dtable.NewRow();
            drow = dtable.NewRow();
            drow["ID"] = "1";
            drow["RegAddr(Hex)"] = "80";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bit7# - INC_4X_HALL_I;  " 
                                + "\r\n#bit6# - INC_1X_HALL_I;" 
                                + "\r\n#bit[5:4]# - SEL_SENSOR[1:0];" 
                                + "\r\n#bit3# - SEL_SW_MODE_B;  #bit2# - SEL_SW_MODE_A;" 
                                + "\r\n#bit1# - SEL_VR_SOURSE;  #bit0# - SEL_IR_SOURSE;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "2";
            drow["RegAddr(Hex)"] = "81";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bit[7:4]# - S1_A[3:0];  " + "\r\n#bit[3:2]# - TRIM_VBG[1:0];  " + "\r\n#bit[1:0]# - TCth[1:0]";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "3";
            drow["RegAddr(Hex)"] = "82";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bit[7:4]# - TC2[3:0];  #bit[3:0]# - TC1[3:0]";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "4";
            drow["RegAddr(Hex)"] = "83";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:6]# - FAST_STARTUP[1:0];   " 
                                + "\r\n#bits[5:4]# - MULTI_DRIVE_MODE[1:0];" 
                                + "\r\n#bit3# - DIS_CHOP_CK;  #bit2# - S2_DOUBLE;" 
                                +"\r\n#bit1# - S3_OUT_DRV;  #bit0# - SEL_BIG_CAP;";
            dtable.Rows.Add(drow);



            drow = dtable.NewRow();
            drow["ID"] = "5";
            drow["RegAddr(Hex)"] = "84";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:5]# - RCC_SEL_SET1[2:0];" +   "\r\n#bits[4:0]# - TRIM_VREF1_SET1[4:0];";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "6";
            drow["RegAddr(Hex)"] = "85";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:5]# - RCC_SEL_SET1[5:3];   " + "\r\n#bits[4:0]# - TRIM_VREF2_SET1[4:0];";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "7";
            drow["RegAddr(Hex)"] = "86";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:5]# - RCC_SEL_SET2[2:0];   " + "\r\n#bits[4:0]# - TRIM_VREF1_SET2[4:0];";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "8";
            drow["RegAddr(Hex)"] = "87";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:5]# - RCC_SEL_SET2[5:3];   " + "\r\n#bits[4:0]# - TRIM_VREF2_SET2[4:0];";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "9";
            drow["RegAddr(Hex)"] = "88";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[3:2]# - MASTER[1:0];   " + "\r\n#bit1# - POST_TRIM;   " + "\r\n#bit0# - OTP_MODE;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "10";
            drow["RegAddr(Hex)"] = "42";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bit7# - DIGITAL_CLK_DIS;  #bit6# - ANA_TEST_EN;"
                                + "\r\n#bit[5:4]# - ANA_TEST_SEL[1:0];"
                                + "\r\n#bit[3:2]# - FUSE_R_SEL[1:0];"
                                + "\r\n#bit1# - NORMAL_MODE;  #bit0# - DATA_PIN_DIS;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "11";
            drow["RegAddr(Hex)"] = "43";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bit3# - FUSE_READ_LATCH;  #bit2# - FUSE_READ;"
                                + "\r\n#bit1# - FUSE_SUPPLY_EN;  #bit0# - FUSE_CLK_EN;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "12";
            drow["RegAddr(Hex)"] = "4D";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:0]# - DEV_ID_0x61;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "13";
            drow["RegAddr(Hex)"] = "4E";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:0]# - PROG_PILOT;";
            dtable.Rows.Add(drow);

            drow = dtable.NewRow();
            drow["ID"] = "14";
            drow["RegAddr(Hex)"] = "4F";
            drow["RegValue(Hex)"] = "00";
            drow["Discription"] = "#bits[7:0]# - SOFT_RESET;";
            dtable.Rows.Add(drow);


            SL620_Tab_DataGridView.DataSource = dtable;

            SL620_Tab_DataGridView.Columns[1].Width = 50;
            SL620_Tab_DataGridView.Columns[2].Width = 90;
            SL620_Tab_DataGridView.Columns[3].Width = 90;
            //SL620_Tab_DataGridView.Columns[4].Width = 480;
            SL620_Tab_DataGridView.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            SL620_Tab_DataGridView.Columns[1].ReadOnly = true;
            //SL910_Tab_DataGridView.Columns[2].ReadOnly = true;
            SL620_Tab_DataGridView.Columns[4].ReadOnly = true;

            //SL620_Tab_DataGridView.Columns[0].ReadOnly = true;
            //SL620_Tab_DataGridView.Columns[1].ReadOnly = true;
            //SL620_Tab_DataGridView.Columns[3];

            SL620_Tab_DataGridView.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            SL620_Tab_DataGridView.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            SL620_Tab_DataGridView.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //SL620_Tab_DataGridView.Columns[0].FillWeight = 50;
            SL620_Tab_DataGridView.Columns[1].DefaultCellStyle.BackColor = Color.LightGray;
            SL620_Tab_DataGridView.Columns[2].DefaultCellStyle.BackColor = Color.LightGray;
            SL620_Tab_DataGridView.Columns[4].DefaultCellStyle.BackColor = Color.LightGray;

            //SL910_Tab_DataGridView. = false;
            SL620_Tab_DataGridView.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            SL620_Tab_DataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            SL620_Tab_DataGridView.Update();

            //dock datagridview
            SL620_Tab_DataGridView.Dock = DockStyle.Fill;
        }

        private void InitChar910TabDataGrid()
        {
            try
            {
                string filename = System.Windows.Forms.Application.StartupPath;
                OpenFileDialog openProj = new OpenFileDialog();
                openProj.Title = "open a script.";
                openProj.Filter = "SCT(.sct)|*.sct";
                //importFile.RestoreDirectory = true;
                if (openProj.ShowDialog() == DialogResult.OK)
                {
                    filename = openProj.FileName;
                    //DeserializeMethod(filename);
                }
                else
                    return;

                //string filename = System.Windows.Forms.Application.StartupPath;
                //filename += @"\Script.sct";

                StreamReader sr = new StreamReader(filename);
                string[] tempMsg = {"","","",""};
                string[] msg;
                int scriptStepCount = 0;

                this.txt_Char910_ScriptName.Text = sr.ReadLine().ToString();
                Char910_Tab_DataGridView.Rows.Clear();

                while (!sr.EndOfStream)
                {
                    msg = sr.ReadLine().Split(",".ToCharArray());
                    

                    if (msg[0] != "")
                    {
                        for (int i = 0; i < msg.Length; i++)
                            tempMsg[i] = msg[i];
                        for (int i = msg.Length; i < tempMsg.Length; i++)
                            tempMsg[i] = "null";

                        scriptStepCount++;
                        this.Char910_Tab_DataGridView.Rows.Add(CurrentSensorV3.Properties.Resources.PROCESS_READY,
                            "Step" + scriptStepCount.ToString(), 
                            tempMsg[0],
                            tempMsg[1],
                            tempMsg[2],
                            tempMsg[3],
                            "null",
                            "View Details"
                        );
                    }

                    if ((scriptStepCount - 1) % 2 == 1)
                        Char910_Tab_DataGridView.Rows[scriptStepCount - 1].DefaultCellStyle.BackColor = Color.LightGray;

                }
                sr.Close();
            }
            catch
            {
                MessageBox.Show("Load config file failed, please choose correct file!");
            }

            Char910_Tab_DataGridView.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Char910_Tab_DataGridView.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            Char910_Tab_DataGridView.Update();
        }

        private void SL610CoarseOffsetUp()
        {
            if (Ix_ForOffsetATable == 0)
                Ix_ForOffsetATable = 15;
            else if (Ix_ForOffsetATable == 8)
                DisplayOperateMes("Reach to Max Coarse Offset!", Color.DarkRed);
            else
                Ix_ForOffsetATable--;

            bit_op_mask = bit7_Mask;
            Reg81Value &= ~bit_op_mask;
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask;
            Reg82Value &= ~bit_op_mask;

            Reg81Value |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);
        }

        private void SL610CoarseOffsetDown()
        {
            if (Ix_ForOffsetATable == 15)
                Ix_ForOffsetATable = 0;
            else if (Ix_ForOffsetATable == 7)
                DisplayOperateMes("Reach to Min Coarse Offset!", Color.DarkRed);
            else
                Ix_ForOffsetATable++;

            bit_op_mask = bit7_Mask;
            Reg81Value &= ~bit_op_mask;
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask;
            Reg82Value &= ~bit_op_mask;

            Reg81Value |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);
        }

        private void SL610FineOffsetUp()
        {
            if (Ix_ForOffsetBTable == 0)
                Ix_ForOffsetBTable = 15;
            else if (Ix_ForOffsetBTable == 8)
                DisplayOperateMes("Reach to Max Fine Offset!", Color.DarkRed);
            else
                Ix_ForOffsetBTable--;

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
        }

        private void SL610FineOffsetDown()
        {
            if (Ix_ForOffsetBTable == 15)
                Ix_ForOffsetBTable = 0;
            else if (Ix_ForOffsetBTable == 7)
                DisplayOperateMes("Reach to Min Fine Offset!", Color.DarkRed);
            else
                Ix_ForOffsetBTable++;

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
        }

        private void SL610CoarseGainUp()
        {
            btn_GainCtrlPlus_PreT_Click(null, null);
        }

        private void SL610CoarseGainDown()
        {
            btn_GainCtrlMinus_PreT_Click(null, null);
        }

        private void SL610FineGainUp()
        {
            if (Ix_ForPrecisonGainCtrl == 0)
                DisplayOperateMes("Reach to Max fine Gain!", Color.DarkRed);
            else
            {
                Ix_ForPrecisonGainCtrl--;

                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                Reg80Value &= ~bit_op_mask;
                Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            }
        }

        private void SL610FineGainDown()
        {
            if (Ix_ForPrecisonGainCtrl == 31)
                DisplayOperateMes("Reach to Min fine Gain!", Color.DarkRed);
            else
            {
                Ix_ForPrecisonGainCtrl++;

                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                Reg80Value &= ~bit_op_mask;
                Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            }
        }



        private void SL910_Tab_DataGridView_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            

            Rectangle rectangle = new Rectangle(e.RowBounds.Location.X,
                e.RowBounds.Location.Y,
                SL910_Tab_DataGridView.RowHeadersWidth - 4,
                e.RowBounds.Height);

            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(),
                SL910_Tab_DataGridView.RowHeadersDefaultCellStyle.Font,
                rectangle,
                SL910_Tab_DataGridView.RowHeadersDefaultCellStyle.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        #endregion Methods

        #region Events

        private void contextMenuStrip_Copy_MouseUp(object sender, MouseEventArgs e)
        {
            this.txt_OutputLogInfo.Copy();
        }

        private void contextMenuStrip_Paste_Click(object sender, EventArgs e)
        {
            this.txt_OutputLogInfo.Paste();
        }

        private void contextMenuStrip_Clear_MouseUp(object sender, MouseEventArgs e)
        {
            this.txt_OutputLogInfo.Text = null;
            //解决Scroll Bar的刷新问题。
            this.txt_OutputLogInfo.ScrollBars = RichTextBoxScrollBars.None;
            this.txt_OutputLogInfo.ScrollBars = RichTextBoxScrollBars.Both;
        }

        private void contextMenuStrip_SelAll_Click(object sender, EventArgs e)
        {
            this.txt_OutputLogInfo.SelectAll();
        }

        private void txt_TargetGain_TextChanged(object sender, EventArgs e)
        {
            try
            {
                //temp = (4500d - 2000d) / double.Parse(this.txt_TargetGain.Text);
                if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                    return;
                TargetGain_customer = double.Parse((sender as TextBox).Text);
                //TargetGain_customer = (double.Parse((sender as TextBox).Text) * 2000d)/IP;
            }
            catch
            {
                string tempStr = string.Format("Target gain set failed, will use default value {0}", this.TargetGain_customer);
                DisplayOperateMes(tempStr, Color.Red);
            }
            finally
            {
                //TargetGain_customer = TargetGain_customer;      //Force to update text to default.
            }

            //double temp = 2000d / TargetGain_customer;
            //this.IP = temp;  
            //this.txt_IP_EngT.Text = temp.ToString();
            //this.txt_IP_PreT.Text = temp.ToString();
            //this.txt_IP_AutoT.Text = temp.ToString();
        }

        private void btn_PowerOn_OWCI_ADC_Click(object sender, EventArgs e)
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_ON))
                DisplayOperateMes("Power On!");
            else
                DisplayOperateMes("Power on failed!");
        }

        private void btn_PowerOff_OWCI_ADC_Click(object sender, EventArgs e)
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_POWER_OFF))
                DisplayOperateMes("Power off succeeded!");
            else
                DisplayOperateMes("Power off failed!");
            //uint[] uTest = new uint[16];
            //for (uint i = 0; i < 16; i++)
            //{
            //    uTest[i] = i;
            //}

            //MultiSiteDisplayResult(uTest);


        }

        private void btn_enterNomalMode_Click(object sender, EventArgs e)
        {
            EnterNomalMode();
        }
        
        private void btn_ADCReset_Click(object sender, EventArgs e)
        {
            if (!oneWrie_device.ADCReset())
                DisplayOperateMes("ADC Reset Failed!", Color.Red);
            else
                DisplayOperateMes("ADC Reset succeeded!");
        }

        private void btn_CalcGainCode_EngT_Click(object sender, EventArgs e)
        {
            //Rough Trim
            string baseMes = "Calculate Gain Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }

            double testGain = GainCalculate();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Test Gain = " + testGain.ToString());
            }

            double gainTuning = 100 * GainTuningCalc_Customer(testGain, TargetGain_customer);   //计算修正值，供查表用
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Ideal Gain = " + gainTuning.ToString("F4") + "%");
            }

            Ix_ForPrecisonGainCtrl = LookupPreciseGain(gainTuning, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precise Gain Index = " + Ix_ForPrecisonGainCtrl.ToString() +
                    ";Choosed Gain = " + PreciseTable_Customer[0][Ix_ForPrecisonGainCtrl].ToString() + "%");
            }

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg1 Value = " + Reg80Value.ToString() +
                    "(+ 0x" + Convert.ToInt32(PreciseTable_Customer[1][Ix_ForPrecisonGainCtrl]).ToString("X") + ")");
            }
        }

        private bool MultiSiteOffsetAlg(uint[] reg_TMS )
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            reg_TMS[0] += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            reg_TMS[1] += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x81 Value = 0x" + reg_TMS[0].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg0x82 Value = 0x" + reg_TMS[1].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            reg_TMS[2] &= ~bit_op_mask;
            reg_TMS[2] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg0x83 Value = 0x" + reg_TMS[2].ToString("X2") + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
            return true;
        }

        private void btn_offset_Click(object sender, EventArgs e)
        {
            string baseMes = "Offset Trim Operation:";
            if (bAutoTrimTest)
            {
                DisplayOperateMes(baseMes);
            }
            double offsetTuning = 100 * OffsetTuningCalc_Customer();
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Lookup offset = " + offsetTuning.ToString("F4") + "%");
            }

            Ix_ForOffsetATable = LookupOffset(ref offsetTuning, OffsetTableA_Customer);
            //offsetTuning = offsetTuning / OffsetTableA_Customer[0][Ix_ForOffsetATable]; 
            Ix_ForOffsetBTable = LookupOffset(ref offsetTuning, OffsetTableB_Customer);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Offset TableA chose Index = " + Ix_ForOffsetATable.ToString() +
                    ";Choosed OffsetA = " + OffsetTableA_Customer[0][Ix_ForOffsetATable].ToString("F4"));
                DisplayOperateMes("Offset TableB chose Index = " + Ix_ForOffsetBTable.ToString() +
                    ";Choosed OffsetB = " + OffsetTableB_Customer[0][Ix_ForOffsetBTable].ToString("F4"));
            }

            Reg81Value += Convert.ToUInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]);
            Reg82Value += Convert.ToUInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg2 Value = " + Reg81Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[1][Ix_ForOffsetATable]).ToString("X") + ")");
                DisplayOperateMes("Reg3 Value = " + Reg82Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableA_Customer[2][Ix_ForOffsetATable]).ToString("X") + ")");
            }

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("Reg4 Value = " + Reg83Value.ToString() + "(+ 0x" + Convert.ToInt32(OffsetTableB_Customer[1][Ix_ForOffsetBTable]).ToString("X") + ")");
            }
        }

        private void btn_writeFuseCode_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;


            bool fuseMasterBit = false;
            DialogResult dr = MessageBox.Show("Do you want to Fuse master bit?", "Fuse master bit??", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.Cancel)
                return;
            else if (dr == System.Windows.Forms.DialogResult.Yes)
                fuseMasterBit = true;

            try
            {
                string temp;
                uint _dev_addr = this.DeviceAddress;

                //Enter test mode
                uint _reg_addr = 0x55;
                uint _reg_data = 0xAA;
                oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                //Reg80
                temp = this.txt_reg80_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                uint _reg_Addr = 0x80;
                uint _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg1(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg1 Failed!", Color.Red);

                //Reg81
                temp = this.txt_reg81_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x81;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg2(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg2 Failed!", Color.Red);

                //Reg82
                temp = this.txt_reg82_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x82;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg3(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg3 Failed!", Color.Red);

                //Reg83
                temp = this.txt_reg83_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                _reg_Addr = 0x83;
                _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                    DisplayOperateMes("Write Reg4(0x" + _reg_Value.ToString("X") + ") succeeded!");
                else
                    DisplayOperateMes("Write Reg4 Failed!", Color.Red);

                if (fuseMasterBit)
                {
                    //Reg84
                    _reg_Addr = 0x84;
                    _reg_Value = 0x07;

                    if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                        DisplayOperateMes("Master bit fused succeeded!");
                    else
                        DisplayOperateMes("Master bit fused Failed!", Color.Red);
                }

            }
            catch
            {
                MessageBox.Show("Write data format error!");
            }
        }

        private void txt_reg80_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_reg81_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_reg82_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string temp = this.txt_reg82_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                if (temp.Length > 2)
                    temp = temp.Substring(0, 2);
                uint regValue = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

                if (Reg82Value == regValue)
                    return;
                else
                {
                    this.Reg82Value = regValue;
                    DisplayOperateMes("Enter Reg3 value succeeded!");
                }
            }
            catch
            {
                DisplayOperateMes("Enter Reg3 value failed!", Color.Red);
            }
            finally
            {
                this.txt_reg82_EngT.Text = "0x" + this.Reg82Value.ToString("X2");
            }
        }

        private void txt_reg83_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string temp = this.txt_reg83_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                if (temp.Length > 2)
                    temp = temp.Substring(0, 2);
                uint regValue = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
                if (Reg83Value == regValue)
                    return;
                else
                {
                    this.Reg83Value = regValue;
                    DisplayOperateMes("Enter Reg3 value succeeded!");
                }
            }
            catch
            {
                DisplayOperateMes("Enter Reg value failed!", Color.Red);
            }
            finally
            {
                this.txt_reg83_EngT.Text = "0x" + this.Reg83Value.ToString("X2");
            }
        }

        private void txt_RegValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt_Regx = sender as TextBox;
            e.KeyChar = Convert.ToChar(e.KeyChar.ToString().ToUpper());
            string str = "\r\b0123456789abcdefABCDEF";//This will allow the user to enter numeric HEX values only.

            e.Handled = !(str.Contains(e.KeyChar.ToString()));

            if (e.Handled)
                return;
            else
            {
                if (e.KeyChar.ToString() == "\r")
                {
                    RegTextChangedDisplay(txt_Regx);
                    txt_Regx.SelectionStart = txt_Regx.Text.Length;
                    //try
                    //{
                    //    //string temp = txt_Regx.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                    //    //uint _reg_value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
                    //    RegTextChangedDisplay(txt_Regx);
                    //}
                    //catch
                    //{
                    //    txt_Regx.Text = this.
                    //}
                }
            }

            #region Comment out
            //if (txt_Regx.Text.Length >= 2)
            //{
            //    if (txt_Regx.Text.StartsWith("0x") | txt_Regx.Text.StartsWith("0X"))
            //    {
            //        if (txt_Regx.Text.Length >= 4)
            //        {
            //            if ((e.KeyChar == '\b') | ((txt_Regx.SelectionLength >= 1) & (txt_Regx.SelectionStart >= 2)) |
            //                           (txt_Regx.SelectionLength == txt_Regx.Text.Length))
            //            {
            //                e.Handled = !(str.Contains(e.KeyChar.ToString()));
            //                RegTextChangedDisplay(txt_Regx);
            //                return;
            //            }
            //            else
            //            {
            //                e.Handled = true;
            //                return;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (e.KeyChar != '\b' | (txt_Regx.SelectionLength == txt_Regx.Text.Length))
            //        {
            //            e.Handled = true;
            //            txt_Regx.Text = "0x" + txt_Regx.Text;
            //            RegTextChangedDisplay(txt_Regx);
            //            return;
            //        }

            //    }
            //}
            //e.Handled = !(str.Contains(e.KeyChar.ToString()));
            //if (e.Handled | txt_Regx.Text.StartsWith("0x") | txt_Regx.Text.StartsWith("0X"))
            //{
            //    return;
            //}
            //else
            //{
            //    txt_Regx.Text = "0x" + txt_Regx.Text;
            //    RegTextChangedDisplay(txt_Regx);
            //    txt_Regx.SelectionStart = txt_Regx.Text.Length;
            //}
            #endregion Comment out
        }

        private void RegTextChangedDisplay(TextBox txtReg)
        {
            if ((txtReg == this.txt_reg80_EngT) | (txtReg == this.txt_Reg80_PreT))
                this.txt_reg80_TextChanged(null, null);
            else if ((txtReg == this.txt_reg81_EngT) | (txtReg == this.txt_Reg81_PreT))
                this.txt_reg81_TextChanged(null, null);
            else if ((txtReg == this.txt_reg82_EngT) | (txtReg == this.txt_Reg82_PreT))
                this.txt_reg82_TextChanged(null, null);
            else if ((txtReg == this.txt_reg83_EngT) | (txtReg == this.txt_Reg83_PreT))
                this.txt_reg83_TextChanged(null, null);
        }

        private void rbt_5V_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_VDD_5V_EngT.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);

            string message;
            if (rbt_VDD_5V_EngT.Checked)
                message = "VDD chose 5V";
            else
                message = "VDD chose external power";

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_withCap_Vout_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_withCap_Vout_EngT.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);

            string message;
            if (rbt_withCap_Vout_EngT.Checked)
                message = "Vout with Cap set";
            else
                message = "Vout without Cap set";

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_withCap_Vref_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            if (rbt_withCap_Vref.Checked)
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
            else
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITHOUT_CAP);

            string message;
            if (rbt_withCap_Vref.Checked)
                message = "Vref with Cap set";
            else
                message = "Vref without Cap set";
            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbt_signalPathSeting_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            string message;
            //L-Vout
            if (rbt_signalPathSeting_Vout_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                message = "Vout to VIn set";
            }
            else if (rbt_signalPathSeting_Vout_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
                message = "Vout to CONFIG set";
            }
            //L-Vref
            else if (rbt_signalPathSeting_Vref_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
                message = "Vref to VIn set";
            }
            else if (rbt_signalPathSeting_Vref_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VREF);
                message = "Vref to CONFIG set";
            }
            //L-VCS
            else if (rbt_signalPathSeting_VCS_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);
                message = "VCS to VIn set";
            }
            else if (rbt_signalPathSeting_VCS_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VCS);
                message = "VSC to CONFIG set";
            }
            //L-510out
            else if (rbt_signalPathSeting_510Out_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_510OUT);
                message = "510out to VIn set";
            }
            else if (rbt_signalPathSeting_510Out_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_510OUT);
                message = "510out to CONFIG set";
            }
            //L-Mout
            else if (rbt_signalPathSeting_Mout_EngT.Checked && rbt_signalPathSeting_AIn_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
                message = "Mout to VIn set";
            }
            else if (rbt_signalPathSeting_Mout_EngT.Checked && rbt_signalPathSeting_Config_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_MOUT);
                message = "Mout to CONFIG set";
            }
            else
            {
                message = "Signal path routing failed!";
                return;
            }

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void rbtn_CSResistorByPass_EngT_CheckedChanged(object sender, EventArgs e)
        {
            bool setResult;
            string message;
            if (rbtn_CSResistorByPass_EngT.Checked)
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_BYPASS_CURRENT_SENCE);
                message = "Vout to VIn set";
            }
            else
            {
                setResult = oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);
                message = "Vout to CONFIG set";
            }

            if (setResult)
            {
                if (bAutoTrimTest)
                {
                    message += " succeeded!";
                    DisplayOperateMes(message);
                }
            }
            else
            {
                if (bAutoTrimTest)
                {
                    message += " Failed!";
                    DisplayOperateMes(message, Color.Red);
                }
            }
        }

        private void btn_burstRead_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            EnterTestMode();

            //Read Back 0x80~0x85
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[5];
            BurstRead(_reg_addr_start, 5, _readBack_data);


            //uint data = 0;
            //data = oneWrie_device.I2CRead_Single(this.DeviceAddress, 0x80);
            //DisplayOperateMes(string.Format("Reg0x80 = 0x{0}", data.ToString("X2")));
        }

        private void btn_burstRead(int length)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            EnterTestMode();

            //Read Back 0x80~0x85
            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[9];
            BurstRead(_reg_addr_start, 5, _readBack_data);

            //return _readBack_data;
        }

        private void btn_MarginalRead_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            MarginalReadPreset();
        }

        private void btn_SafetyRead_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            SafetyReadPreset();
        }

        private void btn_Reload_Click(object sender, EventArgs e)
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            try
            {
                uint _reg_addr = 0x55;
                uint _reg_data = 0xAA;
                oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

                _reg_addr = 0x43;
                _reg_data = 0x0B;

                bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
                //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
                if (writeResult)
                    DisplayOperateMes("Reload succeeded!");
                else
                    DisplayOperateMes("I2C write failed, Reload Failed!", Color.Red);

                //Delay 100ms
                Thread.Sleep(100);
                DisplayOperateMes("Delay 100ms");

                _reg_addr = 0x43;
                _reg_data = 0x0;

                writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
                //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
                if (writeResult)
                    DisplayOperateMes("Reset Reg0x43 succeeded!");
                else
                    DisplayOperateMes("Reset Reg0x43 failed!", Color.Red);
            }
            catch
            {
                DisplayOperateMes("Reload Failed!", Color.Red);
            }
        }
        
        private void numUD_TargetGain_Customer_ValueChanged(object sender, EventArgs e)
        {
            targetGain_customer = (double)(sender as NumericUpDown).Value;
        }

        private void numUD_IPxForCalc_Customer_ValueChanged(object sender, EventArgs e)
        {
            StrIPx_Auto = (sender as NumericUpDown).Value.ToString("F1") + "A";
            selectedCurrent_Auto = (double)(sender as NumericUpDown).Value;
        }

        private void AutoTrimTab_Enter(object sender, EventArgs e)
        {
            //Backup value for autotrim
            StoreReg80ToReg83Value();
        }


        //bool bAutoTrimTest = false;
        private void btn_AutomaticaTrim_Click(object sender, EventArgs e)
        {
            #region Check HW connection
            if (!bUsbConnected)
            {
                DisplayOperateMes("Please Confirm HW Connection!", Color.Red);
                return;
            }
            #endregion

            #region UART Initialize
            if (ProgramMode == 0)
            {
                //if (ProgramMode == 0 && bUartInit == false)
                //{

                //UART Initialization
                if (oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize succeeded!");
                else
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Power);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //1. Current Remote CTL
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                    DisplayOperateMes("Set Current Remote succeeded!");
                else
                    DisplayOperateMes("Set Current Remote failed!");

                //Delay 300ms
                //Thread.Sleep(300);
                Delay(Delay_Power);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //2. Current On
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    DisplayOperateMes("Set Current to IP succeeded!");
                else
                    DisplayOperateMes("Set Current to IP failed!");

                //Delay 300ms
                Delay(Delay_Power);
                //DisplayOperateMes("Delay 300ms");

                //3. Set Voltage
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
                else
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));


                //Delay 300ms
                Delay(Delay_Power);
                //DisplayOperateMes("Delay 300ms");

                //bUartInit = true;
                //}
            }
            #endregion UART Initialize

            #region Trim Routines
            DateTime StartTime = System.DateTime.Now;

            if (this.cmb_Module_PreT.SelectedItem.ToString() == "5V" || this.cmb_Module_PreT.SelectedItem.ToString() == "3.3V")
            {
                if (SocketType == 0)
                    AutomaticaTrim_5V_SingleSite();
                else if (SocketType == 1)
                    AutomaticaTrim_5V_DiffMode();
                else if (SocketType == 2)
                {

                    Reg80Value = 0x05;
                    Reg81Value = 0x0F;
                    Reg82Value = 0x51;
                    Reg83Value = 0x30;
                    Reg84Value = 0x00;
                    Reg85Value = 0x00;
                    Reg86Value = 0x00;
                    Reg87Value = 0x00;

                    AutoTrim_SL620_SingleEnd();
                }
                else
                    return;
            }

            DateTime StopTime = System.DateTime.Now;
            TimeSpan ts = StopTime - StartTime;

            DisplayOperateMes("Program Time Span = " + ts.Seconds.ToString() + "s");
            #endregion

        }
        
        private void AutomaticaTrim_5V_SingleSite()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            double dGainTestMinusTarget = 1;
            double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.lbl_passOrFailed.ForeColor = Color.Black;
            this.lbl_passOrFailed.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;

                MultiSiteRoughGainCodeIndex[i] = Ix_ForRoughGainCtrl;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            if(!oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V))
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            RePower();
            Delay(Delay_Sync);
            this.lbl_passOrFailed.Text = "Trimming";
            /* Get module current */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS);          
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE);

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;

            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;

            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                PowerOff();
                MessageBox.Show(String.Format("电流偏低，检查模组是否连接！"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "短路!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region Saturation judgement
            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Red;
                this.lbl_passOrFailed.Text = "FAIL!";
                return;
            }

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    DisplayOperateMes("V0A once power on = " + dMultiSiteVout0A[idut].ToString("F3"));

                    if (dMultiSiteVout0A[idut] < 2.6 && dMultiSiteVout0A[idut] > 1.6)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "混料!";
                        return;
                    }
                    else if (dMultiSiteVout0A[idut] < 0.5 || dMultiSiteVout0A[idut] > 4.8)
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "短路!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("DUT digital communication fail!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0 )
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP/2));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "短路!";
                    return;
                }
                else
                {
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "饱和!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "短路!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("请将IP降至0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                MessageBox.Show(String.Format("请确认IP方向!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("请确认电流为{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 
                    0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                ReloadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                Delay(Delay_Sync);
                /* Margianl read, compare with writed code; 
                    * if ( = ), go on
                    * else bMarginal = true; */
                MarginalReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bMarginal = false;

                if (bMASK)
                {
                    if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                        (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) |
                        (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | (tempReadback[4] < 1))
                        bMarginal = true;
                }
                else
                {
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                        (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bMarginal = true;
                }

                if (bSAFEREAD)
                {
                    SafetyReadPreset();
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    bSafety = false;
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                            (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | 
                            (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bSafety = true;
                }

                #region Re-Test after Trim
                if (bFastVersion == false)
                {
                    //capture Vout
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                    RePower();
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                    Delay(Delay_Sync);
                    dMultiSiteVout0A[idut] = AverageVout();
                    sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                    DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                    /* Change Current to IP  */
                    if (ProgramMode == 0)
                    {
                        if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                            DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                        else
                        {
                            DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            TrimFinish();
                            return;
                        }
                    }
                    else
                    {
                        dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                            PowerOff();
                            RestoreReg80ToReg83Value();
                            return;
                        }
                    }

                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                }
                #endregion

                sDUT.bReadMarginal = bMarginal;
                sDUT.bReadSafety = bSafety;

                if (!(bMarginal | bSafety))
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
                else
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Mriginal");
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    if (bMRE)
                        this.lbl_passOrFailed.Text = "FAIL!";
                    else
                        this.lbl_passOrFailed.Text = "PASS!";

                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety Read ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
            }


            #endregion No need Trim case

            #region For low sensitivity case, with IP
            dGainPreset = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            dGainTest = 1000d * (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP;
            if ( (dGainTest * 100d / dGainPreset) < (TargetGain_customer * ThresholdOfGain))
            {
                dGainTestMinusTarget = dGainTest / TargetGain_customer;
                //dGainPreset = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;

                if (this.cmb_IPRange_PreT.SelectedItem.ToString() == "1.5x610")
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                        bDutValid[idut] = false;
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "低敏!";
                        return;
                    }

                }
                else
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        if (dGainTest * 1.5 / dGainPreset >= (TargetGain_customer * ThresholdOfGain))
                        {
                            MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer((TargetGain_customer 
                                * 100d / (dGainTest * 1.5d) * dGainPreset), RoughTable_Customer);
                            MultiSiteRoughGainCodeIndex[idut] -= 1;
                            MultiSiteReg3[idut] |= 0xC0;
                            /* Rough Gain Code*/
                            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                            MultiSiteReg0[idut] &= ~bit_op_mask;
                            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                            bit_op_mask = bit0_Mask;
                            MultiSiteReg1[idut] &= ~bit_op_mask;
                            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                        }
                        else
                        {
                            DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                            bDutValid[idut] = false;
                            uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                            TrimFinish();
                            sDUT.iErrorCode = uDutTrimResult[idut];
                            PrintDutAttribute(sDUT);
                            this.lbl_passOrFailed.ForeColor = Color.Yellow;
                            this.lbl_passOrFailed.Text = "低敏!";
                            return;
                        }
                    }
                }


                DisplayOperateMes("RoughGainCodeIndex of DUT" + " = " + MultiSiteRoughGainCodeIndex[idut].ToString("F0"));
                DisplayOperateMes("SelectedRoughGain = " + RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]].ToString());
                DisplayOperateMes("CalcCode:");
                DisplayOperateMes("0x80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("0x81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
                DisplayOperateMes("0x82 = 0x" + MultiSiteReg2[idut].ToString("X2"));
                DisplayOperateMes("0x83 = 0x" + MultiSiteReg3[idut].ToString("X2"));

                /*  power on */
                RePower();
                EnterTestMode();
                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                BurstRead(0x80, 5, tempReadback);
                /* Get vout @ IP */
                EnterNomalMode();

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));


                /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    //decrease gain preset
                    MultiSiteRoughGainCodeIndex[idut] -= 1;
                    /* Rough Gain Code*/
                    bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                    bit_op_mask = bit0_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);

                    /*  power on */
                    RePower();
                    EnterTestMode();
                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    BurstRead(0x80, 5, tempReadback);
                    /* Get vout @ IP */
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = AverageVout();
                    sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                    if (dMultiSiteVoutIP[idut] > saturationVout)
                    {
                        DisplayOperateMes("Module" + " Vout is SATURATION!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "饱和!";
                        return;
                    }
                }

                /* Change Current to 0A */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                /*  power on */
                Delay(Delay_Fuse);
                this.lbl_passOrFailed.Text = "Processing!";
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));
            }
            #endregion For low sensitivity case, with IP

            #region Adapting algorithm
            #region coarse gain
            tempG1 = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupRoughGain_Customer(autoAdaptingGoughGain, RoughTable_Customer);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain];
            if (autoAdaptingPresionGain >= 100)
                Ix_forAutoAdaptingPresionGain = 0;
            else
                Ix_forAutoAdaptingPresionGain = LookupPreciseGain_Customer(autoAdaptingPresionGain, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("tempG1" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("tempG2" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + PreciseTable_Customer[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_forAutoAdaptingRoughGain]);

            bit_op_mask = bit0_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_forAutoAdaptingRoughGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Presion Gain Code*/
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain]);
            #endregion 

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            #region fine gain
            if (bFastVersion == false)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                EnterNomalMode();

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                /* Change Current to 0A */
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("请将电流降至0A"), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                /*  power on */
                Delay(Delay_Fuse);
                dMultiSiteVout0A[idut] = AverageVout();
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4)
                {
                    /* Presion Gain Code*/
                    bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain + 1]);
                }
                else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -4)
                {
                    /* Presion Gain Code*/
                    bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain - 1]);
                }
            }
            #endregion

            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            #region Offset tuning
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            //btn_offset_Click(null, null);
            uint[] regTMultiSite = new uint[3];

            MultiSiteOffsetAlg(regTMultiSite);
            MultiSiteReg1[idut] |= regTMultiSite[0];
            MultiSiteReg2[idut] |= regTMultiSite[1];
            MultiSiteReg3[idut] |= regTMultiSite[2];

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            ix_forOffsetIndex_Rough = 0;
            ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            ix_forOffsetIndex_Rough_Complementary = ix_forOffsetIndex_Rough;
            DisplayOperateMes("\r\nProcessing...");

            /* Repower on 5V */
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
            DisplayOperateMes("MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVout0A[idut] > TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 7)
                    ix_forOffsetIndex_Rough = 7;
                else if (ix_forOffsetIndex_Rough == 15)
                    ix_forOffsetIndex_Rough = 0;
                else
                    ix_forOffsetIndex_Rough += 1;
            }
            else if (dMultiSiteVout0A[idut] < TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 8)
                    ix_forOffsetIndex_Rough = 8;
                else if (ix_forOffsetIndex_Rough == 0)
                    ix_forOffsetIndex_Rough = 15;
                else
                    ix_forOffsetIndex_Rough -= 1;
            }
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            MultiSiteReg3[idut] &= ~bit_op_mask;
            MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout_0A_Complementary = AverageVout();
            DisplayOperateMes("\r\nMultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout_0A_Complementary = " + dMultiSiteVout_0A_Complementary.ToString("F3"));

            if (Math.Abs(dMultiSiteVout0A[idut] - TargetOffset) < Math.Abs(dMultiSiteVout_0A_Complementary - TargetOffset))
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough_Complementary]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }
            else
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }
            #endregion 

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                0x83, MultiSiteReg3[idut], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                    0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                    0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
            }
            Delay(Delay_Sync);
            /* Margianl read, compare with writed code; 
                * if ( = ), go on
                * else bMarginal = true; */
            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                    (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) | (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | 
                    (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | 
                        (tempReadback[4] < 1))
                    bMarginal = true;
            }

            if (bSAFEREAD)
            {
                SafetyReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bSafety = false;
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | 
                        (tempReadback[4] < 1))
                    bSafety = true;
            }

            sDUT.bReadMarginal = bMarginal;
            sDUT.bReadSafety = bSafety;

            if (!(bMarginal | bSafety))
            {
                DisplayOperateMes("DUT" + "Pass! Bin Normal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
            }
            else
            {
                DisplayOperateMes("DUT" + "Pass! Bin Mriginal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
            }

            #endregion

            #region Bin

            if (bFastVersion == false)
            {
                /* Repower on 5V */
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                #region IP control
                if (ProgramMode == 0)
                {
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("请将电流升至{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }
                #endregion

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_MARGINAL)
                {
                    if (TargetOffset * (1 - 0.01) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.01) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.01) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.01))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                        if (bMRE)
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Red;
                            this.lbl_passOrFailed.Text = "FAIL!";
                        }
                        else
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Green;
                            this.lbl_passOrFailed.Text = "PASS!";
                        }
                    }
                    else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                        dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin2accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin2accuracy / 100d))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_5;
                        if (bMRE)
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Red;
                            this.lbl_passOrFailed.Text = "FAIL!";
                        }
                        else
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Green;
                            this.lbl_passOrFailed.Text = "PASS!";
                        }
                    }
                    else if (TargetOffset * (1 - bin3accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                        dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin3accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_6;
                        if (bMRE)
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Red;
                            this.lbl_passOrFailed.Text = "FAIL!";
                        }
                        else
                        {
                            this.lbl_passOrFailed.ForeColor = Color.Green;
                            this.lbl_passOrFailed.Text = "PASS!";
                        }
                    }
                    else
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                    }

                }

                /* bin1,2,3 */
                else if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_NORMAL)
                {
                    if (TargetOffset * (1 - 0.01) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.01) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.01) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.01))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                    else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                        dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin2accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin2accuracy / 100d))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                    else if (TargetOffset * (1 - bin3accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                        dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin3accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                        (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                    {
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                    else
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                    }
                }
            }
            else
            {
                if (!(bMarginal | bSafety))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                }
                else
                {
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutomaticaTrim_5V_DiffMode()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            double dGainTestMinusTarget = 1;
            double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.lbl_passOrFailed.ForeColor = Color.Black;
            this.lbl_passOrFailed.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = Ix_ForRoughGainCtrl;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.lbl_passOrFailed.Text = "Trimming!";
            /* Get module current */
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC VIN to VCS");
            }
            else
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            Delay(Delay_Sync);
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC current sensor");
            }

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;
            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                PowerOff();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("????,????????!"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                //MessageBox.Show(String.Format("????,???????!"), "Error", MessageBoxButtons.OK);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "??!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Red;
                this.lbl_passOrFailed.Text = "FAIL!";
                return;
            }

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = ReadVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("?????,?????!"), "Error", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("??????!", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("??????!", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            //dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            //dMultiSiteVoutIP[idut] = AverageVout();
            dMultiSiteVoutIP[idut] = ReadVout();
            TargetOffset = ReadRef();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("??????{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = ReadVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "??!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("????,????????!"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "??!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "??!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??IP??0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = ReadVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("???IP??!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("??????{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            else
            {
                if (dMultiSiteVout0A[idut] < TargetOffset * 0.97 || dMultiSiteVout0A[idut] > TargetOffset * 1.03)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 
                    0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);
                ReloadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                Delay(Delay_Sync);
                /* Margianl read, compare with writed code; 
                    * if ( = ), go on
                    * else bMarginal = true; */
                MarginalReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bMarginal = false;

                if (bMASK)
                {
                    if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                        (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) |
                        (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) | (tempReadback[4] < 1))
                        bMarginal = true;
                }
                else
                {
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                        (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bMarginal = true;
                }

                if (bSAFEREAD)
                {
                    //Delay(Delay_Sync);
                    SafetyReadPreset();
                    Delay(Delay_Sync);
                    BurstRead(0x80, 5, tempReadback);
                    bSafety = false;
                    if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                            (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) |
                            (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) | (tempReadback[4] < 1))
                        bSafety = true;
                }

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = ReadVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = ReadVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                sDUT.bReadMarginal = bMarginal;
                sDUT.bReadSafety = bSafety;

                if (!(bMarginal | bSafety))
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
                else
                {
                    DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Mriginal");
                    //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    if (bMRE)
                        this.lbl_passOrFailed.Text = "FAIL!";
                    else
                        this.lbl_passOrFailed.Text = "PASS!";

                    DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                    DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                    DisplayOperateMes("Safety Read ->" + bSafety.ToString());
                    MultiSiteDisplayResult(uDutTrimResult);
                    TrimFinish();
                    PrintDutAttribute(sDUT);
                    return;
                }
            }


            #endregion No need Trim case

            #region For low sensitivity case, with IP

            dGainTest = 1000d * (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP;
            if (dGainTest < (TargetGain_customer * ThresholdOfGain))
            {
                dGainTestMinusTarget = dGainTest / TargetGain_customer;
                dGainPreset = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;

                if (this.cmb_IPRange_PreT.SelectedItem.ToString() == "1.5x610")
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                        bDutValid[idut] = false;
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.lbl_passOrFailed.ForeColor = Color.Red;
                        //this.lbl_passOrFailed.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("?????,????????!"), "Warning", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }

                }
                else
                {
                    if (dGainTestMinusTarget >= dGainPreset)
                    {
                        MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer
                            (TargetGain_customer * 100d / dGainTest * dGainPreset, RoughTable_Customer);
                        /* Rough Gain Code*/
                        bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                        MultiSiteReg0[idut] &= ~bit_op_mask;
                        MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                        bit_op_mask = bit0_Mask;
                        MultiSiteReg1[idut] &= ~bit_op_mask;
                        MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                    }
                    else
                    {
                        if (dGainTest * 1.5 / dGainPreset >= (TargetGain_customer * ThresholdOfGain))
                        {
                            MultiSiteRoughGainCodeIndex[idut] = (uint)LookupRoughGain_Customer((TargetGain_customer
                                * 100d / (dGainTest * 1.5d) * dGainPreset), RoughTable_Customer);
                            MultiSiteRoughGainCodeIndex[idut] -= 1;
                            MultiSiteReg3[idut] |= 0xC0;
                            /* Rough Gain Code*/
                            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                            MultiSiteReg0[idut] &= ~bit_op_mask;
                            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                            bit_op_mask = bit0_Mask;
                            MultiSiteReg1[idut] &= ~bit_op_mask;
                            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);
                        }
                        else
                        {
                            DisplayOperateMes("DUT" + idut.ToString() + " Sensitivity is NOT enough!", Color.Red);
                            bDutValid[idut] = false;
                            uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_LOW_SENSITIVITY;
                            TrimFinish();
                            sDUT.iErrorCode = uDutTrimResult[idut];
                            //this.lbl_passOrFailed.ForeColor = Color.Red;
                            //this.lbl_passOrFailed.Text = "MOA!";
                            PrintDutAttribute(sDUT);
                            //dr = MessageBox.Show(String.Format("?????,????????!"), "Warning", MessageBoxButtons.OK);
                            this.lbl_passOrFailed.ForeColor = Color.Yellow;
                            this.lbl_passOrFailed.Text = "??!";
                            return;
                        }
                    }
                }


                DisplayOperateMes("RoughGainCodeIndex of DUT" + " = " + MultiSiteRoughGainCodeIndex[idut].ToString("F0"));
                DisplayOperateMes("SelectedRoughGain = " + RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]].ToString());
                DisplayOperateMes("CalcCode:");
                DisplayOperateMes("0x80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("0x81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
                DisplayOperateMes("0x82 = 0x" + MultiSiteReg2[idut].ToString("X2"));
                DisplayOperateMes("0x83 = 0x" + MultiSiteReg3[idut].ToString("X2"));

                /*  power on */
                RePower();
                EnterTestMode();
                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                BurstRead(0x80, 5, tempReadback);
                /* Get vout @ IP */
                EnterNomalMode();

                /* Change Current to IP  */
                //3. Set Voltage
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = ReadVout();
                sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));


                /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    //decrease gain preset
                    MultiSiteRoughGainCodeIndex[idut] -= 1;
                    /* Rough Gain Code*/
                    bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                    MultiSiteReg0[idut] &= ~bit_op_mask;
                    MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][MultiSiteRoughGainCodeIndex[idut]]);

                    bit_op_mask = bit0_Mask;
                    MultiSiteReg1[idut] &= ~bit_op_mask;
                    MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][MultiSiteRoughGainCodeIndex[idut]]);

                    /*  power on */
                    RePower();
                    EnterTestMode();
                    RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                    BurstRead(0x80, 5, tempReadback);
                    /* Get vout @ IP */
                    EnterNomalMode();
                    Delay(Delay_Fuse);
                    dMultiSiteVoutIP[idut] = ReadVout();
                    sDUT.dVoutIPMiddle = dMultiSiteVoutIP[idut];
                    DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                    /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
                    if (dMultiSiteVoutIP[idut] > saturationVout)
                    {
                        DisplayOperateMes("Module" + " Vout is SATURATION!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                        TrimFinish();
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        //this.lbl_passOrFailed.ForeColor = Color.Red;
                        //this.lbl_passOrFailed.Text = "MOA!";
                        PrintDutAttribute(sDUT);
                        //dr = MessageBox.Show(String.Format("????,????????!"), "Warning", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }
                }

                /* Change Current to 0A */
                //3. Set Voltage
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("??????0A"), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                /*  power on */
                Delay(Delay_Fuse);
                this.lbl_passOrFailed.Text = "Processing!";
                dMultiSiteVout0A[idut] = ReadVout();
                sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                //V0A is abnormal
                //if( Math.Abs(sDUT.dVout0AMiddle - sDUT.dVout0ANative) > 0.005 )
                //{
                //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OK);
                //    if (dr == DialogResult.OK)
                //    {
                //        DisplayOperateMes("V0A abnormal, Rebuild rough gain code for low gain case!");
                //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                //        PowerOff();
                //        RestoreReg80ToReg83Value();
                //        return;
                //    }
                //}
            }

            #endregion For low sensitivity case, with IP

            #region Adapting algorithm

            tempG1 = RoughTable_Customer[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupRoughGain_Customer(autoAdaptingGoughGain, RoughTable_Customer);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain];
            Ix_forAutoAdaptingPresionGain = LookupPreciseGain_Customer(autoAdaptingPresionGain, PreciseTable_Customer);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("tempG1" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("tempG2" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + RoughTable_Customer[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + PreciseTable_Customer[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_forAutoAdaptingRoughGain]);

            bit_op_mask = bit0_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_forAutoAdaptingRoughGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Presion Gain Code*/
            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            MultiSiteReg0[idut] &= ~bit_op_mask;
            MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = ReadVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            /*  power on */
            Delay(Delay_Fuse);
            //this.lbl_passOrFailed.Text = "Processing!";
            dMultiSiteVout0A[idut] = ReadVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain + 1]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                MultiSiteReg0[idut] &= ~bit_op_mask;
                MultiSiteReg0[idut] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_forAutoAdaptingPresionGain - 1]);
            }



            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = ReadVout();
            TargetOffset = ReadRef() - 0.004;
            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));


            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            //btn_offset_Click(null, null);
            uint[] regTMultiSite = new uint[3];

            DiffModeOffsetAlg(regTMultiSite);
            MultiSiteReg1[idut] |= regTMultiSite[0];
            MultiSiteReg2[idut] |= regTMultiSite[1];
            MultiSiteReg3[idut] |= regTMultiSite[2];

            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            ix_forOffsetIndex_Rough = 0;
            ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            ix_forOffsetIndex_Rough_Complementary = ix_forOffsetIndex_Rough;
            DisplayOperateMes("\r\nProcessing...");

            /* Repower on 5V */
            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = ReadVout();
            sDUT.dVout0AMiddle = dMultiSiteVout0A[idut];
            DisplayOperateMes("MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVout0A[idut] > TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 7)
                    ix_forOffsetIndex_Rough = 7;
                else if (ix_forOffsetIndex_Rough == 15)
                    ix_forOffsetIndex_Rough = 0;
                else
                    ix_forOffsetIndex_Rough += 1;
            }
            else if (dMultiSiteVout0A[idut] < TargetOffset)
            {
                if (ix_forOffsetIndex_Rough == 8)
                    ix_forOffsetIndex_Rough = 8;
                else if (ix_forOffsetIndex_Rough == 0)
                    ix_forOffsetIndex_Rough = 15;
                else
                    ix_forOffsetIndex_Rough -= 1;
            }
            bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
            MultiSiteReg3[idut] &= ~bit_op_mask;
            MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);

            RePower();
            EnterTestMode();
            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout_0A_Complementary = ReadVout();
            DisplayOperateMes("\r\nMultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            DisplayOperateMes("ix_forOffsetIndex_Rough = " + ix_forOffsetIndex_Rough.ToString());
            DisplayOperateMes("dMultiSiteVout_0A_Complementary = " + dMultiSiteVout_0A_Complementary.ToString("F3"));

            //V0A is abnormal
            //if (Math.Abs(sDUT.dVout0AMiddle - dMultiSiteVout_0A_Complementary) > 0.005)
            //{
            //    dr = MessageBox.Show(String.Format("Vout @ 0A is abnormal"), "Warning!", MessageBoxButtons.OKCancel);
            //    if (dr == DialogResult.Cancel)
            //    {
            //        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
            //        PowerOff();
            //        RestoreReg80ToReg83Value();
            //        return;
            //    }
            //}

            if (Math.Abs(dMultiSiteVout0A[idut] - TargetOffset) < Math.Abs(dMultiSiteVout_0A_Complementary - TargetOffset))
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough_Complementary]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }
            else
            {
                bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                MultiSiteReg3[idut] &= ~bit_op_mask;
                MultiSiteReg3[idut] |= Convert.ToUInt32(OffsetTableB_Customer[1][ix_forOffsetIndex_Rough]);
                DisplayOperateMes("Last MultiSiteReg3 = 0x" + MultiSiteReg3[idut].ToString("X2"));
            }

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                0x83, MultiSiteReg3[idut], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");
            //Delay(Delay_Fuse);

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, MultiSiteReg0[idut], 
                    0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 
                    0x83, MultiSiteReg3[idut], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
                //Delay(Delay_Fuse);
            }
            Delay(Delay_Sync);
            /* Margianl read, compare with writed code; 
                * if ( = ), go on
                * else bMarginal = true; */
            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (MultiSiteReg0[idut] & 0xE0)) | (tempReadback[1] & 0x81) != (MultiSiteReg1[idut] & 0x81) |
                    (tempReadback[2] & 0x99) != (MultiSiteReg2[idut] & 0x99) | (tempReadback[3] & 0x83) != (MultiSiteReg3[idut] & 0x83) |
                    (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) |
                        (tempReadback[4] < 1))
                    bMarginal = true;
            }

            if (bSAFEREAD)
            {
                //Delay(Delay_Sync);
                SafetyReadPreset();
                Delay(Delay_Sync);
                BurstRead(0x80, 5, tempReadback);
                bSafety = false;
                if (((tempReadback[0] & 0xFF) != (MultiSiteReg0[idut] & 0xFF)) | (tempReadback[1] & 0xFF) != (MultiSiteReg1[idut] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (MultiSiteReg2[idut] & 0xFF) | (tempReadback[3] & 0xFF) != (MultiSiteReg3[idut] & 0xFF) |
                        (tempReadback[4] < 1))
                    bSafety = true;
            }

            sDUT.bReadMarginal = bMarginal;
            sDUT.bReadSafety = bSafety;

            if (!(bMarginal | bSafety))
            {
                DisplayOperateMes("DUT" + "Pass! Bin Normal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
            }
            else
            {
                DisplayOperateMes("DUT" + "Pass! Bin Mriginal");
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_MARGINAL;
            }
            //sDUT.iErrorCode = uDutTrimResult[idut];

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            dMultiSiteVout0A[idut] = ReadVout();
            TargetOffset = ReadRef();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = ReadVout();
            sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_MARGINAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_4;
                    //this.lbl_passOrFailed.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                    }
                    else
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_5;
                    //this.lbl_passOrFailed.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                    }
                    else
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_6;
                    //this.lbl_passOrFailed.ForeColor = Color.Green;
                    if (bMRE)
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Red;
                        this.lbl_passOrFailed.Text = "FAIL!";
                    }
                    else
                    {
                        this.lbl_passOrFailed.ForeColor = Color.Green;
                        this.lbl_passOrFailed.Text = "PASS!";
                    }
                }
                else
                {
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                }

            }

            /* bin1,2,3 */
            //if ((!bMarginal) && (!bSafety))
            else if (uDutTrimResult[idut] == (uint)PRGMRSULT.DUT_BIN_NORMAL)
            {
                if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                }
                else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                    dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                    (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                    this.lbl_passOrFailed.ForeColor = Color.Green;
                    this.lbl_passOrFailed.Text = "PASS!";
                }
                else
                {
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                }
            }

            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }

        private void AutoTrim_SL620_SingleEnd()
        {
            #region Define Parameters
            DialogResult dr;
            bool bMarginal = false;
            bool bSafety = false;
            //uint[] tempReadback = new uint[5];
            double dVout_0A_Temp = 0;
            //TargetOffset = 2.5;
            double dVip_Target = TargetOffset + TargetVoltage_customer;
            //double dGainTestMinusTarget = 1;
            //double dGainTest = 0;
            ModuleAttribute sDUT;
            sDUT.dIQ = 0;
            sDUT.dVoutIPNative = 0;
            sDUT.dVout0ANative = 0;
            sDUT.dVoutIPMiddle = 0;
            sDUT.dVout0AMiddle = 0;
            sDUT.dVoutIPTrimmed = 0;
            sDUT.dVout0ATrimmed = 0;
            sDUT.iErrorCode = 00;
            sDUT.bDigitalCommFail = false;
            sDUT.bNormalModeFail = false;
            sDUT.bReadMarginal = false;
            sDUT.bReadSafety = false;
            sDUT.bTrimmed = false;

            // PARAMETERS DEFINE FOR MULTISITE
            uint idut = 0;
            uint uDutCount = 16;
            //bool bValidRound = false;
            //bool bSecondCurrentOn = false;
            double dModuleCurrent = 0;
            bool[] bGainBoost = new bool[16];
            bool[] bDutValid = new bool[16];
            bool[] bDutNoNeedTrim = new bool[16];
            uint[] uDutTrimResult = new uint[16];
            double[] dMultiSiteVoutIP = new double[16];
            double[] dMultiSiteVout0A = new double[16];

            /* autoAdaptingGoughGain algorithm*/
            double autoAdaptingGoughGain = 0;
            double autoAdaptingPresionGain = 0;
            double tempG1 = 0;
            double tempG2 = 0;
            //double dGainPreset = 0;
            int Ix_forAutoAdaptingRoughGain = 0;
            int Ix_forAutoAdaptingPresionGain = 0;

            int ix_forOffsetIndex_Rough = 0;
            int ix_forOffsetIndex_Rough_Complementary = 0;
            double dMultiSiteVout_0A_Complementary = 0;

            DisplayOperateMes("\r\n**************" + DateTime.Now.ToString() + "**************");
            DisplayOperateMes("Start...");
            this.lbl_passOrFailed.ForeColor = Color.Black;
            this.lbl_passOrFailed.Text = "START!";

            for (uint i = 0; i < uDutCount; i++)
            {
                dMultiSiteVoutIP[i] = 0d;
                dMultiSiteVout0A[i] = 0d;

                MultiSiteReg0[i] = Reg80Value;
                MultiSiteReg1[i] = Reg81Value;
                MultiSiteReg2[i] = Reg82Value;
                MultiSiteReg3[i] = Reg83Value;
                MultiSiteReg4[i] = Reg84Value;
                MultiSiteReg5[i] = Reg85Value;
                MultiSiteReg6[i] = Reg86Value;
                MultiSiteReg7[i] = Reg87Value;

                MultiSiteRoughGainCodeIndex[i] = 0;

                uDutTrimResult[i] = 0u;
                bDutNoNeedTrim[i] = false;
                bDutValid[i] = false;
                bGainBoost[i] = false;
            }
            #endregion Define Parameters

            #region Get module current
            //clear log
            DisplayOperateMesClear();
            /*  power on */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            Delay(Delay_Sync);
            this.lbl_passOrFailed.Text = "Trimming!";
            /* Get module current */
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC VIN to VCS");
            }
            else
            {
                DisplayOperateMes("Set ADC VIN to VCS failed", Color.Red);
                PowerOff();
                return;
            }
            Delay(Delay_Sync);
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
            {
                if (bAutoTrimTest)
                    DisplayOperateMes("Set ADC current sensor");
            }

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;


            dModuleCurrent = GetModuleCurrent();
            sDUT.dIQ = dModuleCurrent;
            if (dCurrentDownLimit > dModuleCurrent)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_ABNORMAL;
                PowerOff();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("????,????????!"), "Warning", MessageBoxButtons.OK);
                return;
            }
            else if (dModuleCurrent > dCurrentUpLimit)
            {
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"), Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_CURRENT_HIGH;
                PowerOff();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                //MessageBox.Show(String.Format("????,???????!"), "Error", MessageBoxButtons.OK);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "??!";
                return;
            }
            else
                DisplayOperateMes("Module " + " current is " + dModuleCurrent.ToString("F3"));

            #endregion Get module current

            #region Saturation judgement


            //Redundency delay in case of power off failure.
            Delay(Delay_Sync);
            EnterTestMode();
            //Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
            {
                DisplayOperateMes("DUT" + " has some bits Blown!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMED_SOMEBITS;
                TrimFinish();
                sDUT.bTrimmed = false;
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Red;
                this.lbl_passOrFailed.Text = "FAIL!";
                return;
            }



            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[0] != MultiSiteReg0[idut] || tempReadback[1] != MultiSiteReg1[idut]
                || tempReadback[2] != MultiSiteReg2[idut] || tempReadback[3] != MultiSiteReg3[idut])
            {
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] == 0)
                {
                    RePower();
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                    Delay(Delay_Fuse);
                    dMultiSiteVout0A[idut] = AverageVout();
                    if (dMultiSiteVout0A[idut] < 4.5 && dMultiSiteVout0A[idut] > 1.5)
                    {
                        DisplayOperateMes("DUT Trimmed!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_TRIMMRD_ALREADY;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.bTrimmed = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("?????,?????!"), "Error", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }
                    else
                    {
                        DisplayOperateMes("VOUT Short!", Color.Red);
                        uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SHORT;
                        TrimFinish();
                        sDUT.bDigitalCommFail = true;
                        sDUT.iErrorCode = uDutTrimResult[idut];
                        PrintDutAttribute(sDUT);
                        //MessageBox.Show(String.Format("??????!", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                        this.lbl_passOrFailed.ForeColor = Color.Yellow;
                        this.lbl_passOrFailed.Text = "??!";
                        return;
                    }
                }
                else
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_COMM_FAIL;
                    TrimFinish();
                    sDUT.bDigitalCommFail = true;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    //MessageBox.Show(String.Format("??????!", Color.YellowGreen), "Warning", MessageBoxButtons.OK);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            /* Get vout @ IP */
            EnterNomalMode();

            /* Change Current to IP  */
            //dr = MessageBox.Show(String.Format("Please Change Current To {0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    TrimFinish();
                    return;
                }
            }
            else if (ProgramMode == 1)
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }


            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /*Judge PreSet gain; delta Vout target >= delta Vout test * 86.07% */
            if (dMultiSiteVoutIP[idut] > saturationVout)
            {
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 5);
                    Delay(Delay_Sync);
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 5));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", 5));
                        TrimFinish();
                        return;
                    }
                }
                else if (ProgramMode == 1)
                {
                    dr = MessageBox.Show(String.Format("??????{0}A", 5), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPNative = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

                //set current back to IP
                if (ProgramMode == 0)
                {
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0);
                    Delay(Delay_Sync);
                    oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));
                }

                if (dMultiSiteVoutIP[idut] > saturationVout)
                {
                    DisplayOperateMes("Module" + " Vout is VDD!", Color.Red);
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_VDD;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "??!";
                    return;
                }
                else
                {
                    //dr = MessageBox.Show(String.Format("????,????????!"), "Warning", MessageBoxButtons.OK);
                    TrimFinish();
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_SATURATION;
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Yellow;
                    this.lbl_passOrFailed.Text = "??!";
                    return;
                }
            }
            else if (dMultiSiteVoutIP[idut] < minimumVoutIP)
            {
                DisplayOperateMes("Module" + " Vout is too Low!", Color.Red);
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_VOUT_LOW;
                TrimFinish();
                sDUT.iErrorCode = uDutTrimResult[idut];
                PrintDutAttribute(sDUT);
                this.lbl_passOrFailed.ForeColor = Color.Yellow;
                this.lbl_passOrFailed.Text = "??!";
                return;
            }

            #endregion Saturation judgement

            #region Get Vout@0A
            /* Change Current to 0A */
            //3. Set Voltage
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??IP??0A!"), "Try Again", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ANative = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (dMultiSiteVoutIP[idut] < dMultiSiteVout0A[idut])
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("???IP??!"), "Try Again", MessageBoxButtons.OK);
                return;
            }
            else if (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut] < VoutIPThreshold)
            {
                TrimFinish();
                //PrintDutAttribute(sDUT);
                MessageBox.Show(String.Format("??????{0}A!!!", IP), "Try Again", MessageBoxButtons.OK);
                return;
            }

            if (TargetOffset == 2.5)
            {
                if (dMultiSiteVout0A[idut] < 2.25 || dMultiSiteVout0A[idut] > 2.8)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }
            else if (TargetOffset == 1.65)
            {
                if (dMultiSiteVout0A[idut] < 1.0 || dMultiSiteVout0A[idut] > 2.5)
                {
                    uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_OFFSET_ABN;
                    TrimFinish();
                    sDUT.iErrorCode = uDutTrimResult[idut];
                    PrintDutAttribute(sDUT);
                    this.lbl_passOrFailed.ForeColor = Color.Red;
                    this.lbl_passOrFailed.Text = "FAIL!";
                    return;
                }
            }

            #endregion  Get Vout@0A

            #region No need Trim case
            if ((TargetOffset - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= (TargetOffset + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= (TargetVoltage_customer + 0.001)
                && (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= (TargetVoltage_customer - 0.001))
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
                Delay(Delay_Sync);
                RePower();
                //Delay(Delay_Sync);
                EnterTestMode();

                RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
                Delay(Delay_Sync);
                RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
                Delay(Delay_Sync);


                BurstRead(0x80, 5, tempReadback);
                /* fuse */
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Processing...");
                //Delay(Delay_Fuse);             

                //capture Vout
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
                RePower();
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Delay(Delay_Sync);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

                Delay(Delay_Sync);
                dMultiSiteVout0A[idut] = AverageVout();
                sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
                DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

                /* Change Current to IP  */
                if (ProgramMode == 0)
                {
                    //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                    if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                        DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                    else
                    {
                        DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        TrimFinish();
                        return;
                    }
                }
                else
                {
                    dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                    {
                        DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                        PowerOff();
                        RestoreReg80ToReg83Value();
                        return;
                    }
                }

                Delay(Delay_Fuse);
                dMultiSiteVoutIP[idut] = AverageVout();
                sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
                DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));
                //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);

                DisplayOperateMes("DUT" + idut.ToString() + "Pass! Bin Normal");
                //uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_NORMAL;
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                sDUT.iErrorCode = uDutTrimResult[idut];
                this.lbl_passOrFailed.ForeColor = Color.Green;
                this.lbl_passOrFailed.Text = "PASS!";
                DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
                DisplayOperateMes("Marginal Read ->" + bMarginal.ToString());
                DisplayOperateMes("Safety REad ->" + bSafety.ToString());
                MultiSiteDisplayResult(uDutTrimResult);
                TrimFinish();
                PrintDutAttribute(sDUT);
                return;

            }


            #endregion No need Trim case

            #region Adapting algorithm

            tempG1 = sl620CoarseGainTable[0][MultiSiteRoughGainCodeIndex[idut]] / 100d;
            tempG2 = (TargetGain_customer / ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP)) / 1000d;

            autoAdaptingGoughGain = tempG1 * tempG2 * 100d;
            DisplayOperateMes("IdealGoughGain = " + autoAdaptingGoughGain.ToString("F4"));

            Ix_forAutoAdaptingRoughGain = LookupCoarseGain_SL620(autoAdaptingGoughGain, sl620CoarseGainTable);
            autoAdaptingPresionGain = 100d * autoAdaptingGoughGain / sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain];
            Ix_forAutoAdaptingPresionGain = LookupFineGain_SL620(autoAdaptingPresionGain, sl620FineGainTable);
            if (bAutoTrimTest)
            {
                DisplayOperateMes("IP = " + IP.ToString("F0"));
                DisplayOperateMes("TargetGain_customer" + idut.ToString() + " = " + TargetGain_customer.ToString("F4"));
                DisplayOperateMes("(dMultiSiteVoutIP - dMultiSiteVout0A)/IP = " + ((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) / IP).ToString("F4"));
                DisplayOperateMes("Pre-set gain" + idut.ToString() + " = " + tempG1.ToString("F4"));
                DisplayOperateMes("Target gain / Test gain" + idut.ToString() + " = " + tempG2.ToString("F4"));
                DisplayOperateMes("Ix_forAutoAdaptingRoughGain" + idut.ToString() + " = " + Ix_forAutoAdaptingRoughGain.ToString("F0"));
                DisplayOperateMes("Ix_forAutoAdaptingPresionGain" + idut.ToString() + " = " + Ix_forAutoAdaptingPresionGain.ToString("F0"));
                DisplayOperateMes("autoAdaptingGoughGain" + idut.ToString() + " = " + sl620CoarseGainTable[0][Ix_forAutoAdaptingRoughGain].ToString("F4"));
                DisplayOperateMes("autoAdaptingPresionGain" + idut.ToString() + " = " + sl620FineGainTable[0][Ix_forAutoAdaptingPresionGain].ToString("F4"));
            }

            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg1[idut] &= ~bit_op_mask;
            MultiSiteReg1[idut] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_forAutoAdaptingRoughGain]);


            if (bAutoTrimTest)
            {
                //DisplayOperateMes("Rough Gain RegValue80 = 0x" + MultiSiteReg0[idut].ToString("X2"));
                DisplayOperateMes("Rough Gain RegValue81 = 0x" + MultiSiteReg1[idut].ToString("X2"));
            }

            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain]);

            if (bAutoTrimTest)
            {
                DisplayOperateMes("Precesion Gain RegValue86 = 0x" + MultiSiteReg6[idut].ToString("X2"));
                DisplayOperateMes("Precesion Gain RegValue87 = 0x" + MultiSiteReg7[idut].ToString("X2"));
                DisplayOperateMes("***new add approach***");
            }

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* Change Current to 0A */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0u));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0u));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????0A"), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            /*  power on */
            Delay(Delay_Fuse);
            //this.lbl_passOrFailed.Text = "Processing!";
            dMultiSiteVout0A[idut] = AverageVout();
            DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) > 4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain + 1]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain + 1]);
            }
            else if (((dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) * 1000 - TargetGain_customer * IP) < -4)
            {
                /* Presion Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg6[idut] &= ~bit_op_mask;
                MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain - 1]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                MultiSiteReg7[idut] &= ~bit_op_mask;
                MultiSiteReg7[idut] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_forAutoAdaptingPresionGain - 1]);
            }

            if (bAutoTrimTest)
                DisplayOperateMes("***new approach end***");

            dVout_0A_Temp = dMultiSiteVout0A[idut];
            if (bAutoTrimTest)
                DisplayOperateMes("DUT" + " Vout @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Offset trim code calculate */
            Vout_0A = dMultiSiteVout0A[idut];

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_CoarseOffsetCode = 0;
            if (Vout_0A > TargetOffset)
                ix_CoarseOffsetCode = Convert.ToUInt32(Math.Floor(1000d * (Vout_0A - TargetOffset) / 14));
            else if (Vout_0A < TargetOffset)
                ix_CoarseOffsetCode = 31 - Convert.ToUInt32(Math.Floor(1000d * (TargetOffset - Vout_0A) / 14));

            MultiSiteReg6[idut] &= ~bit_op_mask;
            MultiSiteReg6[idut] |= ix_CoarseOffsetCode;

            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_CoarseOffsetCode = " + ix_CoarseOffsetCode.ToString());

            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);

            //RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            EnterNomalMode();
            Delay(Delay_Fuse);
            dMultiSiteVout0A[idut] = AverageVout();
            Vout_0A = dMultiSiteVout0A[idut];

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            uint ix_FineOffsetCode = 0;
            if (Vout_0A > TargetOffset)
                ix_FineOffsetCode = Convert.ToUInt32(Math.Floor(1000d * (Vout_0A - TargetOffset) / 4));
            else if (Vout_0A < TargetOffset)
                ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Floor(1000d * (TargetOffset - Vout_0A) / 4));

            MultiSiteReg7[idut] &= ~bit_op_mask;
            MultiSiteReg7[idut] |= ix_FineOffsetCode;

            //ix_forOffsetIndex_Rough = LookupOffsetIndex(MultiSiteReg3[idut] & bit_op_mask, OffsetTableB_Customer);
            //ix_FineOffsetCode = ix_forOffsetIndex_Rough;
            DisplayOperateMes("Vout_0A = " + Vout_0A.ToString("F3"));
            DisplayOperateMes("ix_FineOffsetCode = " + ix_FineOffsetCode.ToString());
            DisplayOperateMes("\r\nProcessing...");

            DisplayOperateMes("Processing...");

            #endregion Adapting algorithm

            #region Fuse
            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();

            RegisterWrite(4, new uint[8] { 0x80, MultiSiteReg0[idut], 0x81, MultiSiteReg1[idut], 0x82, MultiSiteReg2[idut], 0x83, MultiSiteReg3[idut] });
            Delay(Delay_Sync);
            RegisterWrite(4, new uint[8] { 0x84, MultiSiteReg4[idut], 0x85, MultiSiteReg5[idut], 0x86, MultiSiteReg6[idut], 0x87, MultiSiteReg7[idut] });
            Delay(Delay_Sync);
            RegisterWrite(1, new uint[2] { 0x88, 0x02 });

            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");

            #endregion

            #region Bin
            /* Repower on 5V */
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            RePower();
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Fuse);

            dMultiSiteVout0A[idut] = AverageVout();
            sDUT.dVout0ATrimmed = dMultiSiteVout0A[idut];
            DisplayOperateMes("Vout" + " @ 0A = " + dMultiSiteVout0A[idut].ToString("F3"));

            /* Change Current to IP  */
            if (ProgramMode == 0)
            {
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0u))
                    DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
                else
                {
                    DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    TrimFinish();
                    return;
                }
            }
            else
            {
                dr = MessageBox.Show(String.Format("??????{0}A", IP), "Change Current", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    DisplayOperateMes("AutoTrim Canceled!", Color.Red);
                    PowerOff();
                    RestoreReg80ToReg83Value();
                    return;
                }
            }

            Delay(Delay_Fuse);
            dMultiSiteVoutIP[idut] = AverageVout();
            sDUT.dVoutIPTrimmed = dMultiSiteVoutIP[idut];
            DisplayOperateMes("Vout" + " @ IP = " + dMultiSiteVoutIP[idut].ToString("F3"));

            /* bin1,2,3 */
            if (TargetOffset * (1 - 0.001) <= dMultiSiteVout0A[idut] && dMultiSiteVout0A[idut] <= TargetOffset * (1 + 0.001) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + 0.001) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - 0.001))
            {
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_1;
                this.lbl_passOrFailed.ForeColor = Color.Green;
                this.lbl_passOrFailed.Text = "PASS!";
            }
            else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
            {
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_2;
                this.lbl_passOrFailed.ForeColor = Color.Green;
                this.lbl_passOrFailed.Text = "PASS!";
            }
            else if (TargetOffset * (1 - bin2accuracy / 100d) <= dMultiSiteVout0A[idut] &&
                dMultiSiteVout0A[idut] <= TargetOffset * (1 + bin2accuracy / 100d) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) <= TargetVoltage_customer * (1 + bin3accuracy / 100d) &&
                (dMultiSiteVoutIP[idut] - dMultiSiteVout0A[idut]) >= TargetVoltage_customer * (1 - bin3accuracy / 100d))
            {
                uDutTrimResult[idut] = (uint)PRGMRSULT.DUT_BIN_3;
                this.lbl_passOrFailed.ForeColor = Color.Green;
                this.lbl_passOrFailed.Text = "PASS!";
            }
            else
            {
                this.lbl_passOrFailed.ForeColor = Color.Red;
                this.lbl_passOrFailed.Text = "FAIL!";
            }


            #endregion Bin

            #region Display Result and Reset parameters
            DisplayOperateMes("Bin" + " = " + uDutTrimResult[idut].ToString());
            MultiSiteDisplayResult(uDutTrimResult);
            TrimFinish();
            sDUT.iErrorCode = uDutTrimResult[idut];
            PrintDutAttribute(sDUT);
            DisplayOperateMes("Next...");
            #endregion Display Result and Reset parameters
        }
         
        private double abs(double p)
        {
            throw new NotImplementedException();
        }

        //sel_vr button
        private void btn_sel_vr_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x82;
            _reg_Value = 0x08;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_nc_1x_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x83;
            _reg_Value = 0x01;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Write NC_1X", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_ch_ck_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x82;
            _reg_Value = 0x80;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void btn_sel_cap_Click(object sender, EventArgs e)
        {
            uint _dev_addr = 0x73;  //Device Address
            uint _reg_Addr;
            uint _reg_Value;


            //Enter normal mode
            _reg_Addr = 0x55;
            _reg_Value = 0xAA;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Test Mode Before Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }

            _reg_Addr = 0x81;
            _reg_Value = 0x08;
            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayAutoTrimOperateMes("Enter Normal Mode", true, 32);
            else
            {
                //DisplayAutoTrimResult(false);
                //DisplayAutoTrimResult(false, 0x0006, "I2C Conmunication Error!");
                return;
            }
        }

        private void txt_dev_addr_onewire_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_dev_addr_onewire_EngT.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
                this.DeviceAddress = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                temp = string.Format("Device address set failed, will use default adrress {0}", this.DeviceAddress);
                DisplayOperateMes(temp, Color.Red);
                this.txt_dev_addr_onewire_EngT.Text = "0x" + this.DeviceAddress.ToString("X2");
            }
            finally 
            {
                //this.txt_dev_addr_onewire_EngT.Text = "0x" + this.DeviceAddress.ToString("X2");
            }
        }

        private void btn_Reset_EngT_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Flash result->{0}", oneWrie_device.ResetBoard());
        }

        private void btn_ModuleCurrent_EngT_Click(object sender, EventArgs e)
        {
            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VCS))
                DisplayOperateMes("Set ADC VIN to VCS");
            else
                DisplayOperateMes("Set ADC VIN to VCS failed",Color.Red);

            if (oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_SET_CURRENT_SENCE))
                DisplayOperateMes("Set ADC current sensor");
            else
                DisplayOperateMes("Set ADC current sensor failed", Color.Red);

            this.txt_ModuleCurrent_EngT.Text = GetModuleCurrent().ToString("F1");
            this.txt_ModuleCurrent_PreT.Text = this.txt_ModuleCurrent_EngT.Text;
        }

        private void txt_sampleNum_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_sampleNum_EngT.Text;
                SampleRateNum = UInt32.Parse((temp == "" ? "0" : temp));
            }
            catch
            {
                temp = string.Format("Sample rate number set failed, will use default value {0}", this.SampleRateNum);
                DisplayOperateMes(temp, Color.Red);
            }
            finally 
            {
                this.txt_sampleNum_EngT.Text = this.SampleRateNum.ToString();
            }
        }

        private void txt_sampleRate_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = this.txt_sampleRate_EngT.Text;
                SampleRate = UInt32.Parse((temp == "" ? "0" : temp));   //Get the KHz value
                SampleRate *= 1000;     //Change to Hz
            }
            catch
            {
                temp = string.Format("Sample rate set failed, will use default value {0}", this.SampleRate/1000);
                DisplayOperateMes(temp, Color.Red);
            }
            finally
            {
                this.txt_sampleRate_EngT.Text = (this.SampleRate / 1000).ToString();
            }
        }

        private void btn_VoutIP_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            rbt_signalPathSeting_Vout_EngT.Checked = true;

            Vout_IP = AverageVout();
            DisplayOperateMes("Vout @ IP = " + Vout_IP.ToString("F3"));
        }

        private void btn_Vout0A_EngT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            rbt_signalPathSeting_Vout_EngT.Checked = true;

            Vout_0A = AverageVout();
            DisplayOperateMes("Vout @ 0A = " + Vout_0A.ToString("F3"));
        }

        private void btn_Vout_PreT_Click(object sender, EventArgs e)
        {
            RePower();
            MultiSiteSocketSelect(0);
            EnterTestMode();

            int wrNum = 4;
            uint[] data = new uint[2 * wrNum];
            data[0] = 0x80;
            data[1] = Reg80Value;
            data[2] = 0x81;
            data[3] = Reg81Value;
            data[4] = 0x82;
            data[5] = Reg82Value;
            data[6] = 0x83;
            data[7] = Reg83Value;

            if (!RegisterWrite(wrNum, data))
               DisplayOperateMes("Register write failed!", Color.Red);

            EnterNomalMode();

            Delay(Delay_Fuse);

            txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");
        }

        private void btn_GainCtrlPlus_PreT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITHOUT_CAP);

            //RePower();

            //EnterTestMode();

            if (Ix_ForRoughGainCtrl < 15)
                Ix_ForRoughGainCtrl++;

            int wrNum = 4;
            uint[] data = new uint[2 * wrNum];
            data[0] = 0x80;
            data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            data[2] = 0x81;
            data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81
            data[4] = 0x82;
            data[5] = Reg82Value;                                                        //Reg0x82
            data[6] = 0x83;
            data[7] = Reg83Value;                                                        //Reg0x83

            //back up to register 
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= data[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= data[3];

            //if (!RegisterWrite(wrNum, data))
             //   DisplayOperateMes("Register write failed!", Color.Red);

            //EnterNomalMode();
            //txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");

            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITH_CAP);
        }

        private void btn_GainCtrlMinus_PreT_Click(object sender, EventArgs e)
        {
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITHOUT_CAP);

            //RePower();

            //EnterTestMode();

            if (Ix_ForRoughGainCtrl > 0)
                Ix_ForRoughGainCtrl--;

            int wrNum = 4;
            uint[] data = new uint[2 * wrNum];
            data[0] = 0x80;
            data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            data[2] = 0x81;
            data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81
            data[4] = 0x82;
            data[5] = Reg82Value;                                                        //Reg0x82
            data[6] = 0x83;
            data[7] = Reg83Value;                                                        //Reg0x83

            //back up to register 
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= data[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= data[3];

            //if (!RegisterWrite(wrNum, data))
            //    DisplayOperateMes("Register write failed!", Color.Red);

            //EnterNomalMode();
            //txt_PresetVoutIP_PreT.Text = AverageVout().ToString("F3");

            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VOUT_WITH_CAP);
        }

        private void cmb_Module_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModuleTypeIndex = (sender as ComboBox).SelectedIndex;

            //if (ModuleTypeIndex == 2)
            //{
            //    TargetOffset = 1.65;
            //    saturationVout = 3.25;
            //}
            //else if (ModuleTypeIndex == 1 )
            //{
            //    TargetOffset = 2.5;
            //    saturationVout = 4.9;
            //}
            //else 
            //{
            //    //TargetOffset = 2.5;
            //    saturationVout = 4.9;
            //}
        }

        private void numUD_SlopeK_ValueChanged(object sender, EventArgs e)
        {
            this.k_slope = (double)this.numUD_SlopeK.Value;
        }

        private void numUD_OffsetB_ValueChanged(object sender, EventArgs e)
        {
            this.b_offset = (double)this.numUD_OffsetB.Value;
        }

        private void txt_IP_EngT_TextChanged(object sender, EventArgs e)
        {
            string temp;
            try
            {
                temp = (sender as TextBox).Text;
                this.IP = double.Parse(temp); 
            }
            catch
            {
                temp = string.Format("IP set failed, will use default value {0}", this.IP);
                DisplayOperateMes(temp, Color.Red);
            }
            finally
            {
                this.IP = this.IP;  //force update GUI
            }

            TargetGain_customer = targetVoltage_customer * 1000d / IP;
        }

        private void cmb_SensitivityAdapt_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit0 & bit1 of 0x83 */
            bit_op_mask = bit0_Mask | bit1_Mask;
            uint[] valueTable = new uint[3]
            {
                0x0,
                0x03,
                0x02
            };

            int ix_TableStart = this.cmb_SensitivityAdapt_PreT.SelectedIndex;
            //back up to register and update GUI
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= valueTable[ix_TableStart];
            this.txt_SensitivityAdapt_AutoT.Text = this.cmb_SensitivityAdapt_PreT.SelectedItem.ToString();
        }

        private void cmb_TempCmp_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit4 & bit5 & bit6 of 0x81 */
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask;
            uint[] valueTable = new uint[8]
            {
                0x0,
                0x10,
                0x20,
                0x30,
                0x40,
                0x50,
                0x60,
                0x70
            };

            int ix_TableStart = this.cmb_TempCmp_PreT.SelectedIndex;
            //back up to register and update GUI
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= valueTable[ix_TableStart];            
            this.txt_TempComp_AutoT.Text = this.cmb_TempCmp_PreT.SelectedItem.ToString();
        }

        private void cmb_IPRange_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit7 of 0x82 and 0x83 */
            bit_op_mask = bit7_Mask | bit6_Mask;
            uint[] valueTable = new uint[10]
            {
                0x0,0x0,
                0x0,0x40,
                0x0,0x80,
                0x0,0xC0,
                0x80,0x0 
            };

            int ix_TableStart = this.cmb_IPRange_PreT.SelectedIndex * 2;
            //back up to register and update GUI
            Reg82Value &= ~bit7_Mask;
            Reg82Value |= valueTable[ix_TableStart];
            Reg83Value &= ~bit_op_mask;
            Reg83Value |= valueTable[ix_TableStart + 1];
            this.txt_IPRange_AutoT.Text = this.cmb_IPRange_PreT.SelectedItem.ToString();
        }

        private void cmb_SensingDirection_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit5 & bit6 of 0x82 */
            bit_op_mask = bit5_Mask | bit6_Mask;
            uint[] valueTable = new uint[4]
            {
                0x0,
                0x20,
                0x40,
                0x60
            };

            int ix_TableStart = this.cmb_SensingDirection_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg82Value &= ~bit_op_mask;
            Reg82Value |= valueTable[ix_TableStart];
        }

        private void cmb_OffsetOption_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit3 & bit4 of 0x82 */
            bit_op_mask = bit3_Mask | bit4_Mask;
            uint[] valueTable = new uint[4]
            {
                0x0,
                0x08,
                0x10,
                0x18
            };

            int ix_TableStart = this.cmb_OffsetOption_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg82Value &= ~bit_op_mask;
            Reg82Value |= valueTable[ix_TableStart];        //Reg0x82
        }

        private void cmb_PolaritySelect_EngT_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* bit1 & bit2 of 0x81 */
            bit_op_mask = bit1_Mask | bit2_Mask;
            uint[] valueTable = new uint[3]
            {
                0x0,
                0x04,
                0x06
            };

            int ix_TableStart = this.cmb_PolaritySelect_EngT.SelectedIndex;
            //back up to register and update GUI
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= valueTable[ix_TableStart];        //Reg0x81
        }

        private void cmb_SocketType_AutoT_SelectedIndexChanged(object sender, EventArgs e)
        {
            SocketType = this.cmb_SocketType_AutoT.SelectedIndex;
            if (SocketType == 0)
                DisplayOperateMes("SL610-Signle-End");
            else if (SocketType == 1)
                DisplayOperateMes("SL610-Diff-Mode");
            else if (SocketType == 2)
                DisplayOperateMes("SL620-Single-Mode");
            else
                DisplayOperateMes("Invalid Socket Type", Color.DarkRed); ;
        }

        private void rbtn_VoutOptionHigh_EngT_CheckedChanged(object sender, EventArgs e)
        {
            /* bit6 of 0x83 */
            //bit_op_mask = bit6_Mask;
            //Reg83Value &= ~bit_op_mask;
            //if (this.rbtn_VoutOptionHigh_EngT.Checked)
            //{
            //    Reg83Value |= 0x40;
            //}
            //else
            //{
            //    Reg83Value |= 0x0;
            //}
        }

        private void rbtn_InsideFilterOff_EngT_CheckedChanged(object sender, EventArgs e)
        {
            /* bit3 of 0x81 */
            bit_op_mask = bit3_Mask;
            Reg81Value &= ~bit_op_mask;
            if(rbtn_InsideFilterOff_EngT.Checked)
            {
                Reg81Value |= 0x08;
            }
            else
            {
                Reg81Value |= 0x0;
            }
        }
        
        private void btn_SaveConfig_PreT_Click(object sender, EventArgs e)
        {
            try
            {
                string filename = System.Windows.Forms.Application.StartupPath;;
                filename += @"\config.cfg";

                StreamWriter sw = File.CreateText(filename);
                sw.WriteLine("/* Current Sensor Console configs, CopyRight of SenkoMicro, Inc */");
                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/
                string msg;
                // module type: 
                msg = string.Format("module type|{0}|{1}",
                    this.cmb_Module_PreT.SelectedIndex.ToString(), this.cmb_Module_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Current Range
                msg = string.Format("IP Range|{0}|{1}",
                    this.cmb_IPRange_PreT.SelectedIndex.ToString(), this.cmb_IPRange_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Sensitivity Adapt
                msg = string.Format("Sensitivity Adapt|{0}|{1}",
                    this.cmb_SensitivityAdapt_PreT.SelectedIndex.ToString(), this.cmb_SensitivityAdapt_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Temprature Compensation
                msg = string.Format("Temprature Compensation|{0}|{1}",
                    this.cmb_TempCmp_PreT.SelectedIndex.ToString(), this.cmb_TempCmp_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // Chosen Gain
                msg = string.Format("Preset Gain|{0}|{1}",
                    this.Ix_ForRoughGainCtrl.ToString(), RoughTable_Customer[0][Ix_ForRoughGainCtrl].ToString("F2"));
                sw.WriteLine(msg);

                // Target Voltage
                msg = string.Format("Target Voltage|{0}",
                    this.txt_targetvoltage_PreT.Text );
                sw.WriteLine(msg);

                // IP
                msg = string.Format("IP|{0}",
                    this.txt_IP_PreT.Text );
                sw.WriteLine(msg);

                // ADC Offset
                msg = string.Format("ADC Offset|{0}",
                    this.txt_AdcOffset_PreT.Text);
                sw.WriteLine(msg);

                // Vout @ 0A
                msg = string.Format("Voffset|{0}|{1}",
                    this.cmb_Voffset_PreT.SelectedIndex.ToString(), this.TargetOffset.ToString());
                    //this.cmb_Voffset_PreT.SelectedIndex.ToString(), this.cmb_Voffset_PreT.SelectedItem.ToString());
                sw.WriteLine(msg);

                // bin2 accuracy
                msg = string.Format("bin2 accuracy|{0}",
                    this.txt_bin2accuracy_PreT.Text);
                sw.WriteLine(msg);

                // bin3 accuracy
                msg = string.Format("bin3 accuracy|{0}",
                    this.txt_bin3accuracy_PreT.Text);
                sw.WriteLine(msg);

                // Tab visible code
                msg = string.Format("TVC|{0}",
                    this.uTabVisibleCode);
                sw.WriteLine(msg);

                // MRE display or not
                msg = string.Format("MRE|{0}",
                    Convert.ToUInt32(bMRE));
                sw.WriteLine(msg);

                // MASK or NOT
                msg = string.Format("MASK|{0}",
                    Convert.ToUInt32(bMASK));
                sw.WriteLine(msg);

                // SAFETY READ or NOT
                msg = string.Format("SAFEREAD|{0}",
                    Convert.ToUInt32(bSAFEREAD));
                sw.WriteLine(msg);

                // Senseing Directon
                msg = string.Format("Sensing Direction |{0}|{1}",
                    this.cmb_SensingDirection_EngT.SelectedIndex.ToString(), this.cmb_SensingDirection_EngT.SelectedItem.ToString());
                sw.WriteLine(msg);

                //vout capture latency of IP ON
                msg = string.Format("Delay | {0}", this.txt_Delay_PreT.Text);
                sw.WriteLine(msg);

                sw.Close();
            }
            catch
            {
                MessageBox.Show("Save config file failed!");
            }
        }

        private void btn_loadconfig_AutoT_Click(object sender, EventArgs e)
        {
            try
            {
                string filename = System.Windows.Forms.Application.StartupPath;
                filename += @"\config.cfg";

                StreamReader sr = new StreamReader(filename);
                string comment = sr.ReadLine();
                string[] msg;
                int ix;
                /* ******************************************************
                 * module type, Current Range, Sensitivity adapt, Temprature Cmp, and preset gain 
                 * combobox type: name|combobox index|selected item text
                 * preset gain: name|index in table|percentage
                 *******************************************************/
                // module type
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_Module_PreT.SelectedIndex = ix;
                this.txt_ModuleType_AutoT.Text = msg[2];

                // IP Range
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_IPRange_PreT.SelectedIndex = ix;

                // Sensitivity adapt
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_SensitivityAdapt_PreT.SelectedIndex = ix;

                // Temprature Compensation
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_TempCmp_PreT.SelectedIndex = ix;

                // Preset Gain
                msg = sr.ReadLine().Split("|".ToCharArray());
                Ix_ForRoughGainCtrl = uint.Parse(msg[1]);

                // Target Voltage
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_targetvoltage_PreT.Text = msg[1];

                // IP
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_IP_PreT.Text = msg[1];

                // ADC Offset
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                this.txt_AdcOffset_AutoT.Text = msg[1];
                AadcOffset = double.Parse(msg[1]);

                // Vout @ 0A
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_Voffset_PreT.SelectedIndex = ix;
                this.txt_VoutOffset_AutoT.Text = msg[2];

                // bin2 accuracy
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                bin2accuracy = double.Parse(msg[1]);
                //this.txt_bin2accuracy_PreT.Text = msg[1];
                this.txt_bin2accuracy_PreT.Text = bin2accuracy.ToString();

                // bin3 accuracy
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                bin3accuracy = double.Parse(msg[1]);
                //this.txt_bin3accuracy_PreT.Text = msg[1];
                this.txt_bin3accuracy_PreT.Text = bin3accuracy.ToString();

                // Tab visible code
                msg = sr.ReadLine().Split("|".ToCharArray());
                //ix = int.Parse(msg[1]);
                uTabVisibleCode = uint.Parse(msg[1]);

                //MRE diapaly or not
                msg = sr.ReadLine().Split("|".ToCharArray());
                bMRE = Convert.ToBoolean(uint.Parse(msg[1]));

                //MASK or NOT
                msg = sr.ReadLine().Split("|".ToCharArray());
                bMASK = Convert.ToBoolean(uint.Parse(msg[1]));

                //SAFETY READ or NOT
                msg = sr.ReadLine().Split("|".ToCharArray());
                bSAFEREAD = Convert.ToBoolean(uint.Parse(msg[1]));

                // Sensing Direction
                msg = sr.ReadLine().Split("|".ToCharArray());
                ix = int.Parse(msg[1]);
                this.cmb_SensingDirection_EngT.SelectedIndex = ix;

                // Delay
                msg = sr.ReadLine().Split("|".ToArray());
                Delay_Fuse = int.Parse(msg[1]);
                this.txt_Delay_PreT.Text = msg[1];

                sr.Close();

                //Backup value for autotrim
                StoreReg80ToReg83Value();
            }
            catch
            {
                MessageBox.Show("Load config file failed, please choose correct file!");
            }
        }
       
        private void txt_targetvoltage_PreT_TextChanged(object sender, EventArgs e)
        {
            //targetVoltage_customer = double.Parse((sender as TextBox).Text);
            //TargetGain_customer = (targetVoltage_customer * 2000d) / IP;

            try
            {
                //temp = (4500d - 2000d) / double.Parse(this.txt_TargetGain.Text);
                if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                    return;
                TargetVoltage_customer = double.Parse((sender as TextBox).Text);
                //TargetGain_customer = (double.Parse((sender as TextBox).Text) * 2000d)/IP;
            }
            catch
            {
                string tempStr = string.Format("Target voltage set failed, will use default value {0}", this.TargetVoltage_customer);
                DisplayOperateMes(tempStr, Color.Red);
            }
            finally
            {
                //TargetVoltage_customer = TargetVoltage_customer;      //Force to update text to default.
            }

            TargetGain_customer = (TargetVoltage_customer * 1000d) / IP;
        }

        private void txt_ChosenGain_PreT_TextChanged(object sender, EventArgs e)
        {
            //data[1] = Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80
            //data[3] = Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81

            //Reset rough gain used register bits
            /* bit5 & bit6 & bit7 of 0x80 */
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            Reg80Value &= ~bit_op_mask;
            Reg80Value |= Convert.ToUInt32(RoughTable_Customer[1][Ix_ForRoughGainCtrl]);     //Reg0x80[1];

            /* bit0 of 0x81 */
            bit_op_mask = bit0_Mask;
            Reg81Value &= ~bit_op_mask;
            Reg81Value |= Convert.ToUInt32(RoughTable_Customer[2][Ix_ForRoughGainCtrl]);     //Reg0x81;
        }

        private void txt_reg80_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg80_PreT.Text = this.txt_reg80_EngT.Text;
        }

        private void txt_reg81_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg81_PreT.Text = this.txt_reg81_EngT.Text;
        }

        private void txt_reg82_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg82_PreT.Text = this.txt_reg82_EngT.Text;
        }

        private void txt_reg83_EngT_TextChanged(object sender, EventArgs e)
        {
            this.txt_Reg83_PreT.Text = this.txt_reg83_EngT.Text;
        }

        private void btn_AdcOut_EngT_Click(object sender, EventArgs e)
        {
            double temp = 0;
            //oneWrie_device.ADCSigPathSet(OneWireInterface.ADCControlCommand.ADC_VIN_TO_VOUT);
            rbt_signalPathSeting_AIn_EngT.Checked = true;
            //rbt_signalPathSeting_Vout_EngT.Checked = true;
            temp = AverageVout();
            this.txt_AdcOut_EngT.Text = temp.ToString("F3");
            //Vout_0A = AverageVout();
            DisplayOperateMes("ADC Out = " + temp.ToString("F3"));
        }

        private void txt_AdcOffset_PreT_TextChanged(object sender, EventArgs e)
        {
            if ((sender as TextBox).Text.ToCharArray()[(sender as TextBox).Text.Length - 1].ToString() == ".")
                return;
            AadcOffset = double.Parse((sender as TextBox).Text);
            //AadcOffset = AadcOffset;
        }

        private void cmb_Voffset_PreT_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ix = 0;
            ix = this.cmb_Voffset_PreT.SelectedIndex;
            if( ix == 0 )
            {
                TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x00;        //Reg0x82
                //Reg82Value = 0x18;
                this.cmb_OffsetOption_EngT.SelectedIndex = 0;
                this.txt_VoutOffset_AutoT.Text = "2.5";
            }
            else if( ix == 1 )
            {
                TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x08;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 1;
                this.txt_VoutOffset_AutoT.Text = "2.5";
            }
            else if (ix == 2)
            {
                if (ModuleTypeIndex == 2)
                    TargetOffset = 1.65;
                else
                    TargetOffset = 2.5;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x10;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 2;
                this.txt_VoutOffset_AutoT.Text = TargetOffset.ToString();
            }
            else if (ix == 3)
            {
                TargetOffset = 1.65;
                bit_op_mask = bit3_Mask | bit4_Mask;
                Reg82Value &= ~bit_op_mask;
                Reg82Value |= 0x18;        //Reg0x82
                //Reg82Value = 0x00;
                this.cmb_OffsetOption_EngT.SelectedIndex = 3;
                this.txt_VoutOffset_AutoT.Text = "1.65";
            }
        }

        private void btn_Vout_AutoT_Click(object sender, EventArgs e)
        {
            uint uDutCount = 16;
            uint idut = 0;
            double[] uVout = new double[16];

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            RePower();
            for (idut = 0; idut < uDutCount; idut++)
            {
                MultiSiteSocketSelect(idut);
                Delay(Delay_Power);
                uVout[idut] = AverageVout();
                DisplayOperateMes("Vout[" + idut.ToString() + "] @ 0A = " + uVout[idut].ToString("F3"));
            }

            MultiSiteDisplayVout(uVout);
        }

        private void btn_EngTab_Connect_Click(object sender, EventArgs e)
        {
            bool result = false;
            //#region One wire
            //if (!bUsbConnected)
            result = oneWrie_device.ConnectDevice();

            if (result)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.YellowGreen;
                this.toolStripStatusLabel_Connection.Text = "Connected";
                btn_GetFW_OneWire_Click(null, null);
                bUsbConnected = true;
            }
            else
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
            }
            //#endregion

            //UART Initialization
            if (oneWrie_device.UARTInitilize(9600, 1))
                DisplayOperateMes("UART Initilize succeeded!");
            else
            {
                DisplayOperateMes("UART Initilize failed!", Color.Red);
                return;
            }
            //ding hao
            Delay(Delay_Power);
            //DisplayAutoTrimOperateMes("Delay 300ms");

            //1. Current Remote CTL
            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0);


            //Delay 300ms
            //Thread.Sleep(300);
            Delay(Delay_Power);
            //DisplayAutoTrimOperateMes("Delay 300ms");

            //2. Current On
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP));

            //Delay 300ms
            Delay(Delay_Power);
            //DisplayOperateMes("Delay 300ms");

            //3. Set Voltage
            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 20u);
                //DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
            //else
            //    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));

            //numUD_pilotwidth_ow_ValueChanged(null,null);
            //numUD_pilotwidth_ow_ValueChanged(null,null);
            //num_UD_pulsewidth_ow_ValueChanged
        }

        private void init_SL910_Ip( uint sl910_ip )
        {
            bool result = false;

            result = oneWrie_device.ConnectDevice();

            if (result)
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.YellowGreen;
                this.toolStripStatusLabel_Connection.Text = "Connected";
                btn_GetFW_OneWire_Click(null, null);
                bUsbConnected = true;
            }
            else
            {
                this.toolStripStatusLabel_Connection.BackColor = Color.IndianRed;
                this.toolStripStatusLabel_Connection.Text = "Disconnected";
                return;
            }

            //UART Initialization
            if (!oneWrie_device.UARTInitilize(9600, 1))
            {
                DisplayOperateMes("UART Initilize failed!", Color.Red);
                return;
            }

            Delay(Delay_Power);

            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0);

            Delay(Delay_Power);

            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, sl910_ip);

            Delay(Delay_Power);

            oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 20u);
        }

        private void btn_EngTab_Ipoff_Click(object sender, EventArgs e)
        {
            //Set Voltage
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, 0u))
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0))
            { }//DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", 0));
            else
            {
                DisplayOperateMes(string.Format("Set Current to {0}A failed!", 0));
            }
        }

        private void IpOff( )
        {
            if ( !oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTOFF, 0))
            {
                DisplayOperateMes(string.Format("IP Off failed!"));
            }
        }

        private void IpOn()
        {
            if (!oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
            {
                DisplayOperateMes(string.Format("IP On failed!"));
            }
        }

        private void btn_EngTab_Ipon_Click(object sender, EventArgs e)
        {
            //Set Voltage
            //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, Convert.ToUInt32(IP)))
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
            {}//DisplayOperateMes(string.Format("Set Current to {0}A succeeded!", IP));
            else
            {
                DisplayOperateMes(string.Format("Set Current to {0}A failed!", IP));
            }
        }

        private void cmb_ProgramMode_AutoT_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProgramMode = this.cmb_ProgramMode_AutoT.SelectedIndex;
            if (ProgramMode == 0)
                DisplayOperateMes("Automatic Program");
            else if (ProgramMode == 1)
                DisplayOperateMes("Manual Program");
            else
                DisplayOperateMes("Invalid Program Mode", Color.DarkRed);
        }       

        private void btn_StartPoint_BrakeT_Click(object sender, EventArgs e)
        {
            double dStartPoint = 0;
            //double dStopPoint = 0;
            bool bTerminate = false;
            Ix_OffsetA_TunningTab = 0;
            Ix_OffsetB_TunningTab = 0;
            //uint[] BrakeReg = new uint[5];

            //BrakeReg = [0;0;0;0;0];

            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, TunningTabReg[0], 0x81, TunningTabReg[1], 0x82, TunningTabReg[2], 0x83, TunningTabReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != TunningTabReg[0] || tempReadback[1] != TunningTabReg[1] || tempReadback[2] != TunningTabReg[2] || tempReadback[3] != TunningTabReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }
                
                EnterNomalMode();
                dStartPoint = AverageVout();
                DisplayOperateMes("start point = " + dStartPoint.ToString("F3"));
                if (dStartPoint < 0.09)
                {
                    bTerminate = true;
                }

                if (Ix_OffsetA_TunningTab < 8)
                {
                    bit_op_mask = bit7_Mask;
                    TunningTabReg[1] &= ~bit_op_mask;
                    TunningTabReg[1] |= Convert.ToUInt32(OffsetTableA_Customer[1][Ix_OffsetA_TunningTab]);

                    bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask;
                    TunningTabReg[2] &= ~bit_op_mask;
                    TunningTabReg[2] |= Convert.ToUInt32(OffsetTableA_Customer[2][Ix_OffsetA_TunningTab]);

                    Ix_OffsetA_TunningTab++;
                }
                else if (Ix_OffsetB_TunningTab < 8)
                {
                    bit_op_mask = bit2_Mask | bit3_Mask | bit4_Mask | bit5_Mask;
                    TunningTabReg[3] &= ~bit_op_mask;
                    TunningTabReg[3] |= Convert.ToUInt32(OffsetTableB_Customer[1][Ix_OffsetB_TunningTab]);

                    Ix_OffsetB_TunningTab++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust start point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }
        }

        private void btn_StopPoint_BrakeT_Click(object sender, EventArgs e)
        {
            double dStopPoint = 0;
            bool bTerminate = false;
            Ix_GainRough_TunningTab = 0;
            Ix_GainFine_TunningTab = 0;
            //uint[] BrakeReg = new uint[5];
            TunningTabReg[0] |= 0xE0;
            TunningTabReg[1] |= 0x01;

            //BrakeReg = [0;0;0;0;0];

            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, TunningTabReg[0], 0x81, TunningTabReg[1], 0x82, TunningTabReg[2], 0x83, TunningTabReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != TunningTabReg[0] || tempReadback[1] != TunningTabReg[1] || tempReadback[2] != TunningTabReg[2] || tempReadback[3] != TunningTabReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }

                EnterNomalMode();
                dStopPoint = AverageVout();
                DisplayOperateMes("stop point = " + dStopPoint.ToString("F3"));
                if (dStopPoint >= 4.9)
                {
                    bTerminate = true;
                }

                if (Ix_GainRough_TunningTab < 16)
                {
                    bit_op_mask = bit7_Mask | bit6_Mask | bit5_Mask ;
                    TunningTabReg[0] &= ~bit_op_mask;
                    TunningTabReg[0] |= Convert.ToUInt32(RoughTable_Customer[1][Ix_GainRough_TunningTab]);

                    bit_op_mask = bit0_Mask;
                    TunningTabReg[1] &= ~bit_op_mask;
                    TunningTabReg[1] |= Convert.ToUInt32(RoughTable_Customer[2][Ix_GainRough_TunningTab]);

                    Ix_GainRough_TunningTab++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust stop point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }

            bTerminate = false;
            while (!bTerminate)
            {
                RePower();
                EnterTestMode();
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] + tempReadback[1] + tempReadback[2] + tempReadback[3] + tempReadback[4] != 0)
                {
                    DisplayOperateMes("DUT" + " has been Blown!", Color.Red);
                    PowerOff();
                    return;
                }

                RegisterWrite(4, new uint[8] { 0x80, TunningTabReg[0], 0x81, TunningTabReg[1], 0x82, TunningTabReg[2], 0x83, TunningTabReg[3] });
                BurstRead(0x80, 5, tempReadback);
                if (tempReadback[0] != TunningTabReg[0] || tempReadback[1] != TunningTabReg[1] || tempReadback[2] != TunningTabReg[2] || tempReadback[3] != TunningTabReg[3])
                {
                    DisplayOperateMes("DUT digital communication fail!", Color.Red);
                    PowerOff();
                    return;
                }

                EnterNomalMode();
                dStopPoint = AverageVout();
                DisplayOperateMes("stop point = " + dStopPoint.ToString("F3"));
                if (dStopPoint <= 4.9)
                {
                    bTerminate = true;
                }

                if (Ix_GainFine_TunningTab < 32)
                {
                    bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
                    TunningTabReg[0] &= ~bit_op_mask;
                    TunningTabReg[0] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_GainFine_TunningTab]);

                    Ix_GainFine_TunningTab++;
                }
                else
                {
                    DisplayOperateMes("Unable to adjust stop point!", Color.Red);
                    PowerOff();
                    bTerminate = true;
                }
            }
            Ix_GainFine_TunningTab--;
            bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
            TunningTabReg[0] &= ~bit_op_mask;
            TunningTabReg[0] |= Convert.ToUInt32(PreciseTable_Customer[1][Ix_GainFine_TunningTab]);
        }

        private void btn_Fuse_BrakeT_Click(object sender, EventArgs e)
        {
            bool bMarginal = false;

            //Fuse
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            RePower();
            EnterTestMode();
            RegisterWrite(5, new uint[10] { 0x80, TunningTabReg[0], 0x81, TunningTabReg[1], 0x82, TunningTabReg[2], 0x83, TunningTabReg[3], 0x84, 0x07 });
            BurstRead(0x80, 5, tempReadback);
            FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
            DisplayOperateMes("Trimming...");
            //Delay(Delay_Fuse);

            ReloadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            if (tempReadback[4] == 0)
            {
                RePower();
                EnterTestMode();
                RegisterWrite(5, new uint[10] { 0x80, TunningTabReg[0], 0x81, TunningTabReg[1], 0x82, TunningTabReg[2], 0x83, TunningTabReg[3], 0x84, 0x07 });
                BurstRead(0x80, 5, tempReadback);
                FuseClockOn(DeviceAddress, (double)num_UD_pulsewidth_ow_EngT.Value, (double)numUD_pulsedurationtime_ow_EngT.Value);
                DisplayOperateMes("Trimming...");
                //Delay(Delay_Fuse);
            }
            Delay(Delay_Sync);

            MarginalReadPreset();
            Delay(Delay_Sync);
            BurstRead(0x80, 5, tempReadback);
            bMarginal = false;
            if (bMASK)
            {
                if (((tempReadback[0] & 0xE0) != (TunningTabReg[0] & 0xE0)) | (tempReadback[1] & 0x81) != (TunningTabReg[1] & 0x81) |
                    (tempReadback[2] & 0x99) != (TunningTabReg[2] & 0x99) | (tempReadback[3] & 0x83) != (TunningTabReg[3] & 0x83) | (tempReadback[4] < 1))
                    bMarginal = true;
            }
            else
            {
                if (((tempReadback[0] & 0xFF) != (TunningTabReg[0] & 0xFF)) | (tempReadback[1] & 0xFF) != (TunningTabReg[1] & 0xFF) |
                        (tempReadback[2] & 0xFF) != (TunningTabReg[2] & 0xFF) | (tempReadback[3] & 0xFF) != (TunningTabReg[3] & 0xFF) | (tempReadback[4] < 7))
                    bMarginal = true;
            }
            if (bMarginal)
            {
                DisplayOperateMes("MRE");
            }

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            PowerOff();

        }
       
        private void btn_CommunicationTest_Click(object sender, EventArgs e)
        {
            //bool bCommPass = false;
            uint channel;
            channel = Convert.ToUInt32(tb_Channel_AutoTab.Text);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.sp
            MultiSiteSocketSelect(channel);

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            //RePower();
            //EnterTestMode();
            //RegisterWrite(5, new uint[10] { 0x80, 0xAA, 0x81, 0xAA, 0x82, 0xAA, 0x83, 0xAA, 0x84, 0x07 });
            ////DisplayOperateMes("Write In Data is: ");
            //DisplayOperateMes("Reg{0} = 0xAA");
            //DisplayOperateMes("Reg{1} = 0xAA");
            //DisplayOperateMes("Reg{2} = 0xAA");
            //DisplayOperateMes("Reg{3} = 0xAA");
            //DisplayOperateMes("Reg{4} = 0x07");
            //BurstRead(0x80, 5, tempReadback);

            //if (tempReadback[0]!=0xAA || tempReadback[1]!=0xAA || tempReadback[2]!=0xAA || tempReadback[3]!=0xAA || tempReadback[4]!=0x07)
            //{
            //    //bCommPass = false;
            //    DisplayOperateMes("Communication Fail!", Color.Red);
            //    return;
            //}

            //Delay(Delay_Sync);

            //RegisterWrite(5, new uint[10] { 0x80, 0x55, 0x81, 0x55, 0x82, 0x55, 0x83, 0x55, 0x84, 0x07 });
            //DisplayOperateMes("Write In Data is: ");
            //DisplayOperateMes("Reg{0} = 0x55");
            //DisplayOperateMes("Reg{1} = 0x55");
            //DisplayOperateMes("Reg{2} = 0x55");
            //DisplayOperateMes("Reg{3} = 0x55");
            //DisplayOperateMes("Reg{4} = 0x07");
            //BurstRead(0x80, 5, tempReadback);

            //if (tempReadback[0] != 0x55 || tempReadback[1] != 0x55 || tempReadback[2] != 0x55 || tempReadback[3] != 0x55 || tempReadback[4] != 0x07)
            //{
            //    //bCommPass = false;
            //    DisplayOperateMes("Communication Fail!", Color.Red);
            //    return;
            //}

            DisplayOperateMes("Communication Pass! ");
        }                  

        private void btn_SafetyHighRead_EngT_Click(object sender, EventArgs e)
        {
            rbt_signalPathSeting_Vout_EngT.Checked = true;
            rbt_signalPathSeting_Config_EngT.Checked = true;

            SafetyHighReadPreset();
        }             

        private void btn_BrakeTab_InitializeUart_Click(object sender, EventArgs e)
        {
            #region UART Initialize
            //if (ProgramMode == 0)
            {

                //UART Initialization
                if (oneWrie_device.UARTInitilize(9600, 1))
                    DisplayOperateMes("UART Initilize succeeded!");
                else
                    DisplayOperateMes("UART Initilize failed!");
                //ding hao
                Delay(Delay_Sync);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //1. Current Remote CTL
                //if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_REMOTE, 0))
                //    DisplayOperateMes("Set Current Remote succeeded!");
                //else
                //    DisplayOperateMes("Set Current Remote failed!");

                //Delay 300ms
                //Thread.Sleep(300);
                Delay(Delay_Sync);
                //DisplayAutoTrimOperateMes("Delay 300ms");

                //2. Current On
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_OUTPUTON, 0))
                    DisplayOperateMes("Set Current On succeeded!");
                else
                    DisplayOperateMes("Set Current On failed!");

                //Delay 300ms
                Delay(Delay_Sync);
                //DisplayOperateMes("Delay 300ms");

                //3. Set Voltage
                if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETVOLT, 6u))
                    DisplayOperateMes(string.Format("Set Voltage to {0}V succeeded!", 6));
                else
                    DisplayOperateMes(string.Format("Set Voltage to {0}V failed!", 6));


                //Delay 300ms
                Delay(Delay_Sync);
                //DisplayOperateMes("Delay 300ms");


            }
            #endregion UART Initialize





        }

        private void txt_Delay_PreT_TextChanged(object sender, EventArgs e)
        {
            Delay_Fuse = int.Parse(txt_Delay_PreT.Text);
        }
        
        #endregion Events

        #region SL910 Tab
        private void btn_Eng_SL910_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            //numUD_pilotwidth_ow_ValueChanged(null, null);
            numUD_pilotwidth_ow_ValueChanged(null, null);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            Delay(Delay_Sync);

            //try
            {
                //string temp;
                uint _dev_addr = this.DeviceAddress;

                //Enter test mode
                uint _reg_addr = 0x55;
                uint _reg_data = 0xAA;
                oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                Thread.Sleep(1);
            }
        }

        private void btn_Eng_TestCom_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            //numUD_pilotwidth_ow_ValueChanged(null, null);

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //rbt_signalPathSeting_Config_EngT.Checked = true;

            //try
            {
                //string temp;
                uint _dev_addr = this.DeviceAddress;

                //Enter test mode
                uint _reg_addr = 0x42;
                uint _reg_data = 0x01;
                oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                Thread.Sleep(1);
            }
        }

        private void btn_Eng_Test_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;
            string temp;

            uint _reg_Addr = 0x00;
            uint _reg_Value = 0x00;

            temp = this.txt_Eng_910Addr.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
            //uint _reg_Addr = 0x80;
            _reg_Addr = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

            temp = this.txt_Eng_910Data.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
            //uint _reg_Addr = 0x80;
            _reg_Value = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);

            

            if (oneWrie_device.I2CWrite_Single(_dev_addr, _reg_Addr, _reg_Value))
                DisplayOperateMes("Write Reg0x"+ _reg_Addr.ToString("X")+":(0x" + _reg_Value.ToString("X") + ") succeeded!");
            else
                DisplayOperateMes("Write Reg Failed!", Color.Red);
        }

        private void btn_Eng_test_read_Click(object sender, EventArgs e)
        {
            string temp;

            uint _reg_addr_start = 0x80;
            uint[] _readBack_data = new uint[1];

            temp = this.txt_Eng_910Addr.Text.TrimStart("0x".ToCharArray()).TrimEnd("H".ToCharArray());
            //uint _reg_Addr = 0x80;

            _reg_addr_start = UInt32.Parse((temp == "" ? "0" : temp), System.Globalization.NumberStyles.HexNumber);
            
            BurstRead(_reg_addr_start, 1, _readBack_data);
        }

        private void btn_Eng_Analogmode_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;

            //Enter test mode
            uint _reg_addr = 0x42;
            uint _reg_data = 0x04;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
            //oneWrie_device.I2CWriteSingleAux(_dev_addr, _reg_addr, _reg_data);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
        }

        private void btn_SL910_TestMode_Click(object sender, EventArgs e)
        {
            btn_Eng_SL910_Click(null,null);
            Delay(Delay_Sync);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
            //btn_Eng_TestCom_Click(null, null);
        }

        private void btn_SL910_NormalMode_Click(object sender, EventArgs e)
        {
            btn_Eng_Analogmode_Click(null, null);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
        }

        private void btn_SL1910_Wrtie_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            uint _reg_data = 0x00;
            

            totalRows = SL910_Tab_DataGridView.Rows.Count -1;

            for (index = 0; index < totalRows ; index++ )
            {
                if (Convert.ToBoolean(SL910_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL910_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    st = SL910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    _reg_data = Convert.ToUInt32(st, 16);

                    oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
                    
                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": write Value 0x" + _reg_data.ToString("X2") + " to Reg 0x" + _reg_addr.ToString("X2") ;
                    DisplayOperateMes(st);
                }
                Thread.Sleep(5);

            }  
        }

        private void btn_SL910_Read_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            uint[] _reg_data = new uint[2];

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //Delay(Delay_Power);

            totalRows = SL910_Tab_DataGridView.Rows.Count - 1;

            for (index = 0; index < totalRows ; index++)
            {
                if (Convert.ToBoolean(SL910_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL910_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    //st = SL910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    //_reg_data = Convert.ToUInt32(st, 16);

                    //_reg_data[0] = oneWrie_device.I2CRead_Single(_dev_addr, _reg_addr);
                    oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr, 2, _reg_data);
                    SL910_Tab_DataGridView.Rows[index].Cells[3].Value = _reg_data[0].ToString("X2");

                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": read Value 0x" + _reg_data[0].ToString("X2") + " from Reg 0x" + _reg_addr.ToString("X2");
                    DisplayOperateMes(st);
                }
                Thread.Sleep(5);

            }  
        }
       
        private void btn_SL1910_WriteAll_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            uint _reg_data = 0x00;


            totalRows = SL910_Tab_DataGridView.Rows.Count - 1;

            for (index = 0; index < totalRows; index++)
            {
                //if (Convert.ToBoolean(SL910_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL910_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    st = SL910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    _reg_data = Convert.ToUInt32(st, 16);

                    oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": write Value 0x" + _reg_data.ToString("X2") + " to Reg 0x" + _reg_addr.ToString("X2");
                    DisplayOperateMes(st);

                    //st = "ID = " + (index + 1).ToString() + ": write Value " + _reg_data.ToString("D") + " to Reg " + _reg_addr.ToString("D");
                    //DisplayOperateMes(st + "\r\n");

                }
                Thread.Sleep(5);

            }  
        }

        private void btn_SL1910_ReadAll_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            //uint _reg_data = 0x00;
            uint[] _reg_data = new uint[2];


            totalRows = SL910_Tab_DataGridView.Rows.Count - 1;

            for (index = 0; index < totalRows; index++)
            {
                //if (Convert.ToBoolean(SL910_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL910_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    //st = SL910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    //_reg_data = Convert.ToUInt32(st, 16);

                    oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr, 2, _reg_data);
                    //_reg_data[0] = oneWrie_device.I2CRead_Single(_dev_addr, _reg_addr);
                    SL910_Tab_DataGridView.Rows[index].Cells[3].Value = _reg_data[0].ToString("X2");

                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": read Value 0x" + _reg_data[0].ToString("X2") + " from Reg 0x" + _reg_addr.ToString("X2");
                    DisplayOperateMes(st);
                }
                Thread.Sleep(5);

            }  
        }
       
        private void btn_SL910_ReadFuse_Click(object sender, EventArgs e)
        {
            uint readLevel = 1;
            uint _dev_addr = this.DeviceAddress;
            uint[] tempReadback = new uint[5];

            btn_Eng_SL910_Click(null, null);
            for(uint i =0; i<8; i++)
            {
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, (uint)(0x81+i), 0xFF);
            }
            //Delay(Delay_Sync);
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0xFF);
            //Delay(Delay_Sync);
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0xFF);
            //Delay(Delay_Sync);
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x88, 0xFF);
            //Delay(Delay_Sync);
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x89, 0xFF);

            Delay(Delay_Sync);
            DisplayOperateMes("Read before FUSE");
            //BurstRead(0x80, 5, tempReadback);
            //for (uint i = 0; i < 5; i++)
            //    tempReadback[i] = 0;
            
            BurstRead(0x85, 5, tempReadback);
            //oneWrie_device.I2CRead_Burst(_dev_addr, 0x85, 5, tempReadback);
            //for (int i = 0; i < 5; i++)
            //    DisplayOperateMes("Reg0x" + (0x85 + i).ToString("X2") + " = 0x" + tempReadback[0].ToString("X2"));

            Delay(Delay_Sync);
            btn_SL910_FuseBank2_Click(null, null);
            Delay(Delay_Fuse);


            readLevel = uint.Parse(this.txt_SL910_ReadLevel.Text);
            //Write 0x00/01/02/03 to reg45 to select read comparsion R value
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x45, readLevel);
            //Write 0x02 to reg43, enable fuse_bank1_supply	
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x02);
            ////Write 0x12 to reg43, enable fuse_bank1_supply, enable fuse read
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x12);
            ////wait for ?ms		
            //Delay(Delay_Sync);
            ////Write 0x32 to reg43, enable fuse_bank1_supply, enable fuse read, enable fuse_read_latch to latch fuse value to internal register	
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x32);
            ////Write 0x12 to reg43, enable fuse_bank1_supply, enable fuse read, disable fuse_latch	
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x12);
            ////Write 0x02 to reg43, enable fuse_bank1_supply, disable fuse_read
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x02);
            ////Write 0x00 to reg43, disable fuse_bank1_supply	
            //oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x00);
            //Write 0x04 to reg43, enable fuse_bank2_supply	
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x04);
            //Write 0x14 to reg43, enable fuse_bank2_supply, enable fuse read
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x14);
            //wait for ?ms	
            Delay(Delay_Sync);
            //Write 0x34 to reg43, enable fuse_bank2_supply, enable fuse read, enable fuse_read_latch to latch fuse value to internal register
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x34);
            //Write 0x14 to reg43, enable fuse_bank2_supply, enable fuse read,disable fuse_latch	
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x14);
            //Write 0x04 to reg43, enable fuse_bank2_supply, disable fuse_read		
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x04);
            //Write 0x00 to reg43, disable fuse_bank2_supply	
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x43, 0x00);
            //read reg80~89 to check OTP reload value	
            DisplayOperateMes("Read after FUSE");
            BurstRead(0x85, 5, tempReadback);  

            if ((tempReadback[0] == 0xFF) && (tempReadback[1] == 0xFF) && (tempReadback[2] == 0xFF) && (tempReadback[3] == 0xFF))
                DisplayOperateMes("***Bank2 PASS***", Color.Green);
            else
                DisplayOperateMes("***Bank2 FAIL***", Color.DarkRed);

            //for (uint i = 0; i < 5; i++)
            //    tempReadback[i] = 0;

            //DisplayOperateMes("Read after FUSE");
            //BurstRead(0x80, 5, tempReadback);

            //if ((tempReadback[4] == 0xFF) && (tempReadback[1] == 0xFF) && (tempReadback[2] == 0xFF) && (tempReadback[3] == 0xFF))
            //    DisplayOperateMes("***Bank1PASS***", Color.Green);
            //else
            //    DisplayOperateMes("***Bank1 FAIL***", Color.DarkRed);
        } 
               
        private void txt_SL910_ReadLevel_TextChanged(object sender, EventArgs e)
        {

        }

        private void btn_SL910_FuseBank2_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;

            //Enter test mode
            uint _reg_addr = 0x43;
            uint _reg_data = 0x04;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x05;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x44;
            _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            btn_fuse_clock_ow_EngT_Click(null, null);

            Thread.Sleep(500);

            _reg_addr = 0x44;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
        }

        private void btn_SL910_FuseBank1_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;

            //Enter test mode
            uint _reg_addr = 0x43;
            uint _reg_data = 0x02;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x03;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x44;
            _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            btn_fuse_clock_ow_EngT_Click(null, null);

            Thread.Sleep(500);

            _reg_addr = 0x44;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
        }       

        private void btn_SL910_910out_Click(object sender, EventArgs e)
        {
            double x = 0;
            //double sl910out = 0;
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
            //Delay(Delay_Power);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //Delay(Delay_Power);
            if (!oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT))
            {
                DisplayOperateMes("HW Error!");
                return;
            }
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);


            x = AverageVout();
            //sl910out = x;
            //if(x<2.505)
            //    sl910out = -1.2212453270876722e-15 * x * x * x * x + 0.002678256381932087 * x * x * x
            //        - 0.012092327564404926 * x * x - 1.9864759515650032 * x + 5.007905923266363;
            ////sl910out = 5 - 2 * sl910out;
            //else
            //    sl910out = -0.0013411399008066843 * x * x * x * x + 0.020141239030325386 * x * x * x 
            //        - 0.11259555996798598 * x * x - 1.726360653315231 * x + 4.769278793797818;
            DisplayOperateMes("vout = " + x.ToString("F3"));
        }

        #endregion SL910

        #region Char Tab
        private void btn_Char910_Load_Click(object sender, EventArgs e)
        {
            InitChar910TabDataGrid();
        }

        private void btn_Char910_Save_Click(object sender, EventArgs e)
        {

        }

        private void btn_Char910_Excute_Click(object sender, EventArgs e)
        {
            string st;
            uint _dev_addr = this.DeviceAddress;
            uint[] _reg_data = new uint[2];
            uint _reg_addr = 0x00;
            int totalRows = Char910_Tab_DataGridView.Rows.Count;

            for ( int index = 0; index < totalRows; index++)
            {
                st = Convert.ToString(Char910_Tab_DataGridView.Rows[index].Cells[2].Value);

                switch (st)
                {
                    case "Wrtie Reg":
                    case "WREG":
                    case "WR":
                    case "WRITE REG":
                    case "write reg":
                    case "wreg":
                    case "wr":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        Color backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        numUD_pilotwidth_ow_ValueChanged(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr,
                            Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString(), 16),
                            Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[4].Value.ToString(), 16));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "Read Reg":
                    case "RREG":
                    case "RR":
                    case "READ REG":
                    case "read reg":
                    case "rreg":
                    case "rr":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        numUD_pilotwidth_ow_ValueChanged(null, null);
                        Delay(Delay_Sync);
                        st = Convert.ToString(Char910_Tab_DataGridView.Rows[index].Cells[3].Value);
                        _reg_addr = Convert.ToUInt32(st, 16);
                        oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr, 2, _reg_data);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "0x"+ _reg_data[0].ToString("X2");
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "config to vout":
                    case "configtovout":
                    case "Config To Vout":
                    case "ConfigToVout":
                    case "CONFIGTOVOUT":
                    case "CONFIG TO VOUT":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "ain to mout":
                    case "aintomout":
                    case "Ain To Mout":
                    case "AinToMout":
                    case "AINTOMOUT":
                    case "AIN TO MOUT":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "get910out":
                    case "get 910 out":
                    case "Get910Out":
                    case "Get 910 Out":
                    case "GET910OUT":
                    case "GET 910 OUT":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
                        btn_SL910_910out_Click(null, null);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = sl910out.ToString("F3");
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "getvout":
                    case "get vout":
                    case "GetVout":
                    case "Get Vout":
                    case "GETVOUT":
                    case "GET VOUT":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                        Delay(Delay_Sync);
                        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                        Delay(Delay_Sync);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = AverageVout().ToString("F3");
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "910othermode":
                    case "910 other mode":
                    case "910 Other Mode":
                    case "910OtherMode":
                    case "910 OTHER MODE":
                    case "910OTHERMODE":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        btn_SL910_TestMode_Click(null,null);
                        Delay(Delay_Sync);
                        oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
                        Delay(Delay_Sync);
                        //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                        //Delay(Delay_Sync);
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep910offset":
                    case "sweep 910 offset":
                    case "Sweep910Offset":
                    case "Sweep 910 Offset":
                    case "SWEEP910OFFSET":
                    case "SWEEP 910 OFFSET":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep910Offset();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep910roughgain":
                    case "sweep 910 rough gain":
                    case "Sweep910RoughGain":
                    case "Sweep 910 Rough Gain":
                    case "SWEEP910ROUGHGAIN":
                    case "SWEEP 910 ROUGH GAIN":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep910RoughGain();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep910finegain":
                    case "sweep 910 fine gain":
                    case "Sweep910FineGain":
                    case "Sweep 910 Fine Gain":
                    case "SWEEP910FINEGAIN":
                    case "SWEEP 910 FINE GAIN":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep910FineGain();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep910linearity":
                    case "sweep 910 linearity":
                    case "Sweep910Linearity":
                    case "Sweep 910 Linearity":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep910Linearity();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "initdmm":
                    case "init dmm":
                    case "Init Dmm":
                    case "InitDmm":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        if (InitDmmPort(Char910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString()))
                        {
                            Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Success";
                            Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                            Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                            Char910_Tab_DataGridView.Update();
                        }
                        else
                        {
                            Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Fail";
                            Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_WRONG;
                            Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                            Char910_Tab_DataGridView.Update();
                            return;
                        }
                        break;

                    case "sweep620linearity":
                    case "sweep 620 linearity":
                    case "Sweep620Linearity":
                    case "Sweep 620 Linearity":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Linearity();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620roughgain":
                    case "sweep 620 rough gain":
                    case "Sweep620RoughGain":
                    case "Sweep 620 Rough Gain":
                    case "SWEEP620ROUGHGAIN":
                    case "SWEEP 620 ROUGH GAIN":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620RoughGain();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620finegain":
                    case "sweep 620 fine gain":
                    case "Sweep620FineGain":
                    case "Sweep 620 Fine Gain":
                    case "SWEEP620FINEGAIN":
                    case "SWEEP 620 FINE GAIN":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620FineGain();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620offset":
                    case "sweep 620 offset":
                    case "Sweep620Offset":
                    case "Sweep 620 Offset":
                    case "SWEEP620OFFSET":
                    case "SWEEP 620 OFFSET":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Offset();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620vout0p1vddoffset":
                    case "sweep 620 vout 0p1vdd offset":
                    case "Sweep620Vout0p1vddOffset":
                    case "Sweep 620 Vout 0p1vdd Offset":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Vout0p1vddOffset();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620tc1":
                    case "sweep 620 tc1":
                    case "Sweep620Tc1":
                    case "Sweep 620 Tc1":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Tc1();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620tc2":
                    case "sweep 620 tc2":
                    case "Sweep620Tc2":
                    case "Sweep 620 Tc2":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Tc2();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620vbg":
                    case "sweep 620 vbg":
                    case "Sweep620Vbg":
                    case "Sweep 620 Vbg":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620Vbg();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620voutoption":
                    case "sweep 620 vout option":
                    case "Sweep620VoutOption":
                    case "Sweep 620 Vout Option":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SweepVoutOption();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "trimtest":
                    case "trim test":
                    case "TrimTest":
                    case "Trim Test":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        TrimTest( Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "trimtest5v":
                    case "trim test 5v":
                    case "TrimTest5v":
                    case "Trim Test 5v":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        TrimTest5v(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sweep620halfvddoffset":
                    case "sweep 620 half vdd offet":
                    case "sweep620HalfVddOffset":
                    case "sweep620 Half Vdd Offset":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sweep620HalfVddOffset();
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc780 char":
                    case "sc780char":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        Sc780Char0623(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "auto calc 620 trim code":
                    case "autocalc620trimcode":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        AutoCalcSL620TrimCode(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value), Convert.ToDouble(Char910_Tab_DataGridView.Rows[index].Cells[4].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sl620 reliability":
                    case "sl620reliability":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        ReliabilityTest(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sl620 power cycle":
                    case "sl620powercycle":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        PowerCycleTest(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc810 char 20a":
                    case "sc810char20a":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SC810_Char_Test_20A(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc810 char 30a":
                    case "sc810char30a":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SC810_Char_Test_30A(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc810 char 10a":
                    case "sc810char10a":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SC810_Char_Test_10A(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc810 char 10ap":
                    case "sc810char10ap":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SC810_Char_Test_10AP(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;

                    case "sc810b mag off":
                    case "sc810bmagoff":
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Processing";
                        backcolorbackup = Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = Color.LightGreen;
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_PROCESSING;
                        Char910_Tab_DataGridView.Update();
                        SC810b_MagOffset(Convert.ToUInt32(Char910_Tab_DataGridView.Rows[index].Cells[3].Value));
                        Char910_Tab_DataGridView.Rows[index].Cells[6].Value = "Done";
                        Char910_Tab_DataGridView.Rows[index].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_RIGHT;
                        Char910_Tab_DataGridView.Rows[index].DefaultCellStyle.BackColor = backcolorbackup;
                        Char910_Tab_DataGridView.Update();
                        break;
                    
                    default:
                        DisplayOperateMes("Invalid Command!");
                        break;
                }

            }  
        }

        private bool InitDmmPort(string str)
        {
            //DisplayOperateMes(dmm.InitSerialPort(str).ToString());
            ////dmm.InitSerialPort(str);
            //double v = 0;
            //v = dmm.readVolt();
            //DisplayOperateMes(v.ToString("F6"));
            //return false;

            return dmm.InitSerialPort(str);            
        }

        private void Sweep910Linearity()
        {
            int delay_temp = 2000;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep910Linearity-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            //string dataFilePath = @"C:\temp\CST\SweepGyroOffsOtp2-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            //string dataFileType = ".csv";
            //string filePath = dataFilePath + dataFileType;

            btn_EngTab_Connect_Click(null, null);

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "IP,Vout1,Vout2,Vout3,Vout4,Vout5";
                writer.WriteLine(headers);

                //string tempstring;

                for (uint i = 20; i > 0; i--)
                {
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    SetIP(i);
                    Delay(Delay_Power);
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    for (uint k = 0; k < 5; k++)
                    {
                        //tempvout[k] = GetMout();
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }

                btn_EngTab_Ipoff_Click(null, null);

                //MessageBox("Please Invert IP!");
                DialogResult dr = MessageBox.Show("Please Invert IP", "IP", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                    return;
                else if( dr == DialogResult.Yes)
                {
                    for (uint i = 0; i < 21; i++)
                    {
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(delay_temp);
                        SetIP(i);
                        Delay(Delay_Power);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(delay_temp);
                        for (uint k = 0; k < 5; k++)
                        {
                            //tempvout[k] = GetMout();
                            tempvout[k] = dmm.readVolt();
                            Delay(Delay_Power);
                        }
                        writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                            + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                    }

                    btn_EngTab_Ipoff_Click(null, null);
                }
                

            }
        }

        private void Sweep910RoughGain()
        {
            int delay_temp = 1000;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep910RoughGain-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 16;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "IP,Vout1,Vout2,Vout3,Vout4,Vout5";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(20);
                Delay(Delay_Power);
                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, i<<4);
                    Delay(delay_temp);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }

                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0);
                btn_EngTab_Ipoff_Click(null, null);
                //SetIP(0);
            }
        }

        private void Sweep910FineGain()
        {
            int delay_temp = 1000;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep910FineGain-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 32;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "IP,Vout1,Vout2,Vout3,Vout4,Vout5";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(20);
                Delay(Delay_Power);
                btn_EngTab_Ipon_Click(null, null);
                Delay(delay_temp);

                for (uint i = 0; i < count; i++)
                {
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x88, i);
                    Delay(delay_temp);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }

                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0);
                btn_EngTab_Ipoff_Click(null, null);
                //SetIP(0);
            }
        }

        private void Sweep910Offset()
        {
            int delay_temp = 100;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep910Offset-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 128;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "IP,Vout1,Vout2,Vout3,Vout4,Vout5";
                writer.WriteLine(headers);

                //string tempstring;

                for (uint i = 0; i < count; i++)
                {
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, i | 0x80);
                    Delay(delay_temp);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }


                for (uint i = 0; i < count; i++)
                {
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, i);
                    Delay(delay_temp);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }

                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0);
            }
        }

        private void SetIP( uint ipcurrent )
        {
            if (oneWrie_device.UARTWrite(OneWireInterface.UARTControlCommand.ADI_SDP_CMD_UART_SETCURR, ipcurrent))
                DisplayOperateMes("Set Current to " + ipcurrent.ToString() + "A");
            else
                DisplayOperateMes("Set Current Fail", Color.Red);
        }

        private double GetMout()
        {
            double x = 0;
            //double sl910out = 0;
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
            Delay(Delay_Power);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //Delay(Delay_Power);
            x = AverageVout();
            //sl910out = x;
            if (x < 2.505)
                sl910out = -1.2212453270876722e-15 * x * x * x * x + 0.002678256381932087 * x * x * x
                    - 0.012092327564404926 * x * x - 1.9864759515650032 * x + 5.007905923266363;
            //sl910out = 5 - 2 * sl910out;
            else
                sl910out = -0.0013411399008066843 * x * x * x * x + 0.020141239030325386 * x * x * x
                    - 0.11259555996798598 * x * x - 1.726360653315231 * x + 4.769278793797818;
            DisplayOperateMes("SL910 vout = " + sl910out.ToString("F3"));
            return sl910out;
        }   

        private void btn_SL910_910to94_out_Click(object sender, EventArgs e)
        {
            btn_Eng_Analogmode_Click(null, null);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
        }

        private void btn_SOP14_TestMode_Click(object sender, EventArgs e)
        {
            btn_Eng_SL910_Click(null, null);
            Delay(Delay_Sync);
            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_MOUT);
            btn_Eng_TestCom_Click(null, null);
        }

        private void Char910_Tab_DataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            //DisplayOperateMes("added a new row");
            int index = Char910_Tab_DataGridView.RowCount;
            Char910_Tab_DataGridView.Rows[index - 1].Cells[0].Value = CurrentSensorV3.Properties.Resources.PROCESS_READY;
            Char910_Tab_DataGridView.Update();
        }
        #endregion Char910

        #region SL620

        private void btn_SL620Tab_PowerOn_Click(object sender, EventArgs e)
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_5V);
            btn_PowerOn_OWCI_ADC_Click(null,null);
            btn_SL620Tab_PowerOn6V.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn.BackColor = Color.GreenYellow;
            btn_SL620Tab_PowerOn3v3.BackColor = Color.Transparent;
        }

        private void btn_SL620Tab_PowerOn3v3_Click(object sender, EventArgs e)
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_3V3);
            btn_PowerOn_OWCI_ADC_Click(null, null);
            btn_SL620Tab_PowerOn6V.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn3v3.BackColor = Color.GreenYellow;
        }

        private void btn_SL620Tab_PowerOff_Click(object sender, EventArgs e)
        {
            btn_PowerOff_OWCI_ADC_Click(null, null);
            btn_SL620Tab_PowerOn.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn6V.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn3v3.BackColor = Color.Transparent;
        }

        private void btn_SL620Tab_TestKey_Click(object sender, EventArgs e)
        {
            //set pilot firstly
            numUD_pilotwidth_ow_ValueChanged(null, null);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            rbt_signalPathSeting_Config_EngT.Checked = true;

            EnterTestMode();
        }

        private void btn_SL620Tab_NormalMode_Click(object sender, EventArgs e)
        {
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
            //rbt_signalPathSeting_Config_EngT.Checked = true;
            //Thread.Sleep(100);
            Delay(Delay_Sync);

            uint _reg_addr = 0x55;
            uint _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);

            Delay(Delay_Sync);

            _reg_data = oneWrie_device.I2CRead_Single(this.DeviceAddress, 0x42);

            _reg_addr = 0x42;
            _reg_data |= 0x02;

            bool writeResult = oneWrie_device.I2CWrite_Single(this.DeviceAddress, _reg_addr, _reg_data);
            //Console.WriteLine("I2C write result->{0}", oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data));
            if (writeResult)
            {
                if (bAutoTrimTest)
                {
                    DisplayOperateMes("Enter Nomal Mode succeeded!");
                }
            }
            else
                DisplayOperateMes("I2C write failed, Enter Normal Mode Failed!", Color.Red);

            //Thread.Sleep(100);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);

            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
        }

        private void btn_SL620Tab_ReadSelect_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            uint[] _reg_data = new uint[2];

            //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
            //Delay(Delay_Power);

            totalRows = SL620_Tab_DataGridView.Rows.Count - 1;

            for (index = 0; index < totalRows; index++)
            {
                if (Convert.ToBoolean(SL620_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL620_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    //st = SL910_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    //_reg_data = Convert.ToUInt32(st, 16);

                    //_reg_data[0] = oneWrie_device.I2CRead_Single(_dev_addr, _reg_addr);
                    oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr, 2, _reg_data);
                    SL620_Tab_DataGridView.Rows[index].Cells[3].Value = _reg_data[0].ToString("X2");

                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": read Value 0x" + _reg_data[0].ToString("X2") + " from Reg 0x" + _reg_addr.ToString("X2");
                    DisplayOperateMes(st);
                }
                Thread.Sleep(5);

            }  
        }

        private void btn_SL620Tab_WriteSelect_Click(object sender, EventArgs e)
        {
            string st;
            int index = 0;
            int totalRows = 0;
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x00;
            uint _reg_data = 0x00;


            totalRows = SL620_Tab_DataGridView.Rows.Count - 1;

            for (index = 0; index < totalRows; index++)
            {
                if (Convert.ToBoolean(SL620_Tab_DataGridView.Rows[index].Cells[0].Value) == true)
                {
                    st = Convert.ToString(SL620_Tab_DataGridView.Rows[index].Cells[2].Value);
                    _reg_addr = Convert.ToUInt32(st, 16);

                    st = SL620_Tab_DataGridView.Rows[index].Cells[3].Value.ToString();
                    _reg_data = Convert.ToUInt32(st, 16);

                    oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

                    //DisplayOperateMes(st);
                    st = "ID = " + (index + 1).ToString() + ": write Value 0x" + _reg_data.ToString("X2") + " to Reg 0x" + _reg_addr.ToString("X2");
                    DisplayOperateMes(st);
                }
                Thread.Sleep(5);

            }  
        }

        private void btn_SL620Tab_TrimSet1_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;

            //Enter test mode
            uint _reg_addr = 0x43;
            uint _reg_data = 0x03;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            //Thread.Sleep(50);

            //_reg_addr = 0x43;
            //_reg_data = 0x05;
            //oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x44;
            _reg_data = 0xAA;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            btn_fuse_clock_ow_EngT_Click(null, null);

            Thread.Sleep(800);

            _reg_addr = 0x44;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
        }

        private void btn_SL620Tab_PowerOn6V_Click(object sender, EventArgs e)
        {
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VDD_FROM_EXT);
            btn_PowerOn_OWCI_ADC_Click(null, null);
            btn_SL620Tab_PowerOn6V.BackColor = Color.GreenYellow;
            btn_SL620Tab_PowerOn.BackColor = Color.Transparent;
            btn_SL620Tab_PowerOn3v3.BackColor = Color.Transparent;
        }

        private void btn_SL620Tab_ReadTrim_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;

            oneWrie_device.I2CWrite_Single(_dev_addr, 0x42, 0x0C);

            Thread.Sleep(50);

            //Enter test mode
            uint _reg_addr = 0x43;
            uint _reg_data = 0x02;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x0E;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x06;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x02;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);

            Thread.Sleep(50);

            _reg_addr = 0x43;
            _reg_data = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, _reg_addr, _reg_data);
        }

        private void rb_SL620Tab_DigitalMode_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_SL620Tab_DigitalMode.Checked)
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_CONFIG_TO_VOUT);
                Thread.Sleep(50);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITHOUT_CAP);
                DisplayOperateMes("Digital mode, Vout no Cap");
            }
            else if (rb_SL620Tab_AnalogMode.Checked)
            {
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
                Thread.Sleep(50);
                oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
                DisplayOperateMes("Analog mode, Vout with Cap");
            }
        }

        private void Sweep620Linearity()
        {
            int delay_temp = 300;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\Sweep780Linearity";
            filename += ".csv";

            //power on 5V
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);
            //enter test mode
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);
            //enter normal mode
            btn_SL620Tab_NormalMode_Click(null, null);
            Delay(Delay_Sync);

            //Init IP current
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Sync);

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                string headers = "Temp,IP,Vout";
                writer.WriteLine(headers);

                //string tempstring;

                for (uint i = 25; i > 0; i--)
                {
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    SetIP(i);
                    Delay(Delay_Power);
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    for (uint k = 0; k < 2; k++)
                    {
                        //tempvout[k] = GetMout();
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(this.txt_Char910_DutId.Text + "," + Convert.ToString(i) + "," + tempvout[1].ToString("F4"));
                }

                btn_EngTab_Ipoff_Click(null, null);

                //MessageBox("Please Invert IP!");
                DialogResult dr = MessageBox.Show("Please Invert IP", "IP", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    btn_SL620Tab_PowerOff_Click(null, null);
                    return;
                }
                else if (dr == DialogResult.Yes)
                {
                    for (uint i = 0; i <= 25; i++)
                    {
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(delay_temp);
                        //if (i > 0)
                        SetIP(i);
                        Delay(Delay_Power);
                        if(i != 0)
                            btn_EngTab_Ipon_Click(null, null);
                        else
                            btn_EngTab_Ipoff_Click(null, null);
                        Delay(delay_temp);
                        for (uint k = 0; k < 2; k++)
                        {
                            //tempvout[k] = GetMout();
                            tempvout[k] = ReadVout();
                            Delay(Delay_Power);
                        }
                        writer.WriteLine(this.txt_Char910_DutId.Text + "," + "-" + Convert.ToString(i) + "," + tempvout[1].ToString("F4"));
                    }

                    btn_EngTab_Ipoff_Click(null, null);
                }
                //MessageBox("Please Invert IP!");
                dr = MessageBox.Show("Please Invert IP", "IP", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    btn_SL620Tab_PowerOff_Click(null, null);
                    return;
                }
            }

            btn_SL620Tab_PowerOff_Click(null, null);
        }

        private void Sweep620Linearity(uint index)
        {
            int delay_temp = 300;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\Sweep780Linearity";
            filename += ".csv";

            //power on 5V
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);
            //enter test mode
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);
            //enter normal mode
            btn_SL620Tab_NormalMode_Click(null, null);
            Delay(Delay_Sync);

            //Init IP current
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Sync);

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                string headers = "Temp,ID,25,24,23,22,21,20,19";
                writer.WriteLine(headers);

                writer.Write(this.txt_Char910_DutId.Text + "," + Convert.ToString(index) + ",");

                for (uint i = 25; i > 0; i--)
                {
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    SetIP(i);
                    Delay(Delay_Power);
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    for (uint k = 0; k < 2; k++)
                    {
                        //tempvout[k] = GetMout();
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    writer.Write(tempvout[1].ToString("F4") + ",");
                }

                btn_EngTab_Ipoff_Click(null, null);

                //MessageBox("Please Invert IP!");
                DialogResult dr = MessageBox.Show("Please Invert IP", "IP", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    btn_SL620Tab_PowerOff_Click(null, null);
                    return;
                }
                else if (dr == DialogResult.Yes)
                {
                    for (uint i = 0; i <= 25; i++)
                    {
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(delay_temp);
                        //if (i > 0)
                        SetIP(i);
                        Delay(Delay_Power);
                        if (i != 0)
                            btn_EngTab_Ipon_Click(null, null);
                        else
                            btn_EngTab_Ipoff_Click(null, null);
                        Delay(delay_temp);
                        for (uint k = 0; k < 2; k++)
                        {
                            //tempvout[k] = GetMout();
                            tempvout[k] = ReadVout();
                            Delay(Delay_Power);
                        }
                        writer.Write(tempvout[1].ToString("F4") + ",");
                    }

                    btn_EngTab_Ipoff_Click(null, null);
                }

                writer.Write("\r\n");

                //MessageBox("Please Invert IP!");
                dr = MessageBox.Show("Please Invert IP", "IP", MessageBoxButtons.YesNoCancel);
                if (dr == DialogResult.Cancel)
                {
                    btn_SL620Tab_PowerOff_Click(null, null);
                    return;
                }
            }

            btn_SL620Tab_PowerOff_Click(null, null);
        }

        private void Sweep620RoughGain()
        {
            int delay_temp = 1000;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620CoarseGain-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 16;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "code,VIP1,VIP2,Offset";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //IP on
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(Delay_Power);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Power);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, i << 4);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }

                    //IP off
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    tempvout[2] = ReadVout();

                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4"));
                }

                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0);
                btn_EngTab_Ipoff_Click(null, null);
                //SetIP(0);
            }
        }

        private void Sweep620FineGain()
        {
            int delay_temp = 1000;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620FineGain-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 64;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                string headers = "code,VIP1,VIP2,Offset";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);
                

                for (uint i = 0; i < count; i++)
                {
                    //IP on
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(Delay_Power);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Power);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set fine gain code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, (i & 0x07) << 5);
                    Delay(Delay_Sync);
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, (i & 0x38) << 2);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }


                    //power off 
                    //btn_SL620Tab_PowerOff_Click(null, null);
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    tempvout[2] = ReadVout();

                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4"));
                }

                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0);
                btn_EngTab_Ipoff_Click(null, null);
                //SetIP(0);
            }
        }

        private void Sweep620Offset()
        {
            int delay_temp = 1500;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620Offset-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 32;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                writer.WriteLine("Coarse Offset");
                string headers = "IP,Vip1,Vip2,Voffset";
                writer.WriteLine(headers);

                //string tempstring;
                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //IP on
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, i);
                    Delay(delay_temp);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }

                    //power off 
                    //btn_SL620Tab_PowerOff_Click(null, null);
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    tempvout[2] = ReadVout();

                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0);

                writer.WriteLine("Fine Offset");
                for (uint i = 0; i < count; i++)
                {
                    //IP on
                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, i);
                    Delay(delay_temp);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }

                    //power off 
                    //btn_SL620Tab_PowerOff_Click(null, null);
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);
                    tempvout[2] = ReadVout();

                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0);
            }
        }

        private void Sweep620Vout0p1vddOffset()
        {
            int delay_temp = 200;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620Vout0p1vddOffset-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 32;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                writer.WriteLine("Coarse Offset");
                string headers = "IP,Vout1,Vout2,Vout3,Vout4,Vout5";
                writer.WriteLine(headers);

                //string tempstring;

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, i);
                    Delay(Delay_Sync);
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 3);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0);

                writer.WriteLine("Fine Offset");
                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, i);
                    Delay(Delay_Sync);
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 3);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[3].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0);
            }
        }

        private void Sweep620HalfVddOffset()
        {
            int delay_temp = 200;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620VoutHalfVddOffset-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 32;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                writer.WriteLine("Coarse Offset");
                string headers = "code,Vout1,Vout2, Vref";
                writer.WriteLine(headers);

                //string tempstring;

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, i);
                    Delay(Delay_Sync);
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 1);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(delay_temp);
                    }
                    //vref meas
                    tempvout[2] = AverageVout();
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," 
                             + tempvout[1].ToString("F4")+ "," + tempvout[2].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0);

                writer.WriteLine("Fine Offset");
                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set offset code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, i);
                    Delay(Delay_Sync);
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 1);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
                    Delay(Delay_Sync);
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 5; k++)
                    {
                        tempvout[k] = dmm.readVolt();
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = AverageVout();
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[0].ToString("F4") + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4"));
                }
                //oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0);
            }
        }

        private void Sweep620Tc2()
        {
            int delay_temp = 300;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\Sweep620Tc2";
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 16;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                //writer.WriteLine("gain tc2");
                string headers = "Temp,code,VoutIP,Vref,Vout0A";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);
                //btn_EngTab_Ipon_Click(null, null);
                //Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, (i << 4));
                    Delay(Delay_Sync);
                    //set TC th = 60c
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 3);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = ReadRef();

                    //IP off
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);

                    tempvout[3] = ReadVout();
                    tempvout[4] = ReadVout();

                    writer.WriteLine(this.txt_Char910_DutId.Text + "," + Convert.ToString(i) + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }
            }
            btn_SL620Tab_PowerOff_Click(null, null);
        }

        private void Sweep620Tc1()
        {
            int delay_temp = 300;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\Sweep620Tc1";
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 16;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                //writer.WriteLine("gain tc1");
                string headers = "Temp,code,VoutIP,Vref,Vout0A";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);
                //btn_EngTab_Ipon_Click(null, null);
                //Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(Delay_Power);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, (i));
                    Delay(Delay_Sync);
                    //set TC th = 60c
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 3);
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(delay_temp);

                    btn_EngTab_Ipon_Click(null, null);
                    Delay(delay_temp);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout(); 
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = ReadRef(); 

                    //IP off
                    btn_EngTab_Ipoff_Click(null, null);
                    Delay(delay_temp);

                    tempvout[3] = ReadVout();
                    tempvout[4] = ReadVout();

                    writer.WriteLine(this.txt_Char910_DutId.Text + "," + Convert.ToString(i) + "," + tempvout[1].ToString("F4")
                        + "," + tempvout[2].ToString("F4") + "," + tempvout[4].ToString("F4"));
                }
            }
            btn_SL620Tab_PowerOff_Click(null, null);
        }

        private void Sweep620Vbg()
        {
            int delay_temp = 200;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620Vbg-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 4;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                writer.WriteLine("vref vs trim_vbg[1:0]");
                string headers = "code,Vout,Vref";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(20);
                Delay(Delay_Power);
                //btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, (i << 2));
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = ReadRef();
                    writer.WriteLine(Convert.ToString(i) + "," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                }
                btn_EngTab_Ipoff_Click(null, null);
            }
        }

        private void SweepVoutOption()
        {
            int delay_temp = 200;
            double[] tempvout = new double[5];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\Sweep620VoutOption-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            uint _dev_addr = this.DeviceAddress;
            uint count = 5;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(filename);
                writer.WriteLine("sel_vr_sourse, sel_ir_sourse");
                writer.WriteLine("gain");
                string headers = "code,Vout,Vref";
                writer.WriteLine(headers);

                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(20);
                Delay(Delay_Power);
                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Power);

                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    if(i == 4)
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x81);
                    else
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, (i));
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = ReadRef();
                    if(i == 0)
                        writer.WriteLine("2.5V," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 1)
                        writer.WriteLine("0.5vdd," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 2)
                        writer.WriteLine("1.65V," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 3)
                        writer.WriteLine("0.1vdd," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 4)
                        writer.WriteLine("0.5vddi-hall-33%lower," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                }
                btn_EngTab_Ipoff_Click(null, null);

                writer.WriteLine("offset");
                for (uint i = 0; i < count; i++)
                {
                    //power off 
                    btn_SL620Tab_PowerOff_Click(null, null);
                    Delay(delay_temp);
                    //power on 5V
                    btn_SL620Tab_PowerOn_Click(null, null);
                    Delay(Delay_Sync);
                    //enter test mode
                    btn_SL620Tab_TestKey_Click(null, null);
                    Delay(Delay_Sync);
                    //set coarse gain code
                    if (i == 4)
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x81);
                    else
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, (i));
                    Delay(Delay_Sync);
                    //enter normal mode
                    btn_SL620Tab_NormalMode_Click(null, null);
                    Delay(Delay_Sync);

                    for (uint k = 0; k < 2; k++)
                    {
                        tempvout[k] = ReadVout();
                        Delay(Delay_Power);
                    }
                    //vref meas
                    tempvout[2] = ReadRef();
                    if (i == 0)
                        writer.WriteLine("2.5V," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 1)
                        writer.WriteLine("0.5vdd," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 2)
                        writer.WriteLine("1.65V," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 3)
                        writer.WriteLine("0.1vdd," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                    else if (i == 4)
                        writer.WriteLine("0.5vddi-hall-33%lower," + tempvout[1].ToString("F4") + "," + tempvout[2].ToString("F4"));
                }
                btn_EngTab_Ipoff_Click(null, null);
            }
        }

        private void TrimTest(uint count)
        {
            Delay_Power = 600;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SL620TrimTest-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                uint tempResult = 0;
                writer.WriteLine(filename);
                string headers = "ID,read before write,read after write,read after trim,read after power cycle 1,read after power cycle 2,read after power cycle 3";
                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(index.ToString() + ",");

                        //trim set2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn6V_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //read reg
                        //ReadRegSet(_reg_addr_start, 2, _readBack_data);
                        //BurstRead9(this.DeviceAddress, 8, tempReadback);
                        //BurstRead(0x80, 5, tempReadback);
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult > 0)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        //write 0xFF except Reg master bits
                        Delay(Delay_Sync);
                        WriteRegSet();
                        Delay(Delay_Sync);
                        //read back
                        //ReadRegSet(_reg_addr_start, 8, _readBack_data);
                        //_readBack_data = btn_burstRead(4);
                        //BurstRead9(this.DeviceAddress, 8, tempReadback);
                        //btn_burstRead(5);
                        //btn_burstRead_Click(null,null);
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult < 0xFF * 8)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        //Trim
                        btn_SL620Tab_TrimSet1_Click(null, null);
                        Delay(Delay_Fuse * 2);

                        //set trim resistor th
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x42, 0xC0);
                        Delay(Delay_Sync);

                        //read pre-set
                        btn_SL620Tab_ReadTrim_Click(null, null);
                        Delay(Delay_Sync);

                        //read back after trim
                        //ReadRegSet(_reg_addr_start, 8, _readBack_data);
                        //_readBack_data = btn_burstRead(8);
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult < 0xFF * 8)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        for (uint j = 0; j < 3; j++)
                        {
                            PowerOff();
                            Delay(Delay_Power);
                            btn_SL620Tab_PowerOn_Click(null, null);
                            Delay(Delay_Sync);
                            btn_SL620Tab_TestKey_Click(null, null);
                            Delay(Delay_Sync);

                            //read back after power cycle
                            //ReadRegSet(_reg_addr_start, 8, _readBack_data);
                            //_readBack_data = btn_burstRead(8);
                            ResetTempBuf();
                            tempResult = 0;
                            ReadRegSet();
                            for (uint i = 0; i < 9; i++)
                                tempResult += tempReadback[i];
                            if (tempResult < 0xFF * 8)
                                if (j == 2)
                                    writer.Write("fail\r\n");
                                else
                                    writer.Write("fail,");
                            else
                                if (j == 2)
                                    writer.Write("pass\r\n");
                                else
                                    writer.Write("pass,");
                        }
                    }
                }
            }
        }

        private void TrimTest5v(uint count)
        {
            Delay_Power = 600;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SL620TrimTest5v-" + this.txt_Char910_DutId.Text + "-" + System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss");
            filename += ".csv";

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                uint tempResult = 0;
                writer.WriteLine(filename);
                string headers = "ID,read before write,read after write,read after trim,read after power cycle 1,read after power cycle 2,read after power cycle 3";
                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(index.ToString() + ",");

                        //trim set2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //read reg
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult > 0)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        //write 0xFF except Reg master bits
                        Delay(Delay_Sync);
                        WriteRegSet();
                        Delay(Delay_Sync);
                        //read back
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult < 0xFF * 8)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        //Trim
                        btn_SL620Tab_TrimSet1_Click(null, null);
                        Delay(Delay_Fuse * 2);

                        //set trim resistor th
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x42, 0xC0);
                        Delay(Delay_Sync);

                        //read pre-set
                        btn_SL620Tab_ReadTrim_Click(null, null);
                        Delay(Delay_Sync);

                        //read back after trim
                        ResetTempBuf();
                        tempResult = 0;
                        ReadRegSet();
                        for (uint i = 0; i < 9; i++)
                            tempResult += tempReadback[i];
                        if (tempResult < 0xFF * 8)
                            writer.Write("fail,");
                        else
                            writer.Write("pass,");

                        for (uint j = 0; j < 3; j++)
                        {
                            PowerOff();
                            Delay(Delay_Power);
                            btn_SL620Tab_PowerOn_Click(null, null);
                            Delay(Delay_Sync);
                            btn_SL620Tab_TestKey_Click(null, null);
                            Delay(Delay_Sync);

                            //read back after power cycle
                            ResetTempBuf();
                            tempResult = 0;
                            ReadRegSet();
                            for (uint i = 0; i < 9; i++)
                                tempResult += tempReadback[i];
                            if (tempResult < 0xFF * 8)
                                if (j == 2)
                                    writer.Write("fail\r\n");
                                else
                                    writer.Write("fail,");
                            else
                                if (j == 2)
                                    writer.Write("pass\r\n");
                                else
                                    writer.Write("pass,");
                        }
                    }
                }
            }
        }

        private void ReliabilityTest(uint count)
        {
            Delay_Power = 200;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SL620ReliabilityTest";
            filename += ".csv";

            uint[] sl620RegValue = new uint[3];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(20);
            Delay(Delay_Power);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                //string headers = "ID,native vout0A,native vref,native voutIP,native vrefIP,vout0A after meg,vref after meg,voutIP after trim,vrefIP after trim,vout0A after trim,vref0A after trim";
                //writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(index.ToString() + ",");

                        //trim set2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x03);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);
                        Delay(Delay_Sync);


                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                 
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                        //calc trim code
                        sl620RegValue = AutoCalcSL620TrimCode(20, 50);

                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //write trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, sl620RegValue[0]);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, sl620RegValue[1]);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, sl620RegValue[2]);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x88, 0x02);
                        Delay(Delay_Sync);

                        //Trim
                        btn_SL620Tab_TrimSet1_Click(null, null);
                        Delay(Delay_Fuse * 2);

                        //read again
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        DisplayOperateMes("VIP = " + ReadVout().ToString("F3"));
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        DisplayOperateMes("V0A = " + ReadVout().ToString("F3"));
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }
           
        }

        private void PowerCycleTest(uint count)
        {
            Delay_Power = 200;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SL620PowerCycleTest";
            filename += ".csv";

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(20);
            Delay(Delay_Power);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //string headers = "ID,voutIP,vrefIP,vout0A,vref0A,";
                //writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(index.ToString() + ",");

                        //IP ON                      
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);

                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        DisplayOperateMes("VIP = " + ReadVout().ToString("F3"));
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                        //IP OFF
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);

                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        DisplayOperateMes("V0A = " + ReadVout().ToString("F3"));
                        Delay(Delay_Sync);
                        writer.Write(ReadRef().ToString("F3") + ",");        //VREF
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }
        }

        //new under test
        private void CharSC780(uint i)
        {
            uint _dev_addr = this.DeviceAddress;
            double[] tempvout = new double[30];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\SC780Char";
            filename += ".csv";

            uint[,] trimCode = new uint[6, 12] { { 0x07, 0x86, 0xe3, 0x67, 0x26, 0x03, 0x87, 0x65, 0xe5, 0xb7, 0xc5, 0x05 }, 
                                                 { 0x17, 0xe5, 0x04, 0x67, 0x24, 0x25, 0x97, 0x04, 0x25, 0xb7, 0xc4, 0x24 }, 
                                                 { 0x17, 0x64, 0x02, 0x67, 0xa3, 0x05, 0x97, 0x84, 0x02, 0xb7, 0x23, 0x25 },
                                                 { 0x17, 0xc5, 0x25, 0x67, 0xe5, 0x24, 0x97, 0xc5, 0x23, 0xb7, 0x85, 0x43 },
                                                 { 0x07, 0x06, 0x65, 0x57, 0x05, 0x66, 0x87, 0xc5, 0x45, 0xa7, 0x65, 0x64 },
                                                 { 0x07, 0x25, 0xe4, 0x57, 0x05, 0xe2, 0x87, 0xe4, 0xc5, 0xb7, 0x44, 0x05 } };

            using (StreamWriter writer = new StreamWriter(filename, true))
            {

                #region write header
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                //writer.WriteLine("gain tc1");
                string headers = " DUT_ID,Temp,VoutIP_2p5V,Vout0A_2p5V,Vref_2p5V," +
                                    "VoutIP_0p5VDD,Vout0A_0p5VDD,Vref_0p5VDD," +
                                    "VoutIP_iHall,Vout0A_iHall,Vref_iHall," +
                                    "VoutIP_SelSensor,Vout0A_SelSensor,Vref_SelSensor," +
                                    "VoutIP_Invert,Vout0A_Invert,Vref_Invert," +
                                    "VoutIP_VBG,Vout0A_VBG,Vref_VBG," +
                                    "VoutIP_50A,Vout0A_50A,Vref_50A," +
                                    "VoutIP_100A,Vout0A_100A,Vref_100A," +
                                    "VoutIP_150A,Vout0A_150A,Vref_150A," +
                                    "VoutIP_200A,Vout0A_200A,Vref_200A";
                writer.WriteLine(headers);
                #endregion

                #region Char

                #region Init RS232, IP = 25A
                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(25);
                Delay(Delay_Power);
                //btn_EngTab_Ipon_Click(null, null);
                //Delay(Delay_Power);
                #endregion

                #region 2.5V output
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[0] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[1] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[2] = ReadRef();        //VREF
                #endregion

                #region 0.5Vdd output
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 1);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[3] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[4] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[5] = ReadRef();        //VREF

                #endregion

                #region iHall -33%
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x80);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[6] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[7] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[8] = ReadRef();        //VREF

                #endregion

                #region Sel Sensor
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x10);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[9] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[10] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[11] = ReadRef();        //VREF

                #endregion

                #region Invert
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 4);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[12] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[13] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[14] = ReadRef();        //VREF

                #endregion

                #region VBG
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x04);
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[15] = ReadVout();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[16] = ReadVout();       //V0A
                Delay(Delay_Sync);
                tempvout[17] = ReadRef();        //VREF

                #endregion

                #region Init RS232, IP = 50A
                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(50);
                Delay(Delay_Power);
                //btn_EngTab_Ipon_Click(null, null);
                //Delay(Delay_Power);
                #endregion

                #region 50A
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0xFF);  //TC1/2
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //VBG,TcTH
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[18] = ReadVoutSlow();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[19] = ReadVoutSlow();       //V0A
                Delay(Delay_Sync);
                tempvout[20] = ReadRef();        //VREF
                #endregion 50A

                #region 100A
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0xFF);  //TC1/2
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[21] = ReadVoutSlow();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[22] = ReadVoutSlow();       //V0A
                Delay(Delay_Sync);
                tempvout[23] = ReadRef();        //VREF
                #endregion 100A

                #region 150A
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0xFF);  //TC1/2
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[24] = ReadVoutSlow();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[25] = ReadVoutSlow();       //V0A
                Delay(Delay_Sync);
                tempvout[26] = ReadRef();        //VREF
                #endregion 150A

                #region 200A
                //power off and on
                btn_SL620Tab_PowerOff_Click(null, null);
                Delay(Delay_Power);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);

                //enter test mode
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);

                //set trim code
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0xFF);  //TC1/2
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                Delay(Delay_Sync);

                //enter normal mode
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                btn_EngTab_Ipon_Click(null, null);
                Delay(Delay_Sync);
                tempvout[27] = ReadVoutSlow();       //VIP
                Delay(Delay_Sync);

                btn_EngTab_Ipoff_Click(null, null);
                Delay(Delay_Sync);
                tempvout[28] = ReadVoutSlow();       //V0A
                Delay(Delay_Sync);
                tempvout[29] = ReadRef();        //VREF
                #endregion 200A

                #region write to file
                writer.Write(Convert.ToString(i) + ",");
                writer.Write(this.txt_Char910_DutId.Text + ",");
                for (uint j = 0; j < 30; j++)
                {
                    if (j < 29)
                        writer.Write(tempvout[j].ToString("F4") + ",");
                    else
                        writer.Write(tempvout[j].ToString("F4") + "\r\n");
                }

                btn_SL620Tab_PowerOff_Click(null, null);
                #endregion

                #endregion Char

                DisplayOperateMesClear();
            }
        }

        //20170622
        private void Sc780Char(uint q)
        {
            //int delay_temp = 800;
            double[] tempvout = new double[30];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\SC780Char";
            filename += ".csv";

            uint[,] trimCode = new uint[6, 12] { { 0x03, 0x86, 0xe3, 0x63, 0x26, 0x03, 0x83, 0x65, 0xe5, 0xb3, 0xc5, 0x05 }, 
                                                 { 0x13, 0xe5, 0x04, 0x63, 0x24, 0x25, 0x93, 0x04, 0x25, 0xb3, 0xc4, 0x24 }, 
                                                 { 0x13, 0x64, 0x02, 0x63, 0xa3, 0x05, 0x93, 0x84, 0x02, 0xb3, 0x23, 0x25 },
                                                 { 0x13, 0xc5, 0x25, 0x63, 0xe5, 0x24, 0x93, 0xc5, 0x23, 0xb3, 0x85, 0x43 },
                                                 { 0x03, 0x06, 0x65, 0x53, 0x05, 0x66, 0x83, 0xc5, 0x45, 0xa3, 0x65, 0x64 },
                                                 { 0x03, 0x25, 0xe4, 0x53, 0x05, 0xe2, 0x83, 0xe4, 0xc5, 0xb3, 0x44, 0x05 } };
            //trimCode[ = new uint{0,0,0};

            uint _dev_addr = this.DeviceAddress;
            uint count = q;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                
                #region write header
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                //writer.WriteLine("gain tc1");
                string headers = " DUT_ID,Temp,VoutIP_2p5V,Vout0A_2p5V,Vref_2p5V," +
                                    "VoutIP_0p5VDD,Vout0A_0p5VDD,Vref_0p5VDD," +
                                    "VoutIP_iHall,Vout0A_iHall,Vref_iHall," +
                                    "VoutIP_SelSensor,Vout0A_SelSensor,Vref_SelSensor," +
                                    "VoutIP_Invert,Vout0A_Invert,Vref_Invert," +
                                    "VoutIP_VBG,Vout0A_VBG,Vref_VBG," +
                                    "VoutIP_50A,Vout0A_50A,Vref_50A," +
                                    "VoutIP_100A,Vout0A_100A,Vref_100A," +
                                    "VoutIP_150A,Vout0A_150A,Vref_150A," +
                                    "VoutIP_200A,Vout0A_200A,Vref_200A";
                writer.WriteLine(headers);
                #endregion
                   
                for (uint i = 0; i < count; i++)
                {
                    Char910_Tab_DataGridView.Rows[0].Cells[4].Value = i + 1;
                    Char910_Tab_DataGridView.Update();

                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {
                        #region Liniearity
                        //Sweep620Linearity(i);
                        #endregion Liniearity

                        #region TC
                        //Sweep620Tc1();
                        //Sweep620Tc2();
                        #endregion TC

                        #region Char

                        #region Init RS232, IP = 25A
                        btn_EngTab_Connect_Click(null, null);
                        Delay(Delay_Power);
                        SetIP(25);
                        Delay(Delay_Power);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Power);
                        #endregion

                        #region 2.5V output
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x04);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        #endregion

                        #region 0.5Vdd output
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);  //0.5vdd; invert
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[3] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[4] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[5] = ReadRef();        //VREF

                        #endregion

                        #region iHall -33%
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall; invert; 0.5vdd
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[6] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[7] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[8] = ReadRef();        //VREF

                        #endregion

                        #region Sel Sensor
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x15);  //sel sensor; invert
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[9] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[10] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[11] = ReadRef();        //VREF

                        #endregion

                        #region Invert
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[12] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[13] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[14] = ReadRef();        //VREF

                        #endregion

                        #region VBG
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x04);
                        Delay(Delay_Sync);
                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[15] = ReadVout();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[16] = ReadVout();       //V0A
                        Delay(Delay_Sync);
                        tempvout[17] = ReadRef();        //VREF

                        #endregion

                        #region Init RS232, IP = 50A
                        btn_EngTab_Connect_Click(null, null);
                        Delay(Delay_Power);
                        SetIP(50);
                        Delay(Delay_Power);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Power);
                        #endregion

                        #region 50A
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[18] = ReadVoutSlow();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[19] = ReadVoutSlow();       //V0A
                        Delay(Delay_Sync);
                        tempvout[20] = ReadRef();        //VREF
                        #endregion 50A

                        #region 100A
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[21] = ReadVoutSlow();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[22] = ReadVoutSlow();       //V0A
                        Delay(Delay_Sync);
                        tempvout[23] = ReadRef();        //VREF
                        #endregion 100A

                        #region 150A
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[24] = ReadVoutSlow();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[25] = ReadVoutSlow();       //V0A
                        Delay(Delay_Sync);
                        tempvout[26] = ReadRef();        //VREF
                        #endregion 150A

                        #region 200A
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[27] = ReadVoutSlow();       //VIP
                        Delay(Delay_Sync);

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[28] = ReadVoutSlow();       //V0A
                        Delay(Delay_Sync);
                        tempvout[29] = ReadRef();        //VREF
                        #endregion 200A

                        #region write to file
                        writer.Write(Convert.ToString(i) + ",");
                        writer.Write(this.txt_Char910_DutId.Text + ",");
                        for (uint j = 0; j < 30; j++)
                        {
                            if (j < 29)
                                writer.Write(tempvout[j].ToString("F4") + ",");
                            else
                                writer.Write(tempvout[j].ToString("F4") + "\r\n");
                        }

                        btn_SL620Tab_PowerOff_Click(null, null);
                        #endregion

                        #endregion Char
                    }

                    DisplayOperateMesClear();
                }
                
            }
        }

        //20170623
        private void Sc780Char0623(uint q)
        {
            //int delay_temp = 800;
            double[] tempvout = new double[30];
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\rawTestData\SC780Char";
            filename += ".csv";

            uint[,] trimCode = new uint[6, 12] { { 0x03, 0x86, 0xe3, 0x63, 0x26, 0x03, 0x83, 0x65, 0xe5, 0xb3, 0xc5, 0x05 }, 
                                                 { 0x13, 0xe5, 0x04, 0x63, 0x24, 0x25, 0x93, 0x04, 0x25, 0xb3, 0xc4, 0x24 }, 
                                                 { 0x13, 0x64, 0x02, 0x63, 0xa3, 0x05, 0x93, 0x84, 0x02, 0xb3, 0x23, 0x25 },
                                                 { 0x13, 0xc5, 0x25, 0x63, 0xe5, 0x24, 0x93, 0xc5, 0x23, 0xb3, 0x85, 0x43 },
                                                 { 0x03, 0x06, 0x65, 0x53, 0x05, 0x66, 0x83, 0xc5, 0x45, 0xa3, 0x65, 0x64 },
                                                 { 0x03, 0x25, 0xe4, 0x53, 0x05, 0xe2, 0x83, 0xe4, 0xc5, 0xb3, 0x44, 0x05 } };
            //trimCode[ = new uint{0,0,0};

            uint _dev_addr = this.DeviceAddress;
            uint count = q;

            using (StreamWriter writer = new StreamWriter(filename, true))
            {

                #region write header
                writer.WriteLine(System.DateTime.Now.ToString("yy-MM-dd") + "-" + System.DateTime.Now.ToString("hh-mm-ss"));
                //writer.WriteLine("gain tc1");
                string headers = " DUT_ID,Temp,VoutIP_2p5V,Vout0A_2p5V,Vref_2p5V," +
                                    "VoutIP_0p5VDD,Vout0A_0p5VDD,Vref_0p5VDD," +
                                    "VoutIP_iHall,Vout0A_iHall,Vref_iHall," +
                                    "VoutIP_SelSensor,Vout0A_SelSensor,Vref_SelSensor," +
                                    "VoutIP_Invert,Vout0A_Invert,Vref_Invert," +
                                    "VoutIP_VBG,Vout0A_VBG,Vref_VBG," +
                                    "VoutIP_50A,Vout0A_50A,Vref_50A," +
                                    "VoutIP_100A,Vout0A_100A,Vref_100A," +
                                    "VoutIP_150A,Vout0A_150A,Vref_150A," +
                                    "VoutIP_200A,Vout0A_200A,Vref_200A";
                writer.WriteLine(headers);
                #endregion

                for (uint i = 0; i < count; i++)
                {
                    Char910_Tab_DataGridView.Rows[0].Cells[4].Value = i + 1;
                    Char910_Tab_DataGridView.Update();

                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {
                        writer.Write(Convert.ToString(i) + ",");
                        writer.Write(this.txt_Char910_DutId.Text + ",");

                        #region Liniearity
                        //Sweep620Linearity(i);
                        #endregion Liniearity

                        #region TC
                        //Sweep620Tc1();
                        //Sweep620Tc2();
                        #endregion TC

                        #region Char

                        #region Init RS232, IP = 25A
                        btn_EngTab_Connect_Click(null, null);
                        Delay(Delay_Power);
                        SetIP(25);
                        Delay(Delay_Power);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Power);
                        #endregion

                        #region 2.5V output
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x04);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion

                        #region 0.5Vdd output
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);  //0.5vdd; invert
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region Dis Chop
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);  //0.5vdd; invert
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x08);  //dis chop
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region S1_02
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x23);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region S1_04
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x43);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region S1_06
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x63);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region S1_08
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x05);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x83);
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");

                        #endregion

                        #region Init RS232, IP = 50A
                        btn_EngTab_Connect_Click(null, null);
                        Delay(Delay_Power);
                        SetIP(50);
                        Delay(Delay_Power);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Power);
                        #endregion

                        #region 50A - 0
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 50A - 1
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x11);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 50A - 2
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 50A - 3
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x33);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 50A - 4
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 50A - 5
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x55);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 0]);  //TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 1]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 2]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 50A

                        #region 100A - 0
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 100A - 1
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x11);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 100A - 2
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 100A - 3
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x33);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 100A - 4
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 100A - 5
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x55);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 3]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 4]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 5]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 100A

                        #region 150A - 0
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 150A - 1
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x11);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 150A - 2
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 150A - 3
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x33);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 150A - 4
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 150A - 5
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x55);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 6]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 7]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 8]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 150A

                        #region 200A - 0
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region 200A - 1
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x11);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region 200A - 2
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region 200A - 3
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x33);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region 200A - 4
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region 200A - 5
                        //power off and on
                        btn_SL620Tab_PowerOff_Click(null, null);
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);

                        //enter test mode
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);

                        //set trim code
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x55);  //TC1/2
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, trimCode[i, 9]);  //VBG,TcTH
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, trimCode[i, 10]);  //
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, trimCode[i, 11]);  //
                        Delay(Delay_Sync);

                        //enter normal mode
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[0] = ReadVout();       //VIP
                        Delay(Delay_Sync);
                        writer.Write(tempvout[0].ToString("F4") + ",");

                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        tempvout[1] = ReadVout();       //V0A
                        writer.Write(tempvout[1].ToString("F4") + ",");
                        Delay(Delay_Sync);
                        tempvout[2] = ReadRef();        //VREF
                        writer.Write(tempvout[2].ToString("F4") + ",");
                        #endregion 200A

                        #region write to file
                        //writer.Write(Convert.ToString(i) + ",");
                        //writer.Write(this.txt_Char910_DutId.Text + ",");
                        //for (uint j = 0; j < 30; j++)
                        //{
                        //    if (j < 29)
                        //        writer.Write(tempvout[j].ToString("F4") + ",");
                        //    else
                        //        writer.Write(tempvout[j].ToString("F4") + "\r\n");
                        //}
                        writer.Write("\r\n");
                        btn_SL620Tab_PowerOff_Click(null, null);
                        #endregion

                        #endregion Char
                    }

                    DisplayOperateMesClear();
                }

            }
        }

        private void btn_SL620Tab_Vout_Click(object sender, EventArgs e)
        {
            btn_Vout0A_EngT_Click(null, null);
        }

        private void SC810_Char_Test_20A(uint count)
        {
            Delay_Power = 200;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC810-Char-Test-20A";
            filename += ".csv";

            uint[] sc810RegValue = new uint[4];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(500);
            SetIP(20);
            Delay(500);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "ID,vout0A_default,voutIP_default,vout0Ameg_default," +
                                 "voutIP_vbg1,vout0A_vbg1,voutIP_vbg2,vout0A_vbg2,voutIP_vbg3,vout0A_vbg3," + 
                                 "voutIP_tc2,vout0A_tc2,voutIP_tc4,vout0A_tc4,voutIP_tc6,vout0A_tc6,voutIP_tc8,vout0A_tc8," +
                                 "voutIP_0.5vdd,vout0A_0.5vdd," +
                                 "voutIP_0.5vdd_tc2,vout0A_0.5vdd_tc2,voutIP_0.5vdd_tc4,vout0A_0.5vdd_tc4,voutIP_0.5vdd_tc6,vout0A_0.5vdd_tc6,voutIP_0.5vdd_tc8,vout0A_0.5vdd_tc8,";

                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(this.txt_Char910_DutId.Text + index.ToString() + ",");

                        //default 2.5V
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg01
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x24);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg02
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x28);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg03
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x2C);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }

        }

        private void SC810_Char_Test_30A(uint count)
        {
            Delay_Power = 200;
            int Delay_Dissipate = 1000;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC810-Char-Test-30A";
            filename += ".csv";

            uint[] sc810RegValue = new uint[4];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(30);
            Delay(Delay_Power);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "ID,vout0A_default,voutIP_default,vout0Ameg_default," +
                                 "voutIP_vbg1,vout0A_vbg1,voutIP_vbg2,vout0A_vbg2,voutIP_vbg3,vout0A_vbg3," +
                                 "voutIP_tc2,vout0A_tc2,voutIP_tc4,vout0A_tc4,voutIP_tc6,vout0A_tc6,voutIP_tc8,vout0A_tc8," +
                                 "voutIP_0.5vdd,vout0A_0.5vdd," +
                                 "voutIP_0.5vdd_tc2,vout0A_0.5vdd_tc2,voutIP_0.5vdd_tc4,vout0A_0.5vdd_tc4,voutIP_0.5vdd_tc6,vout0A_0.5vdd_tc6,voutIP_0.5vdd_tc8,vout0A_0.5vdd_tc8,";

                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(this.txt_Char910_DutId.Text + index.ToString() + ",");

                        //default 2.5V
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg01
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg02
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x48);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg03
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x4C);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x40);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Dissipate);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }

        }

        private void SC810_Char_Test_10A(uint count)
        {
            Delay_Power = 200;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC810-Char-Test-10A";
            filename += ".csv";

            uint[] sc810RegValue = new uint[4];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(10);
            Delay(Delay_Power);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "ID,vout0A_default,voutIP_default,vout0Ameg_default," +
                                 "voutIP_vbg1,vout0A_vbg1,voutIP_vbg2,vout0A_vbg2,voutIP_vbg3,vout0A_vbg3," +
                                 "voutIP_tc2,vout0A_tc2,voutIP_tc4,vout0A_tc4,voutIP_tc6,vout0A_tc6,voutIP_tc8,vout0A_tc8," +
                                 "voutIP_0.5vdd,vout0A_0.5vdd," +
                                 "voutIP_0.5vdd_tc2,vout0A_0.5vdd_tc2,voutIP_0.5vdd_tc4,vout0A_0.5vdd_tc4,voutIP_0.5vdd_tc6,vout0A_0.5vdd_tc6,voutIP_0.5vdd_tc8,vout0A_0.5vdd_tc8,";

                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(this.txt_Char910_DutId.Text + index.ToString() + ",");

                        //default 2.5V
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg01
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x14);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg02
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x18);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg03
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x1C);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x45);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x45);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x45);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x45);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x45);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x10);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x02);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }

        }

        private void SC810_Char_Test_10AP(uint count)
        {
            Delay_Power = 200;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC810-Char-Test-10AP";
            filename += ".csv";

            uint[] sc810RegValue = new uint[4];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(10);
            Delay(Delay_Power);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "ID,vout0A_default,voutIP_default,vout0Ameg_default," +
                                 "voutIP_vbg1,vout0A_vbg1,voutIP_vbg2,vout0A_vbg2,voutIP_vbg3,vout0A_vbg3," +
                                 "voutIP_tc2,vout0A_tc2,voutIP_tc4,vout0A_tc4,voutIP_tc6,vout0A_tc6,voutIP_tc8,vout0A_tc8," +
                                 "voutIP_0.5vdd,vout0A_0.5vdd," +
                                 "voutIP_0.5vdd_tc2,vout0A_0.5vdd_tc2,voutIP_0.5vdd_tc4,vout0A_0.5vdd_tc4,voutIP_0.5vdd_tc6,vout0A_0.5vdd_tc6,voutIP_0.5vdd_tc8,vout0A_0.5vdd_tc8,";

                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    if (dr == DialogResult.Cancel)
                        return;
                    else if (dr == DialogResult.OK)
                    {

                        //write ID
                        writer.Write(this.txt_Char910_DutId.Text + index.ToString() + ",");

                        //default 2.5V
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg01
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x04);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg02
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x08);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg03
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x0C);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x84);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x00);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, 0x04);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }

        }

        private double ReadVout()
        {
            double dResult = 0;
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(400);
            dResult = AverageVout();
            return dResult;
        }

        private double ReadVoutSlow()
        {
            double dResult = 0;
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VOUT_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VOUT);
            Delay(400);
            dResult = AverageVout();
            return dResult;
        }

        private double ReadRef()
        {
            double dResult = 0;
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VREF_WITH_CAP);
            Delay(Delay_Sync);
            oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_VIN_TO_VREF);
            Delay(500);
            dResult = AverageVout();
            return dResult;
        }

        private void WriteRegSet()
        {
            try
            {
                uint _dev_addr = this.DeviceAddress;

                for (uint i = 0; i < 8; i++)
                {
                    oneWrie_device.I2CWrite_Single(_dev_addr, 0x80 + i, 0xFF);
                }
            }
            catch
            {
                DisplayOperateMes("write reg fail!");
            }
        }

        private void ReadRegSet()
        {
            uint _dev_addr = this.DeviceAddress;
            uint _reg_addr = 0x80;
            uint[] _readBack_data = new uint[4];
            oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr, 4, _readBack_data);
            DisplayOperateMes("Read Reg Data:");
            for (int i = 0; i < 4; i++)
            {
                tempReadback[i] = _readBack_data[i];
                DisplayOperateMes(string.Format("Reg0x8{0} = 0x{1}", i, tempReadback[i].ToString("X2")));
            }

            oneWrie_device.I2CRead_Burst(_dev_addr, _reg_addr + 4, 4, _readBack_data);
            for (int i = 0; i < 4; i++)
            {
                tempReadback[i + 4] = _readBack_data[i];
                DisplayOperateMes(string.Format("Reg0x8{0} = 0x{1}", 4 + i, tempReadback[i + 4].ToString("X2")));
            }
        }

        private void ResetTempBuf()
        {
            for (int i = 0; i < 9; i++)
                tempReadback[i] = 0;        
        }

        private int LookupCoarseGain_SL620(double tuningGain, double[][] gainTable)
        {
            if (tuningGain.ToString() == "Infinity")
            {
                return gainTable[0].Length - 1;
            }

            double temp = Math.Abs(tuningGain);
            if (temp >= 100)
                return 0;

            if (temp <= 12.7)
                return gainTable[0].Length - 1;

            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) > 0)
                    return i - 1;
            }
            return gainTable[0].Length - 1;
        }

        private int LookupFineGain_SL620(double tuningGain, double[][] gainTable)
        {
            double temp = Math.Abs(tuningGain);
            for (int i = 0; i < gainTable[0].Length; i++)
            {
                if (temp - Math.Abs(gainTable[0][i]) >= 0)
                {
                    if ((i > 0) && (i < gainTable[0].Length - 1))
                    {
                        if (Math.Abs(temp - Math.Abs(gainTable[0][i - 1])) <= Math.Abs(temp - Math.Abs(gainTable[0][i])))
                            return (i - 1);
                        else
                            return i;
                    }
                    else
                        return (gainTable[0].Length - 1);
                }
            }
            return 0;
        }

        private uint LookupCoarseOffset_SL620(double os, uint mode)
        {
            if (mode == 0)
            {
                uint ix_CoarseOffsetCode = 0;
                if (os > TargetOffset)
                    ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (os - TargetOffset) / 14));
                else if (os < TargetOffset)
                    ix_CoarseOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset - os) / 14));

                return ix_CoarseOffsetCode;
            }
            else
            {
                uint ix_CoarseOffsetCode = 0;
                if (os > TargetOffset)
                {
                    ix_CoarseOffsetCode = Convert.ToUInt32(Math.Round(1000d * (os / TargetOffset - 1.0d) / 5));
                }
                else if (os < TargetOffset)
                {
                    if (Math.Round(1000d * (1.0d - os / TargetOffset) / 5) == 0)
                        ix_CoarseOffsetCode = 0;
                    else
                        ix_CoarseOffsetCode = 32 - Convert.ToUInt32(Math.Round(1000d * (1.0d - os / TargetOffset) / 5));
                }
                return ix_CoarseOffsetCode;
            }
        }

        private uint LookupFineOffset_SL620(double os, uint mode)
        {
            if (mode == 0)
            {
                uint ix_FineOffsetCode = 0;
                if (os > TargetOffset)
                    ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (os - TargetOffset) / 4));
                else if (os < TargetOffset)
                    ix_FineOffsetCode = 31 - Convert.ToUInt32(Math.Round(1000d * (TargetOffset - os) / 4));

                return ix_FineOffsetCode;
            }
            else
            {
                uint ix_FineOffsetCode = 0;
                if (os > TargetOffset)
                    ix_FineOffsetCode = Convert.ToUInt32(Math.Round(1000d * (os / TargetOffset - 1.0d) / 1.5));
                else if (os < TargetOffset)
                {
                    if (Math.Round(1000d * (1.0d - os / TargetOffset) / 1.5) == 0)
                        ix_FineOffsetCode = 0;
                    else
                        ix_FineOffsetCode = 32 - Convert.ToUInt32(Math.Round(1000d * (1.0d - os / TargetOffset) / 1.5));
                }
                return ix_FineOffsetCode;
            }
        }

        private uint[] AutoCalcSL620TrimCode( uint ip, double tg )
        {
            int ix_Coarse = 0;
            int ix_Fine = 0;
            uint[] sl620RegValue = new uint[3];

            uint _dev_addr = this.DeviceAddress;

            double VIP = 0;
            double V0A = 0;
            double idealGain = 100;
            uint dIP = ip;

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(Delay_Power);
            SetIP(dIP);
            Delay(Delay_Power);
            #endregion

            #region Read VIP and V0A
            //power off and on
            btn_SL620Tab_PowerOff_Click(null, null);
            Delay(Delay_Power);
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);

            //enter test mode
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);

            //set trim code
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
            Delay(Delay_Sync);
            sl620RegValue[0] = 0x03;
            sl620RegValue[1] = 0x00;
            sl620RegValue[2] = 0x00;
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, sl620RegValue[0]);  //VBG,TcTH
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0x00);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0x00);  //
            Delay(Delay_Sync);

            //enter normal mode
            btn_SL620Tab_NormalMode_Click(null, null);
            Delay(Delay_Sync);

            btn_EngTab_Ipon_Click(null, null);
            Delay(Delay_Sync);
            VIP = ReadVout();       //VIP
            Delay(Delay_Sync);

            btn_EngTab_Ipoff_Click(null, null);
            Delay(Delay_Sync);
            V0A = ReadVout();       //V0A
            #endregion

            #region calc Gain Code
            idealGain = tg * 100d * dIP / ((VIP - V0A) * 1000d);
            ix_Coarse = LookupCoarseGain_SL620(idealGain, sl620CoarseGainTable);
            /* Rough Gain Code*/
            bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            sl620RegValue[0] &= ~bit_op_mask;
            sl620RegValue[0] |= Convert.ToUInt32(sl620CoarseGainTable[1][ix_Coarse]);

            ix_Fine = LookupFineGain_SL620(idealGain * 100d / sl620CoarseGainTable[0][ix_Coarse], sl620FineGainTable);
            /* Fine Gain Code*/
            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            sl620RegValue[1] &= ~bit_op_mask;
            sl620RegValue[1] |= Convert.ToUInt32(sl620FineGainTable[1][ix_Fine]);

            bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
            sl620RegValue[2] &= ~bit_op_mask;
            sl620RegValue[2] |= Convert.ToUInt32(sl620FineGainTable[2][ix_Fine]);
            #endregion

            #region Get V0A, calc Coarse Offset Code
            //power off and on
            btn_SL620Tab_PowerOff_Click(null, null);
            Delay(Delay_Power);
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);

            //enter test mode
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);

            //set trim code
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, sl620RegValue[0]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, sl620RegValue[1]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, sl620RegValue[2]);  //
            Delay(Delay_Sync);

            //enter normal mode
            btn_SL620Tab_NormalMode_Click(null, null);
            Delay(Delay_Sync);

            V0A = ReadVout();       //V0A

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            sl620RegValue[1] &= ~bit_op_mask;
            sl620RegValue[1] |= LookupCoarseOffset_SL620(V0A, 1); ;
            #endregion

            #region Get V0A, calc Fine Offset Code
            //power off and on
            btn_SL620Tab_PowerOff_Click(null, null);
            Delay(Delay_Power);
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);

            //enter test mode
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);

            //set trim code
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x85);  //iHall -33%, Invert, 0.5VDD
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x60);  //TC1/2
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, sl620RegValue[0]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, sl620RegValue[1]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, sl620RegValue[2]);  //
            Delay(Delay_Sync);

            //enter normal mode
            btn_SL620Tab_NormalMode_Click(null, null);
            Delay(Delay_Sync);

            V0A = ReadVout();       //V0A

            bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
            sl620RegValue[2] &= ~bit_op_mask;
            sl620RegValue[2] |= LookupFineOffset_SL620(V0A , 1);
            #endregion

            DisplayOperateMes("0x81 = 0x" + sl620RegValue[0].ToString("X2"));
            DisplayOperateMes("0x86 = 0x" + sl620RegValue[1].ToString("X2"));
            DisplayOperateMes("0x87 = 0x" + sl620RegValue[2].ToString("X2"));
            return sl620RegValue;
        }

        #endregion SL620

        #region SL620b/SC810b
        private void SC810b_MagOffset(uint count)
        {
            uint IP_temp = count;
            //int Delay_IP = 300;
            double VIP = 0;
            double V0A = 0;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC810b_MagOffset.csv";
            //filename += ".csv";

            //uint[] sc810RegValue = new uint[4];

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(100);
            SetIP(1);
            Delay(100);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "IP,VIP,V0A";

                writer.WriteLine(headers);

                PowerOff();
                Delay(100);
                btn_SL620Tab_PowerOn_Click(null, null);
                Delay(Delay_Sync);
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);
                btn_SL620Tab_TestKey_Click(null, null);
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x10);
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x80);
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, 0xA0);
                Delay(Delay_Sync);
                oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, 0xA0);
                Delay(Delay_Sync);
                btn_SL620Tab_NormalMode_Click(null, null);
                Delay(Delay_Sync);

                //writer.WriteLine( "0," + ReadVoutSlow().ToString("F3") );       //V0A

                for (uint index = 0; index <= IP_temp; index++)
                {                   
                    SetIP(index);
                    Delay(Delay_Sync);

                    btn_EngTab_Ipon_Click(null, null);
                    VIP = ReadVoutSlow();

                    btn_EngTab_Ipoff_Click(null, null);
                    V0A = ReadVoutSlow();

                    writer.WriteLine( index.ToString() + "," + VIP.ToString("F3") + "," + V0A.ToString("F3") );       //V0A
                    Delay(Delay_Sync);
                    
                    //writer.Write("\r\n");
                    //writer.Close();
                }

                for (uint index = IP_temp; index > 0; index--)
                {
                    SetIP(index - 1);
                    Delay(Delay_Sync);

                    btn_EngTab_Ipon_Click(null, null);
                    VIP = ReadVoutSlow();

                    btn_EngTab_Ipoff_Click(null, null);
                    V0A = ReadVoutSlow();

                    writer.WriteLine((index - 1).ToString() + "," + VIP.ToString("F3") + "," + V0A.ToString("F3"));       //V0A
                    Delay(Delay_Sync);

                    //writer.Write("\r\n");
                    //writer.Close();
                }

                DialogResult dr = MessageBox.Show("Please exchange IP direction", "opeartion", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    //writer.Close();
                    return;
                }
                else if (dr == DialogResult.OK)
                {
                    for (uint index = 0; index <= IP_temp; index++)
                    {
                        SetIP(index);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        VIP = ReadVoutSlow();

                        btn_EngTab_Ipoff_Click(null, null);
                        V0A = ReadVoutSlow();

                        writer.WriteLine( "-" + index.ToString() + "," + VIP.ToString("F3") + "," + V0A.ToString("F3"));       //V0A
                        Delay(Delay_Sync);

                        //writer.Write("\r\n");
                        //writer.Close();
                    }

                    for (uint index = IP_temp; index > 0; index--)
                    {
                        SetIP(index - 1);
                        Delay(Delay_Sync);

                        btn_EngTab_Ipon_Click(null, null);
                        VIP = ReadVoutSlow();

                        btn_EngTab_Ipoff_Click(null, null);
                        V0A = ReadVoutSlow();

                        writer.WriteLine( "-" + (index - 1).ToString() + "," + VIP.ToString("F3") + "," + V0A.ToString("F3"));       //V0A
                        Delay(Delay_Sync);

                        //writer.Write("\r\n");
                        //writer.Close();
                    }
                }
            }

        }

        #endregion SL620b/SC810b

        #region SC780
        //offset temp drift
        private void SC780_Offset_Temp_Drift(uint count)
        {
            Delay_Power = 100;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\SC780_Offset_Temp_Drift.csv";
            //filename += ".csv";

            //uint[] sc810RegValue = new uint[4];

            #region Init RS232
            //btn_EngTab_Connect_Click(null, null);
            //Delay(500);
            //SetIP(20);
            //Delay(500);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                //uint tempResult = 0;
                //writer.WriteLine(filename);
                //if(writer.)
                string headers = "Temp,ID,2.5V,iHall_0.16,iHall_0.33,cGain_3,cGain_6,cGain_9,cGain_12,cGain_15" +
                                 "," +
                                 "," +
                                 "," +
                                 "";

                writer.WriteLine(headers);

                for (uint index = 0; index < count; index++)
                {
                    //MessageBox("DUT ON");
                    ResetTempBuf();
                    //DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    //if (dr == DialogResult.Cancel)
                    //    return;


                    //else if (dr == DialogResult.OK)
                    {
                        MultiSiteSocketSelect(index);
                        //write ID
                        writer.Write(this.txt_Char910_DutId.Text + "," + index.ToString() + ",");

                        //default 2.5V
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0x04);
                        //Delay(Delay_Sync);
                        //oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Sync);
                        //writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        //Delay(Delay_Sync);
                        //btn_EngTab_Ipoff_Click(null, null);
                        //Delay(Delay_Sync);
                        //writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        //Delay(Delay_Sync);

                        //2.5V IHALL-16
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        //Delay(Delay_Sync);
                        //oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x24);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        //btn_EngTab_Ipon_Click(null, null);
                        //Delay(Delay_Sync);
                        //writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        //Delay(Delay_Sync);
                        //btn_EngTab_Ipoff_Click(null, null);
                        //Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg02
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x28);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //vbg03
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x2C);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC4);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x00);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc2
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x22);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc4
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x44);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc6
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x66);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                        //0.5vdd tc8
                        PowerOff();
                        Delay(Delay_Power);
                        btn_SL620Tab_PowerOn_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        btn_SL620Tab_TestKey_Click(null, null);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, 0xC5);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, 0x20);
                        Delay(Delay_Sync);
                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, 0x88);
                        Delay(Delay_Sync);
                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);

                    }
                    writer.Write("\r\n");
                    //writer.Close();
                }
            }

        }
        //gain temp drift

        //

        #endregion SC780

        private void btn_Program_Start_Click(object sender, EventArgs e)
        {
            btn_Program_Tc.Text = "TC\r\n-\r\n-600ppm";
            int i = 0;

            while(true)
            {
                i++;
                Delay(100);
                if (oneWrie_device.SDPSingalPathReadSot())
                {
                    DisplayOperateMes("SOT is assert! --- " + i.ToString());
                    Delay(50);
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_EOT); //EPIO9
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_ONE); //EPIO10
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_TWO); //EPIO11
                    oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_FAIL); //EPIO8
                    //oneWrie_device.SDPSignalPathSet(OneWireInterface.SPControlCommand.SP_WRITE_BIN_RECYCLE); //EPIO12
                }
                else
                    DisplayOperateMes("No SOT! --- " + i.ToString());
            }

        }


        #region Tuning Tab

        private void InitTuningTab()
        {
            TunningTabReg[0] = 0x04;    //iHall -33%; Invert
            TunningTabReg[1] = 0x03;    //TcTh
            TunningTabReg[2] = 0x60;    //Tc2 = 6; Tc1 = 0
            TunningTabReg[3] = 0x30;    //Multi-Driven
            //for (uint i = 0; i < 9; i++)
            this.cb_TuningTab_IpUsage.SelectedIndex = 0;
            this.cb_TuningTab_OutOption.SelectedIndex = 0;
            this.cb_TuningTab_Product.SelectedIndex = 1;
            this.cb_TuningTab_PowerSupply.SelectedIndex = 0;
        }

        private void UpdateTrimCode()
        {
            if(this.cb_TuningTab_OutOption.SelectedText == "0.5VDD")
            {
                TunningTabReg[0] &= 0xC0;    //iHall -33%; Invert
                TunningTabReg[0] |= 0x01;
            }
            else if(this.cb_TuningTab_OutOption.SelectedText == "2.5V")
            {
                TunningTabReg[0] &= 0xC0;    //iHall -33%; Invert
                TunningTabReg[0] |= 0x00;
            }
            else if (this.cb_TuningTab_OutOption.SelectedText == "1.65V")
            {
                TunningTabReg[0] &= 0xC0;    //iHall -33%; Invert
                TunningTabReg[0] |= 0x02;
            } 

            TunningTabReg[1] = 0x03;    //TcTh
            TunningTabReg[2] = 0x60;    //Tc2 = 6; Tc1 = 0
            TunningTabReg[3] = 0x30;    //Multi-Driven
        }

        private void btn_TuningTab_Trim_Click(object sender, EventArgs e)
        {

        }

        private void btn_TuningTab_CoarseGainUp_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Coarse Gain ++");

                if (Ix_GainRough_TunningTab > 0)
                {
                    Ix_GainRough_TunningTab--;
                }

                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[1] &= ~bit_op_mask;
                TunningTabReg[1] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_GainRough_TunningTab]);
            }
            else if(this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_CoarseGainDown_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Coarse Gain --");

                if (Ix_GainRough_TunningTab < 15)
                {
                    Ix_GainRough_TunningTab++;
                }

                /* Rough Gain Code*/
                bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[1] &= ~bit_op_mask;
                TunningTabReg[1] |= Convert.ToUInt32(sl620CoarseGainTable[1][Ix_GainRough_TunningTab]);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_FineGainUp_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Fine Gain ++");

                if (Ix_GainFine_TunningTab > 0)
                {
                    Ix_GainFine_TunningTab--;
                }

                /* Fine Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[6] &= ~bit_op_mask;
                TunningTabReg[6] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_GainFine_TunningTab]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[7] &= ~bit_op_mask;
                TunningTabReg[7] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_GainFine_TunningTab]);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_FineGainDown_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Fine Gain --");

                if (Ix_GainFine_TunningTab < 63)
                {
                    Ix_GainFine_TunningTab++;
                }

                /* Fine Gain Code*/
                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[6] &= ~bit_op_mask;
                TunningTabReg[6] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_GainFine_TunningTab]);

                bit_op_mask = bit5_Mask | bit6_Mask | bit7_Mask;
                TunningTabReg[7] &= ~bit_op_mask;
                TunningTabReg[7] |= Convert.ToUInt32(sl620FineGainTable[2][Ix_GainFine_TunningTab]);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_CoarseOffsetUp_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Coarse Offset ++");

                if (Ix_OffsetA_TunningTab == 0)
                    Ix_OffsetA_TunningTab = 31;
                else if (Ix_OffsetA_TunningTab == 16)
                    Ix_OffsetA_TunningTab =16;
                else
                    Ix_OffsetA_TunningTab--;

                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                TunningTabReg[6] &= ~bit_op_mask;
                TunningTabReg[6] |= Convert.ToUInt32( Ix_OffsetA_TunningTab );
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_CoarseOffsetDown_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Coarse Offset --");

                if (Ix_OffsetA_TunningTab == 31)
                    Ix_OffsetA_TunningTab = 0;
                else if (Ix_OffsetA_TunningTab == 15)
                    Ix_OffsetA_TunningTab = 15;
                else
                    Ix_OffsetA_TunningTab++;

                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                TunningTabReg[6] &= ~bit_op_mask;
                TunningTabReg[6] |= Convert.ToUInt32(Ix_OffsetA_TunningTab);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_FineOffsetUp_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Fine Offset ++");

                if (Ix_OffsetB_TunningTab == 0)
                    Ix_OffsetB_TunningTab = 31;
                else if (Ix_OffsetB_TunningTab == 16)
                    Ix_OffsetB_TunningTab = 16;
                else
                    Ix_OffsetB_TunningTab--;

                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                TunningTabReg[7] &= ~bit_op_mask;
                TunningTabReg[7] |= Convert.ToUInt32(Ix_OffsetB_TunningTab);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_FineOffsetDown_Click(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                DisplayOperateMes("SL62x, Fine Offset --");

                if (Ix_OffsetB_TunningTab == 31)
                    Ix_OffsetB_TunningTab = 0;
                else if (Ix_OffsetB_TunningTab == 15)
                    Ix_OffsetB_TunningTab = 15;
                else
                    Ix_OffsetB_TunningTab++;

                bit_op_mask = bit0_Mask | bit1_Mask | bit2_Mask | bit3_Mask | bit4_Mask;
                TunningTabReg[7] &= ~bit_op_mask;
                TunningTabReg[7] |= Convert.ToUInt32(Ix_OffsetB_TunningTab);
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 0)
            {
                //DisplayOperateMes("Prodcut is SL61x");
            }
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
            {
                //DisplayOperateMes("Prodcut is SL91x");
            }
            else
                DisplayOperateMes("Not Supporting!");
        }

        private void btn_TuningTab_UpdateA_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;
            double VoA = 0;

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
            {
                #region Init RS232
                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);
                SetIP(Convert.ToUInt32(this.tb_TuningTab_Aip.Text));
                Delay(Delay_Power);
                #endregion
            }

            if (this.cb_TuningTab_PowerSupply.SelectedText == "E3631A")
                DisplayOperateMes("Todo...E3631A Control");
            else if (this.cb_TuningTab_PowerSupply.SelectedText == "Manual")
                DisplayOperateMes("Manual Control");
            else if (this.cb_TuningTab_PowerSupply.SelectedText == "OnBoard")
                RePower();

            EnterTestMode();

            //set trim code
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, TunningTabReg[0]);  //iHall -33%, Invert, 0.5VDD
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, TunningTabReg[2]);  //TC1/2
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, TunningTabReg[3]);  //Multi-Driven
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, TunningTabReg[1]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, TunningTabReg[6]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, TunningTabReg[7]);  //
            Delay(Delay_Sync);

            btn_SL620Tab_NormalMode_Click(null, null);

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
            {
                if (Convert.ToUInt32(this.tb_TuningTab_Aip.Text) != 0)
                    btn_EngTab_Ipon_Click(null, null);
            }

            VoA = ReadVout();
            this.tb_TuningTab_Va.Text = VoA.ToString("F3");
            this.tb_TuningTab_VaError.Text = (Convert.ToDouble(this.tb_TuningTab_TargetVa.Text) - VoA).ToString("F3");

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
                btn_EngTab_Ipoff_Click(null, null);
        }

        private void btn_TuningTab_UpdateB_Click(object sender, EventArgs e)
        {
            uint _dev_addr = this.DeviceAddress;
            double VoB = 0;

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
            {
                #region Init RS232
                btn_EngTab_Connect_Click(null, null);
                Delay(Delay_Power);                
                SetIP(Convert.ToUInt32(this.tb_TuningTab_Bip.Text));
                Delay(Delay_Power);
                #endregion
            }

            if (this.cb_TuningTab_PowerSupply.SelectedText == "E3631A")
                DisplayOperateMes("Todo...E3631A Control");
            else if (this.cb_TuningTab_PowerSupply.SelectedText == "Manual")
                DisplayOperateMes("Manual Control");
            else if (this.cb_TuningTab_PowerSupply.SelectedText == "OnBoard")
                RePower();

            EnterTestMode();

            //set trim code
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, TunningTabReg[0]);  //iHall -33%, Invert, 0.5VDD
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, TunningTabReg[2]);  //TC1/2
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, TunningTabReg[3]);  //Multi-Driven
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, TunningTabReg[1]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, TunningTabReg[6]);  //
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, TunningTabReg[7]);  //
            Delay(Delay_Sync);

            btn_SL620Tab_NormalMode_Click(null, null);

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
            {
                if (Convert.ToUInt32(this.tb_TuningTab_Bip.Text) != 0)
                    btn_EngTab_Ipon_Click(null, null);
            }

            VoB = ReadVout();
            this.tb_TuningTab_Vb.Text = VoB.ToString("F3");
            this.tb_TuningTab_VbError.Text = (Convert.ToDouble(this.tb_TuningTab_TargetVb.Text) - VoB).ToString("F3");

            if (this.cb_TuningTab_IpUsage.SelectedIndex == 0)
                btn_EngTab_Ipoff_Click(null, null);
        }

        private void btn_TunningTab_ConfigChange(object sender, EventArgs e)
        {
            if (this.cb_TuningTab_Product.SelectedIndex == 1)
            {
                if (this.cb_TuningTab_OutOption.SelectedIndex == 0)
                    TunningTabReg[0] &= 0xFC;
                else if (this.cb_TuningTab_OutOption.SelectedIndex == 1)
                    TunningTabReg[0] &= 0xFD;
            }
            else if(this.cb_TuningTab_Product.SelectedIndex == 0)
                DisplayOperateMes("Init Code for SL610");
            else if (this.cb_TuningTab_Product.SelectedIndex == 2)
                DisplayOperateMes("Init Code for SL910");
            else
                DisplayOperateMes("To be supported!");
        }

        private void btn_PowerClick(object sender, EventArgs e)
        {
            if (this.btn_TuningTab_Power.Text == "OFF")
            {
                PowerOn();
                this.btn_TuningTab_Power.Text = "ON";
                this.btn_TuningTab_Power.BackColor = Color.YellowGreen;
            }
            else if (this.btn_TuningTab_Power.Text == "ON")
            {
                PowerOff();
                this.btn_TuningTab_Power.Text = "OFF";
                this.btn_TuningTab_Power.BackColor = Color.Snow;
            }
        }

        #endregion

        private void btn_SL620Tab_IpOn_Click(object sender, EventArgs e)
        {
            btn_EngTab_Ipon_Click(null,null);
        }

        private void btn_SL620Tab_IpOff_Click(object sender, EventArgs e)
        {
            btn_EngTab_Ipoff_Click(null, null);
        }

        private void btn_ChannelSelect_Click(object sender, EventArgs e)
        {
            uint channel = Convert.ToUInt32(this.cb_ChannelSelect.SelectedIndex);
            MultiSiteSocketSelect(channel);
            DisplayOperateMes( string.Format("Channel {0} is Selected!", channel));
        }

        private void btn_SL620Tab_TrimSet2_Click(object sender, EventArgs e)
        {

        }

        private void btn_SL620Tab_VoutPair_Click(object sender, EventArgs e)
        {
            btn_SL620Tab_NormalMode_Click(null, null);
            btn_EngTab_Ipon_Click(null,null);
            DisplayOperateMes("VouIP = " + ReadVout().ToString("F3"));
            //ReadVout();
            btn_EngTab_Ipoff_Click(null, null);
            DisplayOperateMes("Vou0A = " + ReadVout().ToString("F3"));
        }

        private void btn_Routine_TcChar_Click(object sender, EventArgs e)
        {
            this.btn_Program_Start.Text = "...";
            this.btn_Program_Start.BackColor = Color.Yellow;

            #region init var

            Delay_Power = 100;
            uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\" + this.txt_Routines_TestCase.Text;
            filename += ".csv";

            //string filename;
            //SaveFileDialog saveDialog = new SaveFileDialog();
            //saveDialog.Title = "请选择save file";
            //saveDialog.Filter = ".csv(*.*)|*.*";
            //if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    filename = saveDialog.FileName;
            //}
            //else
            //    return;


            uint dutCount = Convert.ToUInt32(this.txt_Routines_DutCount.Text);
            uint tcCont = Convert.ToUInt32(this.txt_Routines_TcCount.Text);
            uint tcScale = Convert.ToUInt32( this.txt_Routines_TcCodeScale.Text);

            if (tcCont * tcScale > 16)
            {
                DisplayOperateMes( "请检测TC count和TC Scale的配置", Color.Red);
                return;
            }

            #endregion

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(100);
            SetIP(Convert.ToUInt32(this.txt_Routines_Ip.Text));
            Delay(100);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                #region Header
                writer.WriteLine(System.DateTime.Now.ToString());
                writer.WriteLine(this.txt_Routines_TestCase.Text);
                writer.WriteLine("Silicon = " + this.txt_Routines_SiliconVersion.Text);
                //writer.WriteLine("Temp = " + this.txt_Routines_TestTemp.Text);
                writer.WriteLine("IP = " + this.txt_Routines_Ip.Text + "A");
                string headers = "Temp,TC,Offset,VIP-0,V0A-0,VIP-1,V0A-1,VIP-2,V0A-2,VIP-3,V0A-3,VIP-4,V0A-4,VIP-5,V0A-5,VIP-6,V0A-6,VIP-7,V0A-7," +
                                                "VIP-8,V0A-8,VIP-9,V0A-9,VIP-10,V0A-10,VIP-11,V0A-11,VIP-12,V0A-12,VIP-13,V0A-13,VIP-14,V0A-14,VIP-15,V0A-15,";

                writer.WriteLine(headers);
                #endregion

                for (uint tcIndex = 0; tcIndex < tcCont; tcIndex++)
                {
                    #region 2.5V case
                    //writer.Write(this.txt_Routines_TestTemp.Text + "," + (tcIndex * tcScale).ToString() + ",2v5,");

                    //for (uint index = 0; index < dutCount; index++)
                    //{
                    //    ResetTempBuf();
                    //    //DialogResult dr = MessageBox.Show("Please Plug New Part In Socket", "opeartion", MessageBoxButtons.OKCancel);
                    //    //if (dr == DialogResult.Cancel)
                    //    //    return;
                    //    MultiSiteSocketSelect(index);

                    //    //write ID
                    //    //writer.Write(this.txt_Routines_TestTemp.Text + "," + tcIndex.ToString() + ",2v5," );

                    //    //default 2.5V
                    //    writeTestCode(trimData[index * 2 + 0]);

                    //    oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, tcIndex * tcScale * 16 + tcIndex * tcScale);
                    //    Delay(Delay_Sync);

                    //    btn_SL620Tab_NormalMode_Click(null, null);
                    //    Delay(Delay_Sync);
                    //    //writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                    //    //Delay(Delay_Sync);
                    //    btn_EngTab_Ipon_Click(null, null);
                    //    Delay(Delay_Sync);
                    //    writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                    //    Delay(Delay_Sync);
                    //    btn_EngTab_Ipoff_Click(null, null);
                    //    Delay(Delay_Sync);
                    //    writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                    //    Delay(Delay_Sync);
                    //}
                    //writer.Write("\r\n");
                    #endregion 

                    #region 0.5VDD case
                    writer.Write(this.txt_Routines_TestTemp.Text + "," + (tcIndex * tcScale).ToString() + ",halfVdd,");

                    for (uint index = 0; index < dutCount; index++)
                    {
                        //vbg01
                        writeTestCode(trimData[index * 2 + 1]);

                        oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, tcIndex * tcScale * 16 + tcIndex * tcScale);
                        Delay(Delay_Sync);

                        btn_SL620Tab_NormalMode_Click(null, null);
                        Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);


                        //writer.Write("\r\n");
                        //writer.Close();
                    }

                    writer.Write("\r\n");
                    #endregion
                }
            }           

            this.btn_Program_Start.Text = "Done";
            this.btn_Program_Start.BackColor = Color.Gray;
        }

        void writeTestCode( UInt32[] data )
        {
            uint _dev_addr = this.DeviceAddress;
            
            PowerOff();
            Delay(Delay_Power);
            btn_SL620Tab_PowerOn_Click(null, null);
            Delay(Delay_Sync);
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);
            btn_SL620Tab_TestKey_Click(null, null);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x80, data[0]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x81, data[1]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x82, data[2]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x83, data[3]);
            Delay(Delay_Sync);

            oneWrie_device.I2CWrite_Single(_dev_addr, 0x84, data[4]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x85, data[5]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x86, data[6]);
            Delay(Delay_Sync);
            oneWrie_device.I2CWrite_Single(_dev_addr, 0x87, data[7]);
            Delay(Delay_Sync);
        }

        private void btn_Routins_ReadVout_Click(object sender, EventArgs e)
        {
            this.btn_Program_Start.Text = "...";
            this.btn_Program_Start.BackColor = Color.Yellow;
            //Delay(Delay_Power);
            DisplayOperateMes("Start!");

            #region init var

            Delay_Power = 100;
            //uint _dev_addr = this.DeviceAddress;
            string filename = System.Windows.Forms.Application.StartupPath;
            filename += @"\" + this.txt_Routines_TestCase.Text;
            //+"-" + this.txt_Routines_TestTemp.Text;
            filename += ".csv";

            //RePower();

            //Delay(Delay_Fuse);

            uint dutCount = Convert.ToUInt32(this.txt_Routines_DutCount.Text);
            uint tcCont = Convert.ToUInt32(this.txt_Routines_TcCount.Text);
            uint tcScale = Convert.ToUInt32(this.txt_Routines_TcCodeScale.Text);

            #endregion

            #region Init RS232
            btn_EngTab_Connect_Click(null, null);
            Delay(100);
            SetIP(Convert.ToUInt32(this.txt_Routines_Ip.Text));
            Delay(100);
            #endregion

            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                #region Header
                writer.WriteLine(System.DateTime.Now.ToString());
                writer.WriteLine(this.txt_Routines_TestCase.Text);
                writer.WriteLine("IP = " + this.txt_Routines_Ip.Text + "A");
                string headers = "Temp,TC,VIP-0,V0A-0,VIP-1,V0A-1,VIP-2,V0A-2,VIP-3,V0A-3,VIP-4,V0A-4,VIP-5,V0A-5,VIP-6,V0A-6,VIP-7,V0A-7," +
                                                "VIP-8,V0A-8,VIP-9,V0A-9,VIP-10,V0A-10,VIP-11,V0A-11,VIP-12,V0A-12,VIP-13,V0A-13,VIP-14,V0A-14,VIP-15,V0A-15,";

                writer.WriteLine(headers);
                #endregion

                #region read vout

                for (int k = 0; k < tcCont; k++ )
                {
                    RePower();
                    Delay(1000);

                    writer.Write(this.txt_Routines_TestTemp.Text + "," + (k*tcScale).ToString("X2") + ",");

                    for (uint index = 0; index < dutCount; index++)
                    {
                        MultiSiteSocketSelect(index);
                        Delay(Delay_Sync);

                        EnterTestMode();
                        Delay(Delay_Sync);

                        //I2CWrite(0x82, 0x33);
                        I2CWrite(0x82, Convert.ToUInt32(k * tcScale * 17));
                        DisplayOperateMes("TC = " + (k * tcScale * 17).ToString("X2"));
                        btn_SL620Tab_NormalMode_Click(null,null);

                        Delay(1000);
                        //writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        //Delay(Delay_Sync);
                        btn_EngTab_Ipon_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //VIP
                        Delay(Delay_Sync);
                        btn_EngTab_Ipoff_Click(null, null);
                        Delay(Delay_Sync);
                        writer.Write(ReadVoutSlow().ToString("F3") + ",");       //V0A
                        Delay(Delay_Sync);
                    }
                    writer.Write("\r\n");
                }
                //writer.Write("\r\n");
                #endregion
            }

            this.btn_Program_Start.Text = "Done";
            this.btn_Program_Start.BackColor = Color.Gray;
            DisplayOperateMes("Done!",Color.Red);
        }

        private void btn_Routins_LoadFile_Click(object sender, EventArgs e)
        {
            //uint[][] trimData = new uint[16 * 2][];
            uint dutCount = Convert.ToUInt32(this.txt_Routines_DutCount.Text);

            for (int j = 0; j < 16 * 2; j++)
            {
                for (int i = 0; i < 8; i++)
                    trimData[j] = new uint[8];
            }

            try
            {
                string trimCodeFile = "";
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "请选择code file";
                dialog.Filter = ".cfg(*.*)|*.*";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    trimCodeFile = dialog.FileName;
                }
                else
                    return;

                this.txt_Routines_TestCase.Text = Path.GetFileNameWithoutExtension(trimCodeFile);

                StreamReader code = new StreamReader(trimCodeFile);

                string[] msg;

                for (int j = 0; j < dutCount * 2; j++)
                {
                    msg = code.ReadLine().Split(",".ToCharArray());

                    for (int i = 0; i < 8; i++)
                    {
                        trimData[j][i] = Convert.ToUInt32(msg[i], 16);
                    }
                }
                code.Close();
            }
            catch
            {
                MessageBox.Show("Load code file failed, please choose correct file!");
                return;
            }
        }

        private void btn_SL910_IpOn_Click(object sender, EventArgs e)
        {
            init_SL910_Ip(Convert.ToUInt32(this.txt_SL910_Ip.Text));
            Delay(100);
            IpOn();
        }

        private void btn_SL910_IpOff_Click(object sender, EventArgs e)
        {
            init_SL910_Ip(Convert.ToUInt32(this.txt_SL910_Ip.Text));
            Delay(100);
            IpOff();
        }

        private void button14_Click(object sender, EventArgs e)
        {

        }

        private void btn_Routins_Scan_Click(object sender, EventArgs e)
        {
            //string[] portlist = SerialPort.GetPortNames();

            string[] ss = MulGetHardwareInfo(HardwareEnum.Win32_PnPEntity, "Name");

            this.cmb_Routins_Com.Items.Clear();

            for (int i = 0; i < ss.Length; i++)
            {
                this.cmb_Routins_Com.Items.Add(ss[i]);
            }
            this.cmb_Routins_Com.SelectedIndex = 0;

            //for (int i = 0; i < portlist.Length; i++)
            //{
            //    this.cmb_Routins_Com.Items.Add(portlist[i]);
            //}
            //this.cmb_Routins_Com.SelectedIndex = 0;

            //Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;

            //Microsoft.Win32.RegistryKey software11 = hklm.OpenSubKey("HARDWARE");

            ////打开"HARDWARE"子健
            //Microsoft.Win32.RegistryKey software = software11.OpenSubKey("DEVICEMAP");

            //Microsoft.Win32.RegistryKey sitekey = software.OpenSubKey("SERIALCOMM");

            ////获取当前子健
            //String[] Str2 = sitekey.GetValueNames();

            ////Str2=System.IO.Ports.SerialPort.GetPortNames()；//第二中方法，直接取得串口值

            ////获得当前子健下面所有健组成的字符串数组
            //int ValueCount = sitekey.ValueCount;
            ////获得当前子健存在的健值
            //int i;
            //for (i = 0; i < ValueCount; i++)
            //{
            //    this.cmb_Routins_Com.Items.Add(sitekey.GetValue(Str2[i]));
            //}
        }

        /// <summary>
        /// WMI取硬件信息
        /// </summary>
        /// <param name="hardType"></param>
        /// <param name="propKey"></param>
        /// <returns></returns>
        public static string[] MulGetHardwareInfo(HardwareEnum hardType, string propKey)
        {

            List<string> strs = new List<string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity"))
                {
                    var hardInfos = searcher.Get();
                    foreach (var hardInfo in hardInfos)
                    {
                        
                            
                            {
                                if (hardInfo.Properties[propKey].Value != null)
                                {
                                    if (hardInfo.Properties[propKey].Value.ToString().Contains("(COM"))
                                    {
                                        strs.Add(hardInfo.Properties[propKey].Value.ToString());
                                    }
                                }
                            }
                        
                    }
                    searcher.Dispose();
                }
                return strs.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            { strs = null; }
        }

        private void btn_SL910_GainIncrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[2].Cells[3].Value.ToString(), 16);
            index = (reg_data & bit_op_mask) >> 4;

            if (index > 0)
            {
                index--;
                reg_data &= ~bit_op_mask;
                reg_data |= index << 4;
                SL910_Tab_DataGridView.Rows[2].Cells[3].Value = reg_data.ToString("X2");
                DisplayOperateMes("Gain++");
            }
            else
            {
                DisplayOperateMes("Max Coarse Gain", Color.Red);
            }
            //_reg_data = Convert.ToUInt32(st, 16);

            //MultiSiteReg6[idut] &= ~bit_op_mask;
            //MultiSiteReg6[idut] |= Convert.ToUInt32(sl620FineGainTable[1][Ix_forAutoAdaptingPresionGain]);
        }

        private void btn_SL910_GainDecrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint bit_op_mask = bit4_Mask | bit5_Mask | bit6_Mask | bit7_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[2].Cells[3].Value.ToString(), 16);
            index = (reg_data & bit_op_mask) >> 4;

            if (index < 15)
            {
                index++;
                reg_data &= ~bit_op_mask;
                reg_data |= index << 4;
                SL910_Tab_DataGridView.Rows[2].Cells[3].Value = reg_data.ToString("X2");
                DisplayOperateMes("Gain--");
            }
            else
            {
                DisplayOperateMes("Min Coarse Gain", Color.Red);
            }
        }

        private void btn_SL910_OffsetIncrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint direction = 0;
            uint bit_op_mask = bit7_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[1].Cells[3].Value.ToString(), 16);
            index = (reg_data & (~bit_op_mask));
            direction = (reg_data & bit_op_mask);

            if (direction == 0)
            {
                if (index < 63)
                {
                    //index++;
                    //reg_data &= ~bit_op_mask;
                    //reg_data |= index;
                    reg_data++;
                    SL910_Tab_DataGridView.Rows[1].Cells[3].Value = reg_data.ToString("X2");
                    DisplayOperateMes("Offset++");
                }
                else
                {
                    DisplayOperateMes("Max Offset Gain", Color.Red);
                }
            }
            else 
            {
                if (index > 0)
                {
                    //index++;
                    //reg_data &= ~bit_op_mask;
                    //reg_data |= index;
                    reg_data--;
                    SL910_Tab_DataGridView.Rows[1].Cells[3].Value = reg_data.ToString("X2");
                    DisplayOperateMes("Offset++");
                }
                else
                {
                    DisplayOperateMes("Max Offset Gain", Color.Red);
                }
            }
        }

        private void btn_SL910_OffsetDecrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint direction = 0;
            uint bit_op_mask = bit7_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[1].Cells[3].Value.ToString(), 16);
            index = (reg_data & (~bit_op_mask));
            direction = (reg_data & bit_op_mask);

            if (direction == 0)
            {
                if (index > 0)
                {
                    //index++;
                    //reg_data &= ~bit_op_mask;
                    //reg_data |= index;
                    reg_data--;
                    SL910_Tab_DataGridView.Rows[1].Cells[3].Value = reg_data.ToString("X2");
                    DisplayOperateMes("Offset--");
                }
                else
                {
                    DisplayOperateMes("Min Offset Gain", Color.Red);
                }
            }
            else
            {
                if (index < 63)
                {
                    //index++;
                    //reg_data &= ~bit_op_mask;
                    //reg_data |= index;
                    reg_data++;
                    SL910_Tab_DataGridView.Rows[1].Cells[3].Value = reg_data.ToString("X2");
                    DisplayOperateMes("Offset--");
                }
                else
                {
                    DisplayOperateMes("Min Offset Gain", Color.Red);
                }
            }
        }

        private void btn_SL910_FineGainIncrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[3].Cells[3].Value.ToString(), 16);
            index = (reg_data & bit_op_mask);

            if (index > 0)
            {
                index--;
                reg_data &= ~bit_op_mask;
                reg_data |= index;
                SL910_Tab_DataGridView.Rows[3].Cells[3].Value = reg_data.ToString("X2");
                DisplayOperateMes("Gain++");
            }
            else
            {
                DisplayOperateMes("Max Fine Gain", Color.Red);
            }
        }

        private void btn_SL910_FineGainDecrease_Click(object sender, EventArgs e)
        {
            uint index = 0;
            uint bit_op_mask = bit4_Mask | bit3_Mask | bit2_Mask | bit1_Mask | bit0_Mask;
            uint reg_data = Convert.ToUInt32(SL910_Tab_DataGridView.Rows[3].Cells[3].Value.ToString(), 16);
            index = (reg_data & bit_op_mask);

            if (index < 31)
            {
                index++;
                reg_data &= ~bit_op_mask;
                reg_data |= index;
                SL910_Tab_DataGridView.Rows[3].Cells[3].Value = reg_data.ToString("X2");
                DisplayOperateMes("Gain++");
            }
            else
            {
                DisplayOperateMes("Min Fine Gain", Color.Red);
            }
        }





    }

    
}
