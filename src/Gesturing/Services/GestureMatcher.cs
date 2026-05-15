using Gesturing.Models;

namespace Gesturing.Services;

public static class GestureMatcher
{
    public static GestureRule? Match(List<string> sequence, List<GestureRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.GestureSequence.Count != sequence.Count)
            {
                continue;
            }

            if (SequencesEqual(sequence, rule.GestureSequence))
            {
                return rule;
            }
        }

        return null;
    }

    private static bool SequencesEqual(List<string> a, List<string> b)
    {
        for (int i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
