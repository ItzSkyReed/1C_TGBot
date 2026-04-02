using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;

namespace Application;
using FuzzySharp;

public class Utils
{
    public static int GetBestMatchIndex(string searchTerm, List<string> candidates)
    {
        if (candidates.Count == 0)
            return -1;


        var bestMatch = Process.ExtractOne(
            searchTerm,
            candidates,
            s => s.ToLower(),
            ScorerCache.Get<TokenSetScorer>()
        );
        if (bestMatch.Score < 20) return -1;


        return bestMatch.Index;
    }
}