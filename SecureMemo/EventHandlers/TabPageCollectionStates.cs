using System;
using System.Collections.Generic;
using SecureMemo.DataModels;

namespace SecureMemo.EventHandlers
{
    public class TabPageCollectionStates
    {
        private readonly Stack<TabPageCollectionStateChange> _changeStack;
        private TabPageCollectionStateChange _changesMadeInEdit = TabPageCollectionStateChange.None;
        private List<TabEditorOriginalElementState> originalStateList;

        public TabPageCollectionStates()
        {
            _changeStack = new Stack<TabPageCollectionStateChange>();
            originalStateList = new List<TabEditorOriginalElementState>();
        }


        private int PageAddedCount { get; set; }
        private int PageRemovedCount { get; set; }
        private int PageEventsCount { get; set; }

        public TabPageCollectionStateChange ChangesMadeInEdit => _changesMadeInEdit;

        public void PushChange(TabPageCollectionStateChange item)
        {
            _changeStack.Push(item);
            _changesMadeInEdit = _changesMadeInEdit | item;

            if (item == TabPageCollectionStateChange.PageAdded) PageAddedCount++;

            if (item == TabPageCollectionStateChange.PageRemoved) PageRemovedCount++;
        }

        public void SetInitialState(IEnumerable<TabPageData> tabPageDataCollection)
        {
            originalStateList = new List<TabEditorOriginalElementState>();
            foreach (TabPageData pageData in tabPageDataCollection) originalStateList.Add(new TabEditorOriginalElementState(pageData));
        }

        public TabPageCollectionStateChange GetChanges(IEnumerable<TabPageData> tabPageDataCollection, out List<TabEditorOriginalElementState> originalStates)
        {
            TabPageCollectionStateChange stateChange = TabPageCollectionStateChange.None;

            while (_changeStack.Count > 0)
            {
                TabPageCollectionStateChange change = _changeStack.Pop();
                stateChange |= change;
            }

            originalStates = originalStateList;

            return stateChange;
        }
    }

    public sealed class TabEditorOriginalElementState : IEqualityComparer<TabEditorOriginalElementState>, IComparable<TabEditorOriginalElementState>
    {
        public TabEditorOriginalElementState(string uniqueID)
        {
            UniqueID = uniqueID;
        }

        public TabEditorOriginalElementState(TabPageData pageData)
        {
            UniqueID = pageData.UniqueId;
            PageIndex = pageData.PageIndex;
            LabelText = pageData.TabPageLabel;
        }

        public string UniqueID { get; private set; }
        public int PageIndex { get; set; }
        public string LabelText { get; set; }

        public int CompareTo(TabEditorOriginalElementState other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var uniqueIdComparison = UniqueID.CompareTo(other.UniqueID);
            if (uniqueIdComparison != 0) return uniqueIdComparison;
            var pageIndexComparison = PageIndex.CompareTo(other.PageIndex);
            if (pageIndexComparison != 0) return pageIndexComparison;
            return string.Compare(LabelText, other.LabelText, StringComparison.Ordinal);
        }

        public bool Equals(TabEditorOriginalElementState x, TabEditorOriginalElementState y)
        {
            return x.UniqueID == y.UniqueID;
        }

        public int GetHashCode(TabEditorOriginalElementState obj)
        {
            return obj.UniqueID.GetHashCode();
        }
    }
}