import os, re, sys, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rules_data import RULES

CFG = r"C:\Users\gusta\AppData\Roaming\com.kesomannen.gale\valheim\profiles\Test\BepInEx\config"
veg_txt = open(CFG + r"\BetterMap.vegetation.txt", encoding="utf-8", errors="replace").read()
loc_txt = open(CFG + r"\BetterMap.locations.txt", encoding="utf-8", errors="replace").read()
pre_txt = open(CFG + r"\BetterMap.prefabs.txt", encoding="utf-8", errors="replace").read()

BIOMES = ["Meadows", "BlackForest", "Swamp", "Mountain", "Plains", "Mistlands",
          "AshLands", "DeepNorth", "Ocean"]


def sections(text):
    """Biome -> block of lines."""
    out = collections.defaultdict(list)
    cur = None
    for line in text.split("\n"):
        m = re.match(r"^([A-Za-z]+)\s+\(\d+\)\s*$", line)
        if m and m.group(1) in BIOMES:
            cur = m.group(1)
            continue
        if cur:
            out[cur].append(line)
    return out


veg = sections(veg_txt)
loc = sections(loc_txt)

# ---------- what our rules cover ----------
covered = collections.defaultdict(set)      # biome -> {prefab}
for b, n, ps, c, d, isloc, desc in RULES:
    for p in ps:
        covered[b].add(p)

all_prefab_names = set(re.findall(r"^(\S+)", pre_txt, re.M))

# Objects listed inside locations. Neither of the other two dumps holds them, which is how the
# vegvisirs and the fuling totem stayed invisible: they are placed, not scattered, and not
# harvestable in the way the prefab dump filters for.
inside_names = set()
for _m in re.finditer(r"^        x\d+\s+(\S+)", loc_txt, re.M):
    inside_names.add(re.sub(r"\s*\(\d+\)$", "", _m.group(1)))
all_prefab_names |= inside_names
all_veg_names = set()
all_loc_names = set()
for b in BIOMES:
    for l in veg.get(b, []):
        m = re.match(r"^  (\S+)", l)
        if m:
            all_veg_names.add(m.group(1))
    for l in loc.get(b, []):
        m = re.match(r"^  (\S+)", l)
        if m:
            all_loc_names.add(m.group(1))

print("=" * 78)
print("1. RULES THAT NAME SOMETHING THE GAME DOES NOT HAVE")
print("=" * 78)
bad = 0
for b, n, ps, c, d, isloc, desc in RULES:
    for p in ps:
        known_loc = p in all_loc_names
        known_obj = p in all_veg_names or p in all_prefab_names
        if isloc and not known_loc:
            print("  %-12s %-24s location %-32s NOT IN LOCATION DUMP" % (b, n, p))
            bad += 1
        if not isloc and not known_obj:
            print("  %-12s %-24s object   %-32s NOT IN VEG OR PREFAB DUMP" % (b, n, p))
            bad += 1
print("  none" if bad == 0 else "  %d suspect" % bad)

# ---------- vegetation with a harvest we do not cover ----------
print()
print("=" * 78)
print("2. HARVESTABLE VEGETATION NOT COVERED, BY BIOME")
print("=" * 78)
for b in BIOMES:
    rows = []
    for l in veg.get(b, []):
        m = re.match(r"^  (\S+)\s+qty\s+(\S+)\s+(.*)$", l)
        if not m:
            continue
        name, qty, what = m.groups()
        if "->" not in what:
            continue
        if name in covered[b]:
            continue
        rows.append((name, qty, what.strip()[:64]))
    if rows:
        print("\n-- %s" % b)
        for name, qty, what in sorted(rows):
            print("   %-34s %-14s %s" % (name, qty, what))

# ---------- things inside locations that yield something ----------
print()
print("=" * 78)
print("3. OBJECTS INSIDE LOCATIONS THAT YIELD SOMETHING, BY BIOME")
print("   (the fuling totem class: not scattered, so absent from the vegetation dump)")
print("=" * 78)
for b in BIOMES:
    inside = collections.defaultdict(set)   # object -> {yield}
    holder = collections.defaultdict(set)   # object -> {location}
    cur_loc = None
    for l in loc.get(b, []):
        m = re.match(r"^  (\S+)", l)
        if m:
            cur_loc = m.group(1)
            continue
        m = re.match(r"^        x\d+\s+(\S+)\s+(Pickable|MineRock5?|Beehive|ResourceRoot)\s+->\s*(.*)$", l)
        if m:
            obj, kind, yields = m.groups()
            obj = re.sub(r"\s*\(\d+\)$", "", obj)
            inside[obj].add(yields.strip()[:44])
            holder[obj].add(cur_loc)
    rows = []
    for obj, ys in inside.items():
        if obj in covered[b]:
            continue
        rows.append((obj, sorted(ys)[0], len(holder[obj]), sorted(holder[obj])[:2]))
    if rows:
        print("\n-- %s" % b)
        for obj, y, nloc, where in sorted(rows, key=lambda r: -r[2]):
            print("   %-32s %-40s in %d location(s) %s" % (obj, y, nloc, ",".join(where)))
