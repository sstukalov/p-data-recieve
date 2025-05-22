#define LOG_MESSAGES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using Win32;
using DataIo;

namespace DataIoReader
{
  // Контейнер объектов для организации обмена с устройствами
  //
  // Содержит массив объектов DeviceTxConnection. Один объект выполняет передачу данных для одного устройства.
  // Также формирует запросы данных и отправляет подтверждение приёма.
  //
  // Приём данных выполняется на один локальный порт одним объектом UDP Client. 
  // На разбор передаются данные пакета UDP и IP Endpoint, содержащая информацию об отправителе. 
  // По адресу в IP Endpoint вычисляется индекс объекта DeviceTxConnection.
  // 
  // TODO
  // 1) Принятые данные заносить в очередь и вызывать DataIoRx.parse() в отдельном потоке.
  //    Альтернатива - после приёма запускать асинхронно Task с разбором данных.
  //
  // 2) Для трансляции IP Endpoint в индекс в udpConfig используется операция сравнения строк.
  //    Для udpConfig строить хэш таблицу, по входящей IP Endpoint вычислять хэш.
  public class Reader
  {
    #region Const
    protected const int kRxSizeLimit = 1400;      // Лимит приёма данных по UDP за одну операцию
    protected const int kThreadRxDelay = 0;       // Длительность нахождения в Sleep для потока приёма, мс
    #endregion

    #region Data
    // Внешние обработчики
    private Types.ReceiveDataBlock receiveDataBlock;                  // приём блока данных
    private Types.ReceiveParameterValue receiveParameterValue;        // приём значения параметра
    private Types.TPutToLog putToLog;                                 // вывод сообщения в лог

    private TMSTimer timer;                       // отсчёт интервалов времени

    private DataIoAppRx dataIoRx;                 // разбор входящих пакетов

    private DeviceTxConnection[] deviceTxConn;    // передача в устройства

    private UdpConfig[] udpConfig;                // настройки UDP удаленных устройств

    private UdpClient udpRx;                      // UDP recieve

    private Thread threadRx;                      // обработка приёма по UDP

    private bool run;                             // разрешение работы потока передачи/приёма
                                                  // при установке -> false поток завершает работу
    private int localPort;                        // локальный UDP порт для входящих данных
    #endregion

    #region Public

    // atimer                   таймер для отсчёта интервалов с разрешением 1 мс
    // aPutToLog                вывод сообщений в лог
    // areceiveDataBlock        обработчик принятых блоков данных
    // areceiveParameterValue   обработчик принятого значения параметра
    public Reader(ref TMSTimer atimer, Types.TPutToLog aPutToLog,
      Types.ReceiveDataBlock areceiveDataBlock, Types.ReceiveParameterValue areceiveParameterValue)
    {
      timer = atimer;
      putToLog = aPutToLog;
      receiveDataBlock = areceiveDataBlock;
      receiveParameterValue = areceiveParameterValue;

      deviceTxConn = null;
      udpConfig = null;
      dataIoRx = new DataIoAppRx(rxMessageHandler);
      run = false;
    }


    // Инициализация объекта по настройкам сетевых адресов
    // Создаёт массив объектов исходящих подключений в соответствии с udpCondfig
    //
    // udpConfig      массив сетевых настроек удалённых устройств - для каждого создаётся
    //                объект DeviceTxConnection
    // alocalPort     локальный UDP порт для входящих сообщений
    public void setup(ref UdpConfig[] audpConfig, int alocalPort)
    {
      udpConfig = audpConfig;
      localPort = alocalPort;

      deviceTxConn = new DeviceTxConnection[udpConfig.Length];

      int j = 0;
      for(int i = 0; i < deviceTxConn.Length; i++)
        if(udpConfig[i] != null)
        {
          deviceTxConn[i] = new DeviceTxConnection(ref timer, udpConfig[i], putToLog);
          j++;
        }
        else
          deviceTxConn[i] = null;

#if LOG_MESSAGES
      putToLog(String.Format(" DataReader.setup()  deviceTxConn size : {0}", j));
#endif
    }


    // Возвращает true, если запущен обмен с устройством
    public bool active() 
    { 
      return run; 
    }

  
    // Разрешить/запретить отправку запросов данных
    public void controlDataPoll(bool st)  
    { 
      foreach(var c in deviceTxConn)
        if(c != null)
          c.controlDataPoll(st);
    }

    
    // Пуск обмена
    public void start()
    {
      udpRx = new UdpClient(new IPEndPoint(IPAddress.Any, localPort));

      // Rx Thread
      threadRx = new Thread(this.threadRxRun);
      threadRx.IsBackground = true;

      run = true;
      threadRx.Start();

#if LOG_MESSAGES
      putToLog(String.Format(" start UDP receive   port:{0}", localPort));
#endif

      // Tx Connections
      foreach(var c in deviceTxConn)
        if(c != null)
        {
          c.start();
  //        c.controlDataPoll(true);
        }
    }


