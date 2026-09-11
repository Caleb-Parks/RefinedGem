using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var sts2 = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var d = new CSharpDecompiler(sts2, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
var inv = d.DecompileTypeAsString(new FullTypeName("MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory"));
var idx = inv.IndexOf("_coloredCardTypes", StringComparison.Ordinal);
Console.WriteLine(inv.Substring(Math.Max(0, idx - 200), Math.Min(800, inv.Length - Math.Max(0, idx - 200))));
