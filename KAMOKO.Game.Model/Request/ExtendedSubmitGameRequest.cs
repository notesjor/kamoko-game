using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KAMOKO.Game.Model.Response;

namespace KAMOKO.Game.Model.Request
{
  public class ExtendedSubmitGameRequest
  {
    public SubmitGameRequest Request { get;set;}
    public SubmitGameResponse Response { get;set;}
  }
}
