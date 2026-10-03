using System;

namespace SecureMemo.EventHandlers
{
    public class TabPageCollectionEventArgs : EventArgs
    {
        public TabPageCollectionEventArgs(TabPageCollectionStateChange activeChange)
        {
            ActiveChange = activeChange;
        }

        public TabPageCollectionStateChange ActiveChange { get; set; }
    }

    public class ActivatePageIndexChangedArgs
    {
        public ActivatePageIndexChangedArgs(int previousIndex, int currentIndex)
        {
            PreviousIndex = previousIndex;
            CurrentIndex = currentIndex;
        }

        public int PreviousIndex { get; private set; }
        public int CurrentIndex { get; private set; }
    }
}
