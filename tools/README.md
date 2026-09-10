# tools

The curated pin list lives here, not in the C#.

`rules_data.py` is the list itself: one row per setting, with the biome, the category, whether it
ships ticked, the prefabs or places it watches, and the sentence shown under the setting. This is
the file you edit.

`gen2.py` writes two things from it, and both are committed:

- `scripts/Pins/PinRules.cs`, the table the mod reads
- the settings table in `PINS.md`

It also refuses to write a name BepInEx would reject in a config key, or the same prefab twice in
one biome. Both of those only show up as a broken game otherwise.

```
python tools/gen2.py
```

`audit.py` checks the list against the game rather than against itself, using the three dumps the
mod could write while it was being built. It catches the failure that has no symptom: a rule naming
a prefab or a place the game does not have, which silently pins nothing. It found two of those, the
tar pits and every vegvisir. It needs the dumps, so it only runs where they were generated; the mod
warns about the same thing at startup, which is what ships.

```
python tools/audit.py
```
