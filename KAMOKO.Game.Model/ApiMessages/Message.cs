using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.ApiMessages
{
  public class Message
  {
    public string Type { get; set; } = "msg";
    public string Text { get; set; }
  }
}
