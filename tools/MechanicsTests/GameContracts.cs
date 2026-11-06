using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class GameContracts
{
    internal static void Check(string assembly)
    {
        // Read metadata without loading Unity or executing game code. No game code is copied into this project.
        using var stream = File.OpenRead(assembly);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        int checks = 0;
        void Require(string typeName, string[] methods, string[] fields)
        {
            var matches = reader.TypeDefinitions.Where(h =>
            {
                var candidate = reader.GetTypeDefinition(h);
                string[] path = typeName.Split('+');
                if (reader.GetString(candidate.Name) != path[^1]) return false;
                return path.Length == 1 || (!candidate.GetDeclaringType().IsNil &&
                    reader.GetString(reader.GetTypeDefinition(candidate.GetDeclaringType()).Name) == path[0]);
            }).ToArray();
            if (matches.Length != 1) throw new Exception($"Missing/ambiguous native type: {typeName}");
            TypeDefinition type = reader.GetTypeDefinition(matches[0]);
            var methodNames = type.GetMethods().Select(h => reader.GetString(reader.GetMethodDefinition(h).Name)).ToHashSet();
            var fieldNames = type.GetFields().Select(h => reader.GetString(reader.GetFieldDefinition(h).Name)).ToHashSet();
            foreach (string name in methods)
            { if (!methodNames.Contains(name)) throw new Exception($"Missing native method: {typeName}.{name}"); checks++; }
            foreach (string name in fields)
            { if (!fieldNames.Contains(name)) throw new Exception($"Missing native field: {typeName}.{name}"); checks++; }
        }
        Require("NAutomaticGun", ["Awake", "UpdateSlide", "Shoot", "TryChamber", "ShotsLeft", "RegisterTutorial", "set_chambered"], []);
        Require("MagazineManager", ["Awake", "FitAuthoredCount"], ["items", "slots", "returning"]);
        Require("LoadoutRefit", ["Swap"], []);
        Require("SpriteUpgradeChanger", ["Refresh"], []);
        Require("Gun", ["ExitAimMode"], []);
        Require("MagazineManager+Slot", [], ["occupant"]);
        Require("MagazineManager+Return", [], ["item"]);
        Require("LinearObject", [], ["targetValue", "value"]);
        Console.WriteLine($"PASS: {checks} native hook/field contracts in {Path.GetFileName(assembly)}.");
    }
}
