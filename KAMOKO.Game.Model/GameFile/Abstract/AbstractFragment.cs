namespace KAMOKO.Game.Model.GameFile.Abstract
{
  public abstract class AbstractFragment
  {
    public int Id { get; set; }
    public abstract AbstractFragment GetGameFile();
  }
}