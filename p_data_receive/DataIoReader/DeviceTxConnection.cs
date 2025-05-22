#define LOG_MESSAGES    // вывод сообщений в лог

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;


using Win32;
using DataIo;


namespace DataIoReader
{
  //
  // Передача исходящих запросов в устройство - блок измерения или концентратор связи
  //
  // - Интерфейс для запросов чтения и записи регистров устройства  
  // - Периодическое формирование запросов на передачу данных
  // - Передача сообщений о подтверждении приёма данных
  // - Передача данных по UDP
  // 
  // Содержит UDP Client, для которого создается соединение с отдельным устройством с заданными IP адрес + порт.
  // 
  public class DeviceTxConnection
  {
  #region Const
  protected const int kTxDataRqInterval     = 200;          // Интервал передачи запросов данных при отсутствии данных в устройстве, мс
  protected const int kRqAnswerTimeout      = 60;           // Таймаут ожидания ответа на запрос данных до передачи следующего запроса
  protected const int kRq2RqInterval        = 2;            // Минимальный интервал между исходящими запросами данных, мс
  protected const int kTxSizeLimit          = 1200;         // Лимит передачи данных по UDP за одну операцию
  protected const int kThreadTxDelay        = 20;           // Длительность нахождения в Sleep для потока передачи, мс
  protected const int kMaxDataAckOutMsg     = 20;           // Макс. кол-во сообщений DataAck в одном пакете    
  #endregion

  #region Data
  public delegate void TPutToLog(string s);

  private Types.TPutToLog putToLog;             // вывод отладочных сообщений в лог

  private TMSTimer timer;                       // отсчёт интервалов

  private DataIoAppTx dataIoTx;                 // формирование сообщений и исходящих пакетов

  private Queue<ushort> ackPacketNumbers;       // номера пакетов для передачи подтверждения приёма данных
  private Queue<byte[]> txQueue;                // данные для передачи - сформированные пакеты

  private UdpConfig udpConfig;                  // настройки UDP
  private UdpClient udpTx;                      // передача по UDP

  private Thread threadTx;                      // передача исходящих данных по UDP

  private ulong tmSendRequest;                  // отсчёт интервалов передачи запросов к устройству

  private bool  run,                            // разрешение работы потока передачи/приёма; при установке -> false поток завершает работу
                enableDataRq,                   // разрешение формирования запросов данных
                noData,                         // отсутствие данных в устройстве
                waitAnswer;                     // ожидание ответа на запрос данных

  private uint ipb4;                            // младший октет IP адреса удаленного устройства; используется при выводе отладочных сообщений
  #endregion

  //-------------------------------------------------------------------------
  #region Public
    
  // Конструктор
  // 
  // atimer         ссылка на таймер для отсчёта интервалов с разрешением 1 мс
  // aUdpConfig     настройки UDP: remote ip, remote port, local port
  // aPutToLog      вывод сообщений в лог
  public DeviceTxConnection(ref TMSTimer atimer, UdpConfig aUdpConfig, Types.TPutToLog aPutToLog)
  {
    ipb4 = 0;

    dataIoTx = new DataIoAppTx();

    timer = atimer;

    udpConfig = aUdpConfig;
    putToLog = aPutToLog;

    tmSendRequest = 0;

    run = false;
    enableDataRq = false;

    ackPacketNumbers = new Queue<ushort>();
    txQueue = new Queue<byte[]>();
  }


  // Возвращает true, если запущен обмен с устройством
  public bool active() { return run; }

    
  // Пуск обмена
  public void start()
  {
    waitAnswer = false;
    noData = true;
    ackPacketNumbers.Clear();
    txQueue.Clear();

    //-------- UDP i/o --------
    udpTx = new UdpClient();

    if(udpConfig.txBroadcast)
    {
      udpTx.Connect(IPAddress.Broadcast, udpConfig.remotePort);
      ipb4 = 255;
    }
    else
    {
      udpTx.Connect(udpConfig.remoteAddress, udpConfig.remotePort);
      ipb4 = udpTx.Client.RemoteEndPoint.Serialize()[7];
    }
#if LOG_MESSAGES
    putToLog(String.Format(" [{2}] connect to {0}:{1}", udpConfig.remoteAddress, udpConfig.remotePort, ipb4));
#endif

    //-------- Thread --------
    threadTx = new Thread(this.threadTxRun);
    threadTx.IsBackground = true;

    run = true;
    threadTx.Start();
  }

    
  // Останов обмена
  public void stop()
  {
    if(run)
    {
      run = false;
      Utils.stopThread(ref threadTx, 10, 20);
    }

    if(udpTx != null)
    {
      udpTx.Close(); udpTx = null;
    }
  }


