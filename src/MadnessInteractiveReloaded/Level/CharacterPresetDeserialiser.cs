﻿using MIR.Serialisation;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Globalization;
using System.IO;

namespace MIR;

/// <summary>
/// Load character presets from a file.
/// <see cref="CharacterLook"/>
/// <seealso cref="CharacterStats"/>
/// </summary>
public static class CharacterPresetDeserialiser
{
    private const string dataPrefix = "data:";
    private static readonly KeyValueDeserialiser<ExperimentCharacterPreset> deserialiser;

    static CharacterPresetDeserialiser()
    {
        deserialiser = new KeyValueDeserialiser<ExperimentCharacterPreset>(nameof(CharacterPresetDeserialiser));
        deserialiser.RegisterString("name", (i, v) => i.Name = v);

        deserialiser.RegisterString("look", (i, v) =>
        {
            if (v.StartsWith(dataPrefix))
            {
                var f = Path.GetTempFileName();
                File.WriteAllBytes(f, Convert.FromBase64String(v[dataPrefix.Length..]));
                i.Look = CharacterLookDeserialiser.Load(f);
            }
            else
                i.Look = Registries.Looks.Get(v);
        });

        deserialiser.RegisterString("stats", (i, v) =>
        {
            if (v.StartsWith(dataPrefix))
            {
                var f = Path.GetTempFileName();
                File.WriteAllBytes(f, Convert.FromBase64String(v[dataPrefix.Length..]));
                i.Stats = CharacterStatsDeserialiser.Load(f);
            }
            else
                i.Stats = Registries.Stats.Get(v);
        });
    }

    public static ExperimentCharacterPreset Load(string path) => deserialiser.Deserialise(path);

    public static void Save(string name, CharacterLook look, CharacterStats stats, string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine(GameVersion.Version.ToString());
        writer.WriteLine("name {0}", name);
        if (Registries.Looks.TryGetKeyFor(look, out var k))
            writer.WriteLine("look {0}", k);
        else
        {
            var f = Path.GetTempFileName();
            CharacterLookDeserialiser.Save(look, f);
            writer.WriteLine("look {0}", dataPrefix + Convert.ToBase64String(File.ReadAllBytes(f)));
        }

        if (Registries.Stats.TryGetKeyFor(stats, out k))
            writer.WriteLine("stats {0}", k);
        else
        {
            var f = Path.GetTempFileName();
            CharacterStatsDeserialiser.Save(stats, f);
            writer.WriteLine("stats {0}", dataPrefix + Convert.ToBase64String(File.ReadAllBytes(f)));
        }
        writer.Dispose();
    }
}