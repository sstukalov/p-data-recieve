using System;

using DataIoReader;

namespace p_data_receive
{
  //
  // Контейнер объектов конфигурации
  //
  [Serializable]
  public class TAppConfig
  {
    public UdpConfig[] udpConfig;

    public TAppConfig(ref UdpConfig[] audpConfig)
    {
      udpConfig = audpConfig;
    }

    public TAppConfig()
    {
    }
  };
}
