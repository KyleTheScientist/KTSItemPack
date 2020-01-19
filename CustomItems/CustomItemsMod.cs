using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System.Reflection;
using ItemAPI;
using System.IO;

namespace CustomItems
{
    public class CustomItemsMod : ETGModule
    {
        public bool setup;
        private static string version = "2.2.4";

        public override void Init()
        {
        }

        public override void Start()
        {
            Setup();
        }

        string[] itemList = {
            "Baby Good Blob",
            "Blood Bank",
            "Blood Shield",
            "Boss Bullets",
            "Cloak and Dagger",
            "Cursed Ring",
            "D-Chest",
            "Ice Pack",
            "Magic Mirror",
            "Mask",
            "Material Emancipation Grill",
            "Mimic Whistle",
            "Capture Sphere",
            "Ring of Guon Swiftness",
            "Ruby Lotus",
            "Scroll of Approximate Knowledge",
            "Slot Machine",
            "Sticky Bomb",
            "Sweating Bullets",
            "Terrifying Mask",
            "Thermometer",
            "Toy Drone"
        };

        public void Setup()
        {
            if (setup) return;
            try
            {
                Tools.Init();
                ItemBuilder.Init();

                //Phase 1
                BloodBank.Init();
                BloodShield.Init();
                BossBullets.Init();
                ChestReroller.Init();
                CursedRing.Init();
                HologramItem.Init();
                IcePack.Init();
                LightningGuon.Init();
                MimicWhistle.Init();
                ScrollOfApproxKnowledge.Init();
                SlotMachine.Init();
                SweatingBullets.Init();
                TerrifyingMask.Init();

                //Phase 2
                BabyGoodBlob.Init();
                CloakAndDagger.Init();
                Drone.Init();
                MagicMirror.Init();
                Pikachu.Init();
                RubyLotus.Init();
                StickyBomb.Init();
                Thermometer.Init();
                NinjaMask.Init();

                setup = true;
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }


            ETGModConsole.Commands.AddUnit("kts", e =>
            {
                ETGModConsole.Log("Custom Items: ");
                foreach (string s in itemList)
                {
                    ETGModConsole.Log("    " + s);
                }
            });

            /*
            ETGModConsole.Commands.AddUnit("dissectCompanion", e =>
            {
                var companion = GameObject.FindObjectOfType<CompanionController>();
                Tools.Print("Companion Components: ");
                foreach (var comp in companion.GetComponents<Component>())
                {
                    Tools.Print("    " + comp.GetType());
                }


                Tools.Print("Movement: " + companion.behaviorSpeculator?.MovementBehaviors?.Count);
                if (companion.behaviorSpeculator?.MovementBehaviors != null)
                {
                    foreach (var b in companion.behaviorSpeculator.MovementBehaviors)
                    {
                        Tools.Print("   " + b.GetType());
                    }
                }

                Tools.Print("Target: " + companion.behaviorSpeculator?.TargetBehaviors?.Count);
                if (companion.behaviorSpeculator?.TargetBehaviors != null)
                {
                    foreach (var b in companion.behaviorSpeculator.TargetBehaviors)
                    {
                        Tools.Print("   " + b.GetType());
                    }
                }

                Tools.Print("Attack: " + companion.behaviorSpeculator?.AttackBehaviors?.Count);
                if (companion.behaviorSpeculator?.AttackBehaviors != null)
                {
                    foreach (var b in companion.behaviorSpeculator.AttackBehaviors)
                    {
                        Tools.Print("   " + b.GetType());
                    }
                }

                Tools.Print("Override: " + companion.behaviorSpeculator?.OverrideBehaviors?.Count);
                if (companion.behaviorSpeculator?.MovementBehaviors != null)
                {
                    foreach (var b in companion.behaviorSpeculator.OverrideBehaviors)
                    {
                        Tools.Print("   " + b.GetType());
                    }
                }

                Tools.Print("Other: " + companion.behaviorSpeculator?.OtherBehaviors?.Count);
                if (companion.behaviorSpeculator?.MovementBehaviors != null)
                {
                    foreach (var b in companion.behaviorSpeculator.OtherBehaviors)
                    {
                        Tools.Print("   " + b.GetType());
                    }
                }


                //Tools.Print("Speculator: " + (companion.behaviorSpeculator != null));
                //companion.gameObject.AddComponent<BehaviorSpeculator>().GetCopyOf(companion.behaviorSpeculator);
            });

            ETGModConsole.Commands.AddUnit("showCompanionHitboxes", e =>
            {
                var list = GameObject.FindObjectsOfType<CompanionController>();
                foreach(var companion in list)
                    companion.specRigidbody.ShowHitBox();
            });


            ETGModConsole.Commands.AddUnit("playsound", args =>
            {
                AkSoundEngine.PostEvent(args[0], GameManager.Instance.PrimaryPlayer.gameObject);
            });

            ETGModConsole.Commands.AddUnit("setshader", (string[] args) =>
            {
                GameManager.Instance.PrimaryPlayer.SetOverrideShader(ShaderCache.Acquire(args[0]));
            });
            */

            ETGModConsole.Commands.AddUnit("dissectorbital", e =>
            {
                var list = GameObject.FindObjectsOfType<PlayerOrbital>();
                foreach (var orbital in list)
                {
                    orbital.gameObject.Dissect();
                    Tools.Print("Orbital tier:" + orbital.GetOrbitalTier());
                    Tools.Print("Orbital tier index:" + orbital.GetOrbitalTierIndex());

                    foreach (var f in typeof(PlayerOrbital).GetFields())
                    {
                        Tools.Print($"{f.Name}: {f.GetValue(orbital)}");
                    }

                }
            });

            ETGModConsole.Log($"Custom Items Mod {version} Initialized");
        }

        public override void Exit()
        {
        }
    }
}
