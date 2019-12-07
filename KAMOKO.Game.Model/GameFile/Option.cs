using KAMOKO.Game.Model.GameFile.Abstract;

namespace KAMOKO.Game.Model.GameFile
{
  public class Option : AbstractFragment
  {
    public string[] Texts { get; set; }
    public int[] Votes { get; set; }
    public override AbstractFragment GetGameFile()
    {
      var answers = new int[Votes.Length];
      for (var i = 0; i < answers.Length; i++)
        answers[i] = -1;

      return new Option { Texts = (string[])Texts.Clone(), Votes = answers, Id = Id };
    }
  }
}