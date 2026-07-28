using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class VehicleColorService(ApplicationDbContext context) : IVehicleColorService
{
    private static readonly List<VehicleColor> SeedColors =
    [
        new() { Name = "Black", NameAr = "أسود", HexCode = "#000000", SortOrder = 1 },
        new() { Name = "Jet Black", NameAr = "أسود جيت", HexCode = "#0A0A0A", SortOrder = 2 },
        new() { Name = "Obsidian Black", NameAr = "أسود أوبسيديان", HexCode = "#1C1C1C", SortOrder = 3 },
        new() { Name = "Midnight Black", NameAr = "أسود منتصف الليل", HexCode = "#191970", SortOrder = 4 },
        new() { Name = "Graphite", NameAr = "جرافيت", HexCode = "#383838", SortOrder = 5 },
        new() { Name = "Charcoal", NameAr = "فحمي", HexCode = "#36454F", SortOrder = 6 },
        new() { Name = "Dark Gray", NameAr = "رمادي غامق", HexCode = "#555555", SortOrder = 7 },
        new() { Name = "Gray", NameAr = "رمادي", HexCode = "#808080", SortOrder = 8 },
        new() { Name = "Silver", NameAr = "فضي", HexCode = "#C0C0C0", SortOrder = 9 },
        new() { Name = "Brilliant Silver", NameAr = "فضي لامع", HexCode = "#C8C8C8", SortOrder = 10 },
        new() { Name = "Aluminum", NameAr = "ألمنيوم", HexCode = "#A9A9A9", SortOrder = 11 },
        new() { Name = "Titanium", NameAr = "تيتانيوم", HexCode = "#878681", SortOrder = 12 },
        new() { Name = "Gunmetal", NameAr = "رمادي معدني", HexCode = "#2A3439", SortOrder = 13 },
        new() { Name = "Nardo Gray", NameAr = "رمادي ناردو", HexCode = "#8D9093", SortOrder = 14 },
        new() { Name = "White", NameAr = "أبيض", HexCode = "#FFFFFF", SortOrder = 15 },
        new() { Name = "Pearl White", NameAr = "أبيض لؤلؤي", HexCode = "#F8F8FF", SortOrder = 16 },
        new() { Name = "Ivory White", NameAr = "أبيض عاجي", HexCode = "#FFFFF0", SortOrder = 17 },
        new() { Name = "Snow White", NameAr = "أبيض ثلجي", HexCode = "#FFFAFA", SortOrder = 18 },
        new() { Name = "Cream", NameAr = "كريمي", HexCode = "#FFFDD0", SortOrder = 19 },
        new() { Name = "Beige", NameAr = "بيج", HexCode = "#F5F5DC", SortOrder = 20 },
        new() { Name = "Champagne", NameAr = "شمباني", HexCode = "#F7E7CE", SortOrder = 21 },
        new() { Name = "Gold", NameAr = "ذهبي", HexCode = "#FFD700", SortOrder = 22 },
        new() { Name = "Rose Gold", NameAr = "ذهب وردي", HexCode = "#B76E79", SortOrder = 23 },
        new() { Name = "Bronze", NameAr = "برونزي", HexCode = "#CD7F32", SortOrder = 24 },
        new() { Name = "Copper", NameAr = "نحاسي", HexCode = "#B87333", SortOrder = 25 },
        new() { Name = "Brown", NameAr = "بني", HexCode = "#8B4513", SortOrder = 26 },
        new() { Name = "Chocolate Brown", NameAr = "بني شوكولاتة", HexCode = "#7B3F00", SortOrder = 27 },
        new() { Name = "Mocha", NameAr = "موكا", HexCode = "#967969", SortOrder = 28 },
        new() { Name = "Mahogany", NameAr = "ماهوجني", HexCode = "#C04000", SortOrder = 29 },
        new() { Name = "Red", NameAr = "أحمر", HexCode = "#FF0000", SortOrder = 30 },
        new() { Name = "Bright Red", NameAr = "أحمر فاقع", HexCode = "#FF2400", SortOrder = 31 },
        new() { Name = "Candy Red", NameAr = "أحمر حلوى", HexCode = "#D2042D", SortOrder = 32 },
        new() { Name = "Ruby Red", NameAr = "أحمر ياقوتي", HexCode = "#9B111E", SortOrder = 33 },
        new() { Name = "Crimson", NameAr = "أحمر قرمزي", HexCode = "#DC143C", SortOrder = 34 },
        new() { Name = "Burgundy", NameAr = "بورجوندي", HexCode = "#800020", SortOrder = 35 },
        new() { Name = "Maroon", NameAr = "أحمر كستنائي", HexCode = "#800000", SortOrder = 36 },
        new() { Name = "Wine Red", NameAr = "أحمر خمري", HexCode = "#722F37", SortOrder = 37 },
        new() { Name = "Orange", NameAr = "برتقالي", HexCode = "#FFA500", SortOrder = 38 },
        new() { Name = "Burnt Orange", NameAr = "برتقالي محروق", HexCode = "#CC5500", SortOrder = 39 },
        new() { Name = "Copper Orange", NameAr = "برتقالي نحاسي", HexCode = "#DA8A67", SortOrder = 40 },
        new() { Name = "Yellow", NameAr = "أصفر", HexCode = "#FFFF00", SortOrder = 41 },
        new() { Name = "Canary Yellow", NameAr = "أصفر كناري", HexCode = "#FFEF00", SortOrder = 42 },
        new() { Name = "Lemon Yellow", NameAr = "أصفر ليموني", HexCode = "#FFF44F", SortOrder = 43 },
        new() { Name = "Mustard", NameAr = "خردلي", HexCode = "#E1AD01", SortOrder = 44 },
        new() { Name = "Green", NameAr = "أخضر", HexCode = "#008000", SortOrder = 45 },
        new() { Name = "British Racing Green", NameAr = "أخضر سباق بريطاني", HexCode = "#004225", SortOrder = 46 },
        new() { Name = "Forest Green", NameAr = "أخضر غابي", HexCode = "#228B22", SortOrder = 47 },
        new() { Name = "Dark Green", NameAr = "أخضر غامق", HexCode = "#006400", SortOrder = 48 },
        new() { Name = "Olive Green", NameAr = "أخضر زيتوني", HexCode = "#556B2F", SortOrder = 49 },
        new() { Name = "Lime Green", NameAr = "أخضر ليموني", HexCode = "#32CD32", SortOrder = 50 },
        new() { Name = "Mint Green", NameAr = "أخضر نعناعي", HexCode = "#98FF98", SortOrder = 51 },
        new() { Name = "Emerald Green", NameAr = "أخضر زمردي", HexCode = "#50C878", SortOrder = 52 },
        new() { Name = "Blue", NameAr = "أزرق", HexCode = "#0000FF", SortOrder = 53 },
        new() { Name = "Navy Blue", NameAr = "أزرق كحلي", HexCode = "#000080", SortOrder = 54 },
        new() { Name = "Dark Blue", NameAr = "أزرق غامق", HexCode = "#00008B", SortOrder = 55 },
        new() { Name = "Royal Blue", NameAr = "أزرق ملكي", HexCode = "#4169E1", SortOrder = 56 },
        new() { Name = "Electric Blue", NameAr = "أزرق كهربائي", HexCode = "#7DF9FF", SortOrder = 57 },
        new() { Name = "Sky Blue", NameAr = "أزرق سماوي", HexCode = "#87CEEB", SortOrder = 58 },
        new() { Name = "Light Blue", NameAr = "أزرق فاتح", HexCode = "#ADD8E6", SortOrder = 59 },
        new() { Name = "Aqua Blue", NameAr = "أزرق مائي", HexCode = "#00FFFF", SortOrder = 60 },
        new() { Name = "Teal", NameAr = "شرشيري", HexCode = "#008080", SortOrder = 61 },
        new() { Name = "Turquoise", NameAr = "فيروزي", HexCode = "#40E0D0", SortOrder = 62 },
        new() { Name = "Cyan", NameAr = "سيان", HexCode = "#00FFFF", SortOrder = 63 },
        new() { Name = "Purple", NameAr = "أرجواني", HexCode = "#800080", SortOrder = 64 },
        new() { Name = "Deep Purple", NameAr = "أرجواني غامق", HexCode = "#673AB7", SortOrder = 65 },
        new() { Name = "Violet", NameAr = "بنفسجي", HexCode = "#8F00FF", SortOrder = 66 },
        new() { Name = "Lavender", NameAr = "خزامي", HexCode = "#E6E6FA", SortOrder = 67 },
        new() { Name = "Plum", NameAr = "برقوقي", HexCode = "#8E4585", SortOrder = 68 },
        new() { Name = "Magenta", NameAr = "ماجنتا", HexCode = "#FF00FF", SortOrder = 69 },
        new() { Name = "Pink", NameAr = "وردي", HexCode = "#FFC0CB", SortOrder = 70 },
        new() { Name = "Hot Pink", NameAr = "وردي فاقع", HexCode = "#FF69B4", SortOrder = 71 },
        new() { Name = "Coral", NameAr = "مرجاني", HexCode = "#FF7F50", SortOrder = 72 },
        new() { Name = "Pearl Blue", NameAr = "أزرق لؤلؤي", HexCode = "#6A8DFF", SortOrder = 73 },
        new() { Name = "Pearl Black", NameAr = "أسود لؤلؤي", HexCode = "#1A1A1A", SortOrder = 74 },
        new() { Name = "Pearl Red", NameAr = "أحمر لؤلؤي", HexCode = "#AA0114", SortOrder = 75 },
        new() { Name = "Pearl Gray", NameAr = "رمادي لؤلؤي", HexCode = "#B0B0B0", SortOrder = 76 },
        new() { Name = "Metallic Silver", NameAr = "فضي معدني", HexCode = "#BFC1C2", SortOrder = 77 },
        new() { Name = "Metallic Gray", NameAr = "رمادي معدني", HexCode = "#6E7072", SortOrder = 78 },
        new() { Name = "Metallic Blue", NameAr = "أزرق معدني", HexCode = "#3B6EA5", SortOrder = 79 },
        new() { Name = "Metallic Green", NameAr = "أخضر معدني", HexCode = "#2E8B57", SortOrder = 80 },
        new() { Name = "Metallic Red", NameAr = "أحمر معدني", HexCode = "#B22222", SortOrder = 81 },
        new() { Name = "Metallic Brown", NameAr = "بني معدني", HexCode = "#8B5A2B", SortOrder = 82 },
        new() { Name = "Metallic Bronze", NameAr = "برونزي معدني", HexCode = "#8C7853", SortOrder = 83 },
        new() { Name = "Matte Black", NameAr = "أسود مطفي", HexCode = "#121212", SortOrder = 84 },
        new() { Name = "Matte Gray", NameAr = "رمادي مطفي", HexCode = "#696969", SortOrder = 85 },
        new() { Name = "Matte White", NameAr = "أبيض مطفي", HexCode = "#F5F5F5", SortOrder = 86 },
        new() { Name = "Matte Blue", NameAr = "أزرق مطفي", HexCode = "#1E3A8A", SortOrder = 87 },
        new() { Name = "Matte Green", NameAr = "أخضر مطفي", HexCode = "#355E3B", SortOrder = 88 },
        new() { Name = "Matte Red", NameAr = "أحمر مطفي", HexCode = "#8B0000", SortOrder = 89 },
        new() { Name = "Satin Black", NameAr = "أسود ساتان", HexCode = "#242424", SortOrder = 90 },
        new() { Name = "Satin Silver", NameAr = "فضي ساتان", HexCode = "#AFAFAF", SortOrder = 91 },
        new() { Name = "Satin Blue", NameAr = "أزرق ساتان", HexCode = "#3A5FCD", SortOrder = 92 },
        new() { Name = "Satin Gray", NameAr = "رمادي ساتان", HexCode = "#7E7F7F", SortOrder = 93 },
        new() { Name = "Two-Tone Black/White", NameAr = "ثنائي اللون أسود/أبيض", HexCode = "#808080", SortOrder = 94 },
        new() { Name = "Two-Tone Red/Black", NameAr = "ثنائي اللون أحمر/أسود", HexCode = "#990000", SortOrder = 95 },
        new() { Name = "Two-Tone Blue/White", NameAr = "ثنائي اللون أزرق/أبيض", HexCode = "#4F81BD", SortOrder = 96 },
        new() { Name = "Custom", NameAr = "مخصص", HexCode = "#FFFFFF", SortOrder = 97 },
        new() { Name = "Other", NameAr = "آخر", HexCode = "#999999", SortOrder = 98 },
        new() { Name = "Unknown", NameAr = "غير معروف", HexCode = "#CCCCCC", SortOrder = 99 }
    ];

    public async Task<List<VehicleColor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!await context.VehicleColors.AnyAsync(cancellationToken))
        {
            context.VehicleColors.AddRange(SeedColors);
            await context.SaveChangesAsync(cancellationToken);
        }

        return await context.VehicleColors
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
