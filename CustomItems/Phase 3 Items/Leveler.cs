using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using StatType = PlayerStats.StatType;
using MonoMod.RuntimeDetour;
namespace CustomItems
{
    public class Leveler : GunVolleyModificationItem
    {
        public static void Init()
        {
            string itemName = "iLevel"; //The name of the item
            string resourceName = "CustomItems/Resources/P3/ilevel"; //Refers to an embedded png in the project. Make sure to embed your resources!

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<Leveler>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Hold [Reload] to use";
            string longDesc = "All stats down. Collect XP by killing enemies and convert it into stat upgrades." +
                "\n\nThis exciting new app uses Blockchain and machine learning algorithms to modify your genetic code!" +
                "\n\nStrength! Dexterity! Charisma! Luck! No aspect of the human form is immutable.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");
            CustomSynergies.Add("Wanna develop an app?", new List<string>() { "kts:ilevel", "ibomb_companion_app" }, ignoreLichEyeBullets: true);

            //Set the rarity of the item
            item.AddStatDowns();
            item.quality = PickupObject.ItemQuality.A;
        }

        public float healthUps;
        public int levelsToSpend = 2;
        float xpToLevel = 700;
        int xp = 0;
        LevelerGUIController m_guiController;
        bool didStatDowns = false;
        ExplosionData IBombExplosionData = new ExplosionData()
        {
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 15,
            doDestroyProjectiles = false,
            doForce = false,
            debrisForce = 30f,
            preventPlayerForce = false,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = false,
            ss = new ScreenShakeSettings(),
            playDefaultSFX = true,
        };

        public override void MidGameSerialize(List<object> data)
        {
            base.MidGameSerialize(data);
            data.Add(levelsToSpend);
            data.Add(xp);
            data.Add(Owner.healthHaver.GetCurrentHealth());
            for (int i = 0; i < stats.Length; i++)
            {
                data.Add(stats[i].level);
            }
        }

        public override void MidGameDeserialize(List<object> data)
        {
            base.MidGameDeserialize(data);
            int levelsToSpend = (int)data[0];
            int xp = (int)data[1];
            float hp = (float)data[2];
            data.RemoveRange(0, 3);
            for (int i = 0; i < stats.Length; i++)
            {
                for (int l = 0; l < (int)data[i]; l++)
                {
                    AddLevel(i, true);
                }
            }

            Owner.healthHaver.ForceSetCurrentHealth(hp);
            this.levelsToSpend = levelsToSpend;
            this.xp = xp;
            m_guiController.UpdateXP(xp / xpToLevel);
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            var defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
            IBombExplosionData.effect = defaultExplosion.effect;
            IBombExplosionData.ignoreList = defaultExplosion.ignoreList;

            m_guiController = player.gameObject.GetOrAddComponent<LevelerGUIController>();
            m_guiController.Build(this, player, ref stats);
            player.OnDealtDamageContext += OnDealtDamage;
            player.OnUsedPlayerItem += HandleIBombSynergy;
        }

        private void HandleIBombSynergy(PlayerController player, PlayerItem item)
        {
            if (!(item is BombCompanionAppItem)) return;
            var enemy = player.CurrentRoom.GetRandomActiveEnemy(false);
            if (!enemy) return;
            Exploder.Explode(enemy.specRigidbody.UnitCenter, IBombExplosionData, Vector2.zero);
        }

        public void AddStatDowns()
        {
            if (didStatDowns) return;
            foreach (var stat in stats)
            {
                var antistat = stat.Copy();
                for (int i = 0; i < antistat.modifiers.Count; i++)
                {
                    var mod = antistat.modifiers[i];
                    mod.amount = stat.penalties[i];
                    this.AddPassiveStatModifier(mod);
                }
            }
            didStatDowns = true;
        }

        public void UpdateCost()
        {
            xpToLevel *= 1.05f;
        }

        public void AddLevel(int selectedStat, bool deserialization = false)
        {

            if (levelsToSpend <= 0 && !deserialization)
            {
                AkSoundEngine.PostEvent("Play_obj_computer_break_01", Owner.gameObject);
                return;
            }
            else
            {
                try
                {
                    Stat stat = stats[selectedStat];
                    if (stat.level < 8)
                    {
                        stat.level++;
                        if (!deserialization) {
                            levelsToSpend--;
                            AkSoundEngine.PostEvent("Play_OBJ_metacoin_collect_01", Owner.gameObject);
                        }
                        m_guiController.SetPoints(levelsToSpend);
                        UpdateCost();
                        if (stat.name.Equals("vitality"))
                        {
                            HandleHealthUp(stat, deserialization);
                        }
                        else
                        {
                            foreach (var modifier in stat.modifiers)
                            {
                                this.AddPassiveStatModifier(modifier);
                            }
                        }
                        Owner.stats.RecalculateStats(Owner, true, true);
                    }
                    else
                    {
                        AkSoundEngine.PostEvent("Play_obj_computer_break_01", Owner.gameObject);
                    }
                }
                catch (Exception e)
                {
                    Tools.PrintException(e);
                }
            }
        }
        private void HandleHealthUp(Stat stat, bool deserialization = false)
        {
            if (Owner.characterIdentity == PlayableCharacters.Robot && !deserialization)
            {
                healthUps += .5f;
                if (healthUps >= 1)
                {
                    healthUps -= 1;
                    Owner.healthHaver.Armor++;
                }
            }
            else
            {
                healthUps += .25f;
                if (healthUps >= 1)
                {
                    this.AddPassiveStatModifier(stat.modifiers[0]);
                    healthUps -= 1;
                }
                else if (!deserialization)
                {
                    LootEngine.SpawnHealth(Owner.sprite.WorldCenter, 1, null);
                }
            }
        }

