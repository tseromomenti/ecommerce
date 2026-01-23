namespace InventoryService.Business.Entities;

public static class ProductImageResolver
{
    public static string GetImageUrl(string productName)
    {
        var name = productName.ToLowerInvariant();

        if (name.Contains("mouse"))
        {
            return "/api/inventory/images/mouse.svg";
        }

        if (name.Contains("keyboard"))
        {
            return "/api/inventory/images/keyboard.svg";
        }

        if (name.Contains("monitor") || name.Contains("display") || name.Contains("screen"))
        {
            return "/api/inventory/images/monitor.svg";
        }

        return "/api/inventory/images/placeholder.svg";
    }
}
