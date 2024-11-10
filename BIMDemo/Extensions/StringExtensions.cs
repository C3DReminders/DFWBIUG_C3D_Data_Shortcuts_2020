using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.Extensions
{
    public static class StringExtensions
    {
        // LevenshteinDistance
        // Calculate the Levenshtein Distance between two strings (the number of insertions,
        // deletions, and substitutions needed to transform the first string into the second)
        // https://stackoverflow.com/questions/5859561/getting-the-closest-string-match
        public static int LevenshteinDistance(this string s1, string s2)
        {
            // Lengths of input strings
            int len1 = s1.Length;
            int len2 = s2.Length;

            // Initialize the distance matrix
            int[,] d = new int[len1 + 1, len2 + 1];

            // Fill the first row and column with incremental distances
            for (int i = 0; i <= len1; i++)
            {
                d[i, 0] = i; // Cost of deletions from s1 to match an empty s2
            }

            for (int j = 0; j <= len2; j++)
            {
                d[0, j] = j; // Cost of insertions to transform an empty s1 to match s2
            }

            // Calculate distances for each substring combination
            for (int j = 1; j <= len2; j++)
            {
                for (int i = 1; i <= len1; i++)
                {
                    // Determine substitution cost
                    int cost = string.Compare(s1[i - 1].ToString(), s2[j - 1].ToString(), StringComparison.OrdinalIgnoreCase) == 0 ? 0 : 1;

                    // Calculate cost for each possible operation
                    int insertCost = d[i - 1, j] + 1;       // Cost of insertion
                    int deleteCost = d[i, j - 1] + 1;       // Cost of deletion
                    int substituteCost = d[i - 1, j - 1] + cost; // Cost of substitution

                    // Choose the minimum cost among insert, delete, or substitute
                    d[i, j] = Math.Min(Math.Min(insertCost, deleteCost), substituteCost);
                }
            }

            // Return the final Levenshtein distance between s1 and s2
            return d[len1, len2];
        }

        public static int LevenshteinDistanceOmitMatch(this string s1, string s2)
        {
            if (s1.Equals(s2, StringComparison.OrdinalIgnoreCase))
            {
                return -100;
            }

            return LevenshteinDistance(s1, s2);
        }
    }
}
