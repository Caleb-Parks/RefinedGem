using System.Reflection;

var sts2Dir = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var resolver = new PathAssemblyResolver(Directory.GetFiles(sts2Dir, "*.dll"));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(Path.Combine(sts2Dir, "sts2.dll"));

var card = asm.GetType("MegaCrit.Sts2.Core.Models.CardModel")!;
foreach (var p in card.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    if (p.Name.Contains("Pool", StringComparison.OrdinalIgnoreCase)
        || p.Name.Contains("Colorless", StringComparison.OrdinalIgnoreCase)
        || p.Name.Contains("Character", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine($"CARD {p.PropertyType.Name} {p.Name}");

var pool = asm.GetType("MegaCrit.Sts2.Core.Models.CardPoolModel")!;
foreach (var p in pool.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    if (p.Name.Contains("Colorless", StringComparison.OrdinalIgnoreCase) || p.Name == "Title")
        Console.WriteLine($"POOL {p.PropertyType.Name} {p.Name}");
