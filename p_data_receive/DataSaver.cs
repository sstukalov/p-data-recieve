using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;


namespace p_data_receive
{
  //
  // Запись данных в файлы
  //
  class DataSaver
  {
    #region Data
    public delegate  void WriteDataToFile(ref FileWriter w, ref DataIoReader.DataBlock data);


    //
    // Хранение информации о файле для сохранения данных
    //
    private class FileServiceData
    {
      public int  index,              // индекс устройства, от которого получены данные - соответствует индексу в массиве конфигурации UDP
                  srcAddress,         // сетевой адрес RS485 блока измерения
                  numAdc;             // номер АЦП

      public FileWriter writer;

      public FileServiceData(int aindex, int src, int nadc, string catalog, string prefix, string ext)
      {
        index = aindex;
        srcAddress = src;
        numAdc = nadc;

        writer = new FileWriter(String.Format("{4}{0}_i{1}_sa{2}_n{3}_", prefix, index, srcAddress, numAdc, catalog), ext);
      }
    }

    private List<FileServiceData> files;                // открытые файлы
    private Queue<DataIoReader.DataBlock> dataQueue;    // блоки данных для сохранения
    private Thread thread;                              // передача исходящих данных по UDP
    private AutoResetEvent queueEvent;                  // синхронизация потока
    bool run;                                           // разрешение выполнения потока
    
    private string  filePrefix,                         // префикс имени файла
                    fileExt,                            // расширение имени файла, включая точку
                    catalog;                            // каталог для записи файла

    WriteDataToFile writeDataToFile;

    #endregion

    #region Public
    public DataSaver(string acatalog, string prefix, string ext, WriteDataToFile writeData)
    {
      catalog = acatalog;
      fileExt = ext;
      filePrefix = prefix;
      writeDataToFile = writeData;

      files = new List<FileServiceData>();

      dataQueue = new Queue<DataIoReader.DataBlock>();
      queueEvent = new AutoResetEvent(true);

      thread = new Thread(this.threadRun);
      thread.IsBackground = true;

      run = true;
      thread.Start();
    }


    // Закрыть все файлы
    public void close()
    {
      foreach(FileServiceData v in files)
        v.writer.close();

      files.Clear();
    }


    // Передача данных на запись.
    // Помещает data в очередь и сигнализирует потоку о наличии данных.
    public void writeData(ref DataIoReader.DataBlock data)
    {
      dataQueue.Enqueue(data);
      queueEvent.Set();

      // Проверка работы без запуска потока
/*      int qSize;
      do
      {
        DataIoReader.DataBlock d = null;

        lock(dataQueue)
        {
          if(dataQueue.Count > 0)
          {
            d = dataQueue.Dequeue();
            qSize = dataQueue.Count;
          }
          else qSize = 0;
        }

        if(d != null)
        {
          FileWriter ww = getWriter(ref d);
          writeDataToFile(ref ww, ref d);
        }
      }while(qSize > 0); */
    }
    #endregion

    #region Private
    // Поиск в files объекта, соответствующего атрибутам в data.
    // Если объект не найден, создаёт новый FileServiceData, заносит его в files и открывает соответствующий файл.
    // При ошибке открытия файла возвращает null.
    private FileWriter getWriter(ref DataIoReader.DataBlock data)
    {
      foreach(FileServiceData v in files)
        if(data.index == v.index  &&  data.srcAddress == v.srcAddress  &&  data.numAdc == v.numAdc)
          return v.writer;

      FileServiceData sd = new FileServiceData(data.index, data.srcAddress, data.numAdc, catalog, filePrefix, fileExt);

      if(sd.writer.open())
      {
        files.Add(sd);
        return sd.writer;
      }
      else 
        return null;
    }


    // Запись блока данных в файл
/*    void writeDataToFile(ref FileWriter w, ref DataIoReader.DataBlock data)
    {
      w.write(String.Format("{0}; {1}; {2}\r\n", data.packet, data.timestamp, data.data.Length));
    } */

    // Метод выполнения потока.
    // Ожидает поступления блока данных в очередь и выполняет запись в файл.
    private void threadRun()
    {
      int qSize;

      while(run)
      {
        queueEvent.WaitOne();
        do
        {
          DataIoReader.DataBlock data = null;

          lock(dataQueue)
          {
            if(dataQueue.Count > 0)
            {
              data = dataQueue.Dequeue();
              qSize = dataQueue.Count;
            }
            else qSize = 0;
          }

          if(data != null)
          {
            FileWriter w = getWriter(ref data);
            writeDataToFile(ref w, ref data);
          }
        }while(qSize > 0);
        queueEvent.Reset();
      }
    }
    #endregion    
  }

  //---------------------------------------------------------------------------
  //
  // Запись в файл
  //
  public class FileWriter
  {
    private object syncObj;

    private FileInfo      fileInfo;
    private StreamWriter  file; 
    private bool          active; 
    private string        prefix,     // префикс имени файла
                          ext;        // расширение файла, включая точку

    public FileWriter(string aprefix, string aext)
    {
      syncObj = new object();
      prefix = aprefix; 
      ext = aext;
      active = false;
    }

    //
    public bool isActive() { return active; }

    //
    public bool open()
    {
      lock(syncObj)
      {
        if(active) return false;
        try
        {
          string name = prefix + GetStrDateTime() + ext;
          fileInfo = new FileInfo(name);
          file = fileInfo.CreateText();
          active = true;
        }
//        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка создания файла"); }
        catch (Exception ex) { }
      }
      return active;
    }

    //
    public void close()
    {
      lock(syncObj)
      {
        if(active)
          try
          {
            file.Close();
            active = false;
            fileInfo = null;
          }
//          catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка закрытия файла"); }
          catch (Exception ex) { }
      }
    }

    public void write(string s)
    {
      lock(syncObj)
      {
        try
        {
          if(active) 
            file.Write(s);
        }
//        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка записи файла"); }
        catch (Exception ex) { }
      }
    }

    private string GetStrDateTime()
    {
      DateTime dt = DateTime.Now;
      string s = "";

      s += dt.Year.ToString().Remove(0, 2);
      s += CorrectDate(dt.Month);
      s += CorrectDate(dt.Day);
      s += "_";
      s += CorrectDate(dt.Hour);
      s += CorrectDate(dt.Minute);
      s += CorrectDate(dt.Second);
      return s;
    }

    private string CorrectDate(int Date)
    {
      if (Date < 10) return ("0" + Date.ToString());
      return Date.ToString();
    }
  }

}
