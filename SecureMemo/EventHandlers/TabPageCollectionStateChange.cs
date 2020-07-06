using System;

namespace SecureMemo.EventHandlers
{
    [Flags]
    public enum TabPageCollectionStateChange
    {
        None = 0x0,
        PageAdded = 0b1,
        PageRemoved = 0b10,
        PageShifted = 0b100,
        PageShiftedPosition = 0b1000,
        PageTitleModified = 0b10000,
        NewDatabaseCreated = 0b100000,
        PageLabelChanged = 0b1000000
    }
}