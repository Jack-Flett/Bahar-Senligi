using System;
using System.Collections.Generic;
using System.Linq;
using BaharSenligi.Models;

namespace BaharSenligi.Services
{
    public class SelectionResult
    {
        public List<Question> Questions { get; set; } = new();
        public string? Warning { get; set; }
    }

    public class QuestionSelector
    {
        private readonly Random _rng = new();

        /// <summary>
        /// Selects `count` questions balanced across selected categories.
        /// Fallback chain:
        ///   1. Fresh questions (not in askedIds)
        ///   2. Already-asked questions recycled
        ///   3. Borrow from other selected categories
        ///   4. Warn that fewer questions are available
        /// </summary>
        public SelectionResult Select(
            List<Question> allQuestions,
            List<Guid> selectedCategoryIds,
            int count,
            HashSet<Guid>? askedIds = null)
        {
            askedIds ??= new HashSet<Guid>();

            var pool = allQuestions
                .Where(q => selectedCategoryIds.Contains(q.CategoryId))
                .ToList();

            if (pool.Count == 0)
                return new SelectionResult { Warning = "Seçili kategorilerde hiç soru yok." };

            var fresh = pool.Where(q => !askedIds.Contains(q.Id)).ToList();
            var used = pool.Where(q => askedIds.Contains(q.Id)).ToList();

            var warnings = new List<string>();
            var selected = new List<Question>();

            // How many per category ideally
            int catCount = selectedCategoryIds.Count;
            int perCat = count / catCount;
            int remainder = count % catCount;

            var freshByCat = selectedCategoryIds.ToDictionary(
                id => id,
                id => fresh.Where(q => q.CategoryId == id).OrderBy(_ => _rng.Next()).ToList());

            var usedByCat = selectedCategoryIds.ToDictionary(
                id => id,
                id => used.Where(q => q.CategoryId == id).OrderBy(_ => _rng.Next()).ToList());

            // Collect per category
            var catTargets = selectedCategoryIds
                .Select((id, i) => (id, target: perCat + (i < remainder ? 1 : 0)))
                .ToList();

            var leftovers = new List<Question>(); // excess fresh questions available for borrowing

            foreach (var (id, target) in catTargets)
            {
                var picks = new List<Question>();
                var f = freshByCat[id];
                var u = usedByCat[id];

                // Take fresh first
                int take = Math.Min(target, f.Count);
                picks.AddRange(f.Take(take));

                if (picks.Count < target)
                {
                    // Recycle used
                    int need = target - picks.Count;
                    int recycled = Math.Min(need, u.Count);
                    if (recycled > 0)
                    {
                        picks.AddRange(u.Take(recycled));
                        warnings.Add($"Bazı sorular daha önce sorulmuş ({id})");
                    }
                }

                selected.AddRange(picks);

                // Track excess fresh for borrowing
                if (f.Count > target)
                    leftovers.AddRange(f.Skip(target));
            }

            // If still short, borrow from leftover fresh of other categories
            if (selected.Count < count)
            {
                int need = count - selected.Count;
                var borrow = leftovers
                    .Where(q => !selected.Contains(q))
                    .OrderBy(_ => _rng.Next())
                    .Take(need)
                    .ToList();

                if (borrow.Count > 0)
                {
                    selected.AddRange(borrow);
                    warnings.Add("Bazı kategorilerden ekstra soru alındı (eşit dağılım mümkün değildi).");
                }
            }

            // Still short? Borrow used from other cats
            if (selected.Count < count)
            {
                int need = count - selected.Count;
                var borrowUsed = usedByCat.Values
                    .SelectMany(x => x)
                    .Where(q => !selected.Contains(q))
                    .OrderBy(_ => _rng.Next())
                    .Take(need)
                    .ToList();

                if (borrowUsed.Count > 0)
                {
                    selected.AddRange(borrowUsed);
                    warnings.Add("Daha önce sorulmuş sorular tekrar eklendi.");
                }
            }

            string? warning = null;
            if (selected.Count < count)
            {
                warning = $"Yalnızca {selected.Count} soru mevcut, yarışma {selected.Count} soruyla yapılacak.";
            }
            else if (warnings.Count > 0)
            {
                warning = string.Join(" ", warnings.Distinct());
            }

            // Final shuffle
            selected = selected.OrderBy(_ => _rng.Next()).ToList();

            return new SelectionResult { Questions = selected, Warning = warning };
        }
    }
}
