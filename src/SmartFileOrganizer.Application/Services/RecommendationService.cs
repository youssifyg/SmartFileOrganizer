// RecommendationService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Application.Services
{
    /// <summary>
    /// Implements rule‑based scoring to recommend a single file to keep per duplicate group.
    /// Scoring criteria (weights): Preferred folder (+10), Ignored folder (‑10), Newest file (+5), Clean filename (+2).
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly ISettingsProvider _settingsProvider;

        private const int PreferredWeight = 10;
        private const int IgnoredWeight = -10;
        private const int NewestWeight = 5;
        private const int CleanNameWeight = 2;

        public RecommendationService(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public async Task<FileRecord> RecommendAsync(DuplicateGroup group)
        {
            var preferredFolders = await _settingsProvider.GetPreferredFoldersAsync();
            var ignoredFolders = await _settingsProvider.GetIgnoredFoldersAsync();

            var newest = group.Files.OrderByDescending(f => f.Modified).FirstOrDefault();

            FileRecord? best = null;
            int bestScore = int.MinValue;

            foreach (var file in group.Files)
            {
                int score = 0;

                var folder = Path.GetDirectoryName(file.Path) ?? string.Empty;
                if (preferredFolders.Any(p => folder.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    score += PreferredWeight;
                if (ignoredFolders.Any(i => folder.StartsWith(i, StringComparison.OrdinalIgnoreCase)))
                    score += IgnoredWeight;
                if (newest != null && file.Path == newest.Path)
                    score += NewestWeight;

                var fileName = Path.GetFileNameWithoutExtension(file.Path);
                bool hasDigits = fileName.Any(char.IsDigit);
                bool containsTemp = fileName.IndexOf("temp", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!hasDigits && !containsTemp)
                    score += CleanNameWeight;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = file;
                }
            }

            if (best == null)
                throw new InvalidOperationException("Duplicate group contains no files.");

            best.IsRecommended = true;
            return best;
        }
    }
}
