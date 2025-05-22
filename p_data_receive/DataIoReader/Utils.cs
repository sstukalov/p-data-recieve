using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataIoReader
{
  class Utils
  {
    //
    static public void stopThread(ref Thread th, int cntLim, int delay)
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

  }
}
