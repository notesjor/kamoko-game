using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.Helper
{
  public static class RandomHelper
  {
    private static Random _rnd = new Random(DateTime.Now.Millisecond);

    public static int GetNumber() => _rnd.Next(10000000, 99999999);

    public static int GetNumber(int min, int max) => _rnd.Next(min, max);

    public static string GetGuid() => Guid.NewGuid().ToString("N");
  }
}