  // Разрешить/запретить отправку запросов данных
  public void controlDataPoll(bool st)  { enableDataRq = st; }
  
    
  // Уведомление о получении сообщения DataAbsense
  public void notifyNoData()  
  { 
    noData = true; 
    waitAnswer = false;
    }

    
  // Уведомление о получении блока данных
  // Добаялет в очередь номер пакета для передачи подтверждения приёма данных
  //
  // packetNumber       номер принятого пакета
  public void notifyData(ushort packetNumber)
  {
    noData = false;
    waitAnswer = false;

    lock(ackPacketNumbers)
    {
      ackPacketNumbers.Enqueue(packetNumber);
    }
  }
  #endregion

  #region Private
  
  // Метод потока передачи данных
  private void threadTxRun()
  {
    while (run)
    {
      int cnt = 0, ackSz;

      // Инициализация формирования исходящего пакета
      dataIoTx.startPacket();

      // Передача подтверждения приёма блоков данных
      lock(ackPacketNumbers)
      {
        while(ackPacketNumbers.Count > 0  &&  ++cnt < kMaxDataAckOutMsg)
        {
          ushort pn = ackPacketNumbers.Dequeue();
          byte[] data = { Convert.ToByte(pn & 0xFF), Convert.ToByte((pn >> 8) & 0xFF) };

          dataIoTx.addMessageAppType(DataIo.Const.kMsgApp_DataAcknowledge, ref data);

#if LOG_MESSAGES
          putToLog(String.Format(" [{1}] send DataAck  pn:{0}", pn, ipb4));
  //          putToLog(String.Format(" {2} [{1}] send DataAck  pn:{0}", pn, ipb4, Thread.CurrentThread.ManagedThreadId));
#endif
        }
        ackSz = ackPacketNumbers.Count;
      }

      ulong tm = timer.getms();

      // Периодическая отправка запросов на получение данных по условиям:
      // - периодические запросы при отсутствии данных
      // - есть данные  +  ожидание ответа  +  таймаут получения ответа
      // - есть данные  +  был ответ на предыдущий запрос  +  истёк мин. интервал между запросами
      if(enableDataRq  &&  
          ((tm >= tmSendRequest + kTxDataRqInterval)  ||  
          (!noData  &&   waitAnswer  &&  (tm >= tmSendRequest + kRqAnswerTimeout))  ||
          (!noData  &&  !waitAnswer  &&  (tm >= tmSendRequest + kRq2RqInterval))))
      {
        tmSendRequest = tm;
        dataIoTx.addMessageAppType(DataIo.Const.kMsgApp_RequestData);
        waitAnswer = true;

#if LOG_MESSAGES
        putToLog(String.Format(" [{0}] send DataRq   sq:{1}", ipb4, dataIoTx.getSeqNumber()));
#endif
      }

      // Передача сформированного пакета
      if(dataIoTx.size() > 0)
      {
        byte[] txdata = null;
        // Получить ссылку на внутренний буфер dataIoTx
        // Внимание: размер данных для передачи не соответствует размеру массива
        int length = Convert.ToInt32(dataIoTx.getPacketRef(ref txdata));
        int size = udpTx.Send(txdata, length);

  #if LOG_MESSAGES
        if(size != length)
          putToLog(String.Format(" [{0}] tx error!  size:{1}", ipb4, size));
  #endif

        /* string s = " ";
        for(int i = 0; i < length; i++)
          s += String.Format(" {0:X}", txdata[i]);
        putToLog(s); */
      }

      // Передача пакетов из очереди исходящих сообщений
      int txsz = 0;
      cnt = 0;
      do
      {
        byte[] txdata = null;

        lock(txQueue)
        {
          cnt = txQueue.Count;
          if (cnt > 0)
            txdata = txQueue.Peek();
        }

        if(cnt > 0)
        {
          // Данная реализация передает один блок из очереди в одном UDP пакете.
          // Для оптимизации передачи, сообщения должны собираться в пакет на этапе занесения блоков в очередь.
          int size = udpTx.Send(txdata, txdata.Length);
          if (size == 0)
            break;
          else
          {
            lock(txQueue)
              txQueue.Dequeue();

            txsz += size;
            cnt--;
          }
        }
      } while(txQueue.Count > 0  &&  txsz < kTxSizeLimit);

      // Вычисление длительности перевода в Sleep
      int delay;

      if(noData == false)                 // возможно наличие данных в устройстве
        delay = 0;
      else if(cnt > 0  ||  ackSz > 0)     // наличие исходящих данных для передачи
        delay = 2;
      else
        delay = kThreadTxDelay;

      Thread.Sleep(delay);
    }
  }
  #endregion
  }
}     // namespace
