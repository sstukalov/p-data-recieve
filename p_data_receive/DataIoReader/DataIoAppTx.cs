using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DataIo
{
  // Формирование сообщений и запись в выходной пакет
  public class DataIoAppTx
  {
    private const uint kMaxDataSize = DataIo.Const.MaxLayer2DataSize;

    #region Data
    private byte[]  packet;

    private uint    index;                // текущий индекс в packet
    private byte    sequenceNumber; 
    #endregion

    #region Private
    byte nextSeqNumber()
    {
      if(++sequenceNumber >= 254)
        sequenceNumber = 0;
      return sequenceNumber;
    }
    #endregion

    #region Public
    public DataIoAppTx()
    {
      packet = new byte[kMaxDataSize];
      index = 0;
      sequenceNumber = 0;
    }

    // Текущее значение sequenceNumber
    public byte getSeqNumber() { return sequenceNumber; }

    // Размер свободного места в выходном буфере
    public uint free() { return kMaxDataSize - index; }

    // Текущее число байт в выходном буфере
    public uint size() { return index; }

    // Начать формирования исходящего пакета
    public void startPacket() { index = 0; }


    // Получение выходного пакета
    // В data[] заносится ссылка на внутренний буфер с данными
    // В aindex заносится число байт в data[]
    public void getPacket(out byte[] data, out uint aindex)
    {
      data = packet;
      aindex = index;
    }


    // Получение выходных данных
    // Размещает массив и копирует в него выходные данные
    public byte[] getPacket()
    {
      byte[] data = new byte[index];
      Array.Copy(packet, 0, data, 0, index);
      return data;
    }

    // Получение выходных данных
    // В data[] заносится ссылка на внутренний буфер с исходящими данными.
    // Возвращает размер данных в data[]. 
    // Внимание: возвращаемое значение не соответствует data.Length.
    public uint getPacketRef(ref byte[] data)
    {
      data = packet;
      return index;
    }


    // Добавить сообщение в пакет для передачи
    //
    // msg      структура одного из типов: MsgParameterWrite, MsgParameterRead, MsgParameterValue
    //
    // return   true    сообщение добавлено в пакет
    //          false   нет места в выходном буфере или неизвестный тип сообщения
    public bool addMessage<T>(T msg) where T : struct
    {
      ushort size = 0;
      switch (DataIo.Types.StructToByte(msg)[0])
      {
        case 0x10:
          size = Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgParameterWrite)));
          break;

        case 0x11:
          size = Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgParameterRead)));
          break;

        case 0x12:
          size = Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgParameterValue)));
          break;

        case 0x13:
          size = Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgReadBlock)));
          break;

        /* case 0x80:
          byte[] Arr = new byte[Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader))];
          for (int i = 0; i < Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)); i++)
            Arr[i] = DataIo.Types.StructToByte(msg)[i];
          DataIo.Types.MsgAppTypeHeader NewStr = DataIo.Types.ByteToStruct<DataIo.Types.MsgAppTypeHeader>(Arr);

          size = Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)) + NewStr.dataSize);
          if (size > free()) return false;

          Array.Copy(DataIo.Types.StructToByte(msg), 0, packet, index, Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));
          index += Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

          if(NewStr.dataSize > 0)
          {
            Array.Copy(DataIo.Types.StructToByte(msg), Convert.ToByte(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader))), packet, index, Convert.ToByte(NewStr.dataSize));
            index += NewStr.dataSize;
          }
          return true; */

        default:
          return false;
      }
      if (size > free())
        return false;

      Array.Copy(DataIo.Types.StructToByte(msg), 0, packet, index, size);
      index = Convert.ToUInt16(index + size);
      return true;
    }


    // Добавить в выходной пакет сообщение MsgAppType с блоком данных.
    // Размер данных для копирования из data задается в поле msg.data.
    //
    // msg      заголовок сообщения MsgAppType
    // data     блок данных сообщения
    //
    // return   true    сообщение добавлено в пакет
    //          false   нет места в выходном буфере
    public bool addMessageAppType(DataIo.Types.MsgAppTypeHeader msg, ref byte[] data)
    {
      // полный размер сообщения (заголовок + данные)
      int size = Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)) + Convert.ToInt32(msg.dataSize);

      if (size > free())
        return false;

      msg.sequenceNumber = nextSeqNumber();

      Array.Copy(DataIo.Types.StructToByte(msg), 0, packet, index, Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

      index += Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

      if (data != null && msg.dataSize > 0)
      {
        Array.Copy(data, 0, packet, index, msg.dataSize);
        index += msg.dataSize;
      }
      return true;
    }


    // Добавить в выходной пакет сообщение MsgAppType с блоком данных.
    // Размер данных берётся из data.
    //
    // msgType    id сообщения MsgApp
    // data     блок данных сообщения
    //
    // return   true    сообщение добавлено в пакет
    //          false   нет места в выходном буфере
    public bool addMessageAppType(ushort msgType, ref byte[] data)
    {
      DataIo.Types.MsgAppTypeHeader msg = new DataIo.Types.MsgAppTypeHeader();
      msg.id = DataIo.Const.kMsgAppType;
      msg.dataSize = Convert.ToUInt16(data.Length);
      msg.type = msgType;

      return addMessageAppType(msg, ref data);
    }

