using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.IO;

namespace App
{
  static class PrintMessage
  {
    static TFilterParameters  filter;       // ссылка на фильтр операций Read Write PValue
    static TOFlags          flags;          // ссылка на флаги управления выводом из Form1
    static Queue<TGUI_Msg>  GUI_MsgQ;       // ссылка на очередь для вывода на GUI Form1
    static TStatControl     statControl;    // ссылка на обработчик статистики данных

    // ссылки на объекты сохранения логов
    static TSaveToFileInfo fSamples,       // отсчеты данных
                            fPackets,       // сводная информация о блоках данных
                            fLog;           // лог

    //
    public static void SetStatControlPtr(ref TStatControl s)
    {
      statControl = s;
    }

    //
    public static void SetOFilterPtr(ref TFilterParameters f)
    {
      filter = f;
    }

    //
    public static void SetOFlagsPtr(ref TOFlags f)
    {
      flags = f;
    }

    //
    public static void SetGUI_MsgPtr(ref Queue<TGUI_Msg> q)
    {
      GUI_MsgQ = q;
    }

    //
    public static void SetLogFiles(ref TSaveToFileInfo samples, ref TSaveToFileInfo packets, ref TSaveToFileInfo log, 
                                   ref TSaveToFileInfo svSamples, ref TSaveToFileInfo svPackets)
    {
      fSamples  = samples;
      fPackets  = packets;
      fLog      = log;
      fSvSamples  = svSamples;
      fSvPackets  = svPackets;
    }

