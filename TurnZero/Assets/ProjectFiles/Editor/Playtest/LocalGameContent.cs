using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TurnZero.Editor
{
    internal static class LocalGameContent
    {
        internal static BattleConfiguration Create()
        {
            foreach (string folder in new[]
            {
                "Data/Characters",
                "Data/Equipment",
                "Data/Fields",
                "Art/Models",
                "Art/Materials",
                "Art/Audio",
                "Prefabs/UI",
                "Prefabs/World",
                "Scenes"
            })
            {
                Directory.CreateDirectory(GameDefinitions.AssetRoot + folder);
            }

            AssetDatabase.Refresh();
            CharacterDefinition[] characters = new CharacterDefinition[GameDefinitions.CharactersPerTeam];
            string[] names = { "Vanguard", "Shade", "Pyre", "Binder", "Deadeye" };
            string[] models = { "Guardian", "Scout", "Ranger", "Support", "Ranger" };
            string[] portraits = { "Guardian", "Scout", "Ranger", "Support", "Ranger" };
            int[] health = { 14, 7, 10, 9, 6 };
            int[] moves = { 3, 6, 4, 4, 2 };
            int[] ranges = { 3, 4, 5, 5, 8 };
            int[] damage = { 3, 2, 3, 3, 3 };
            int[] skillRange = { 2, 4, 5, 5, 12 };
            int[] skillArea = { 1, 0, 3, 3, 0 };
            int[] skillDamage = { 6, 0, 2, 4, 5 };
            int[] skillCost = { 4, 3, 5, 5, 4 };
            string[] skills = { "BREACH", "SHADOW STEP", "CINDER FIELD", "BINDING SHOT", "PRECISION" };
            string[] descriptions =
            {
                "Heavy damage to the target tile.",
                "Move unseen through the next turn.",
                "Burning area lasts three turns; damages enemies once per zone per turn.",
                "Damage enemies and ROOT them next turn.",
                "Strike one distant tile with precision."
            };
            SkillEffect[] effects = new[]
            {
                SkillEffect.Damage,
                SkillEffect.StealthMove,
                SkillEffect.BurningGround,
                SkillEffect.RootDamage,
                SkillEffect.Precision
            };

            for (int i = 0; i < GameDefinitions.CharactersPerTeam; i++)
            {
                int slot = i;

                characters[i] = Asset<CharacterDefinition>($"Data/Characters/CHR-0{i + 1}-{names[i]}.asset", asset =>
                {
                    asset.Values = new CharacterSpec
                    {
                        Id = $"CHR-0{slot + 1}",
                        Name = names[slot],
                        Health = health[slot],
                        SkillEffect = effects[slot],
                        Move = Action(
                            "MOVE",
                            "Follow an orthogonal path. Allies and enemies may share tiles.",
                            moves[slot],
                            0,
                            0,
                            slot == 4 ? 3 : 2,
                            2,
                            1),
                        Attack = Action(
                            "BASIC ATTACK",
                            "Attack the chosen tile. A miss still spends AP. Enemy targets only.",
                            ranges[slot],
                            slot == 2 ? 2 : 1,
                            damage[slot],
                            slot == 4 ? 3 : 2,
                            3,
                            1),
                        Skill = Action(
                            skills[slot],
                            descriptions[slot],
                            skillRange[slot],
                            skillArea[slot],
                            skillDamage[slot],
                            skillCost[slot],
                            slot == 1 ? 1 : 3,
                            slot >= 3 ? 2 : 1)
                    };
                    asset.Portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ProjectFiles/Art/Prototype/{portraits[slot]}CardPortrait.asset");
                    asset.Model = Model(names[slot], models[slot]);
                    asset.Accent = new[]
                    {
                        new Color(0.2f,0.79f,0.74f),
                        new Color(0.55f,0.72f,0.38f),
                        new Color(0.97f,0.55f,0.24f),
                        new Color(0.52f,0.64f,0.91f),
                        new Color(0.78f,0.57f,0.87f)
                    }[slot];
                });
            }

            EquipmentDefinition[] equipment = new EquipmentDefinition[3];

            equipment[0] = Asset<EquipmentDefinition>("Data/Equipment/EQP-01-Portal.asset", asset =>
            {
                asset.Values = new EquipmentSpec
                {
                    Id = "EQP-01",
                    Name = "Portal",
                    Effect = EquipmentEffect.Portal,
                    Health = 5,
                    Duration = 3,
                    ExitRange = 10,
                    Action = Action(
                        "BUILD PORTAL",
                        "Choose an explored entrance and exit. Three-turn build; persists after the builder dies.",
                        1,
                        1,
                        0,
                        5,
                        4,
                        1)
                };
                asset.Icon = Resources.Load<Sprite>("PrototypeUI/item");
            });
            equipment[1] = Asset<EquipmentDefinition>("Data/Equipment/EQP-02-Swap.asset", asset =>
            {
                asset.Values = new EquipmentSpec
                {
                    Id = "EQP-02",
                    Name = "Swap",
                    Effect = EquipmentEffect.Swap,
                    Action = Action(
                        "POSITION SWAP",
                        "Exchange positions with another living ally. Root prevents the exchange. Global range.",
                        999,
                        1,
                        0,
                        5,
                        4,
                        1)
                };
                asset.Icon = Resources.Load<Sprite>("PrototypeUI/move");
            });
            equipment[2] = Asset<EquipmentDefinition>("Data/Equipment/EQP-03-Beacon.asset", asset =>
            {
                asset.Values = new EquipmentSpec
                {
                    Id = "EQP-03",
                    Name = "Beacon",
                    Effect = EquipmentEffect.Beacon,
                    Health = 3,
                    Action = Action(
                        "VISION BEACON",
                        "Place a 3 HP structure that reveals four tiles around it until destroyed.",
                        2,
                        4,
                        0,
                        4,
                        1,
                        2)
                };
                asset.Icon = Resources.Load<Sprite>("PrototypeUI/skill");
            });
            FieldDefinition[] fields = new FieldDefinition[3];
            string[] fieldNames = { "Courtyard", "Moss garden", "Slate keep" };
            Color[] colors = new[]
            {
                new Color(0.25f,0.30f,0.32f),
                new Color(0.20f,0.30f,0.24f),
                new Color(0.23f,0.25f,0.34f)
            };

            for (int i = 0; i < 3; i++)
            {
                int slot = i;

                fields[i] = Asset<FieldDefinition>($"Data/Fields/Field-0{i + 1}.asset", asset =>
                {
                    asset.DisplayName = fieldNames[slot];
                    asset.Ground = colors[slot];
                    asset.Alternate = colors[slot] * 1.12f;

                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 12; x++)
                        {
                            asset.HomeTiles[y * 12 + x] = slot == 1 ? ((x + y) % 3 == 0 ? 1 : 0) : slot == 2 ? (x % 3 == 0 ? 1 : 0) : (x + y) % 2;
                        }
                    }
                });
            }

            return Asset<BattleConfiguration>("Data/LocalBattleConfiguration.asset", asset =>
            {
                asset.Characters = characters;
                asset.Equipment = equipment;
                asset.Fields = fields;
                asset.BaseModel = Model("Base", "Base");

                asset.LocalProfileNotes = "LOCAL PLAYTEST PROFILE v2 — local controls and undefined content remain provisional.\nManhattan ranges and effect radius N. Attacks retain their reserved target and range eligibility. Same-phase attacks remain valid; HP updates per attack and deaths settle at phase end.\nPhases 0–4: revive at 0; movement advances concurrently by tile within each movement phase. First item contact wins; contacts tied in one step award each team once and remove the item immediately.\nBurn hits on creation and at its original skill phase on later turns; duration includes the creation turn and has no movement-entry damage. Stealth and root share status-effect definitions; operational beacons detect stealth.\nKill AP is awarded at turn wrap, capped at 25, with a visible actual-increase cue. Death clears all cooldowns. Revive next turn at full HP in own base vision, cost 15 decreasing to 10, clearing status effects and preventing another action that turn.\nEquipment decks contain 1–5 distinct reusable crafting choices; only three item types are currently defined. Crafting replaces equipment only after confirmation and resets its cooldown. Overlapping swaps and installation claims fail without cost. Portal progress includes its creation turn and completes at phase 4 end; linked ends die together.\nReady may be unlocked before planning ends; both ready or the deadline starts one resolution. Enemy identity and confirmed death are public; hidden revival requires observation, and hidden paths or attack sources are not replayed.\nUndefined upgrades, consumables, progression, online play and final input choices await their documented decisions.";
            });
        }

        internal static ActionSpec Action(
            string name,
            string description,
            int range,
            int area,
            int damage,
            int cost,
            int priority,
            int cooldown)
        {
            return new ActionSpec
            {
                Name = name,
                Description = description,
                Range = range,
                Area = area,
                Damage = damage,
                Cost = cost,
                Priority = priority,
                Cooldown = cooldown
            };
        }

        internal static T Asset<T>(string relative, System.Action<T> initialize) where T : ScriptableObject
        {
            string path = GameDefinitions.AssetRoot + relative;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            initialize(asset);
            AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        private static GameObject Model(string name, string original)
        {
            string path = GameDefinitions.AssetRoot + $"Art/Models/{name}.prefab";

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (saved != null)
            {
                return saved;
            }

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ProjectFiles/Prototype/Prefabs/Characters/{original}.prefab");
            GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(source);

            PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            clone.name = name;

            foreach (TMPro.TMP_Text text in clone.GetComponentsInChildren<TMPro.TMP_Text>(true).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(text.gameObject);
            }

            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true).ToArray())
            {
                UnityEngine.Object.DestroyImmediate(behaviour);
            }

            saved = PrefabUtility.SaveAsPrefabAsset(clone, path);
            UnityEngine.Object.DestroyImmediate(clone);

            return saved;
        }

        internal static AudioClip TurnSound()
        {
            string path = GameDefinitions.AssetRoot + "Art/Audio/TurnStart.wav";

            if (!File.Exists(path))
            {
                const int rate = 22050;
                const int count = rate / 4;

                using (BinaryWriter stream = new BinaryWriter(File.Create(path)))
                {
                    stream.Write(Encoding.ASCII.GetBytes("RIFF"));
                    stream.Write(36 + count * 2);
                    stream.Write(Encoding.ASCII.GetBytes("WAVEfmt "));

                    stream.Write(16);
                    stream.Write((short)1);
                    stream.Write((short)1);
                    stream.Write(rate);
                    stream.Write(rate * 2);
                    stream.Write((short)2);
                    stream.Write((short)16);

                    stream.Write(Encoding.ASCII.GetBytes("data"));
                    stream.Write(count * 2);

                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / rate;
                        double envelope = Math.Min(1, t * 80) * Math.Exp(-t * 14);

                        stream.Write((short)(Math.Sin(t * Math.PI * 2 * (t < 0.1 ? 523.25 : 659.25)) * envelope * 8000));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
