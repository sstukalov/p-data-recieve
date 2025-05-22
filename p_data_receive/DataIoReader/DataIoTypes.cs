using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DataIo
{
  public class Types
  {
    // заголовок пакета уровня 2
    [StructLayout(LayoutKind.Explicit)]
    public struct LR2_HEADER
    {
      [MarshalAs(UnmanagedType.U2)]
      [FieldOffset(0)]
      public ushort preamble;					// преамбула
      [MarshalAs(UnmanagedType.I1)]
      [FieldOffset(2)]
      public byte destAddress;		    // адрес приемника
      [MarshalAs(UnmanagedType.I1)]
      [FieldOffset(3)]
      public byte srcAddress;			    // адрес источника
      [MarshalAs(UnmanagedType.U2)]
      [FieldOffset(4)]
      public ushort dataSize;					// количество байт данных
      [MarshalAs(UnmanagedType.U2)]
      [FieldOffset(6)]
      public ushort crc16;						// crc заголовка
    }

    // структура пакета уровня 2
//    [StructLayout(LayoutKind.Sequential, Size = 254)]
//    [StructLayout(LayoutKind.Sequential, Size = MaxLayer2DataSize + sizeof(LR2_HEADER) + sizeof(ushort)]
    [StructLayout(LayoutKind.Sequential, Size = Const.MaxLayer2DataSize + 8 + 2)]
    public struct LR2_PACKET
    {
      public LR2_HEADER header;                                                   // заголовок пакета

      [MarshalAs(UnmanagedType.U2)]
      public ushort crc16;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = Const.MaxLayer2DataSize, ArraySubType = UnmanagedType.U1)]
      public byte[] data;                                                         // блок данных

    }

    public enum TErrors
    {
      // layer 2
      errorRxDataCRC = 0,				// ошибка crc принятых данных
      errorRxHeaderCRC,				  // ошибка crc заголовка
      errorRxTimeout,					  // таймаут приема пакета
      errorLossRxPacket,			  // переполнение приемного буфера - не считан принятый пакет
      errorTxFrame,						  // ошибка передачи пакета
      // layer 3
      errorUnknownMsgId 			  // неизвестный идентификатор сообщения
    }


    //***************************************************************************
    //                  Базовые типы сообщений
    //***************************************************************************

    // id  - идентификатор типа сообщения - см. Msg..

    // secuenceNumber  - порядковый номер запроса - присваивается в исходящем
    // запросе прикладной программой, этот же номер возвращается в ответном
    // сообщении и используется прикладной программой для сопоставления запроса
    // и ответного сообщения.
    // Зарезервированные значения:
    //   0xff - для исходящих сообщений от мастера, не предполагающих ответ
    //   0xfe - для сообщений, генерируемых устройством без запроса

    // parameterNumber  - номер параметра

    // parameterValue   - значение параметра

    // result           - результат выполнения операции, см. Result..

    // Запись параметра
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgParameterWrite
    {
      [FieldOffset(0)]
      public byte id;
      [FieldOffset(1)]
      public byte sequenceNumber;
      [FieldOffset(2)]
      public ushort parameterNumber;
      [FieldOffset(4)]
      public UInt32 parameterValue;
    }

    // Чтение параметра
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgParameterRead
    {
      [FieldOffset(0)]
      public byte id;
      [FieldOffset(1)]
      public byte sequenceNumber;
      [FieldOffset(2)]
      public ushort parameterNumber;
    }

    // Чтение блока параметров
