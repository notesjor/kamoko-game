using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.Request
{
  public class SubmitGameRequest
  {
    public string GameId { get; set; }
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int[][][] Answers { get; set; }
    public int[][] Time { get; set; }
  }
}
