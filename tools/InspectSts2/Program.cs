using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var sts2 = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var d = new CSharpDecompiler(sts2, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
foreach (var t in d.TypeSystem.GetAllTypeDefinitions().Where(t => t.Name == "RoomType"))
    Console.WriteLine(t.FullName);
