using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using Win32;


namespace App.DataIoReader
{ 
  /*
  Приём данных от устройства (блок измерения или концентратор связи)

  - Передача и приём данных по UDP
  - Периодическое формирование запросов к устройству
  - Приём данных и передача на разбор
  - Интерфейс для операций чтения и записи регистров устройства  
  */
  public class DevicePoll
  {
    #region Const
    // Лимиты передачи и приёма данных по UDP за одну операцию
    protected const int kTxSizeLimit = 600;
    protected const int kRxSizeLimit = 1400;

    // Длительность нахождения в Sleep для потоков приёма/передачи, мс
    protected const int kThreadTxDelay = 50;
    protected const int kThreadRxDelay = 50;
    #endregion

    #region Data
    public delegate void TPutToLog(string s);

    private TPutToLog putToLog;             // вывод отладочных сообщений в лог

    private DataIoAppTx dataIoTx;           // нижний уровень протокола
    private DataIoAppRx dataIoRx;

    private TMSTimer timer;                 // отсчёт интервалов

    private Thread threadTx;                // UDP transmit
    private Thread threadRx;                // UDP receive

    private ulong tmSendRequest;            // отсчёт интервалов передачи запросов к устройству

    private bool run,                       // разрешение работы потока передачи/приёма; при установке -> false поток завершает работу
                 enableDataRq,              // разрешение формирования запросов данных
                 noData;                    // отсутствие данных в устройстве

    private volatile UdpConfig udpConfig;   // настройки UDP

    private UdpClient udpTx, udpRx;         // UDP transmit + recieve
    #endregion

    //-------------------------------------------------------------------------
    #region Public
    // 
    // atimer         ссылка на таймер для отсчёта интервалов с разрешением 1 мс
    // aUdpConfig     настройки UDP: remote ip, remote port, local port
    public DevicePoll(ref TMSTimer atimer, UdpConfig aUdpConfig, TPutToLog aPutToLog)
    {
      dataIoTx = new DataIoAppTx();
      dataIoRx = new DataIoAppRx(rxMessageHandler);

      timer = atimer;

      udpConfig = aUdpConfig;
      putToLog = aPutToLog;

      tmSendRequest = 0;

      run = false;
      enableDataRq = false;
      noData = false;
    }

    // Возвращает true, если запущен обмен с устройством
    public bool active() { return run; }

    // Пуск обмена
    public void start()
    {
      //-------- UDP i/o --------
      udpTx = new UdpClient();

      if(udpConfig.txBroadcast)
        udpTx.Connect(IPAddress.Broadcast, udpConfig.remotePort);
      else
        udpTx.Connect(udpConfig.remoteAddress, udpConfig.remotePort);


      udpRx = new UdpClient(new IPEndPoint(IPAddress.Any, udpConfig.localPort));

      //-------- Threads --------
      threadTx = new Thread(this.threadTxRun);
      threadTx.IsBackground = true;

      threadRx = new Thread(this.threadRxRun);
      threadRx.IsBackground = true;

      run = true;
      threadTx.Start();
      threadRx.Start();
    }

    // Останов обмена
    public void stop()
    {
      if(run)
      {
        run = false;
        stopThread(ref threadTx, 10, 20);
        stopThread(ref threadRx, 10, 20);
      }

      if(udpRx != null) { udpRx.Close(); udpRx = null; }
      if(udpTx != null) { udpTx.Close(); udpTx = null; }
    }


    // Разрешить/запретить отправку запросов данных
    public void enableDataPoll()  { enableDataRq = true; }
    public void disableDataPoll() { enableDataRq = false; }

    // Уведомление о получении сообщения DataAbsense
    public void notifyNoData()  { noData = true; }

    // Уведомление о получении блока данных
    public void notifyData()    { noData = false; }
    #endregion

    //-------------------------------------------------------------------------
    #region Private
    // Обработчик сообщений из принятого пакета
    // Вызывается из dataIoRx.parse()
    private void rxMessageHandler(ref byte[] msg, ushort destAddress, ushort srcAddress, uint index)
    {
      putToLog(String.Format(" rx message id:0x{0:X} sz:{1}", msg[0], msg.Length));
    }

    
    //
    private void threadTxRun()
    {
      while (run)
      {
        // Периодическая отправка запросов на получение данных
        if(enableDataRq  &&  timer.getms() >= tmSendRequest + 1000)
        {
          tmSendRequest = timer.getms();

          dataIoTx.startPacket();
          dataIoTx.addMessageAppType(DataIo.Const.kMsgApp_RequestData);

          byte[] txdata = null;
          int length = Convert.ToInt32(dataIoTx.getPacketRef(ref txdata));
          udpTx.Send(txdata, length);
        }

        // Передача внешних запросов из очереди исходящих сообщений
        /*
        int txsz = 0, cnt = 0;
        byte[] txbuf = null;
        do
        {
          lock(txQueue)
          {
            cnt = txQueue.Count;
            if (cnt > 0)
              txbuf = txQueue.Peek();
          }

          if(cnt > 0)
          {
            //PrintMessage.LogOut(" UDP_Tx:Send\r\n");

            int size = udpTx.Send(txbuf, txbuf.Length);
            if (size == 0)
              break;
            else
            {
              lock(txQueue)
                txQueue.Dequeue();

              txsz += size;
//              PrintMessage.PrintLowIO(true, ref txbuf);
            }
          }
        } while(txQueue.Count > 0  &&  txsz < kTxSizeLimit);
        */
        Thread.Sleep(kThreadTxDelay);
      }
    }

    //
    private void threadRxRun()
    {
      while(run)
      {
        IPEndPoint ip = null;
        byte[] rxdata = udpRx.Receive(ref ip);
        if(rxdata.Length > 0)
        {
          /*
          lock(rxQueue)
          {
            rxQueue.Enqueue(data);
          }
          PrintMessage.PrintLowIO(false, ref data);
          */
          dataIoRx.parse(ref rxdata, rxdata.Length);

putToLog(String.Format(" rx [0]:0x{0:X} sz:{1}", rxdata[0], rxdata.Length));
        }
        Thread.Sleep(kThreadRxDelay);
      }
    }

    
    //
    private void stopThread(ref Thread th, int cntLim, int delay)
    {
      if(th != null)
      {
        int cnt = 0;
        while(th.IsAlive && cnt < cntLim)
        {
          cnt++;
          Thread.Sleep(delay);
        }
        if(th.IsAlive) th.Abort();
      }
    }
    #endregion
  }
}