/*  typedef struct tagMsgReadBlock
  {
  	Uint8		id;
  	Uint8		sequenceNumber;
  	Uint16	base;							// номер начального параметра
  	Uint8		size;							// число параметров
  	Uint8		reserved;
  }TMsgReadBlock; */
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgReadBlock
    {
      [FieldOffset(0)]
      public byte id;
      [FieldOffset(1)]
      public byte sequenceNumber;
      [FieldOffset(2)]
      public ushort baseAddr;
      [FieldOffset(4)]
      public byte   size;
      [FieldOffset(5)]
      public byte   reserved;
    }

    // Значение параметра - передается в ответ на запросы "Запись параметра"
    // и "Чтение параметра". В поле result передается результат выполнения операции.
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgParameterValue
    {
      [FieldOffset(0)]
      public byte id;
      [FieldOffset(1)]
      public byte sequenceNumber;
      [FieldOffset(2)]
      public ushort parameterNumber;
      [FieldOffset(4)]
      public int parameterValue;
      [FieldOffset(8)]
      public short result;
    }

    // Заголовок сообщения прикладного типа
    // type     - номер прикладного типа
    // dataSize - размер связанного блока данных
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgAppTypeHeader
    {
      [FieldOffset(0)]
      public byte id;
      [FieldOffset(1)]
      public byte sequenceNumber;
      [FieldOffset(2)]
      public ushort type;
      [FieldOffset(4)]
      public ushort dataSize;
    }

    
    // Сообщение прикладного типа
    [StructLayout(LayoutKind.Sequential)]
    public struct MsgAppType
    {
      public MsgAppTypeHeader header;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 238)]
      public byte[] data;
    }
    

    // Дата/время
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgAppDateTime
    {
      [FieldOffset(0)]
      public MsgAppTypeHeader header;			// dataSize = sizeof(Uint8)*2 + sizeof(Uint16)
      [FieldOffset(6)]
      public ushort year;
      [FieldOffset(8)]
      public byte month;
      [FieldOffset(9)]
      public byte dayOfMonth;
      [FieldOffset(10)]
      public byte hours;
      [FieldOffset(11)]
      public byte minutes;
      [FieldOffset(12)]
      public byte seconds;
      [FieldOffset(13)]
      public byte reserved;

      public MsgAppDateTime(byte i)
        : this()
      {
        header.id = Const.kMsgAppType;
        header.type = Const.kMsgApp_DateTime;
        header.dataSize = Convert.ToUInt16(Marshal.SizeOf(typeof(MsgAppDateTime)) - Marshal.SizeOf(typeof(MsgAppTypeHeader)));
      }
    }


    //
    // Подтверждение блока данных и блока данных из УХД.
    // DataAck и SvDataAck имеют аналогичую структуру, отличается MessageID.
    // DataAck.packetNumber     номер блока данных
    // SvDataAck.packetNumber   номер страницы УХД
    //
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgAppDataAcknowledge
    {
      [FieldOffset(0)]
      public MsgAppTypeHeader header;			// dataSize = sizeof(Uint16)
      [FieldOffset(6)]
      public ushort packetNumber;
    }


    //*************************************************************************
    //                            Блоки данных
    //*************************************************************************

    //
    // Заголовок пакета данных
    //
    [StructLayout(LayoutKind.Explicit)]
    public struct TDataBufferHeader
    {
      [FieldOffset(0)]
      public ushort packetNumber;			  // номер пакета 
      [FieldOffset(2)]
      public ushort dataSize;		        // число ОТСЧЕТОВ ДАННЫХ ИСПОЛЬЗУЕМОГО ТИПА в буфере data
      [FieldOffset(4)]
      public UInt32 sync;				        // значение таймера синхронизации для первого отсчета в буфере
    }

    //
    // Содержит служебную информацию и блок данных, которые будут помещены в 
    // выходной пакет - для передачи или записи в УХД
    //
    [StructLayout(LayoutKind.Explicit)]
    public struct TDataBuffer
    {
      [FieldOffset(0)]
      public ushort packetNumber;			  // номер пакета - заполняется при переводе буфера из состояния заполнение данными в состояние ожидания передачи в КС или УХД
      // циклическая нумерация от 0 до 0xffff

      [FieldOffset(2)]
      public ushort dataSize;		        // число ОТСЧЕТОВ ДАННЫХ ИСПОЛЬЗУЕМОГО ТИПА в буфере data
      [FieldOffset(4)]
      public UInt32 sync;				        // значение таймера синхронизации для первого отсчета в буфере

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = Const.MAX_DATA_BUFFER_SIZE - 8, ArraySubType = UnmanagedType.I1)]
      [FieldOffset(8)]
      public byte[] data;

      public byte getServiceInfoSize() { return Convert.ToByte(Marshal.SizeOf(packetNumber) + Marshal.SizeOf(dataSize) + Marshal.SizeOf(sync)); }

      public TDataBuffer(byte i)
        : this()
      {
        data = new byte[Const.MAX_DATA_BUFFER_SIZE - 8];
      }
    }


    //
    // Блок параметров - ответ на MsgReadBlock
    // Здесь определение заголовка блока данных прикладного сообщения, 
    // после него должен размещаться массив значений параметров
    //