/*    public bool addMessageAppType(ushort msgType, ref byte[] data)
    {
      DataIo.Types.MsgAppTypeHeader msg = new DataIo.Types.MsgAppTypeHeader();
      msg.id = DataIo.Const.kMsgAppType;
      //      m.sequenceNumber = NextSeqNumber();
      msg.sequenceNumber = nextSeqNumber();
      msg.dataSize = Convert.ToUInt16(data.Length);
      msg.type = msgType;

      // полный размер сообщения (заголовок + данные)
      int size = Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)) + Convert.ToInt32(msg.dataSize);

      if(size > free())
        return false;

      // Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length);

      // copy header to tx buffer
      Array.Copy(DataIo.Types.StructToByte(msg), 0,
                 packet, index,
                 Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

      index += Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

      if(data != null && msg.dataSize > 0)
      {
        // copy data to tx buffer
        Array.Copy(data, 0, packet, index, msg.dataSize);
        index += msg.dataSize;
      }
      return true;
    } */

    // Добавить в выходной пакет сообщение MsgAppType, не содержащее данных
    //
    // msgType    id сообщения MsgApp
    //
    // return   true    сообщение добавлено в пакет
    //          false   нет места в выходном буфере
    public bool addMessageAppType(ushort msgType)
    {
      if (Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)) > free())
        return false;

      DataIo.Types.MsgAppTypeHeader msg = new DataIo.Types.MsgAppTypeHeader();
      msg.id = DataIo.Const.kMsgAppType;
      msg.sequenceNumber = nextSeqNumber();
      msg.dataSize = 0;
      msg.type = msgType;

      Array.Copy(DataIo.Types.StructToByte(msg), 0, packet, index, Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));
      index += Convert.ToUInt16(Marshal.SizeOf(typeof(DataIo.Types.MsgAppTypeHeader)));

      return true;
    }

    #endregion
    /*
        // Формирование сообщения `Запись параметра`
        // pn       номер параметра  
        // v        значение параметра
        public void addMsgPWrite(int pn, int v)
        {

        }

        // Формирование сообщения `Чтение параметра`
        // pn       номер параметра  
        public void addMsgPRead(int pn)
        {

        }

        // Формирование сообщения `Запрос данных`
        public void addMsgRqData()
        {

        }

        // Формирование сообщения `Подтверждение приёма данных`
        //
        // packet     номер пакета  
        public void addMsgDataAck(int packet)
        {
        } */
  }
}
