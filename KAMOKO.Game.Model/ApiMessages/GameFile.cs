using KAMOKO.Game.Model.GameFile;

namespace KAMOKO.Game.Model.ApiMessages
{
  public class GameFile : Message
  {
    public GameFile() => Type = "gam";
    public QuestSentence[] Questions { get; set; }
  }
}