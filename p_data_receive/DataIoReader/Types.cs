using System;

namespace DataIoReader
{
  public class Types
  {
    // Вывод текстового сообщения 
    public delegate void TPutToLog(string message);

    // Приём блока данных
    public delegate void ReceiveDataBlock(ref DataBlock data);

    // Приём значения параметра
    public delegate void ReceiveParameterValue(int parameterNumber, int ParameterValue, int index);
  }


  // 
  // Данные измерения и дополнительная информация для идентификации источника данных
  //
  public class DataBlock
  {
    // Диапазоны значений номеров пакетов для АЦП1 и АЦП2
    public const ushort kMinPnAdc1  = 1;
    public const ushort kMaxPnAdc1  = 1000;
    public const ushort kMinPnAdc2  = 1001;
    public const ushort kMaxPnAdc2  = 2000;

    public Int32[]  data;               // отсчёты данных
    public ushort   packet;             // номер пакета
    public ulong    timestamp;          // метка времени для первого отсчёта
    public int      index,              // индекс устройства, от которого получены данные - соответствует индексу в массиве конфигурации UDP
                    srcAddress,         // сетевой адрес RS485 блока измерения
                    numAdc,             // номер АЦП - вычисляется по номеру пакета в блоке данных
                    numCh;              // число каналов данных

    // db         блок данных
    // nc         число каналов измерения
    // aindex     индекс устройства - источника данных
    // sa         сетевой адрес RS485 блока измерения
    public DataBlock(ref DataIo.Types.TDataBuffer db, int nc, int aindex, int sa)
    {
      packet = db.packetNumber;
      timestamp = db.sync;
      numCh = nc;
      index = aindex;
      srcAddress = sa;

      // hdr.dataSize содержит число многоканальных отсчётов
      data = new int[db.dataSize * nc];

      for(int i=0; i < data.Length; i++)
        data[i] = BitConverter.ToInt32(db.data, i * 4);

      if(packet >= kMinPnAdc1  &&  packet <= kMaxPnAdc1)
        numAdc = 1;
      else if(packet >= kMinPnAdc2  &&  packet <= kMaxPnAdc2)
        numAdc = 2;
      else
        numAdc = 0;
    }
  }


  //
  // Конфигурация UDP
  //
  [Serializable]
  public class UdpConfig
  {
    public int localPort,
                remotePort;
    public string remoteAddress;
    public bool txBroadcast;

    public UdpConfig()
    {
      localPort = 4023;
      remotePort = 3000;
      remoteAddress = "192.168.0.100";
      txBroadcast = false;
    }

    public UdpConfig(int alocalPort, int aremotePort, string aremoteAddress, bool atxBroadcast)
    {
      localPort = alocalPort;
      remotePort = aremotePort;
      remoteAddress = aremoteAddress;
      txBroadcast = atxBroadcast;
    }

    public void copy(UdpConfig c)
    {
      if (c != null)
      {
        localPort = c.localPort;
        remotePort = c.remotePort;
        remoteAddress = c.remoteAddress;
        txBroadcast = c.txBroadcast;
      }
    }
  }
}
