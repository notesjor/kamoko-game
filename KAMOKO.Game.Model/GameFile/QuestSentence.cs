using System.Collections.Generic;
using System.Linq;
using KAMOKO.Game.Model.GameFile.Abstract;

namespace KAMOKO.Game.Model.GameFile
{
  public class QuestSentence
  {
    public List<AbstractFragment> Fragments { get; set; } = new List<AbstractFragment>();

    public void AddConstant(string text) => Fragments.Add(new Constant { Id = Fragments.Count, Text = text });

    public void AddOption(string[] texts, int[] votes) => Fragments.Add(new Option { Id = Fragments.Count, Texts = texts, Votes = votes });

    public QuestSentence GetGameFile()
    {
      return new QuestSentence { Fragments = Fragments.Select(x => x.GetGameFile()).ToList() };
    }
  }
}
