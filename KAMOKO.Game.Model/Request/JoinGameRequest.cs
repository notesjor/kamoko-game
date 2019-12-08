using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.Request
{
  public class JoinGameRequest
  {
    public bool IsPrivateGame { get; set; }
    public Guid GameId { get; set; }
  }
}
