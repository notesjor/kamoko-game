using KAMOKO.Game.Model.GameFile.Abstract;

namespace KAMOKO.Game.Model.GameFile
{
  public class Constant : AbstractFragment
  {
    public string Text { get; set; }

    public override AbstractFragment GetGameFile()
    {
      return new Constant { Text = Text, Id = Id };
    }
  }
}