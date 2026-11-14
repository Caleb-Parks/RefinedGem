using System.Reflection;
using System.Runtime.Loader;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

var sts2Path = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll";
var d = new CSharpDecompiler(sts2Path, new DecompilerSettings { ThrowOnAssemblyResolveErrors = false });
var ts = d.TypeSystem;

bool HasMethod(string typeName, string methodName, params string[] paramTypeNames)
{
    var type = ts.FindType(new FullTypeName(typeName)).GetDefinition();
    if (type is null)
    {
        Console.WriteLine($"MISSING TYPE {typeName}");
        return false;
    }

    var methods = type.Methods.Where(m =>
        (methodName == ".ctor" && m.IsConstructor) || m.Name == methodName);

    foreach (var m in methods)
    {
        var names = m.Parameters.Select(p =>
        {
            var n = p.Type.Name;
            // Collapse generic sugar from decompiler
            if (n == "IEnumerable")
                return "IEnumerable";
            return n;
        }).ToArray();

        if (names.SequenceEqual(paramTypeNames))
        {
            Console.WriteLine($"OK {typeName}.{methodName}({string.Join(", ", paramTypeNames)})");
            return true;
        }
    }

    Console.WriteLine($"MISSING {typeName}.{methodName}({string.Join(", ", paramTypeNames)})");
    Console.WriteLine("  candidates:");
    foreach (var m in methods)
        Console.WriteLine($"    ({string.Join(", ", m.Parameters.Select(p => p.Type.FullName))})");
    return false;
}

bool HasField(string typeName, string fieldName)
{
    var type = ts.FindType(new FullTypeName(typeName)).GetDefinition();
    var ok = type?.Fields.Any(f => f.Name == fieldName) == true;
    Console.WriteLine(ok ? $"OK FIELD {typeName}.{fieldName}" : $"MISSING FIELD {typeName}.{fieldName}");
    return ok;
}

var ok = true;
ok &= HasMethod("MegaCrit.Sts2.Core.Factories.CardFactory", "CreateForMerchant", "Player", "IEnumerable", "IEnumerable", "CardType");
ok &= HasMethod("MegaCrit.Sts2.Core.Factories.CardFactory", "CreateForMerchant", "Player", "IEnumerable", "IEnumerable", "CardRarity");
ok &= HasMethod("MegaCrit.Sts2.Core.Factories.CardFactory", "FilterForRun", "IRunState", "IEnumerable");
ok &= HasMethod("MegaCrit.Sts2.Core.Factories.CardFactory", "GetDefaultTransformationOptions", "CardModel", "Boolean");
ok &= HasMethod("MegaCrit.Sts2.Core.Factories.CardFactory", "GetFilteredTransformationOptions", "CardModel", "IEnumerable", "Boolean");
ok &= HasMethod("MegaCrit.Sts2.Core.Models.AncientEventModel", "RelicOption", "RelicModel", "String");
ok &= HasMethod("MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby", ".ctor", "GameMode", "Boolean", "INetGameService", "IStartRunLobbyListener", "Int32");
ok &= HasMethod("MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby", ".ctor", "GameMode", "Boolean", "INetGameService", "IStartRunLobbyListener", "TimeServerResult", "Int32");
ok &= HasMethod("MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory", "CreateForNormalMerchant", "Player");
ok &= HasMethod("MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory", "CreateForBlitzMerchant", "Player");
ok &= HasField("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary", "_foolFilter");
ok &= HasField("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary", "_colorlessFilter");

// Ensure old signatures are gone so we don't silently retarget wrong overloads.
var factory = ts.FindType(new FullTypeName("MegaCrit.Sts2.Core.Factories.CardFactory")).GetDefinition()!;
var staleMerchant = factory.Methods.Any(m =>
    m.Name == "CreateForMerchant"
    && m.Parameters.Count == 3
    && m.Parameters[0].Type.Name == "Player");
var staleFilter = factory.Methods.Any(m => m.Name == "FilterForPlayerCount");
if (staleMerchant)
{
    Console.WriteLine("WARN unexpected 3-arg CreateForMerchant still present");
    ok = false;
}
else
{
    Console.WriteLine("OK stale 3-arg CreateForMerchant absent");
}

if (staleFilter)
{
    Console.WriteLine("WARN FilterForPlayerCount still present");
    ok = false;
}
else
{
    Console.WriteLine("OK FilterForPlayerCount absent");
}

Console.WriteLine(ok ? "Compatibility probe passed." : "Compatibility probe FAILED.");
Environment.Exit(ok ? 0 : 1);