        private void OnDealtDamage(PlayerController player, float amount, bool fatal, HealthHaver target)
        {
            if (!fatal) return;
            xp += (int)target.GetMaxHealth();
            if (xp >= xpToLevel)
            {
                int levels = xp / (int)xpToLevel;
                xp %= (int)xpToLevel;
                levelsToSpend += levels;
                m_guiController.SetPoints(levelsToSpend);
                if (!blinking)
                {
                    blinking = true;
                    StartCoroutine(LevelUpEffect(player));
                    var fx = Tools.sharedAuto1.LoadAsset<GameObject>("vfx_synergrace_bless");
                    player.PlayEffectOnActor(fx, new Vector3(-10 / 16f, 0, 0));
                    AkSoundEngine.PostEvent("Play_NPC_magic_blessing_01", player.gameObject);
                }
            }
            m_guiController.UpdateXP(xp / xpToLevel);
        }


        Color onColor = new Color(0, 100, 100);
        bool blinking = false;
        IEnumerator LevelUpEffect(PlayerController player)
        {
            bool on = false;
            Material outlineMaterial = SpriteOutlineManager.GetOutlineMaterial(player.sprite);
            Color orig = outlineMaterial.GetColor("_OverrideColor");
            for (int i = 0; i <= 6; i++)
            {
                on = !on;
                if (on)
                    outlineMaterial.SetColor("_OverrideColor", onColor);
                else
                    outlineMaterial.SetColor("_OverrideColor", orig);
                if (i == 6) break;
                yield return new WaitForSeconds(.2f);
            }

            outlineMaterial.SetColor("_OverrideColor", orig);
            blinking = false;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnDealtDamageContext -= OnDealtDamage;
            player.OnUsedPlayerItem -= HandleIBombSynergy;
            player.gameObject.GetComponent<LevelerGUIController>()?.Destroy();
            return base.Drop(player);
        }

        public Stat[] stats = new Stat[]
        {
            new Stat() { name = "strength", penalties = new float[] { -.125f, -.5f}, modifiers = new List<StatModifier>()
            {

                new StatModifier()
                {
                    amount = (.5f + .125f) / 8f,
                    statToBoost = StatType.Damage,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
                new StatModifier()
                {
                    amount = (2f + .5f) /8f,
                    statToBoost = StatType.ThrownGunDamage,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                }
            }},
            new Stat() { name = "dexterity", penalties = new float[] { .125f, .125f}, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = -(.5f + .125f) / 8f,
                    statToBoost = StatType.ReloadSpeed,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
                new StatModifier()
                {
                    amount = -(.5f + .125f) / 8f,
                    statToBoost = StatType.Accuracy,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                }
            }},
            new Stat() { name = "swiftness", penalties = new float[] { -.625f }, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = (2.5f + .625f) / 8f,
                    statToBoost = StatType.MovementSpeed,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
            }},
            new Stat() { name = "agility", penalties = new float[] { 0 , -.125f, -.04f }, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = 10f / 8f,
                    statToBoost = StatType.DodgeRollDamage,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
                new StatModifier()
                {
                    amount = (.5f + .125f) / 8f,
                    statToBoost = StatType.DodgeRollSpeedMultiplier,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
                new StatModifier()
                {
                    amount = (.16f + .04f) / 8f,
                    statToBoost = StatType.DodgeRollDistanceMultiplier,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                }
            }},
            new Stat() { name = "vitality", penalties = new float[] { 0 }, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = 1,
                    statToBoost = StatType.Health,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                }
            }},
            new Stat() { name = "charisma", penalties = new float[] { .125f }, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = -(.5f + .125f) / 8f,
                    statToBoost = StatType.GlobalPriceMultiplier,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                }
            }},
            new Stat() { name = "luck", penalties = new float[] { 0, 0 }, modifiers = new List<StatModifier>()
            {
                new StatModifier()
                {
                    amount = 1,
                    statToBoost = StatType.Coolness,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
                new StatModifier()
                {
                    amount = .2f / 8f,
                    statToBoost = StatType.MoneyMultiplierFromEnemies,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                },
            }},
        };

        //negate damage
        //dodge roll as separate stat
        public class Stat
        {
            public string name;
            public int level;
            public List<StatModifier> modifiers;
            public float[] penalties;

            public Stat Copy()
            {
                Stat copy = new Stat()
                {
                    name = this.name,
                    level = this.level,
                    modifiers = new List<StatModifier>()
                };
                foreach (var mod in modifiers)
                {
                    copy.modifiers.Add(new StatModifier()
                    {
                        amount = mod.amount,
                        modifyType = mod.modifyType,
                        statToBoost = mod.statToBoost
                    });
                }
                return copy;
            }
        }
    }
}
