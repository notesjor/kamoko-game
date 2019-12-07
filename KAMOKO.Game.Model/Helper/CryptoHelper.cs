using System;
using System.Security.Cryptography;
using System.Text;

namespace KAMOKO.Game.Model.Helper
{
  public static class CryptoHelper
  {
    private static MD5 _hash = MD5.Create(); // MD5 reicht aus

    public static string Hash(string msg) => Convert.ToBase64String(_hash.ComputeHash(Encoding.UTF8.GetBytes(msg)));

    public static string Blockchain(string seed, DateTime ts, string msg) => Hash(seed + msg + ts.ToLongTimeString());
  }
}