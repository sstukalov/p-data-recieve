using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DataIo
{
  public class Const
  {
    public const int MaxLayer2DataSize = 1280;        // макс. размер данных пакета уровня 2 

    // Макс. размер блока данных в TDataBuffer
    public const int MAX_DATA_BUFFER_SIZE = 1472;     // UDP - в пакете только прикладные сообщения

    public const byte BROADCAST_ADDRESS = 255;        // Широковещательный адрес

    public const ushort Layer2Preamble = 0x55AA;
    public const byte   Layer2PreambleB0 = 0xAA;
    public const byte   Layer2PreambleB1 = 0x55;


    //***************************************************************************
    //                     Идентификаторы сообщений
    //***************************************************************************

    public const byte kMsgParameterWrite   = 0x10;
    public const byte kMsgParameterRead    = 0x11;
    public const byte kMsgParameterValue   = 0x12;
    public const byte kMsgReadBlock        = 0x13;
    public const byte kMsgReadBlock2       = 0x14;
    public const byte kMsgAppType = 0x80;


    //***************************************************************************
    //     Коды результата операции для запросов на запись и чтение параметра
    //***************************************************************************

    public const short kResultOk = 0;
    public const short kResultBadParameter = -1;		// недопустимое значение номера параметра
    public const short kResultBadValue = -2;   		  // недопустимое значение параметра
    public const short kResultOperationError = -3;	// ошибка выполнения операции


    //*************************************************************
    //             Сообщения прикладного типа
    //*************************************************************

    // значения поля type для сообщений прикладного типа
    public const byte kMsgApp_RegBlock               = 0x08;		// M >> S
    public const byte kMsgApp_RegBlock2              = 0x09;		// M >> S
    public const byte kMsgApp_RequestData            = 0x10;		// M >> S
    public const byte kMsgApp_DataAcknowledge        = 0x11;		// M >> S
    public const byte kMsgApp_DataAbsence            = 0x12;		// M << S
    public const byte kMsgApp_DataAcknowledgeOk      = 0x13;		// M << S
    public const byte kMsgApp_DataAcknowledgeError   = 0x14;		// M << S
    public const byte kMsgApp_RequestStateInfo       = 0x15;		// M >> S
    public const byte kMsgApp_StateInfo              = 0x16;		// M << S
    public const byte kMsgApp_DataAbsence2           = 0x17;		// M << S
    public const byte kMsgApp_DataInt32              = 0x20;		// M << S
    public const byte kMsgApp_DataInt16              = 0x21;		// M << S
    public const byte kMsgApp_DataEx                 = 0x22;		// M << S
    public const byte kMsgApp_RequestDateTime        = 0x30;		// M >> S
    public const byte kMsgApp_DateTime               = 0x31;		// M << >> S
    public const byte kMsgApp_Wakeup                 = 0x32;		// M >> S

    // блоки данных с разным кол-вом каналов
    public const byte kMsgApp_DataInt16x2 = 0x62;		// M << S
    public const byte kMsgApp_DataInt16x3 = 0x63;		// M << S
    public const byte kMsgApp_DataInt16x4 = 0x64;		// M << S
    public const byte kMsgApp_DataInt16x6 = 0x66;		// M << S
    public const byte kMsgApp_DataInt16x8 = 0x68;		// M << S
    public const byte kMsgApp_DataInt32x2 = 0x6a;		// M << S
    public const byte kMsgApp_DataInt32x3 = 0x6b;		// M << S
    public const byte kMsgApp_DataInt32x4 = 0x6c;		// M << S
    public const byte kMsgApp_DataInt32x8 = 0x6d;		// M << S

    // 0x100 .. 0x109  - зарезервировано для BootSender

    // Унифицированные идентификаторы типов данных
    public const UInt16 kMsgApp_DataInt16xBase = 0x200;  // Модуль ввода AI16/8 - Int16
    public const UInt16 kMsgApp_DataInt32xBase = 0x300;  // Модуль ввода AI16/8 - Int32

    public const UInt16 MAX_NCHANNELS = 32;             // макс. число каналов в многоканальных БД

    // Сообщения, используемые модулем BootSender
    //
    // Контейнер для передачи различных команд, содержащих небольшой объем данных.
    // Блок данных kMsgAppType.data[] представляется как массив UInt32[] kMsgData.
    public const UInt16 kMsgApp_BootService  = 0x100;
    // Данные для записи в буфер RAM
    public const UInt16 kMsgApp_BootRamData  = 0x101;
    // Ответ от устройства на команду или блок данных
    public const UInt16 kMsgApp_BootAck      = 0x102;
  }
}