/*    typedef struct tagMsgParameterBlockHeader
    {
	    DataIO::TMsgAppTypeHeader	header;			// sizeof(header) = 6
															    //															offset	
	    Uint16		base;							// номер начального регистра		7 8
	    Uint8			size;							// число параметров							9
	    Uint8			errCount;					// счетчик ошибок								10
	    Uint16		reserved;					//															11

	    tagMsgParameterBlockHeader(void)
	    {
		    header.id 			= DataIO::MsgAppType;
		    header.type			= MsgApp_RegBlock;
		    errCount				= 0;
	    }
	
	    // b  - номер начального регистра
	    // sz - число параметров
	    void setParameters(Uint16 b, Uint16 sz)
	    {
		    size = sz;
		    header.dataSize = size*sizeof(Uint32) + sizeof(struct tagMsgParameterBlockHeader) - sizeof(DataIO::TMsgAppTypeHeader);
	    }      
	
	    // возвращает указатель на блок данных прикладного сообщения для передачи в addMessage
	    Uint8* getAppDataPtr(void)
	    {                                                                                     
		    return (Uint8*)&base;
	    }                  
    }TMsgParameterBlockHeader;     */


    //
    // Заголовок прикладного сообщения типа "Блок параметров"
    //
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgParameterBlockHeader
    {
      [FieldOffset(0)]
      public MsgAppTypeHeader header;			

      [FieldOffset(6)]
		  public UInt16		baseAddr;					// номер начального регистра	

      [FieldOffset(8)]
	    public byte			size;						// число параметров						

      [FieldOffset(9)]
	    public byte			errCount;					// счетчик ошибок							

      [FieldOffset(10)]
	    public UInt16		reserved;					//														
    }

    //
    //  MsgParameterBlockHeader + блок данных
    //
    [StructLayout(LayoutKind.Explicit)]
    public struct MsgParameterBlock
    {
      [FieldOffset(0)]
      public MsgAppTypeHeader header;			

      [FieldOffset(6)]
	    public UInt16		baseAddr;					// номер начального регистра	

      [FieldOffset(8)]
	    public byte			size;							// число параметров						

      [FieldOffset(9)]
	    public byte			errCount;					// счетчик ошибок							

      [FieldOffset(10)]
	    public UInt16		reserved;					//														

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = Const.MAX_DATA_BUFFER_SIZE - 8, ArraySubType = UnmanagedType.I1)]
      [FieldOffset(12)]
      public byte[] data;

/*      public MsgParameterBlock(byte i)
        : this()
      {
        data = new byte[MAX_DATA_BUFFER_SIZE - 8];
      } */
    }


    //*************************************************************************
    //         Методы преобразования структур и массивов байт
    //*************************************************************************

    // Получение массива байт из структуры
    public static byte[] StructToByte<T>(T value) where T : struct
    {
      byte[] arr = new byte[Marshal.SizeOf(value)];
      GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned);
      IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0);
      Marshal.StructureToPtr(value, ptr, true);
      gch.Free();
      return arr;
    }

    public static byte[] StructToByte1<T>(T value) where T : struct
    {
      byte rawsize = Convert.ToByte(Marshal.SizeOf(value));
      IntPtr buffer = Marshal.AllocHGlobal(rawsize);
      Marshal.StructureToPtr(value, buffer, false);
      byte[] rawdata = new byte[rawsize];
      Marshal.Copy(buffer, rawdata, 0, rawsize);
      Marshal.FreeHGlobal(buffer);
      return rawdata;
    }

    // Получение структры из массива байт
    public static T ByteToStruct<T>(byte[] arr, int index = 0) where T : struct
    {
      GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned);
      IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(arr, index);
      T ret = (T)Marshal.PtrToStructure(ptr, typeof(T));
      gch.Free();
      return ret;
    }
  }
}