    //
    public static string getTimeStr()
    {
       return String.Format("{0,2:00}:{1,2:00}:{2,2:00}.{3,3:000}",
          DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
    }

    //---------------------------------------------------------------------------------------------
    //
    // Передача строки для вывода в лог.
    // Формирует соотв. объект и помещает его в очередь сообщений для GUI.
    //
    // msg            строка для вывода
    // suppressTime   true - не добавлять в вывод текущее время
    //
    public static void LogOut(string msg, bool suppressTime=false)
    {
      string s;
      if (suppressTime == false)
      {
        string tm = String.Format("{0,2:00}:{1,2:00}:{2,2:00}.{3,3:000}",
          DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
        s = tm + " " + msg;
      }
      else s = msg;
      
      lock (GUI_MsgQ)
      {
        GUI_MsgQ.Enqueue(new TGUI_Msg(s));
        
        if(fLog.isActive())
          fLog.write(s);
      }
    }

    //---------------------------------------------------------------------------------------------
    #region PrintMessages
    //
    // Первичные данные по интерфейсу связи
    //
    // isTx     true - исходящие данные
    //
    public static void PrintLowIO(bool isTx,  ref byte[] buffer)
    {
      string s = "";
      if (flags.LowIO)
        s = string.Format(isTx ? "Tx:{0}" : "Rx:{0}", buffer.Length);    

      if (!isTx  ||  (isTx  &&  flags.OutMsg))
      {
        if (flags.Raw)
        {
          s += " :";
          s += string.Concat(buffer.Select(b => (b.ToString("X2") + " ")));
        }
        if (flags.Ascii)
        {
          s += " :";
          s += string.Concat(buffer.Select(b => (Convert.ToChar(b))));
        }
      }

      if(s.Length > 0) LogOut(s + "\r\n");
    }

    //
    public static void PrintMsgValue(DataIo.Types.MsgParameterValue msg, ushort destAddress, ushort srcAddress, bool outMsg)
    {
      if(flags.RW  &&  filter.checkOperation(srcAddress, msg.parameterNumber, TFilterParameters.TOpCode.opPValue))
      {
        string fmt = "";

        if (flags.PV_Format2)
          fmt = "PValue         n:{3:d2}\tv:{4}\t\t0x{4:X}\r\n";
        else
        {
          if (flags.PV_Hex) fmt = "PValue         da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d2}  pv:{4} {4:X}\trc:{5}\r\n";
          else fmt = "PValue         da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d2}  pv:{4}\trc:{5}\r\n";
        }

        string s = String.Format(fmt, destAddress, srcAddress, msg.sequenceNumber, msg.parameterNumber, msg.parameterValue, msg.result);
        LogOut(outMsg ? ">" + s :  " " + s);
      }
    }

    //
    public static void PrintMsgWrite(DataIo.Types.MsgParameterWrite msg, ushort destAddress, ushort srcAddress, bool outMsg)
    {
      if(flags.RW  &&  filter.checkOperation(destAddress, msg.parameterNumber, TFilterParameters.TOpCode.opWrite))
      {
        string fmt = "";
        if(flags.PV_Hex) fmt = "PWrite         da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d2}  pv:{4}\t{4:X}\r\n";
        else             fmt = "PWrite         da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d2}  pv:{4}\r\n";
        
        string s = String.Format(fmt, destAddress, srcAddress, msg.sequenceNumber, msg.parameterNumber, msg.parameterValue);
        LogOut(outMsg ? ">" + s :  " " + s);
      }
    }

    //
    public static void PrintMsgRead(DataIo.Types.MsgParameterRead msg, ushort destAddress, ushort srcAddress, bool outMsg)
    {
      if(flags.RW  &&  filter.checkOperation(destAddress, msg.parameterNumber, TFilterParameters.TOpCode.opRead))
      {
        string s = String.Format("PRead          da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d2}\r\n",
          destAddress, srcAddress, msg.sequenceNumber, msg.parameterNumber);
        LogOut(outMsg ? ">" + s :  " " + s);
      }
    }

    //
    public static void PrintMsgReadBlock(DataIo.Types.MsgReadBlock msg, ushort destAddress, ushort srcAddress, bool outMsg)
    {
      if (flags.RegBlock /*&& filter.checkOperation(destAddress, msg.parameterNumber, TFilterParameters.TOpCode.opRead)*/)
      {
        string s = String.Format("PReadBlock     da:{0:d2} sa:{1:d2} seq:{2:d3}  base:{3:d2} n:{4}\r\n",
          destAddress, srcAddress, msg.sequenceNumber, msg.baseAddr, msg.size);
        LogOut(outMsg ? ">" + s : " " + s);
      }
    }


    //
    // Извлечение данных из MsgAppType в DataBuffer
    //
    private static void MsgAppTypeToDataBuffer(byte[] message, ref DataIo.Types.TDataBuffer p)
    {
      byte[] arr = new byte[message.Length];
      for (int i = Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)), j = 0; i < message.Length; i++, j++)
        arr[j] = message[i];
      p = DataIo.Types.ByteToStruct<DataIo.Types.TDataBuffer>(arr);
    }

    
    //
    // Формирует по MsgAppType структуру расширенного типа MsgApp..
    //
    // message    low level буфер, содержащий структуру MsgAppTypeHeader + данные сообщения
    // p          структура - расширение MsgAppType, содержащая MsgAppTypeHeader + данные
    //
    private static void MsgAppTypeToExtendedStruct<T>(byte[] message, ref T p) where T : struct
    {
      int size = Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)) + message.Length;
      byte[] arr = new byte[size];
      for (int i =0; i < size; i++)
        arr[i] = message[i];
      p = DataIo.Types.ByteToStruct<T>(arr);
    }

    //---------------------------------------------------------------------------------------------
    //
    public static void PrintMsgAppType(byte[] message, ushort destAddress, ushort srcAddress, bool outMsg)
    {
      DataIo.Types.MsgAppStateInfo       psi   = new DataIo.Types.MsgAppStateInfo();
      DataIo.Types.MsgAppStateInfo_31    psi31 = new DataIo.Types.MsgAppStateInfo_31();
      DataIo.Types.MsgAppStateInfo_32    psi32 = new DataIo.Types.MsgAppStateInfo_32();
      DataIo.Types.TDataBuffer              p     = new DataIo.Types.TDataBuffer();
      DataIo.Types.MsgAppDateTime        p1    = new DataIo.Types.MsgAppDateTime();
      DataIo.Types.MsgAppSvData          p2    = new DataIo.Types.MsgAppSvData();
      DataIo.Types.tagSvSvDataReadError     p5    = new DataIo.Types.tagSvSvDataReadError();
      DataIo.Types.MsgAppSvDateTime      p6    = new DataIo.Types.MsgAppSvDateTime();
      DataIo.Types.MsgAppDataAcknowledge p7    = new DataIo.Types.MsgAppDataAcknowledge();
      DataIo.Types.MsgAppWakeup          p8    = new DataIo.Types.MsgAppWakeup(); 

      byte[] byteHeader = new byte[Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader))];
      for (int i = 0; i < Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)); i++) byteHeader[i] = message[i];
      DataIo.Types.MsgAppTypeHeader msg = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppTypeHeader>(byteHeader);

      bool isDataBuffer = false;    // устанавливается при заполнении p или p2

      // вывод в лог и/или сохранение в файл отсчетов данных;  или вывод статистики - выполняется в printData..
      bool fprint  = flags.Samples || flags.MeanData || flags.HexData || statControl.active();
      bool fsave   = fSamples.isActive();
      bool fSvSave = fSvSamples.isActive();
                                                                       
      //--------------------------------
      ushort type = Convert.ToUInt16(message[3]);   
      type <<= 8;
      type |= message[2];

      int nc = 1;			                                // число каналов для блоков данных
      int ds = 0;                                     // dataSize - число отсчетов данных

      //---------------------------------------------------
      // Обработка кодирования сетевого адреса устройства в типе сообщения - концентратор КС3 (2025)
      int srcNetAddress = 0;
      bool srcNetAddressCoded = false;

      if ((type & 0x8000) != 0)
      {
        srcNetAddress = (type >> 8) & 0x7F;
        srcNetAddressCoded = true;
        type &= 0x00FF;
      }
      //---------------------------------------------------
      // Обработка кодирования индекса АЦП в типе сообщения - для каких устройств??
      int adcIndex = 0;
      if (!srcNetAddressCoded)
      {
        adcIndex = (type & 0x0C00) >> 11;     // индекс АЦП - источника данных
        type &= 0x03FF;                       // обнулить индекс АЦП
      }
      //---------------------------------------------------
      msg.type = type;

      // "нормализовать" id для многоканальных блоков данных и вычислить число каналов
      if (type >= DataIo.Const.kMsgApp_DataInt16xBase  &&  type <= DataIo.Const.kMsgApp_DataInt16xBase + DataIo.Types.MAX_NCHANNELS)
	    {
		    nc = type - DataIo.Const.kMsgApp_DataInt16xBase;
		    type = DataIo.Const.kMsgApp_DataInt16xBase;
	    }
	    if(type >= DataIo.Const.kMsgApp_DataInt32xBase  &&  type <= DataIo.Const.kMsgApp_DataInt32xBase + DataIo.Types.MAX_NCHANNELS)
	    {
		    nc = type - DataIo.Const.kMsgApp_DataInt32xBase;
		    type = DataIo.Const.kMsgApp_DataInt32xBase;
	    }

      // определение числа каналов для фиксированных типов данных
      switch(type)
      {
        default: break;
        case DataIo.Const.kMsgApp_DataInt16x2: nc = 2; break;
        case DataIo.Const.kMsgApp_DataInt16x3: nc = 3; break;
    	  case DataIo.Const.kMsgApp_DataInt16x4: nc = 4; break;
    	  case DataIo.Const.kMsgApp_DataInt16x6: nc = 6; break;
    	  case DataIo.Const.kMsgApp_DataInt16x8: nc = 8; break;
    	  case DataIo.Const.kMsgApp_DataInt32x2: nc = 2; break;
    	  case DataIo.Const.kMsgApp_DataInt32x3: nc = 3; break;
    	  case DataIo.Const.kMsgApp_DataInt32x4: nc = 4; break;
    	  case DataIo.Const.kMsgApp_DataInt32x8: nc = 8; break;

    	  case DataIo.Const.kMsgApp_SvData16x2: 	nc = 2; break;
    	  case DataIo.Const.kMsgApp_SvData16x3: 	nc = 3; break;
    	  case DataIo.Const.kMsgApp_SvData16x4: 	nc = 4; break;
    	  case DataIo.Const.kMsgApp_SvData16x6: 	nc = 6; break;
    	  case DataIo.Const.kMsgApp_SvData16x8: 	nc = 8; break;
    	  case DataIo.Const.kMsgApp_SvData32x2: 	nc = 2; break;
    	  case DataIo.Const.kMsgApp_SvData32x3: 	nc = 3; break;
    	  case DataIo.Const.kMsgApp_SvData32x4: 	nc = 4; break;
    	  case DataIo.Const.kMsgApp_SvData32x6: 	nc = 6; break;
    	  case DataIo.Const.kMsgApp_SvData32x8: 	nc = 8; break;
      }

      //--------------------------------
      string s=null, sp="??";

      switch (type)             
      {
        default:
          if(flags.App)
            s = String.Format("AppType        da:{0:d2} sa:{1:d2} seq:{2:d3} type:{3} n:{4}\r\n", destAddress, srcAddress, msg.sequenceNumber, msg.type, msg.dataSize);
          break;


        #region MsgApp_Boot handlers

        case DataIo.Const.kMsgApp_BootAck:
          {
            DataIo.Types.MsgApp_BootAck pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootAck>(message);
//            MsgAppTypeToExtendedStruct(message, ref pb);

            switch (Convert.ToUInt32(pb.cmdCode))
            {
              default:
                s = String.Format("BootAck        da:{0:d2} sa:{1:d2} seq:{2:d3}  cmd:{3} rc:{4}\r\n",
                  destAddress, srcAddress, msg.sequenceNumber, pb.cmdCode, pb.cmdResult);
                break;

              case DataIo.Types.kBootAck_RamData:
                {
                  DataIo.Types.MsgApp_BootAck_PacketNumber pb1 = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootAck_PacketNumber>(message);
//                  MsgAppTypeToExtendedStruct(message, ref pb1);
                  s = String.Format("BootAck_RamDat da:{0:d2} sa:{1:d2} seq:{2:d3}  cmd:{3} rc:{4} pn:{5}\r\n",
                    destAddress, srcAddress, msg.sequenceNumber, pb1.cmdCode, pb1.cmdResult, pb1.packetNumber);
                }
                break;

              case DataIo.Types.kBootAck_DeviceInfo:
                {
                  DataIo.Types.MsgApp_BootAck_DeviceInfo pb1 = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootAck_DeviceInfo>(message);
                  s = String.Format("BootAck_DeviceInfo  da:{0:d2} sa:{1:d2} seq:{2:d3}  cmd:{3} rc:{4}\r\n",
                    destAddress, srcAddress, msg.sequenceNumber, pb1.cmdCode, pb1.cmdResult);
                }
                break;
            }
          }
          break;

        case DataIo.Const.kMsgApp_BootService:
          {
            DataIo.Types.MsgApp_BootService_Arg1 pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootService_Arg1>(message);
//            MsgAppTypeToExtendedStruct(message, ref pb);

            string cmd = "";

            switch(pb.data0)
            {
              default: cmd = pb.data0.ToString();  break;
              case DataIo.Types.kCmdBootService_GetDeviceInfo:    cmd = "GetDeviceInfo";  break;
              case DataIo.Types.kCmdBootService_Erase:            cmd = "Erase";          break;
              case DataIo.Types.kCmdBootService_Res0:             cmd = "Res0";           break;
              case DataIo.Types.kCmdBootService_WriteRamToFlash:  cmd = "WrRamToFlash";   break;
              case DataIo.Types.kCmdBootService_Check:            cmd = "CheckImage";     break;
              case DataIo.Types.kCmdBootService_RunBootloader:    cmd = "RunBootloader";  break;
            }

            switch (pb.data0)
            {
              default:
                s = String.Format("BootService    da:{0:d2} sa:{1:d2} seq:{2:d3}  cmd:{3}\r\n",
                  destAddress, srcAddress, msg.sequenceNumber, cmd);
                break;

              case DataIo.Types.kCmdBootService_Erase:
              case DataIo.Types.kCmdBootService_WriteRamToFlash:
                {
                  DataIo.Types.MsgApp_BootService_Arg3 pb1 = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootService_Arg3>(message);
                  s = String.Format("BootService    da:{0:d2} sa:{1:d2} seq:{2:d3}  cmd:{3} {4} {5}\r\n",
                    destAddress, srcAddress, msg.sequenceNumber, cmd, pb1.data1, pb1.data2);
                }
                break;
            }
          }
          break;

        case DataIo.Const.kMsgApp_BootRamData:
          {
            DataIo.Types.MsgApp_BootRamDataHdr pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgApp_BootRamDataHdr>(message);
    
            s = String.Format("BootRamData    da:{0:d2} sa:{1:d2} seq:{2:d3}  offset:{3} pn{4} msg.size:{5} size:{6}\r\n",
                destAddress, srcAddress, msg.sequenceNumber, pb.offset, pb.packetNumber, msg.dataSize, msg.dataSize-pb.getServiceSize());
          }
          break;

        #endregion MsgApp_Boot handlers

        case DataIo.Const.kMsgApp_RegBlock:
          if (flags.RegBlock)
          {
//            DataIo.Types.MsgParameterBlockHeader pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgParameterBlockHeader>(message);
            DataIo.Types.MsgParameterBlock pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgParameterBlock>(message);
            s = String.Format("RegBlock       da:{0:d2} sa:{1:d2} seq:{2:d3}  base:{3} n:{4} err:{5}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, pb.baseAddr, pb.size, pb.errCount);
          }
          break;

        case DataIo.Const.kMsgApp_DataAbsence:
          if(flags.DataRq)
            s = String.Format("DataAbsence    da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_DataAbsence2:
          if (flags.DataRq)
            s = String.Format("DataAbsence2   da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_RequestData:
          if(flags.DataRq)
            s = String.Format("RequestData    da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_DataAcknowledgeOk:
        case DataIo.Const.kMsgApp_DataAcknowledgeError:
          if(flags.State || flags.DataRq)
          {
            string s1 = type == DataIo.Const.kMsgApp_DataAcknowledgeOk ?  "DataAckOk   " : "DataAckError";
            s = String.Format("{3}   da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber, s1);
          }
          break;

        case DataIo.Const.kMsgApp_DataAcknowledge:
          if(flags.State || flags.DataRq)
          {
            p7 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppDataAcknowledge>(message);
            s = String.Format("DataAck        da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{3:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber, p7.packetNumber);
          }
          break;

        case DataIo.Const.kMsgApp_StateInfo:
          if(flags.State)
          {
            psi = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppStateInfo>(message);
            s = String.Format("StateInfo      da:{0:d2} sa:{1:d2} seq:{2:d3}  Ub:{3} ps:0x{4:X2} dis:0x{5:X2} dbn:{6}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, psi.Ubat, psi.powerState, psi.dataInputState, psi.dataBlocksNumber);
          }
          break;

        case DataIo.Const.kMsgApp_StateInfo_31:
          if(flags.State)
          {
            psi31 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppStateInfo_31>(message);
            s = String.Format("StateInfo31    da:{0:d2} sa:{1:d2} seq:{2:d3}  sy:{3} Up:{4} dis:0x{5:X2} dbn:{6} ht:{7} t:{8} {9} {10} {11}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, 
              psi31.sync, psi31.Upwr, psi31.dataInputState, psi31.dataBlocksNumber, psi31.s0, psi31.t[0], psi31.t[1], psi31.t[2], psi31.t[3]);
          }
          break;

        case DataIo.Const.kMsgApp_StateInfo_32:
          if(flags.State)
          {
            psi32 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppStateInfo_32>(message);
            s = String.Format("StateInfo32    da:{0:d2} sa:{1:d2} seq:{2:d3}  Ub:{3} ps:0x{4:X2} dis:0x{5:X2} dbn:{6} t:{7} {8}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, 
              psi32.Ubat, psi32.powerState, psi32.dataInputState, psi32.dataBlocksNumber, psi32.t[0], psi32.t[1]);
          }
          break;

        case DataIo.Const.kMsgApp_StateInfo_9221:
          if (flags.State)
          {
            DataIo.Types.MsgAppStateInfo_9221 psi9221 = new DataIo.Types.MsgAppStateInfo_9221();
            psi9221 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppStateInfo_9221>(message);
            s = String.Format("StateInfo9221  da:{0:d2} sa:{1:d2} seq:{2:d3}  Ub:{3} ps:0x{4:X2} dis:0x{5:X2} dbn:{6} t:{7} {8} R150:{9:X2}\r\n",
              destAddress, srcAddress, msg.sequenceNumber,
              psi9221.Ubat, psi9221.powerState, psi9221.dataInputState, psi9221.dataBlocksNumber, psi9221.t[0], psi9221.t[1], psi9221.r150);
          }
          break;

        case DataIo.Const.kMsgApp_StateInfo_5430:
          if (flags.State)
          {
            DataIo.Types.MsgAppStateInfo_5430 psi5430 = new DataIo.Types.MsgAppStateInfo_5430();
            psi5430 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppStateInfo_5430>(message);
            s = String.Format("StateInfo5430  da:{0:d2} sa:{1:d2} seq:{2:d3}  R17:0x{3:X4} flags:0x{4:X2} dbn1:{5} dbn2:{6} R150:{7:X2}\r\n",
              destAddress, srcAddress, msg.sequenceNumber,
              psi5430.r17, psi5430.flags, psi5430.dataBlocksNumber1, psi5430.dataBlocksNumber2, psi5430.r150);
          }
          break;

        case DataIo.Const.kMsgApp_DataDevice9400:
        case DataIo.Const.kMsgApp_DataInt32:
        case DataIo.Const.kMsgApp_DataInt16:
          if(flags.DataBlocksInfo || fPackets.isActive())
          {
            MsgAppTypeToDataBuffer(message, ref p);
            isDataBuffer = true;

              if (msg.type == DataIo.Const.kMsgApp_DataDevice9400) sp = "Data9400";
              else if (msg.type == DataIo.Const.kMsgApp_DataInt32) sp = "DataI32 ";
              else if (msg.type == DataIo.Const.kMsgApp_DataInt16) sp = "DataI16 ";
    
            if(flags.DataBlocksInfo)
            {
              // adcIndex=0 не выводится (основной АЦП)
              s = String.Format(adcIndex == 0 ? "{3}       da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{5:d3} sy:{6} sz:{7}\r\n" :
                                                "{3}:{4}     da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{5:d3} sy:{6} sz:{7}\r\n",
                destAddress, srcAddress, msg.sequenceNumber, sp, adcIndex, p.packetNumber, p.sync, p.dataSize);
            }
            if(fPackets.isActive())
            {
              string ss = String.Format("{0};{1};{2};{3};{4};{5}\r\n", sp, adcIndex, msg.sequenceNumber, p.packetNumber, p.sync, p.dataSize);
              fPackets.write(ss);
            }
          }
          break;

        case DataIo.Const.kMsgApp_DataInt32x2:
        case DataIo.Const.kMsgApp_DataInt16x2:
        case DataIo.Const.kMsgApp_DataInt32x3:
		    case DataIo.Const.kMsgApp_DataInt16x3:
		    case DataIo.Const.kMsgApp_DataInt32x4:
		    case DataIo.Const.kMsgApp_DataInt16x4:
        case DataIo.Const.kMsgApp_DataInt16x6:
        case DataIo.Const.kMsgApp_DataInt16xBase:
		    case DataIo.Const.kMsgApp_DataInt32xBase:
          //
          // ВНИМАНИЕ: для многоканальных типов данных в p.dataSize передается число 
          //           _многоканальных_отсчетов_.
          //           Число отсчетов базового типа (Uint16 Int16 Int32) равно (p.dataSize * nc).
          //
          if(flags.DataBlocksInfo || fPackets.isActive() || fprint)
          {
            MsgAppTypeToDataBuffer(message, ref p);
            isDataBuffer = true;

            if (msg.type == DataIo.Const.kMsgApp_DataInt32x2)      { sp = "DataI32x2"; nc = 2; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt32x3) { sp = "DataI32x3"; nc = 3; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt32x4) { sp = "DataI32x4"; nc = 4; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt16x2) { sp = "DataI16x2"; nc = 2; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt16x3) { sp = "DataI16x3"; nc = 3; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt16x4) { sp = "DataI16x4"; nc = 4; ds = p.dataSize; }
            else if (msg.type == DataIo.Const.kMsgApp_DataInt16x6) { sp = "DataI16x6"; nc = 6; ds = p.dataSize; }
            // "нормализованный" type
            else if (type == DataIo.Const.kMsgApp_DataInt16xBase) { sp = String.Format("DataI16#{0}", nc); ds = p.dataSize / nc; }
            else if (type == DataIo.Const.kMsgApp_DataInt32xBase) { sp = String.Format("DataI32#{0}", nc); ds = p.dataSize / nc; }
            
            if(flags.DataBlocksInfo)
            {
              if(srcNetAddressCoded)
                s = String.Format("{3}      da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{5:d4} sy:{6} sz:{7} n:{8} addr:{9}\r\n",
                  destAddress, srcAddress, msg.sequenceNumber, sp, adcIndex, p.packetNumber, p.sync, p.dataSize, ds, srcNetAddress);
              else
                s = String.Format(adcIndex == 0 ? "{3}      da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{5:d4} sy:{6} sz:{7} n:{8}\r\n" :
                                                  "{3}:{4}    da:{0:d2} sa:{1:d2} seq:{2:d3}  pn:{5:d4} sy:{6} sz:{7} n:{8}\r\n",
  //                destAddress, srcAddress, msg.sequenceNumber, sp, adcIndex, p.packetNumber, p.sync, p.dataSize, p.dataSize/nc);
                  destAddress, srcAddress, msg.sequenceNumber, sp, adcIndex, p.packetNumber, p.sync, p.dataSize, ds);
            }
            if(fPackets.isActive())
            {
//              string ss = String.Format("{0};{1};{2};{3};{4};{5}\r\n", sp, adcIndex, msg.sequenceNumber, p.packetNumber, p.sync, p.dataSize/nc);
              string ss = String.Format("{0};{1};{2};{3};{4};{5}\r\n", sp, adcIndex, msg.sequenceNumber, p.packetNumber, p.sync, p.dataSize);
              fPackets.write(ss);
            }
          }
          break;

        case DataIo.Const.kMsgApp_DateTime:
          if(flags.App)
          {
            p1 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppDateTime>(message);
            s = String.Format("DateTime       da:{0:d2} sa:{1:d2} seq:{2:d3}  {3:d2}.{4:d2}.{5:d4} {6:d2}:{7:d2}:{8:d2}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, 
              p1.dayOfMonth, p1.month, p1.year, p1.hours, p1.minutes, p1.seconds);
          }
          break;

        case DataIo.Const.kMsgApp_RequestDateTime:
          if(flags.App)
            s = String.Format("RequestDateTime  da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_Wakeup:
          if(flags.Wakeup)
          {
            p8 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppWakeup>(message);
            s = String.Format("Wakeup         da:{0:d2} sa:{1:d2} seq:{2:d3}  3:{3:X8} 2:{4:X8} 1:{5:X8} 0:{6:X8}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, p8.addr[3],p8.addr[2],p8.addr[1],p8.addr[0]);
          }
          break;
        
        //-------- Чтение из УХД
        case DataIo.Const.kMsgApp_RequestSvData:
          s = String.Format("RequestSvData  da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_SvDataAcknowledge:
          if (flags.SvDataBlocksInfo)
//            s = String.Format("SvDataAck      da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          {
            p7 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppDataAcknowledge>(message);
            s = String.Format("SvDataAck      da:{0:d2} sa:{1:d2} seq:{2:d3}  pg:{3:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber, p7.packetNumber);
          }
          break;

        case DataIo.Const.kMsgApp_SvDataAbsence:
          if (flags.SvDataBlocksInfo)
            s = String.Format("SvDataAbsence  da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_FlashGoBusy:
          if (flags.SvDataBlocksInfo)
            s = String.Format("FlashGoBusy    da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_SvDataAcknowledgeOk:
          if (flags.SvDataBlocksInfo)
            s = String.Format("SvDataAckOk    da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_SvDataAcknowledgeError:
          if (flags.SvDataBlocksInfo)
            s = String.Format("SvDataAckError da:{0:d2} sa:{1:d2} seq:{2:d3}\r\n", destAddress, srcAddress, msg.sequenceNumber);
          break;

        case DataIo.Const.kMsgApp_SvDataReadError:
          if (flags.SvDataBlocksInfo)
          {
            DataIo.Types.tagSvSvDataReadError p9 = new DataIo.Types.tagSvSvDataReadError();
            p9 = DataIo.Types.ByteToStruct<DataIo.Types.tagSvSvDataReadError>(message);

            s = String.Format("SvDataReadError  da:{0:d2} sa:{1:d2} seq:{2:d3} pg:{3:d} err:{4:d}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, p9.page, p9.error);
          }
          break;

        case DataIo.Const.kMsgApp_SvDateTime:
          if (flags.SvDataBlocksInfo)
          {
            DataIo.Types.MsgAppSvDateTime p10 = new DataIo.Types.MsgAppSvDateTime();
            p10 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppSvDateTime>(message);

            s = String.Format("SvDateTime     da:{0:d2} sa:{1:d2} seq:{2:d3}  pg:{3:d} {4:d2}.{5:d2}.{6:d2} {7:d2}:{8:d2}:{9:d2} r:{10}\r\n",
              destAddress, srcAddress, msg.sequenceNumber, p10.page,
				      p10.year,	p10.month, p10.dayOfMonth, p10.hours, p10.minutes, p10.seconds, p10.reserved);
          }
          break;

        case DataIo.Const.kMsgApp_SvData32:
		    case DataIo.Const.kMsgApp_SvData16:
		    case DataIo.Const.kMsgApp_SvData16x3:
        case DataIo.Const.kMsgApp_SvData16x4:
        case DataIo.Const.kMsgApp_SvData16x6:
        case DataIo.Const.kMsgApp_SvData16x8:
        case DataIo.Const.kMsgApp_SvData32x3:
		    case DataIo.Const.kMsgApp_SvData32x4:
        case DataIo.Const.kMsgApp_SvData32x6:
        case DataIo.Const.kMsgApp_SvData32x8:
        case DataIo.Const.kMsgApp_SvData9400:
          if (flags.SvDataBlocksInfo || fSvPackets.isActive())
          {
            p2 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppSvData>(message);
            isDataBuffer = true;

            if (msg.type      == DataIo.Const.kMsgApp_SvData32)    sp = "SvData32";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData16)    sp = "SvData16";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData16x3)  sp = "SvData16x3";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData16x4)  sp = "SvData16x4";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData16x6)  sp = "SvData16x6";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData16x8)  sp = "SvData16x8";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData32x3)  sp = "SvData32x3";
            else if(msg.type  == DataIo.Const.kMsgApp_SvData32x4)  sp = "SvData32x4";
            else if (msg.type == DataIo.Const.kMsgApp_SvData32x6)  sp = "SvData32x6";
            else if (msg.type == DataIo.Const.kMsgApp_SvData32x8)  sp = "SvData32x8";
            else if (msg.type == DataIo.Const.kMsgApp_SvData9400)  sp = "SvData9400";
            
            if(flags.SvDataBlocksInfo)
            {
//				s.printf("%s:%d     seq:%d  pg:%d pn:%d sy:%d sz:%d   n:%d", mt, adcIndex,
//							p2->header.sequenceNumber, p2->page,
//							p2->data.packetNumber, p2->data.sync, p2->data.dataSize, msg->dataSize);
            
              s = String.Format("{3}     da:{0:2d} sa:{1:d2} seq:{2:d3}  pg:{4:d3} pn:{5:d3} sy:{6} sz:{7}\r\n",
                destAddress, srcAddress, msg.sequenceNumber, sp, p2.page, p2.data.packetNumber, p2.data.sync, p2.data.dataSize);
            }

            if(fSvPackets.isActive())
            {
              string ss = String.Format("{0};{1};{2};{3};{4}\r\n", 
                                        sp, p2.page, p2.data.packetNumber, p2.data.sync, p2.data.dataSize);
              fSvPackets.write(ss);
            }
          }
          break;   
      }
      
      if(s != null) LogOut(outMsg ? ">" + s :  " " + s);

	    if(fprint || fsave)
      {
        if(isDataBuffer == false)
        {
          MsgAppTypeToDataBuffer(message, ref p);
          isDataBuffer = true;
        }

        switch(type)
	      {
		      default: break;

		      case DataIo.Const.kMsgApp_DataInt16x3:
		      case DataIo.Const.kMsgApp_DataInt16x4:
          case DataIo.Const.kMsgApp_DataInt16x6:
            if (fprint) printData_X3_X4_X6(ref p, sp, 0, nc);
            if (fsave) writeData_XN(ref fSamples, ref p, sp, 0, nc, true, p.packetNumber, p.sync);
            break;

		      case DataIo.Const.kMsgApp_DataInt32x3:
		      case DataIo.Const.kMsgApp_DataInt32x4:
            if(fprint) printData_X3_X4_X6(ref p, sp, 1, nc);
            if (fsave) writeData_X3_X4_X6(ref fSamples, ref p, 1, nc);
            break;

		      case DataIo.Const.kMsgApp_DataInt32:
            if(fprint) printData_I16_I32(ref p, "DataI32", 1);
            if(fsave)  writeData_I16_I32(ref fSamples, ref p, 1, 0, 0);
            break;

		      case DataIo.Const.kMsgApp_DataInt16:
            if(fprint) printData_I16_I32(ref p, "DataI16", 0);
            if (fsave) writeData_I16_I32(ref fSamples, ref p, 0, 0, 0);
            break;
		
  		    case DataIo.Const.kMsgApp_DataDevice9400:
            if(fprint) printData_Dev9400(ref p);
            if(fsave)  writeData_Dev9400(ref fSamples, ref p, 0, 0);
            break;

          case DataIo.Const.kMsgApp_DataInt32xBase:
          case DataIo.Const.kMsgApp_DataInt16xBase:
            {
              int t = type == DataIo.Const.kMsgApp_DataInt16xBase ? 0 : 1;
              if(fprint)
                printData_XN(ref p, sp, t, nc);
              if(fsave)
                writeData_XN(ref fSamples, ref p, sp, t, nc, false, p.packetNumber, p.sync);
            }
            break;
        }
      }

      if(fSvSave)
      {
        if (isDataBuffer == false)
        {
          p2 = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppSvData>(message);
          isDataBuffer = true;
        }
        p = p2.data;

        switch (type)
        {
          default: break;

          case DataIo.Const.kMsgApp_SvData16x3:
          case DataIo.Const.kMsgApp_SvData16x4:
          case DataIo.Const.kMsgApp_SvData16x6:
          case DataIo.Const.kMsgApp_SvData16x8:
            writeData_XN(ref fSvSamples, ref p, sp, 0, nc, true, p.packetNumber, p.sync);
            break;

          case DataIo.Const.kMsgApp_SvData32x3:
          case DataIo.Const.kMsgApp_SvData32x4:
          case DataIo.Const.kMsgApp_SvData32x6:
          case DataIo.Const.kMsgApp_SvData32x8:
            writeData_XN(ref fSvSamples, ref p, sp, 1, nc, true);
            break;

          case DataIo.Const.kMsgApp_SvData32:
            writeData_I16_I32(ref fSvSamples, ref p, 1, 1, p2.page);
            break;

          case DataIo.Const.kMsgApp_SvData16:
            writeData_I16_I32(ref fSvSamples, ref p, 0, 1, p2.page);
            break;

          case DataIo.Const.kMsgApp_SvData9400:
            writeData_Dev9400(ref fSvSamples, ref p, 1, p2.page);
            break;
        }
      }
    }
    #endregion PrintMessages

    //----------------------------------------------------------------------------------------------
    #region PrintData
    //
    // size - число БАЙТ
    //
    static void printDataHex(byte[] buffer, int size)
    {
      string s = String.Format("n:{0:d2} : ", size);

      byte[] a = new byte[size];
      Array.Copy(buffer, a, size);

      s += string.Concat(a.Select(b => (b.ToString("X2") + " ")));
      LogOut(s + "\r\n", flags.DataBlocksInfo);
    }

    // 
    static void printData_Dev9400(ref DataIo.Types.TDataBuffer p)
    {
      const double INCL16209_SCALE = (((double)90)/(double)14400);	// нормирующий коэффициент для инклинометра ADIS16209

      if(flags.MeanData)
      {
	      Int32 sum=0, sx=0, sy=0;

	      int i, index=0;
		    for(i=0; i<p.dataSize; i++, index+=8)
        {
          sum += BitConverter.ToInt32(p.data, index);
          sx  += BitConverter.ToInt16(p.data, index+4);
          sy  += BitConverter.ToInt16(p.data, index+6);
        }
        if(i > 0)
        {
          string s = String.Format(" Data_x81; n:{0}; {1}; {2}; {3:0.000}; {4:0.000}\r\n",
            i, sum/i, sx/i, sy/i, sx*INCL16209_SCALE/i, sy*INCL16209_SCALE/i);
          LogOut(s);
        }
	    }

      if(flags.Samples) 
      {
		    for(int i=0, index=0; i<p.dataSize; i++, index+=8)
        {
          Int16 x, y;   // код инклинометра
          x = BitConverter.ToInt16(p.data, index+4);
          y = BitConverter.ToInt16(p.data, index+6);

          string s = String.Format(" {0}; {1}; {2}; {3:0.000}; {4:0.000}\r\n", 
            BitConverter.ToInt32(p.data, index), x, y, x*INCL16209_SCALE, y*INCL16209_SCALE);
          LogOut(s);
        }
      }

      if(statControl.active())
      {
        Int32[] d = new Int32[3];
		    for(int i=0, index=0; i<p.dataSize; i++, index+=8)
        {
          d[0] = BitConverter.ToInt32(p.data, index);
          d[1] = (Int32)BitConverter.ToInt16(p.data, index+4);
          d[2] = (Int32)BitConverter.ToInt16(p.data, index+6);
          statControl.addData(0, d);
        }
      }

      if(flags.HexData) printDataHex(p.data, p.dataSize*4);
    }

    // 
    // Вывод данных I16 и I32. type: 0/1 - I16/I32
    //
    static void printData_I16_I32(ref DataIo.Types.TDataBuffer p, string typeInfo, int type)
    {
      Int32[] d = new Int32[p.dataSize];

	    if(type == 0)
        for(int i=0; i<p.dataSize; i++) d[i] = (Int32)BitConverter.ToInt16(p.data, i*2);
      else
        for(int i=0; i<p.dataSize; i++) d[i] = BitConverter.ToInt32(p.data, i*4);

      if(flags.MeanData)
      {
	      Int32 sum=0, i;

        for(i=0; i<p.dataSize; i++) sum += d[i];

        if(i > 0) sum /= i;
        string s = String.Format(" {0} n:{1:d2} mean:{2}\r\n", typeInfo, i, sum);
        LogOut(s, flags.DataBlocksInfo);
	    }

      if(flags.Samples)
		    for(int i=0; i<p.dataSize; i++)
        {
          string s = String.Format("; {0}\r\n", d[i]);
          LogOut(s, flags.DataBlocksInfo);
        }

      // для DataI16/I32 в статистику выводится массив отсчетов для одного канала
      if(statControl.active())
        statControl.addData(0, 0, d, 0);

      if(flags.HexData) printDataHex(p.data, p.dataSize * (type == 0 ? 2 : 4));
    }

    // 
    // Вывод блоков данных IntNNx3 IntNNx4. 
    // Внимание: для блоков типа IntNNx3 IntNNx4 в dataSize - число многоканальных отсчетов.
    // type: 0/1 - I16/I32
    // nc:   {3, 4, 6}
    //
    static void printData_X3_X4_X6(ref DataIo.Types.TDataBuffer p, string typeInfo, int type, int nc)
    {
      string s;
      int smplSize = type == 0 ? 2 : 4;    // размер одного отсчета в байтах

      if(flags.MeanData)
      {
	      Int32[] sum = {0, 0, 0, 0, 0, 0};
	      int i, index;
        for(i=0, index=0; i<p.dataSize; i++)
          for(int j=0; j<nc; j++)
          {
            sum[j] += type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index);
            index += smplSize;
          }

        if(i > 0)
        {
          if(nc == 3)
            s = String.Format(" {0} n:{1:d2} mean:{2} {3} {4}\r\n", typeInfo, i, sum[0]/i, sum[1]/i, sum[2]/i);
          else if (nc == 4)
            s = String.Format(" {0} n:{1:d2} mean:{2} {3} {4} {5}\r\n", typeInfo, i, sum[0]/i, sum[1]/i, sum[2]/i, sum[3]/i);
//          else if (nc == 6)
          else
            s = String.Format(" {0} n:{1:d2} mean:{2} {3} {4} {5} {6} {7}\r\n", typeInfo, i,
                              sum[0]/i, sum[1]/i, sum[2]/i, sum[3]/i, sum[4]/i, sum[5]/i);
          LogOut(s, flags.DataBlocksInfo);
        }
	    }

      if(flags.Samples)
		    for(int i=0, index=0; i<p.dataSize; i++)
        {
  	      Int32[] v = {0, 0, 0, 0, 0, 0};
          for(int j=0; j<nc; j++)
          {
            v[j] = type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index);
            index += smplSize;
          }

          if(nc == 3)
            s = String.Format(" {0}; {1}; {2}\r\n", v[0], v[1], v[2]);
          else if (nc == 4)
            s = String.Format(" {0}; {1}; {2}; {3}\r\n", v[0], v[1], v[2], v[3]);
          //          else if (nc == 6)
          else
            s = String.Format(" {0}; {1}; {2}; {3}; {4}; {5}\r\n", v[0], v[1], v[2], v[3], v[4], v[5]);
          LogOut(s, flags.DataBlocksInfo);
        }

      if(flags.HexData) printDataHex(p.data, p.dataSize * nc * (type == 0 ? 2 : 4));
    }


    // 
    // Многоканальные блоки данных
    // Внимание: для блоков типа IntNNx3 IntNNx4 в dataSize - число многоканальных отсчетов,
    // для блоков типа MsgApp_DataIntNNxBase+ в dataSize - общее число отсчетов типа Int16 или Int32
    // type: 0/1 - I16/I32
    // nc:   число каналов
    //
    static void printData_XN(ref DataIo.Types.TDataBuffer p, string typeInfo, int type, int nc)
    {
      if(p.dataSize == 0) return;

      if(nc < 1  ||  nc > DataIo.Types.MAX_NCHANNELS)
      {
        throw(new Exception("printData_XN: некорректное число каналов n:" + nc.ToString()));
      }

      string s;
      int m = type == 0 ? 2 : 4;

/*      if(flags.MeanData)
      {
	      Int32[] sum = new Int32[nc];

        for(int i0=0; i0<nc; i0++) sum[i0] = 0;

	      int i, index;
        for(i=0, index=0; i<p.dataSize; i+=nc)
          for(int j=0; j<nc; j++)
          {
            sum[j] += type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index);
            index += m;
          }

        if(i > 0)
        {
          s = String.Format(" {0} n:{1:d2} mean:", typeInfo, i);
          for(int j=0; j<nc; j++)
          {
            string s1 = String.Format("{0} ", sum[j]/i);
            s += s1;
            LogOut(s + "\r\n");
          }
        }
	    }

      if(flags.Samples)
		    for(int i=0, index=0; i<p.dataSize; i+=nc)
        {
          s = "";
          for(int j=0; j<nc; j++)
          {
            s += String.Format("{0}; ", type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index));
            index += m;
          }
          LogOut(s + "\r\n");
        } */

      int npoints = p.dataSize/nc;    // число отсчетов по одному каналу

      // копирование данных в упорядоченный массив
      Int32[][] d = new Int32[npoints][];

	    for(int i=0; i<npoints; i++)   d[i] = new Int32[nc];

	    for(int i=0, index=0; i<npoints; i++)
        if(type == 0)
          for(int j=0; j<nc; j++, index+=m)   d[i][j] = (Int32)BitConverter.ToInt16(p.data, index);
        else
          for(int j=0; j<nc; j++, index+=m)   d[i][j] = BitConverter.ToInt32(p.data, index);

      if(flags.MeanData)
      {
        s = String.Format(" {0} n:{1:d2} mean:", typeInfo, npoints);
        for(int j=0; j<nc; j++)
        {
          Int32 sum=0;
          for(int i=0; i<npoints; i++)   sum += d[i][j];

          string s1 = String.Format("{0} ", sum/npoints);
          s += s1;
        }
        LogOut(s + "\r\n", flags.DataBlocksInfo);
      }

      if (flags.Samples)
		    for(int i=0; i<npoints; i++)
        {
          s = "";
          for(int j=0; j<nc; j++)   s += String.Format("{0}; ", d[i][j]);
          LogOut(s + "\r\n", flags.DataBlocksInfo);
        }

      if(statControl.active())
		    for(int i=0; i<npoints; i++) 
          statControl.addData(0, d[i]);

      if(flags.HexData)
        printDataHex(p.data, p.dataSize * (type == 0 ? 2 : 4));
    }
    #endregion PrintData

    //---------------------------------------------------------------------------------------------
    #region SaveData

    //
    // Summary:
    //    Запись в файл отсчетов данных из DataBuffer
    //
    // Parameters:
    //    f:
    //      файл для записи
    //    p:
    //      dataBuffer
    //    type:
    //      тип отсчетов данных: 0/1 - Int16/Int32
    //    nc:
    //      Число каналов - 3/4/6
    //
    // Returns:
    //     Nothing.
    //
    static void writeData_X3_X4_X6(ref TSaveToFileInfo f, ref DataIo.Types.TDataBuffer p, int type, int nc)
    {
      string s;
      int smplSize = type == 0 ? 2 : 4;    // размер одного отсчета в байтах

      for (int i = 0, index = 0; i < p.dataSize; i++)
      {
        Int32[] v = { 0, 0, 0, 0, 0, 0 };
        for (int j = 0; j < nc; j++)
        {
          v[j] = type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index);
          index += smplSize;
        }

        if (nc == 3)
          s = String.Format("{0};{1};{2};{3};{4}\r\n", p.packetNumber, p.sync, v[0], v[1], v[2]);
        else if (nc == 4)
          s = String.Format("{0};{1};{2};{3};{4};{5}\r\n", p.packetNumber, p.sync, v[0], v[1], v[2], v[3]);
        //          else if (nc == 6)
        else
          s = String.Format("{0};{1};{2};{3};{4};{5};{6};{7}\r\n", p.packetNumber, p.sync, v[0], v[1], v[2], v[3], v[4], v[5]);

        f.write(s);
      }
    }


    //
    // Summary:
    //    Запись в файл отсчетов данных из DataBuffer
    //
    // Parameters:
    //    f:
    //      файл для записи
    //    p:
    //      dataBuffer
    //    type:
    //      тип отсчетов данных: 0/1 - Int16/Int32
    //    fmt:
    //      =1 - вначале выводить значение page - для данных из УХД
    //
    // Returns:
    //     Nothing.
    //
    static void writeData_I16_I32(ref TSaveToFileInfo f, ref DataIo.Types.TDataBuffer p, int type, int fmt, ulong page)
    {
/*	    for(int i=0; i<p.dataSize; i++)
      {
        string s = String.Format("{0};{1};{2}\r\n", p.packetNumber, p.sync, type == 0 ? BitConverter.ToInt16(p.data, i*2) : BitConverter.ToInt32(p.data, i*4));
        fSamples.write(s);
      } */
	    
      string s, s1;
      
      for(int i=0; i<p.dataSize; i++)
      {
        s = String.Format("{0};{1};{2}\r\n", p.packetNumber, p.sync, type == 0 ? BitConverter.ToInt16(p.data, i * 2) : BitConverter.ToInt32(p.data, i * 4));
        
        if (fmt == 0)
          f.write(s);
        else
        {
          s1 = String.Format("{0};", page);
          f.write(s1 + s);
        }
      }
    }

    //
    // Запись в файл отсчетов данных из DataBuffer
    // f      файл для записи
    // p      DataBuffer
    // fmt    =1 - вначале выводить значение page - для данных из УХД
    //
    static void writeData_Dev9400(ref TSaveToFileInfo f, ref DataIo.Types.TDataBuffer p, int fmt, ulong page)
    {
      const double INCL16209_SCALE = (((double)90)/(double)14400);	// нормирующий коэффициент для инклинометра ADIS16209
      string s, s1;

	    for(int i=0, index=0; i<p.dataSize; i++, index+=8)
      {
        Int16 x, y;   // код инклинометра
        x = BitConverter.ToInt16(p.data, index+4);
        y = BitConverter.ToInt16(p.data, index+6);

        s = String.Format("{0};{1};{2};{3};{4};{5};{6}\r\n",
          p.packetNumber, p.sync,
          BitConverter.ToInt32(p.data, index), x, y, x * INCL16209_SCALE, y * INCL16209_SCALE);

        if (fmt == 0)
          f.write(s);
        else
        {
          s1 = String.Format("{0};", page);
          f.write(s1 + s);
        }
      }
    }

    // 
    // Многоканальные блоки данных
    // Внимание: для блоков типа IntNNx3 IntNNx4 в dataSize - число многоканальных отсчетов,
    // для блоков типа MsgApp_DataIntNNxBase+ в dataSize - общее число отсчетов типа Int16 или Int32
    // type           0/1 - I16/I32
    // nc             число каналов
    // packetNumber   номер блока данных
    // sync           timestamp блока данных
    //
/*    static void writeData_XN(ref TSaveToFileInfo f, ref DataIo.Types.TDataBuffer p, string typeInfo, int type, int nc,
                             UInt32 packetNumber=0, UInt32 sync=0)
    {
      if(nc < 1  ||  nc > DataIo.Types.MAX_NCHANNELS)
      {
        throw(new Exception("writeData_XN: некорректное число каналов n:" + nc.ToString()));
      }

      string s;
      int m = type == 0 ? 2 : 4;

      for(int i=0, index=0; i<p.dataSize; i++)
      {
        s = "";

        if (packetNumber > 0)
          s += String.Format("{0};{1};", packetNumber, sync);

        for(int j=0; j<nc; j++)
        {
          s += String.Format("{0};", type == 0 ? BitConverter.ToInt16(p.data, index) : BitConverter.ToInt32(p.data, index));
          index += m;
        }
        f.write(s + "\r\n");
      }
    } */

    // 
    // Многоканальные блоки данных
    // Внимание: для блоков типа IntNNx3 IntNNx4 в dataSize - число многоканальных отсчетов,
    // для блоков типа MsgApp_DataIntNNxBase+ в dataSize - общее число отсчетов типа Int16 или Int32
    // type           0/1 - I16/I32
    // nc             число каналов
    // multySamples   true  - p.dataSize содержит число многоканальных отсчетов
    // multySamples   false - p.dataSize содержит общее число отсчетов типа Int16 или Int32.
    // packetNumber   номер блока данных
    // sync           timestamp блока данных
    //
    static void writeData_XN(ref TSaveToFileInfo f, ref DataIo.Types.TDataBuffer p, string typeInfo, int type, int nc,
                             bool multySamples,
                             UInt32 packetNumber = 0, UInt32 sync = 0)
    {
      if (nc < 1 || nc > DataIo.Types.MAX_NCHANNELS)
      {
        throw (new Exception("writeData_XN: некорректное число каналов n:" + nc.ToString()));
      }

      string s;
      int m = type == 0 ? 2 : 4;

      // Число N-канальных отсчетов в блоке.
      // Для фиксированных типов блоков данных (IntNNx2 .. IntNNx8 etc) p.dataSize - число многоканальных отсчетов.
      // Для нормализованных типов блоков данных (Int16xBase Int32xBase) p.dataSize - общее число отсчетов типа Int16 или Int32.
      int npoints = p.dataSize;

      if(multySamples == false)
        npoints /= nc;

      // копирование данных в упорядоченный массив
      Int32[][] d = new Int32[npoints][];

      for (int i = 0; i < npoints; i++) d[i] = new Int32[nc];

      if (type == 0)
      {
        for (int i = 0, index = 0; i < npoints; i++)
          for (int j = 0; j < nc; j++, index += m) 
            d[i][j] = (Int32)BitConverter.ToInt16(p.data, index);
      }
      else
      {
        for (int i = 0, index = 0; i < npoints; i++)
          for (int j = 0; j < nc; j++, index += m) 
            d[i][j] = BitConverter.ToInt32(p.data, index);
      }

      for (int i = 0; i < npoints; i++)
      {
        if (packetNumber > 0)
          s = String.Format("{0};{1};", packetNumber, sync);
        else
          s = "";

        for (int j = 0; j < nc; j++)
          s += String.Format("{0}; ", d[i][j]);

        f.write(s + "\r\n");
      }
    }

    #endregion SaveData

  }   // static class PrintMessage
}     // namespace ISS
