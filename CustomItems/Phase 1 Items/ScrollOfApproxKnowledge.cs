using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ChestType = Chest.GeneralChestType;
using ItemAPI;

namespace CustomItems
{
    class ScrollOfApproxKnowledge : PassiveItem
    {
        private static string gunFXPath = "CustomItems/Resources/P1/gun_text";
        private static string itemFXPath = "CustomItems/Resources/P1/item_text";
        private static string fxName = "ScrollFX";
        static List<Tuple<Chest, int>> foundChests = new List<Tuple<Chest, int>>();
        private static GameObject gunFXPrefab, itemFXPrefab;

        public static void Init()
        {
            string itemName = "Scroll of Approximate Knowledge"; //The name of the item
            string resourceName = "CustomItems/Resources/P1/approx_scroll"; //Refers to an embedded png in the project. Make sure to embed your resources!

            //Create new GameObject
            GameObject obj = new GameObject();

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<ScrollOfApproxKnowledge>();

            //Generate a new GameObject with a sprite component
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "It's definitely something...";
            string longDesc = "Vaguely describes the contents of chests.\n\n" +
                "It is said that this magically embued toilet paper roll was an Apprentice Gunjurer's last-ditch " +
                "attempt at passing their graduation exam.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;

            BuildPrefabs();
        }

        public static void BuildPrefabs()
        {
            gunFXPrefab = SpriteBuilder.SpriteFromResource(gunFXPath, null);
            itemFXPrefab = SpriteBuilder.SpriteFromResource(itemFXPath, null);

            gunFXPrefab.name = fxName;
            itemFXPrefab.name = fxName;

            GameObject.DontDestroyOnLoad(gunFXPrefab);
            FakePrefab.MarkAsFakePrefab(gunFXPrefab);
            gunFXPrefab.SetActive(false);

            GameObject.DontDestroyOnLoad(itemFXPrefab);
            FakePrefab.MarkAsFakePrefab(itemFXPrefab);
            itemFXPrefab.SetActive(false);
        }

        public override void Pickup(PlayerController player)
        {
            player.gameObject.AddComponent<ApproxScrollBehaviour>();
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            var behavior = player.gameObject.GetComponent<ApproxScrollBehaviour>();
            if (behavior)
            {
                behavior.DestroyAllFX();
            }
            return base.Drop(player);
        }

        private class ApproxScrollBehaviour : BraveBehaviour
        {
            List<Chest> encounteredChests = new List<Chest>();
            PlayerController player;
            Chest nearbyChest;
            Vector2 offset = new Vector2(0, .25f);


            void Start()
            {
                player = GetComponent<PlayerController>();
            }

            void FixedUpdate()
            {
                if (!player || player.CurrentRoom == null)
                    return;
                IPlayerInteractable nearestInteractable = player.CurrentRoom.GetNearestInteractable(player.sprite.WorldCenter, 1f, player);
                if (nearestInteractable != null && nearestInteractable is Chest)
                {
                    var chest = nearestInteractable as Chest;

                    if (!encounteredChests.Contains(chest) && !chest.transform.Find(fxName))
                        InitializeChest(chest);
                    else
                        nearbyChest = chest;
                }
                else
                {
                    nearbyChest = null;
                }

                HandleChests();
            }

            void HandleChests()
            {
                foreach(var chest in encounteredChests)
                {
                    if (!chest)
                        continue;

                    var fx = chest?.transform?.Find(fxName)?.GetComponent<tk2dSprite>();

                    if (!fx)
                        continue;

                    if(chest != nearbyChest)
                        fx.scale = Vector3.Lerp(fx.scale, Vector3.zero, .25f);
                    else
                        fx.scale = Vector3.Lerp(fx.scale, Vector3.one, .25f);

                    if (Vector3.Distance(fx.scale, Vector3.zero) < .01f)
                        fx.scale = Vector3.zero;

                    fx.PlaceAtPositionByAnchor(chest.sprite.WorldTopCenter + offset, tk2dBaseSprite.Anchor.LowerCenter);
                }
            }

            void InitializeChest(Chest chest)
            {
                int guess = GetGuess(chest);
                GameObject prefab;
                if (guess == 0)
                    prefab = gunFXPrefab;
                else
                    prefab = itemFXPrefab;

                var sprite = GameObject.Instantiate(prefab, chest.transform).GetComponent<tk2dSprite>();
                sprite.name = fxName;
                sprite.PlaceAtPositionByAnchor(chest.sprite.WorldTopCenter + offset, tk2dBaseSprite.Anchor.LowerCenter);
                sprite.scale = Vector3.zero;

                nearbyChest = chest;
                encounteredChests.Add(chest);
            }

            int GetGuess(Chest chest)
            {
                var type = chest.ChestType;
                if (type == ChestType.WEAPON)
                    return 0;
                else if (type == ChestType.ITEM)
                    return 1;
                else
                {
                    var contents = chest.PredictContents(player);
                    foreach (var item in contents)
                    {
                        if (item is Gun) return 0;
                        if (item is PlayerItem || item is PassiveItem) return 1;
                    }
                }
                return UnityEngine.Random.Range(0, 2);
            }

            public void DestroyAllFX()
            {
                foreach (var chest in encounteredChests)
                {
                    var fx = chest.transform.Find(fxName);
                    if (fx)
                        Destroy(fx);
                }
                encounteredChests.Clear();
            }
        }
    }
}
