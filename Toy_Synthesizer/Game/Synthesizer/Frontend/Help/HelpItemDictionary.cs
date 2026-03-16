using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Help
{
    public sealed class HelpItemDictionary : IDictionary<string, HelpItem>
    {
        private readonly Dictionary<string, HelpItem> items;

        public HelpItemDictionary(HelpItem[] items)
        {
            this.items = new Dictionary<string, HelpItem>(items.Length);

            for (int index = 0; index != items.Length; index++)
            {
                Add(items[index]);
            }
        }

        public HelpItem this[string key]
        {
            get => items[key];
            set => items[key] = value;
        }

        public ICollection<string> Keys
        {
            get => items.Keys;
        }

        public ICollection<HelpItem> Values
        {
            get => items.Values;
        }

        public int Count
        {
            get => items.Count;
        }

        public bool IsReadOnly
        {
            get => false;
        }

        public void Add(HelpItem item)
        {
            Add(item.Name, item);
        }

        public void Add(string key, HelpItem value)
        {
            items.Add(key, value);
        }

        public void Add(KeyValuePair<string, HelpItem> item)
        {
            items.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(KeyValuePair<string, HelpItem> item)
        {
            return items.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return items.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, HelpItem>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<string, HelpItem> item in items)
            {
                if (arrayIndex > array.Length)
                {
                    return;
                }

                array[arrayIndex++] = item;
            }
        }

        public bool Remove(string key)
        {
            return items.Remove(key);
        }

        public bool Remove(KeyValuePair<string, HelpItem> item)
        {
            return items.Remove(item.Key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out HelpItem value)
        {
            return items.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, HelpItem>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void DisplayAllWithChildren(StringBuilder builder, int newLinesBefore, int newLinesAfter)
        {
            foreach (HelpItem item in Values)
            {
                item.AppendToBuilder(builder, 0, newLinesBefore, newLinesAfter);

                if (item.ChildItems is not null && item.ChildItems.Count != 0)
                {
                    for (int childIndex = 0; childIndex != item.ChildItems.Count; childIndex++)
                    {
                        item.ChildItems[childIndex].AppendToBuilder(builder, 0, 0, newLinesAfter);
                    }
                }
            }
        }

        public void DisplayAllNoChildren(StringBuilder builder, int newLinesBefore, int newLinesAfter)
        {
            foreach (HelpItem item in Values)
            {
                item.AppendToBuilder(builder, 0, newLinesBefore, newLinesAfter);
            }
        }
    }
}
