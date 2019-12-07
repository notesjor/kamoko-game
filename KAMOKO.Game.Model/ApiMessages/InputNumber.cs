namespace KAMOKO.Game.Model.ApiMessages
{
  public class InputNumber : Message
  {
    public InputNumber() => Type = "num";
    public int Min { get; set; } = 5;
    public int Max { get; set; } = 30;
  }
}