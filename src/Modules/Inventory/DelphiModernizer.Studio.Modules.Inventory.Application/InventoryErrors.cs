using DelphiModernizer.Engine.Core;

namespace DelphiModernizer.Studio.Modules.Inventory.Application;

public static class InventoryErrors
{
    public static readonly Error NotScanned = new("Inventory.NotScanned", "The legacy source has not been scanned yet.");

    public static Error Unreadable(string message)
    {
        return new Error("Inventory.InventoryUnreadable", $"The inventory could not be read: {message}");
    }
}
