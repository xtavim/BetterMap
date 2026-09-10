import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rules_data import RULES, NAMES
os.chdir(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# BepInEx rejects these outright in a section or key name, and the failure only shows at startup.
BAD = set(chr(61) + chr(10) + chr(9) + chr(92) + chr(34) + chr(39) + chr(91) + chr(93))
for b, n, ps, c, d, isloc, desc in RULES:
    assert not (BAD & set(n)), "config key would be rejected: " + n
    assert not (BAD & set(b)), b

pairs = set()
for b, n, ps, c, d, isloc, desc in RULES:
    for p in ps:
        assert (b, p) not in pairs, "prefab listed twice in one biome: %s %s" % (b, p)
        pairs.add((b, p))

print("settings: %d   prefab/biome pairs: %d" % (len(RULES), len(pairs)))

CS = ["""using System.Collections.Generic;
using BepInEx.Configuration;

namespace BetterMap.Scripts.Pins
{
    public enum PinCategory
    {
        Ore,
        Forage,
        Dungeon,
        Loot,
        Spawner,
        Vegvisir,
        Beehive,
        Tar,
        Sap,
        Boss,
        Portal
    }

    public static class PinRules
    {
        public class Rule
        {
            public string Name;
            public string Description;
            public string NameToken;
            public string[] Prefabs;
            public Heightmap.Biome Biome;
            public PinCategory Category;
            public bool DefaultOn;
            public bool IsLocation;
            public ConfigEntry<bool> Enabled;
        }

        public static readonly List<Rule> All = new List<Rule>
        {"""]

for b, n, ps, c, d, isloc, desc in RULES:
    CS.append("            new Rule")
    CS.append("            {")
    CS.append('                Name = "%s",' % n)
    CS.append('                Description = "%s",' % desc.replace('"', '\\"'))
    if n in NAMES:
        CS.append('                NameToken = "%s",' % NAMES[n])
    CS.append("                Prefabs = new[] { %s }," % ", ".join('"%s"' % p for p in ps))
    CS.append("                Biome = Heightmap.Biome.%s," % b)
    CS.append("                Category = PinCategory.%s," % c.capitalize())
    CS.append("                DefaultOn = %s," % ("true" if d else "false"))
    CS.append("                IsLocation = %s" % ("true" if isloc else "false"))
    CS.append("            },")

CS.append("        };")
CS.append("    }")
CS.append("}")

open("scripts/Pins/PinRules.cs", "w", encoding="utf-8", newline="\r\n").write("\n".join(CS) + "\n")

MD = ["| biome | setting | category | default | prefabs |", "|---|---|---|---|---|"]
for b, n, ps, c, d, isloc, desc in RULES:
    MD.append("| %s | %s | %s | %s | %s |" % (
        b, n, c, "**on**" if d else "off", ", ".join("`%s`" % p for p in ps)))

t = open("PINS.md", encoding="utf-8").read().replace("\r\n", "\n")
i = t.index("| biome |")
j = t.index("\n---\n", i)
t = t[:i] + "\n".join(MD) + t[j:]
open("PINS.md", "w", encoding="utf-8", newline="\n").write(t)
print("PinRules.cs and PINS.md written")
