using System.Reflection;

var sts2 = Assembly.LoadFrom(@"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll");
var relicModel = sts2.GetType("MegaCrit.Sts2.Core.Models.RelicModel")!;
foreach (var p in relicModel.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
    if (p.Name.Contains("Desc", StringComparison.OrdinalIgnoreCase) || p.Name is "Title" or "Flavor")
        Console.WriteLine(p.PropertyType.Name + " " + p.Name);
}
