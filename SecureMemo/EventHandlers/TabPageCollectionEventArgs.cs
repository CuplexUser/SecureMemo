using System;
using SecureMemo.Library;

namespace SecureMemo.EventHandlers
{
    public class TabPageCollectionEventArgs : EventArgs
    {
        public static readonly TabPageCollectionEventArgs None = new TabPageCollectionEventArgs(); //.ActiveChangeChange=TabPageCollectionStates.TabPageCollectionStateChange.None;

        private TabPageCollectionEventArgs()
        {

        }

        public TabPageCollectionEventArgs(TabPageCollectionStates.TabPageCollectionStateChange activeChange)
        {
            ActiveChange = activeChange;
        }

        public object Sender { get; set; }
        public TabPageCollectionStates.TabPageCollectionStateChange ActiveChange { get; set; }
    }

    public class TabPageCollectionEventHandler
    {
        private TabPageCollectionEventArgs TabEventArgs { get; set; }
        public object Sender;
        TabPageCollectionEventArgs TabEventsEmpty = TabPageCollectionEventArgs.None;


        public TabPageCollectionEventHandler(object sender, TabPageCollectionEventArgs tabEventArgs)
        {
            TabEventArgs = tabEventArgs;
            Sender = sender;


        }
    }
}