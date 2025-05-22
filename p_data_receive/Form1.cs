using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

using Win32;

using DataIoReader;


namespace p_data_receive
{
  public partial class Form1 : Form
  {
    #region Data
    public const int kMaxDevicesNum = 8;      // максимальное количество опрашиваемых устройств

    //*************************************************************************
    //         Набор данных, реализующих функциональность модуля
    //*************************************************************************

    private TMSTimer msTimer;                 // отсчёт интервалов

    private UdpConfig[] udpConfig;            // настройки UDP

    private Reader dataReader;                // реализация опроса устройств и приёма данных

    //*************************************************************************
    // Вспомогательные данные

    private DataSaver savePacketsInfo,        // запись в файл информации о пакетах
                      saveData;               // запись в файл блоков данных

    private bool fSaveData = false;           // сохранять данные в файл
    #endregion

    //-------------------------------------------------------------------------
    #region Управление формой
    public Form1()
    {
      InitializeComponent();

      msTimer = new TMSTimer();

      savePacketsInfo = new DataSaver("data\\", "pn_", ".csv", writeToFile_PacketInfo);
      saveData        = new DataSaver("data\\", "d_", ".csv", writeToFile_Data);

      Directory.CreateDirectory(Environment.CurrentDirectory + "\\data");   // каталог для сохранения данных
    }

    //
    private void Form1_Load(object sender, EventArgs e)
    {
      //-------- UDP config init --------
      //
      // 1) Создаётся массив UdpConfig[] максимального размера со значениями null.
      // 2) Загружается конфигурация из файла. Загруженная конфигурация может содержать значения null.
      // 3) Выполняется копирование загруженной конфигурации в udpConfig, в т.ч. и значения null.
      // 4) Если в массиве все значения null, первый элемент инициализируется значениями по умолчанию.

      udpConfig = new UdpConfig[kMaxDevicesNum];

//      for(int i = 0; i < udpConfig.Length; i++)
//        udpConfig[i] = new UdpConfig();

      TAppConfig c = new TAppConfig(ref udpConfig);

      ReadConfig(ref c);

      // При отсутствии в файле конфигурации блоков, соответствующих к-л объекту в TAppConfig,
      // соответствующее поле в "c" имеет значение null - обрабатывать ситуацию в методах copy()
      // объектов конфигурации.

      for(int i = 0; i < c.udpConfig.Length; i++)
        udpConfig[i] = c.udpConfig[i];

      if(udpConfig[0] == null)
        udpConfig[0] = new UdpConfig();

      //-------- UDP config init end --------

      // Вывести сетевые настройки в таблицу
      tabControlDevicePropertiesSetup();

      // Создать и настроить экземпляр модуля обмена
      dataReader = new Reader(ref msTimer, putToLog, receiveDataBlock, receiveParameterValue);
      dataReader.setup(ref udpConfig, 4023);
    }


    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
      // Закрыть файлы данных
      savePacketsInfo.close();
      saveData.close();

      // Сохранить конфигурацию в файл
      TAppConfig c = new TAppConfig(ref udpConfig);
      WriteConfig(ref c);
    }
    #endregion

    //-------------------------------------------------------------------------
    #region Разные вспомогательные функции
    //
    // Timer tick handler
    //
    private void timer1_Tick(object sender, EventArgs e)
    {
      // 
    }

    
    //
    // Сохранение конфигурации в файл
    //
    private void WriteConfig(ref TAppConfig c)
    {
      try
      {
        string XMLFileName = Environment.CurrentDirectory + "\\config.xml";
        XmlSerializer ser = new XmlSerializer(typeof(TAppConfig));
        TextWriter writer = new StreamWriter(XMLFileName);
        ser.Serialize(writer, c);
        writer.Close();
      }
      catch (Exception e)
      {
        MessageBox.Show(e.Message, "Ошибка сохранения конфигурации");
      }
    }


