namespace KAMOKO.Game.Model.ApiMessages
{
  public class InputSelect : Message
  {
    public InputSelect() => Type = "sel";
    public string[] Options { get; set; }
  }
}