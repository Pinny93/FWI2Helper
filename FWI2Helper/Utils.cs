using System;
using System.Text;

namespace FWI2Helper
{
    public static class Utils
    {
        private static readonly Random s_randomizer = new();

        public static T GetRandomElement<T>(this IReadOnlyList<T> list)
        {
            int index = s_randomizer.Next(0, list.Count);

            return list[index];
        }

        public static string ToCommaSeparatedString(this IEnumerable<string> collection)
        {
            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var curItem in collection)
            {
                if (!first) { builder.Append(", "); }
                else { first = false; }
                builder.Append(curItem);
            }

            return builder.ToString();
        }

        public static IEnumerable<TItem> GetMinimumItems<TItem, TSelector>(this IEnumerable<TItem> enumerable, Func<TItem, TSelector> selector, IComparer<TSelector> comparer = null)
        {
            return enumerable.GetExtremaItems(selector, comparer, (result) => result == 1);
        }

        public static IEnumerable<TItem> GetMaximumItems<TItem, TSelector>(this IEnumerable<TItem> enumerable, Func<TItem, TSelector> selector, IComparer<TSelector> comparer = null)
        {
            return enumerable.GetExtremaItems(selector, comparer, (result) => result == -1);
        }

        private static IEnumerable<TItem> GetExtremaItems<TItem, TSelector>(this IEnumerable<TItem> enumerable, Func<TItem, TSelector> selector, IComparer<TSelector> comparer, Func<int, bool> clearListCondition)
        {
            if (selector == null) { throw new ArgumentNullException(nameof(selector)); }

            List<TItem> extremaItems = new List<TItem>();
            if (comparer == null)
            {
                comparer = Comparer<TSelector>.Default;
            }

            foreach (TItem curItem in enumerable)
            {
                if (extremaItems.Count == 0)
                {
                    extremaItems.Add(curItem);
                    continue;
                }

                int compareResult = comparer.Compare(selector(extremaItems[0]), selector(curItem));

                if (compareResult == 0)
                {
                    extremaItems.Add(curItem);
                }

                if (clearListCondition(compareResult))
                {
                    extremaItems.Clear();
                    extremaItems.Add(curItem);
                }
            }

            return extremaItems;
        }
    }
}