    //
    // Загрузка конфигурации из файла
    //
    private static bool ReadConfig(ref TAppConfig c)
    {
      bool rc = false;
      string XMLFileName = Environment.CurrentDirectory + "\\config.xml";
      if (File.Exists(XMLFileName))
      {
        try
        {
          XmlSerializer ser = new XmlSerializer(typeof(TAppConfig));
          TextReader reader = new StreamReader(XMLFileName);
          c = (TAppConfig)ser.Deserialize(reader);
          reader.Close();
          rc = true;
        }
        catch (Exception e)
        {
          MessageBox.Show(e.Message, "Ошибка загрузки конфигурации");
        }
      }
      return rc;
    }

    
    //
    // Заполнение таблицы сетевых настроек устройств вкладки Управление
    //
    private void tabControlDevicePropertiesSetup()
    {
      int n = 0;    // количество валидных записей в udpConfig[]

      for(int i = 0; i < udpConfig.Length; i++)
        if(udpConfig[i] != null)
          n++;

      tableControlDeviceProperties.Rows.Clear();
      tableControlDeviceProperties.Rows.Add(n);

      for(int i = 0; i < n; i++)
      {
        tableControlDeviceProperties.Rows[i].Cells[0].Value = udpConfig[i].remoteAddress;
        tableControlDeviceProperties.Rows[i].Cells[1].Value = udpConfig[i].localPort;
        tableControlDeviceProperties.Rows[i].Cells[2].Value = udpConfig[i].remotePort;
      }
    }
    #endregion

    //-------------------------------------------------------------------------
    #region Интефейс GUI формы

    // Вывод в лог
    public void putToLogDirect(string s)
    {
      Action act = new Action(() => 
      {
        Log.AppendText(s + "\r\n");
      });

      if(InvokeRequired)
        this.BeginInvoke(act);
      else act();
    }

    // Вывод в лог с добавлением времени
    public void putToLog(string s)
    {
      string tm = String.Format("{0,2:00}:{1,2:00}:{2,2:00}.{3,3:000}",
        DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);

      Action act = new Action(() => 
      {
        Log.AppendText(tm + s + "\r\n");
      });

      if(InvokeRequired)
        this.BeginInvoke(act);
      else act();
    }
    #endregion

    //-------------------------------------------------------------------------
    #region Обработчики главного меню
    private void menuFileExit_Click(object sender, EventArgs e)
    {
      this.Close();
    }
    #endregion

    //-------------------------------------------------------------------------
    #region Обработчики событий от элементов управления формы

    //
    // Пуск обмена с устройствами
    //
    private void bControlStart_Click(object sender, EventArgs e)
    {
      if(!dataReader.active())
      {
        dataReader.controlDataPoll(cbDataPollEnable.Checked);
        dataReader.start();
      }
    }
    
    //
    // Останов
    //
    private void bControlStop_Click(object sender, EventArgs e)
    {
      dataReader.stop();

      if(fSaveData)
      {
        savePacketsInfo.close();
        saveData.close();
      }
    }

    //
    // Управление формированием запросов данных
    //
    private void cbDataPollEnable_CheckedChanged(object sender, EventArgs e)
    {
      dataReader.controlDataPoll(cbDataPollEnable.Checked);
    }

    //
    // Управление сохранением данных
    //
    private void cbSaveData_CheckedChanged(object sender, EventArgs e)
    {
      fSaveData = cbSaveData.Checked;
    }

    #endregion

    //-------------------------------------------------------------------------
    #region Интерфейс к модулю приёма данных
    
    // Приём блока данных
    public void receiveDataBlock(ref DataBlock data)
    {
      if(fSaveData)
      {
        savePacketsInfo.writeData(ref data);
        saveData.writeData(ref data);
      }
    }


    // Приём значения параметра
    public void receiveParameterValue(int parameterNumber, int ParameterValue, int index)
    {
    }
#endregion
    
    //-------------------------------------------------------------------------
    #region Сохранение данных в файл

    // Делегаты, вызываемые из объектов DataSaver.
    // В метод передаются объект FileWriter, реализующий запись строки в файл и блок данных.
    // Метод вызывается один раз для каждого блока данных, переданного через DataSaver.writeData()

    // Запись в файл информации о блоке данных
    void writeToFile_PacketInfo(ref FileWriter w, ref DataIoReader.DataBlock data)
    {
      w.write(String.Format("{0}; {1}; {2}\r\n", data.packet, data.timestamp, data.data.Length));
    }


    // Запись в файл блока данных
    void writeToFile_Data(ref FileWriter w, ref DataIoReader.DataBlock data)
    {
//      for(int i = 0, index = 0; i < data.data.Length; i += data.numCh)
      for(int i = 0; i < data.data.Length; i += 3)             // фиксированный вариант для 3-х каналов данных
        w.write(String.Format("{0};{1};{2};{3};{4}\r\n", data.packet, data.timestamp, data.data[i + 0], data.data[i + 1], data.data[i + 2]));
    }
    #endregion
  }
}
