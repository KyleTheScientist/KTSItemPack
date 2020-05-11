using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System.Reflection;

using System.IO;
using ItemAPI;

namespace CustomItems
{
    public class KTSItemsModule : ETGModule
    {
        public bool setup;
        private static string version = "3.2.0";

        public override void Init()
        {
        }

        public override void Start()
        {
            Setup();
        }

        string[] itemList = {
            "Adhesive Grenade",
            "Baby Good Blob",
            "Big Slime",
            "Blood Bank",
            "Blood Shield",
            "Boss Bullets",
            "Cloak and Dagger",
            "Cursed Ring",
            "D-Chest",
            "Ice Pack",
            "iLevel",
            "Magic Mirror",
            "Mask",
            "Material Emancipation Grill",
            "Mimic Whistle",
            "Capture Sphere",
            "Ring of Guon Swiftness",
            "Ruby Lotus",
            "Scroll of Approximate Knowledge",
            "Slot Machine",
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

                //Phase 3
                Leveler.Init();
                BigSlime.Init();

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

            ETGModConsole.Log($"KTS Item Pack {version} Initialized");
        }

        public override void Exit()
        {
        }
    }
}
