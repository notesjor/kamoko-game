using System.Linq;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameMode.Abstract;
using KAMOKO.Game.Model.GameState;
using KAMOKO.Game.Model.Request;

namespace KAMOKO.Game.Model.GameMode
{
  public class KamokoGameModeComplex : AbstractKamokoGameMode
  {
    public override void Calculate(ref KamokoGameStateServer state, ref SubmitGameRequest submit, out int score, out int total)
    {
      var answers = submit.Answers;

      total = 0;
      score = 0;

      for (var i = 0; i < state.Questions.Length; i++)
      {
        var q = state.Questions[i];
        for (var j = 0; j < q.Fragments.Count; j++)
        {
          var f = q.Fragments[j];
          if (f is Constant)
            continue;

          var o = f as Option;
          if (o == null)
            continue;

          total += o.Votes.Length; // MODE

          if (answers.Length    <= i
           || answers[i]        == null
           || answers[i].Length <= j
           || answers[i][j]     == null) // keine Antwort
            continue;

          // MODE (Alle negativ)
          if (answers[i][j].Count(x => x == -1) == o.Votes.Length && o.Votes.All(x => x == -1))
          {
            score += o.Votes.Length;
            continue;
          }

          for (var k = 0; k < o.Votes.Length; k++)
          {
            if(answers[i][j].Length <= k)
              break;

            if (answers[i][j][k] == o.Votes[k])
              score++;
          }
        }
      }
    }
  }
}