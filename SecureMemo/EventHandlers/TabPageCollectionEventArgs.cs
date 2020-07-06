using System;

namespace SecureMemo.EventHandlers
{
    public class TabPageCollectionEventArgs : EventArgs
    {
        public static readonly TabPageCollectionEventArgs None = new TabPageCollectionEventArgs(); //.ActiveChangeChange=TabPageCollectionStates.TabPageCollectionStateChange.None;

        private TabPageCollectionEventArgs()
        {
        }

        public TabPageCollectionEventArgs(TabPageCollectionStateChange activeChange)
        {
            ActiveChange = activeChange;
        }

        public object Sender { get; set; }
        public TabPageCollectionStateChange ActiveChange { get; set; }
    }

    public class TabPageCollectionEventHandler
    {
        public object Sender;
        private TabPageCollectionEventArgs TabEventsEmpty = TabPageCollectionEventArgs.None;


        public TabPageCollectionEventHandler(object sender, TabPageCollectionEventArgs tabEventArgs)
        {
            TabEventArgs = tabEventArgs;
            Sender = sender;
        }

        private TabPageCollectionEventArgs TabEventArgs { get; set; }
    }

    public class ActivatePageIndexChangedArgs
    {
        public static ActivatePageIndexChangedArgs None = new ActivatePageIndexChangedArgs();

        public ActivatePageIndexChangedArgs(int previousIndex, int currentIndex)
        {
            PreviousIndex = previousIndex;
            CurrentIndex = currentIndex;
        }

        public ActivatePageIndexChangedArgs()
        {
        }

        public int PreviousIndex { get; private set; }
        public int CurrentIndex { get; private set; }
    }
}