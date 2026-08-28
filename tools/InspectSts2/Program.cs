using System.Reflection;

var dir = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var resolver = new PathAssemblyResolver(Directory.GetFiles(dir, "*.dll"));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(Path.Combine(dir, "sts2.dll"));

var lib = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary")!;
foreach (var f in lib.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
{
    if (f.Name.StartsWith("_view") || f.Name is "_searchBar" or "_cardCountLabel" or "_grid")
        Console.WriteLine($"{f.Name} -> {f.FieldType.Name}");
}

var tick = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NLibraryStatTickbox")!;
Console.WriteLine("tickbox base: " + tick.BaseType?.Name);
