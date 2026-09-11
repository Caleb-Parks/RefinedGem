using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var sts2 = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var d = new CSharpDecompiler(sts2, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
foreach (var t in d.TypeSystem.GetAllTypeDefinitions().Where(t => t.Name == "CardCreationResult"))
    Console.WriteLine(t.FullName);

var m = d.TypeSystem.FindType(new FullTypeName("MegaCrit.Sts2.Core.Factories.CardFactory")).GetDefinition();
foreach (var method in m.Methods.Where(x => x.Name == "CreateForReward" && x.Parameters.Count == 3))
    Console.WriteLine(method.ReturnType.ReflectionName + " :: " + method);

var opt = d.TypeSystem.FindType(new FullTypeName("MegaCrit.Sts2.Core.Runs.CardCreationOptions")).GetDefinition();
foreach (var method in opt.Methods.Where(x => x.Name.Contains("Rarity") || x.Name.Contains("With")))
    Console.WriteLine(method);
