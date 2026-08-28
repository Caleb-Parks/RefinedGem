using System;
using System.Linq;
using System.Reflection;

var dir = @"D:\citrus_steam_games\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64";
var resolver = new PathAssemblyResolver(Directory.GetFiles(dir, "*.dll"));
using var mlc = new MetadataLoadContext(resolver);
var asm = mlc.LoadFromAssemblyPath(Path.Combine(dir, "sts2.dll"));

var lib = asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary")!;
Console.WriteLine(lib.GetMethod("ShowCardDetail"));

var holder = asm.GetType("MegaCrit.Sts2.Core.Nodes.Cards.Holders.NCardHolder")!;
foreach (var member in holder.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    Console.WriteLine(member);

var grid = asm.GetType("MegaCrit.Sts2.Core.Nodes.Cards.Holders.NGridCardHolder")!;
foreach (var member in grid.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    Console.WriteLine("grid: " + member);
