using System;
using System.Runtime.InteropServices;
using System.Net;

namespace DataIo
{
  // Разбор входящего пакета и выделение сообщений. 
  // Сообщения передаются внешним обработчикам в соответствии с типом сообщения.
  public class DataIoAppRx
  {
    #region Data
    /// <summary>
    /// Обработчик сообщений, извлекаемых из принятого пакета
    /// </summary>
    /// <param name="msg">Сообщение</param>
    /// <param name="destAddress">Адрес получателя пакета</param>
    /// <param name="scrAddress">Адрес источника пакета</param>
    /// <param name="index">Индекс в msg[]</param>
    // Для MsgAppType index должен быть = 0 - всегда создавать отдельную копию объекта сообщения
    // (в обработчике MsgAppType index не поддерживается).
    public delegate void RxMessageHandler(ref byte[] msg, ushort destAddress, ushort srcAddress, uint index, ref IPEndPoint ip);

    protected RxMessageHandler rxMessageHandler;
    #endregion

    #region Private
    #endregion

    #region Public
    public DataIoAppRx(RxMessageHandler h)
    {
      rxMessageHandler = h;
    }

    /// <summary>
    /// Извлечение сообщений из принятого блока данных.
    /// </summary>
    /// <param name="data">блок данных</param>
    /// <param name="size">количество байт данных в data</param>                                              
    // Выделяет из data сообщения, для каждого сообщения вызывается rxMessageHandler.
    // Возвращает количество обработанных байт из data (суммарная длина выделенных сообщений).
    public ulong parse(ref byte[] data, int size, IPEndPoint ip)
    {
      uint index = 0, msize = 0; 
      ushort id;
      byte[] msgApp = null;

      while (index < size)
      {
        id = data[index];
        switch (id)
        {
          default: return index;

          case Const.kMsgParameterWrite:
            msize = Convert.ToUInt32(Marshal.SizeOf(typeof(Types.MsgParameterWrite)));
            break;

          case Const.kMsgParameterRead:
            msize = Convert.ToUInt32(Marshal.SizeOf(typeof(Types.MsgParameterRead)));
            break;

          case Const.kMsgParameterValue:
            msize = Convert.ToUInt32(Marshal.SizeOf(typeof(Types.MsgParameterValue)));
            break;

          case Const.kMsgReadBlock:
            size = Convert.ToInt32(Marshal.SizeOf(typeof(Types.MsgReadBlock)));
            break;

          // для MsgAppType создается отдельный массив для передачи в handler
          case Const.kMsgAppType:
            //				    msize = sizeof(TMsgAppTypeHeader) + ((TMsgAppTypeHeader*)&data[index])->dataSize;
            int appMsgHeaderSz = Marshal.SizeOf(typeof(Types.MsgAppTypeHeader));
            byte[] appMsgHeader = new byte[appMsgHeaderSz];
            Array.Copy(data, index, appMsgHeader, 0, appMsgHeaderSz);
            msize = Convert.ToUInt16(Marshal.SizeOf(typeof(Types.MsgAppTypeHeader)) + 
              Types.ByteToStruct<Types.MsgAppTypeHeader>(appMsgHeader).dataSize);
            msgApp = new byte[msize];
            Array.Copy(data, index, msgApp, 0, msize);
            break;
        }

        if (msize > size - index)
          return index;

        if (id == Const.kMsgAppType)
          rxMessageHandler(ref msgApp, 0, 0, 0, ref ip);
        else
          rxMessageHandler(ref data, 0, 0, index, ref ip);

        index += msize;
      }
      return index;
    }
    #endregion
  }
}
