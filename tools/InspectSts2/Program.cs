using System.Reflection;

var sts2Dir = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var resolver = new PathAssemblyResolver(Directory.GetFiles(sts2Dir, "*.dll"));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(Path.Combine(sts2Dir, "sts2.dll"));

var lib = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary")!;
foreach (var name in new[] { "UpdateCardPoolFilter", "UpdateFilter", "DisplayCards", "DisplayCardsAfterShortDelay" })
{
    var m = lib.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
    Console.WriteLine($"=== {name} ===");
    foreach (var p in m.GetParameters())
        Console.WriteLine($"  {p.ParameterType.Name} {p.Name}");
}

var filter = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardPoolFilter")!;
foreach (var p in filter.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    Console.WriteLine($"FILTER PROP {p.PropertyType.Name} {p.Name}");
