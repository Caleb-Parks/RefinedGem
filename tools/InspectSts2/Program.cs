using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
var sts2Path = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var decompiler = new CSharpDecompiler(sts2Path, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
foreach (var name in new[] { "MegaCrit.Sts2.Core.Modding.ModManager", "MegaCrit.Sts2.Core.Modding.ModInfo", "MegaCrit.Sts2.Core.Modding.ModRegistry" }) {
  var type = decompiler.TypeSystem.FindType(new FullTypeName(name)).GetDefinition();
  if (type is null) { Console.WriteLine("MISSING " + name); continue; }
  Console.WriteLine("TYPE " + name);
  foreach (var m in type.Methods.Where(m => m.IsPublic && !m.IsConstructor)) Console.WriteLine("  " + m);
  foreach (var p in type.Properties.Where(p => p.IsPublic)) Console.WriteLine("  " + p);
}
