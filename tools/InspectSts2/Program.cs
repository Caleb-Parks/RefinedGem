using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var sts2Path = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var decompiler = new CSharpDecompiler(sts2Path, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
var type = decompiler.TypeSystem.FindType(new FullTypeName("MegaCrit.Sts2.Core.Entities.Merchant.MerchantCardEntry")).GetDefinition()!;
foreach (var p in type.Properties)
  Console.WriteLine("PROP " + p);
foreach (var f in type.Fields)
  Console.WriteLine("FIELD " + f);