    // Останов обмена
    public void stop()
    {
      foreach(var c in deviceTxConn)
        if(c != null)
          c.stop();

      if(run)
      {
        run = false;
        Utils.stopThread(ref threadRx, 10, 20);
      }

      if(udpRx != null)
      {
        udpRx.Close();
        udpRx = null;
      }
    }
    #endregion

    //---------------------------------------------------------------------------
    #region Private
    // Функция потока приёма данных
    private void threadRxRun()
    {
      while(run)
      {
        IPEndPoint ip = null;
        byte[] rxdata = udpRx.Receive(ref ip);

        if(rxdata.Length > 0)
        {
  //        putToLog(String.Format(" rx    sz:{0} [{1}]", rxdata.Length, ip.ToString()));

          /*
          lock(rxQueue)
          {
            rxQueue.Enqueue(data);
          }
          PrintMessage.PrintLowIO(false, ref data);
          */
          dataIoRx.parse(ref rxdata, rxdata.Length, ip);

          // putToLog(String.Format(" rx [0]:0x{0:X} sz:{1}", rxdata[0], rxdata.Length));
        }
        Thread.Sleep(kThreadRxDelay);
      }
    }


    // Трансляция ip адреса в индекс в массиве udpConfig[] (соответствует индексу в deviceTxConn[])
    //
    // return     >= 0    индекс в массиве udpConfig[], соответствующий ip
    //              -1    ip не найден в udpConfig[]
    private int endpointToIndex(ref IPEndPoint ip)
    {
      if(udpConfig != null)
        for(int i = 0; i < udpConfig.Length; i++)
          if(udpConfig[i] != null)
            if(String.Compare(ip.Address.ToString(), udpConfig[i].remoteAddress) == 0)
              return i;

      return -1;
    }


    // Обработчик сообщений из принятого пакета
    // 
    // Вызывается из dataIoRx.parse(). Принимает байтовый массив, содержащий сообщения,
    // выделяет сообщения отдельных типов и вызывает соответствующие обработчики.
    // Принимает IP Endpoint как идентификатор источника данных, в прикладные обработчики передаёт индекс в udpConfig[].
    //
    // msg            массив, содержащий сообщение
    // destAddress    destination address
    // srcAddress     source address
    // index          начальная позиция сообщения в msg
    // ep             IP Endpoint отправителя
    //
    // destAddress и sourceAddress явяляются артефактами протокола и при обмене по UDP не используются.
    private void rxMessageHandler(ref byte[] msg, ushort destAddress, ushort srcAddress, uint index, ref IPEndPoint ep)
    {
      int devIndex = endpointToIndex(ref ep);

      if(devIndex != -1)
        switch(msg[0])
        {
          default:
#if LOG_MESSAGES
            putToLog(String.Format(" msg ?     id:0x{0:X} sz:{1} [{2}] i:{3}", msg[0], msg.Length, ep.ToString(), devIndex));
#endif
            break;

          case DataIo.Const.kMsgParameterWrite:
            //  handleMsgParameterWrite((DataIo.Types.ByteToStruct<DataIo.Types.tagMsgParameterWrite>(msg, index)), destAddress, srcAddress, outMsg);
            break;

          case DataIo.Const.kMsgParameterRead:
            //  handleMsgParameterRead((DataIo.Types.ByteToStruct<DataIo.Types.tagMsgParameterRead>(msg, index)), destAddress, srcAddress, outMsg);
            break;

          case DataIo.Const.kMsgReadBlock:
            //  handleMsgParameterReadBlock((DataIo.Types.ByteToStruct<DataIo.Types.tagMsgReadBlock>(msg, index)), destAddress, srcAddress, outMsg);
            break;

          case DataIo.Const.kMsgParameterValue:
            handleMsgParameterValue(DataIo.Types.ByteToStruct<DataIo.Types.MsgParameterValue>(msg), devIndex);
            break;

          case DataIo.Const.kMsgAppType:
            handleMsgAppType(msg, devIndex);
            break;
        }
      else
      {
#if LOG_MESSAGES
        putToLog(String.Format(" rx endpoint error    msg:0x{0:X} [{1}]", msg[0], ep.ToString()));
#endif
      }
    }


    // Обработчик входящих сообщений MsgParameterValue
    //
    // msg        сообщение DataIO MsgParameterValue
    // index      индекс в udpConfig[] (deviceTxConn[])
    private void handleMsgParameterValue(DataIo.Types.MsgParameterValue msg, int index)
    {
      receiveParameterValue(msg.parameterNumber, msg.parameterNumber, index);
#if LOG_MESSAGES
      putToLog(String.Format(" MsgPValue    pn:0x{0} pv:{1} i:{2}", msg.parameterNumber, msg.parameterValue, index));
#endif
    }


