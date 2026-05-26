using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>Resolved description of a save module: its on-disk id, schema version, and flags.</summary>
    public class SaveModuleDescriptor
    {
        public string Id;
        public int Version;
        public bool Encrypted;
        public bool Compressed;
        public Type ModuleType;
        public string LegacyFileName;   // set only for synthesized v1 modules (used to find old files)
    }

    /// <summary>
    /// Discovers save modules and maps them to descriptors. Two ways a module gets registered:
    ///  - Modern: the class has a [SaveModule] attribute. <see cref="ScanAssemblies"/> finds it via reflection.
    ///  - Legacy: an old v1 module with no attribute. <see cref="GetOrSynthesize"/> builds a descriptor
    ///    on the fly from its ESaveModule enum + FileName, so existing games keep working.
    ///
    /// Lookups work by id (the on-disk identity) or by C# type. The scan runs once, lazily.
    /// </summary>
    public static class SaveModuleRegistry
    {
        private static readonly Dictionary<string, SaveModuleDescriptor> s_ById = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Type, SaveModuleDescriptor> s_ByType = new();
        private static bool s_Scanned;

        public static IReadOnlyDictionary<string, SaveModuleDescriptor> All => s_ById;

        public static SaveModuleDescriptor Get(string id)
        {
            EnsureScanned();
            s_ById.TryGetValue(id, out var d);
            return d;
        }

        public static SaveModuleDescriptor Get(Type type)
        {
            EnsureScanned();
            s_ByType.TryGetValue(type, out var d);
            return d;
        }

        /// <summary>
        /// Return the descriptor for a module instance, synthesizing one for legacy (attribute-less)
        /// modules and caching it. Always succeeds.
        /// </summary>
        public static SaveModuleDescriptor GetOrSynthesize(SaveDataModule instance)
        {
            EnsureScanned();
            var type = instance.GetType();
            if (s_ByType.TryGetValue(type, out var d)) return d;

            d = SynthesizeFromLegacy(instance, type);
            Register(d);
            return d;
        }

        public static void Register(SaveModuleDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            s_ById[descriptor.Id] = descriptor;
            s_ByType[descriptor.ModuleType] = descriptor;
        }

        /// <summary>Force a fresh reflection scan (e.g. after assemblies reload in the editor).</summary>
        public static void ScanAssemblies()
        {
            s_ById.Clear();
            s_ByType.Clear();
            s_Scanned = false;
            EnsureScanned();
        }

        private static void EnsureScanned()
        {
            if (s_Scanned) return;
            s_Scanned = true;

            var baseType = typeof(SaveDataModule);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // GetTypes can throw if an assembly fails to fully load; salvage what we can.
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || !baseType.IsAssignableFrom(t)) continue;
                    var attr = t.GetCustomAttribute<SaveModuleAttribute>(inherit: true);
                    if (attr == null) continue;

                    var d = new SaveModuleDescriptor
                    {
                        Id = attr.Id,
                        Version = attr.Version,
                        Encrypted = attr.Encrypted,
                        Compressed = attr.Compressed,
                        ModuleType = t,
                    };
                    // Two classes claiming the same id would clobber each other's files — reject the duplicate.
                    if (s_ById.TryGetValue(d.Id, out var existing) && existing.ModuleType != t)
                    {
                        Debug.LogError($"[SaveSystem] Duplicate SaveModule id '{d.Id}' on {t.FullName} and {existing.ModuleType.FullName}. Skipping {t.FullName}.");
                        continue;
                    }
                    s_ById[d.Id] = d;
                    s_ByType[t] = d;
                }
            }
        }

        // Build a descriptor for a v1 module: derive the id from its ESaveModule enum name
        // (ECurrency -> "currency"), version 1, and remember its old FileName so the loader can
        // find and migrate the pre-envelope file.
        private static SaveModuleDescriptor SynthesizeFromLegacy(SaveDataModule instance, Type type)
        {
            string id;
            try { id = instance.savemodule.ToString().TrimStart('E', '_').ToLowerInvariant(); }
            catch { id = type.Name.ToLowerInvariant(); }

            string fileName;
            try { fileName = instance.FileName; } catch { fileName = id + ".json"; }

            return new SaveModuleDescriptor
            {
                Id = id,
                Version = 1,
                Encrypted = false,
                Compressed = true,
                ModuleType = type,
                LegacyFileName = fileName,
            };
        }
    }
}
