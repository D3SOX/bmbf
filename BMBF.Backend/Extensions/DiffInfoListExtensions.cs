using System.Collections.Generic;
using System.Linq;
using BMBF.Resources;

namespace BMBF.Backend.Extensions;

public static class DiffInfoListExtensions
{
    /// <summary>
    /// Finds the shortest path to move between two versions (i.e. the path that requires the least number of patches)
    /// Note that this might not actually be the quickest path computationally
    /// TODO: This isn't particularly efficient
    /// </summary>
    /// <param name="diffs">The list of diffs to find the shortest path within</param>
    /// <param name="fromVersion">The version being changed from</param>
    /// <param name="toVersion">The version being changed to</param>
    /// <returns>A List of diffs that should be applied in order to move between the two versions, or null if there is no path between the versions</returns>
    public static List<DiffInfo>? FindShortestPath(this List<DiffInfo> diffs, string fromVersion, string toVersion)
    {
        return FindShortestPathInternal(diffs, fromVersion, toVersion, new List<DiffInfo>());
    }

    private static List<DiffInfo>? FindShortestPathInternal(List<DiffInfo> diffs, string fromVersion, string toVersion, List<DiffInfo> currentPath)
    {
        if (fromVersion == toVersion)
        {
            return currentPath;
        }

        List<DiffInfo>? shortestPath = null;
            
        // Find the diffs that can move us somewhere from our current version
        List<DiffInfo> availableDiffsFromPoint = diffs.Where(diff => diff.FromVersion == fromVersion).ToList();
        for(int i = 0; i < availableDiffsFromPoint.Count; i++)
        {
            // If this is the last diff in this stage, then we can safely use the original list (optimisation)
            // Otherwise, we clone the list
            // In most cases, there will only be one diff from each version anyway
            var newPath = i == availableDiffsFromPoint.Count - 1 ? currentPath : currentPath.ToList();

            var currentDiff = availableDiffsFromPoint[i];
                
            // Recursively find the shortest path from this diff
            newPath.Add(currentDiff);
            var resultingPath = FindShortestPathInternal(diffs, currentDiff.ToVersion, toVersion, newPath);
            // Skip if no path found
            if (resultingPath == null) continue;
                
            // Update the shortest path
            if (shortestPath == null || resultingPath.Count < shortestPath.Count)
            {
                shortestPath = resultingPath;
            }
        }

        return shortestPath;
    }
}