    // Обработчик входящих сообщений MsgAppType
    //
    // msg        массив, содержащий сообщение DataIO MsgAppType
    // index      индекс в udpConfig[] (deviceTxConn[])
    private void handleMsgAppType(byte[] msg, int index)
    {
//      DataIo.Types.TDataBufferHeader    dataBufferHeader;
      DataIo.Types.MsgAppTypeHeader     msgHeader;

      byte[] buffer;

      //-----------------------------------------------------
      // Разбор полей в msg.type
      int msgType;
      ushort type = Convert.ToUInt16(msg[3]);
      type <<= 8;
      type |= msg[2];

      // Концентратор КС3 в типе прикладного сообшения кодирует сетевой адрес RS485 блока измерения - источника данных.
      // Адрес находится в разрядах b14..b8 типа сообщения, при этом b15 устанавливается в 1. 

      ushort srcAddress = 0;      // сетевой адрес блока измерения

      // Обработка кодирования сетевого адреса устройства в типе сообщения - концентратор КС3 (2025)
      if((type & 0x8000) != 0)
      {
        msgType = type & 0x00FF;
        srcAddress = Convert.ToUInt16((type >> 8) & 0x7F);
      }
      else
        msgType = type & 0x03FF;

	    // унифицированный id для многоканальных блоков данных
	    if((type > DataIo.Const.kMsgApp_DataInt16xBase  &&  type <= DataIo.Const.kMsgApp_DataInt16xBase + DataIo.Const.MAX_NCHANNELS)  ||
	       (type > DataIo.Const.kMsgApp_DataInt32xBase  &&  type <= DataIo.Const.kMsgApp_DataInt32xBase + DataIo.Const.MAX_NCHANNELS))
	    {
        msgType = 0x8000;
      }
      //-----------------------------------------------------


      switch(msgType)
      {
        default:
#if LOG_MESSAGES
          putToLog(String.Format(" MsgData ?    type:0x{0:X} mgType:{1}  i:{2}", type, msgType, index));
#endif
          break;

  /*      case DataIo.Types.MsgApp_RegBlock:
          {
            DataIo.Types.MsgParameterBlock pb = DataIo.Types.ByteToStruct<DataIo.Types.MsgParameterBlock>(msg);

  //            s = String.Format("RegBlock       da:{0:d2} sa:{1:d2} seq:{2:d3}  base:{3} sz:{4} err:{5}\r\n",
  //              destAddress, srcAddress, msg.sequenceNumber, pb.baseAddr, pb.size, pb.errCount);

            for(int i=0; i<pb.size; i++)
            {
              TGUI_Msg m1 = new TGUI_Msg(TGUI_Msg.MsgParameterValue, pb.baseAddr + i, BitConverter.ToInt32(pb.data, i * 4), 0);
              lock (GUI_MsgQ)
              {
                GUI_MsgQ.Enqueue(m1);
              }
            }
          }
          break; */

        case 0x8000:
        case DataIo.Const.kMsgApp_DataInt32:
        case DataIo.Const.kMsgApp_DataInt32x3:
          // Заголовок сообщения MsgAppType
          buffer = new byte[Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader))];
          Array.Copy(msg, buffer, buffer.Length);
          msgHeader = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppTypeHeader>(buffer);

          // Заголовок блока данных
          /* buffer = new byte[Marshal.SizeOf(typeof(DataIo.Types.TDataBufferHeader))];
          Array.Copy(msg, Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)), buffer, 0, buffer.Length);
          dataBufferHeader = DataIo.Types.ByteToStruct<DataIo.Types.TDataBufferHeader>(buffer); */

          // Блок данных
          DataIo.Types.TDataBuffer p = new DataIo.Types.TDataBuffer();
          MsgAppTypeToDataBuffer(msg, ref p);

          // Уведомление о приёме блока данных для передачи подтверждения
          deviceTxConn[index].notifyData(p.packetNumber);

          DataBlock dataBlock = new DataBlock(ref p, 3, index, srcAddress);

          // Передать данные на прикладной уровень
          receiveDataBlock(ref dataBlock);

#if LOG_MESSAGES
          string sp;
          if(msgType == DataIo.Const.kMsgApp_DataInt32x3)     sp = "DataI32x3  ";
          else if(msgType == DataIo.Const.kMsgApp_DataInt32)  sp = "DataI32    ";
          else sp = String.Format("Data(0x{0:X})  ", type);

          putToLog(String.Format(" {5}  sq:{6} sa:{0} pn:{1} sy:{2} n:{3}  i:{4}", 
//            srcAddress, dataBufferHeader.packetNumber, dataBufferHeader.sync, dataBufferHeader.dataSize, index, sp, msgHeader.sequenceNumber));
            srcAddress, p.packetNumber, p.sync, p.dataSize, index, sp, msgHeader.sequenceNumber));
#endif
          break;

        case DataIo.Const.kMsgApp_DataAbsence:
        case DataIo.Const.kMsgApp_DataAbsence2:
          deviceTxConn[index].notifyNoData();
#if LOG_MESSAGES
          putToLog(String.Format(" MsgDataAbsense    i:{0}", index));
#endif
          break;
      }
    }
    private static void MsgAppTypeToDataBuffer(byte[] message, ref DataIo.Types.TDataBuffer p)
    {
      byte[] arr = new byte[message.Length];
      for (int i = Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)), j = 0; i < message.Length; i++, j++)
        arr[j] = message[i];
      p = DataIo.Types.ByteToStruct<DataIo.Types.TDataBuffer>(arr);
    }

    #endregion
  }
}     // namespace
