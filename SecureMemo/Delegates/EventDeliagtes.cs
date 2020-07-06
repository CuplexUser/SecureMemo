using SecureMemo.EventHandlers;

namespace SecureMemo.Delegates
{
    /// <summary>
    ///     Event delegates
    /// </summary>
    public static class EventDeliagtes
    {
        /// <summary>
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        public delegate void ActivatePageIndexChanged(object sender, ActivatePageIndexChangedArgs args);

        public delegate void InvokeUiThreadUpdate();

        /// <summary>
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="TabPageCollectionEventArgs" /> instance containing the event data.</param>
        public delegate void TabPageCollectionChanged(object sender, TabPageCollectionEventArgs eventArgs);
    }
}