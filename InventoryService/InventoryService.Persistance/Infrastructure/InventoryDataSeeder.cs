using InventoryService.Persistance.Dtos;

namespace InventoryService.Persistance.Infrastructure;

public static class InventoryDataSeeder
{
    public static void Seed(InventoryDbContext context)
    {
        if (context.Products.Any())
        {
            return;
        }

        var random = new Random(42);
        var now = DateTime.UtcNow;
        var skuCounter = 1;
        var products = new List<ProductEntity>();

        products.AddRange(GenerateClothing(random, now, ref skuCounter));
        products.AddRange(GenerateElectronics(random, now, ref skuCounter));
        products.AddRange(GenerateGroceries(random, now, ref skuCounter));
        products.AddRange(GenerateHousehold(random, now, ref skuCounter));

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    private static List<ProductEntity> GenerateClothing(Random random, DateTime now, ref int skuCounter)
    {
        var brands = new[] { "NorthPeak", "UrbanTrail", "FitLine", "SnowEdge" };
        var colors = new[] { "black", "navy", "gray", "olive" };
        var products = new List<ProductEntity>();
        var items = new (string Name, string Subcategory, decimal BasePrice, string Tags)[]
        {
            ("Winter Jacket", "outerwear", 89m, "warm,winter,jacket,coat,insulated"),
            ("Fleece Hoodie", "tops", 42m, "warm,hoodie,fleece,casual"),
            ("Thermal Pants", "bottoms", 39m, "warm,thermal,pants,winter"),
            ("Rainproof Coat", "outerwear", 75m, "coat,rainproof,weather,waterproof"),
            ("Trail Sneakers", "footwear", 62m, "sneakers,shoes,outdoor,comfort"),
            ("Wool Beanie", "accessories", 19m, "hat,wool,winter,warm")
        };

        foreach (var brand in brands)
        {
            foreach (var item in items)
            {
                foreach (var color in colors)
                {
                    var name = $"{brand} {color} {item.Name}";
                    products.Add(BuildProduct(
                        ref skuCounter,
                        name,
                        $"Comfort-focused {item.Name.ToLowerInvariant()} in {color}. Great for cold weather and daily wear.",
                        "clothing",
                        item.Subcategory,
                        brand,
                        $"{item.Tags},{color},clothing",
                        $"{{\"color\":\"{color}\",\"sizeOptions\":[\"S\",\"M\",\"L\",\"XL\"]}}",
                        "USD",
                        item.BasePrice + random.Next(0, 25),
                        random.Next(5, 120),
                        now));
                }
            }
        }

        return products;
    }

    private static List<ProductEntity> GenerateElectronics(Random random, DateTime now, ref int skuCounter)
    {
        var brands = new[] { "Logitech", "Sony", "Anker", "Samsung", "AeroTech", "HyperInput" };
        var variants = new[] { "standard", "pro" };
        var products = new List<ProductEntity>();
        var items = new (string Name, string Subcategory, decimal BasePrice, string Tags)[]
        {
            ("Wireless Mouse", "computer-accessories", 28m, "mouse,wireless,office,gaming"),
            ("Mechanical Keyboard", "computer-accessories", 64m, "keyboard,mechanical,typing,gaming"),
            ("Noise Cancelling Headphones", "audio", 119m, "audio,headphones,wireless,music"),
            ("Portable Charger", "power", 31m, "charger,powerbank,travel,usb"),
            ("Smart Monitor", "display", 189m, "monitor,display,screen,work"),
            ("Bluetooth Speaker", "audio", 45m, "speaker,bluetooth,portable,music")
        };

        foreach (var brand in brands)
        {
            foreach (var item in items)
            {
                foreach (var variant in variants)
                {
                    var name = $"{brand} {item.Name} {variant}";
                    var premium = variant == "pro" ? 1.25m : 1.0m;
                    products.Add(BuildProduct(
                        ref skuCounter,
                        name,
                        $"{item.Name} {variant} edition with balanced performance for home and office.",
                        "electronics",
                        item.Subcategory,
                        brand,
                        $"{item.Tags},{variant},electronics",
                        $"{{\"variant\":\"{variant}\",\"connectivity\":[\"bluetooth\",\"usb-c\"]}}",
                        "USD",
                        Math.Round((item.BasePrice + random.Next(0, 35)) * premium, 2),
                        random.Next(3, 85),
                        now));
                }
            }
        }

        return products;
    }

    private static List<ProductEntity> GenerateGroceries(Random random, DateTime now, ref int skuCounter)
    {
        var brands = new[] { "FreshBox", "GreenBite", "UrbanPantry", "DailyHarvest", "NatureFuel", "SnackHouse" };
        var products = new List<ProductEntity>();
        var items = new (string Name, string Subcategory, decimal BasePrice, string Tags)[]
        {
            ("Protein Bar Pack", "snacks", 12m, "snacks,protein,fitness,healthy"),
            ("Dark Chocolate Bites", "snacks", 8m, "sweet,chocolate,snacks"),
            ("Spicy Trail Mix", "snacks", 10m, "spicy,nuts,snacks"),
            ("Organic Oat Milk", "beverages", 6m, "organic,milk,vegan,drink"),
            ("Cold Brew Coffee", "beverages", 7m, "coffee,drink,energy"),
            ("Instant Ramen Bowl", "ready-meals", 5m, "ramen,instant,quick,meal")
        };

        foreach (var brand in brands)
        {
            foreach (var item in items)
            {
                products.Add(BuildProduct(
                    ref skuCounter,
                    $"{brand} {item.Name}",
                    $"{item.Name} from {brand}, ideal for fast checkout and pantry restock.",
                    "grocery",
                    item.Subcategory,
                    brand,
                    $"{item.Tags},grocery,food",
                    $"{{\"bundleSize\":\"{random.Next(1, 6)}\"}}",
                    "USD",
                    item.BasePrice + random.Next(0, 6),
                    random.Next(10, 220),
                    now));
            }
        }

        return products;
    }

    private static List<ProductEntity> GenerateHousehold(Random random, DateTime now, ref int skuCounter)
    {
        var brands = new[] { "HomeNest", "BrightLiving", "CleanFlow", "DailyRoom" };
        var variants = new[] { "compact", "standard", "premium" };
        var products = new List<ProductEntity>();
        var items = new (string Name, string Subcategory, decimal BasePrice, string Tags)[]
        {
            ("Laundry Detergent", "cleaning", 14m, "cleaning,laundry,home"),
            ("Dish Soap", "cleaning", 6m, "cleaning,dishes,kitchen"),
            ("Air Freshener", "home-care", 9m, "home,freshener,fragrance"),
            ("Storage Box Set", "organization", 18m, "storage,organization,home"),
            ("LED Desk Lamp", "lighting", 22m, "lamp,lighting,desk"),
            ("Microfiber Towels", "home-care", 11m, "towel,cleaning,household")
        };

        foreach (var brand in brands)
        {
            foreach (var item in items)
            {
                foreach (var variant in variants)
                {
                    var multiplier = variant switch
                    {
                        "compact" => 0.9m,
                        "premium" => 1.3m,
                        _ => 1.0m
                    };

                    products.Add(BuildProduct(
                        ref skuCounter,
                        $"{brand} {item.Name} {variant}",
                        $"{variant} {item.Name.ToLowerInvariant()} for reliable daily household use.",
                        "household",
                        item.Subcategory,
                        brand,
                        $"{item.Tags},{variant},household",
                        $"{{\"variant\":\"{variant}\"}}",
                        "USD",
                        Math.Round((item.BasePrice + random.Next(0, 9)) * multiplier, 2),
                        random.Next(8, 150),
                        now));
                }
            }
        }

        return products;
    }

    private static ProductEntity BuildProduct(
        ref int skuCounter,
        string name,
        string description,
        string category,
        string subcategory,
        string brand,
        string tags,
        string attributesJson,
        string currency,
        decimal price,
        int stock,
        DateTime now)
    {
        var sku = $"SKU-{skuCounter:00000}";
        skuCounter++;
        return new ProductEntity
        {
            Sku = sku,
            ProductName = name,
            Description = description,
            Category = category,
            Subcategory = subcategory,
            Brand = brand,
            Tags = tags,
            AttributesJson = attributesJson,
            CurrencyCode = currency,
            IsActive = true,
            AvailableStock = stock,
            ReservedStock = 0,
            Price = price,
            LastUpdated = now
        };
    }
}
