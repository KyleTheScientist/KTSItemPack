using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

using StatType = PlayerStats.StatType;
using Stat = CustomItems.Leveler.Stat;
namespace CustomItems
{

    public class LevelerGUIController : BraveBehaviour
    {
        readonly string spriteDir = "CustomItems/Resources/P3/Leveler";
        Transform m_extantGUI;
        tk2dSprite m_panel, m_xpBar, m_readMe;
        tk2dSprite[,] m_levels, m_points;
        tk2dSprite[] m_icons;
        PlayerController m_player;
        Vector2
            targetScale = Vector2.zero,
            baseScale = new Vector2(1, 1);
        int selectedStat = 0;
        public Leveler m_item;
        public Stat[] m_stats;
        public static Shader holoShader = ShaderCache.Acquire("Brave/Internal/HologramShader");
        static float lum = 5;
        Color
            lumWhite = new Color(lum, lum, lum),
            lumGrey = new Color(.5f, .5f, .5f),
            lumGreen = new Color(0, lum, 0),
            lumMagenta = new Color(lum, 0, lum);

        public void Build(Leveler item, PlayerController player, ref Stat[] stats)
        {
            if (m_extantGUI != null) return;
            this.m_item = item;
            this.m_player = player;
            this.m_stats = stats;
            m_extantGUI = SpriteBuilder.SpriteFromResource($"{spriteDir}/panel").transform;
            m_panel = m_extantGUI.gameObject.GetComponent<tk2dSprite>();
            m_panel.transform.parent = m_player.transform;
            m_panel.SortingOrder = 0;
            m_panel.IsPerpendicular = false;

            BuildLevels();
            BuildXPAndText();
            shown = false;
            m_panel.scale = Vector2.zero;
            Invoke("InitializeAppearance", .1f);
        }

        Vector3 xpBarOffset = new Vector3(9.5f, 19f, -16f);
        Vector3 readMeOffset = new Vector3(58f, 7f, -16f);
        Vector3 pointsOffset = new Vector3(22f, 6f, -16f);
        void BuildXPAndText()
        {
            m_xpBar = CreateSpriteElement("xpbar", xpBarOffset);
            m_readMe = CreateSpriteElement("readme", readMeOffset);
            float x, y, w = 6, h = 6;
            m_points = new tk2dSprite[5, 2];
            for (int i = 0; i < m_points.GetLength(0); i++)
            {
                for (int j = 0; j < m_points.GetLength(1); j++)
                {
                    x = i * w;
                    y = j * h;
                    m_points[i, j] = CreateSpriteElement("point", new Vector3(x, y, 16f) + pointsOffset);
                }
            }
        }

        Vector3 iconsOffset = new Vector3(23f, 20, -1);
        Vector3 levelsOffset = new Vector3(22, 37, 1);
        void BuildLevels()
        {
            m_levels = new tk2dSprite[m_stats.Length, 8];
            m_icons = new tk2dSprite[m_stats.Length];
            float x, y;
            float levelWidth = 10, levelHeight = 6, spacingX = 8, spacingY = 2;
            for (int i = 0; i < m_levels.GetLength(0); i++)
            {
                x = (i * (levelWidth + spacingX)) + levelsOffset.x;
                m_icons[i] = CreateSpriteElement($"icon_{m_stats[i].name}", new Vector3(x - 3, iconsOffset.y, -16f)); ;
                for (int j = 0; j < m_levels.GetLength(1); j++)
                {
                    y = (j * (levelHeight + spacingY)) + levelsOffset.y;
                    m_levels[i, j] = CreateSpriteElement("level", new Vector3(x, y, 16f));
                }
            }
        }

        tk2dSprite CreateSpriteElement(string name, Vector3 offset)
        {
            var sprite = SpriteBuilder.SpriteFromResource($"{spriteDir}/{name}").GetComponent<tk2dSprite>();
            sprite.transform.SetParent(m_extantGUI);
            sprite.PlaceAtLocalPositionByAnchor(offset / 16f, tk2dBaseSprite.Anchor.LowerLeft);
            sprite.scale = Vector2.zero;
            sprite.SortingOrder = 1;
            sprite.IsPerpendicular = false;
            m_panel.AttachRenderer(sprite);
            return sprite;
        }

        float lastXPPercent = 0;
        public void UpdateXP(float percentage)
        {
            var scale = m_xpBar.scale;
            scale.y *= percentage;
            m_xpBar.scale = scale;
            lastXPPercent = percentage;
        }

        void UpdateLevels()
        {
            for (int i = 0; i < m_stats.Length; i++)
            {
                int level = m_stats[i].level;
                for (int j = 0; j < m_levels.GetLength(1); j++)
                {
                    m_levels[i, j].SetColor(j < level ? lumGreen : lumWhite);
                }
            }
        }

        Vector2 scale;
        void FixedUpdate()
        {
            if (!shown)
            {
                if (ShrinkComponents())
                {
                    scale = m_panel.scale;
                    if (Vector2.Distance(scale, targetScale) < .05f)
                        m_panel.scale = targetScale;
                    else
                        m_panel.scale = Vector2.Lerp(scale, targetScale, .2f);
                }
            }
            else
            {
                float dist = Vector2.Distance(scale, targetScale);
                scale = m_panel.scale;
                if (dist < .05f)
                {
                    m_panel.scale = targetScale;
                    ShrinkComponents();
                }
                else
                    m_panel.scale = Vector2.Lerp(scale, targetScale, .2f);
            }

            m_panel.PlaceAtPositionByAnchor(m_player.primaryHand.sprite.WorldCenter.ToVector3ZUp() + new Vector3(0, 0, 10f), tk2dBaseSprite.Anchor.MiddleCenter);

        }


        public bool ShrinkComponents()
        {
            Vector2 scale = m_levels[0, 0].scale;
            float dist = Vector2.Distance(scale, targetScale);
            if (Vector2.Distance(scale, targetScale) < .05f)
            {
                if (dist != 0)
                    SetComponentScale(targetScale);
                return true;
            }

            SetComponentScale(Vector2.Lerp(scale, targetScale, .4f));
            return false;

        }

        void SetComponentScale(Vector2 scale)
        {
            for (int i = 0; i < m_levels.GetLength(0); i++)
            {
                m_icons[i].scale = scale;
                for (int j = 0; j < m_levels.GetLength(1); j++)
                {
                    m_levels[i, j].scale = scale;
                }
            }

            for (int i = 0; i < m_points.GetLength(0); i++)
                for (int j = 0; j < m_points.GetLength(1); j++)
                    m_points[i, j].scale = scale;

            m_readMe.scale = scale;
            m_xpBar.scale = scale;
            UpdateXP(lastXPPercent);
        }


        bool locked = true;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
                Toggle();
            if (Key(GungeonActions.GungeonActionType.Reload) && KeyTime(GungeonActions.GungeonActionType.Reload) > .5f && !locked)
            {
                Toggle();
                locked = true;
            }

            if (!Key(GungeonActions.GungeonActionType.Reload))
                locked = false;

            if (shown)
            {
                if (KeyDown(GungeonActions.GungeonActionType.SelectLeft))
                    MoveCursor(-1);
                if (KeyDown(GungeonActions.GungeonActionType.SelectRight))
                    MoveCursor(1);
                if (KeyDown(GungeonActions.GungeonActionType.Interact))
                    AddLevel();
            }
        }

        public void SetPoints(int points)
        {
            for (int j = 0; j < m_points.GetLength(1); j++)
                for (int i = 0; i < m_points.GetLength(0); i++)
                    m_points[i, j].SetColor((i + j * 5) >= points ? lumGrey : lumWhite);
        }

        void AddLevel()
        {
            m_item.AddLevel(selectedStat);
            UpdateLevels();
        }

        void MoveCursor(int dir)
        {
            selectedStat = Mathf.Clamp(selectedStat + dir, 0, m_stats.Length - 1);
            for (int i = 0; i < m_icons.Length; i++)
                m_icons[i].SetColor((i == selectedStat) ? lumMagenta : lumWhite);
            AkSoundEngine.PostEvent("Play_UI_menu_select_01", m_player.gameObject);

        }

        void InitializeAppearance()
        {
            m_panel.renderer.material.shader = holoShader;
            foreach (var p in m_panel.GetComponentsInChildren<tk2dSprite>())
                if (p != m_panel)
                    p.SetColor(lumWhite);

            for (int i = 0; i < m_icons.Length; i++)
                m_icons[i].SetColor((i == selectedStat) ? lumMagenta : lumWhite);

            SetPoints(m_item.levelsToSpend);
            UpdateLevels();
        }

        bool shown;
        void Toggle()
        {
            shown = !shown;
            if (shown)
            {
                targetScale = baseScale;
                m_player.SetInputOverride("Leveler");
            }
            else
            {
                m_player.ClearInputOverride("Leveler");
                targetScale = Vector2.zero;
            }
        }

        public void Destroy()
        {
            Destroy(m_extantGUI);
            Destroy(this);
        }

        public float KeyTime(GungeonActions.GungeonActionType action)
        {
            return BraveInput.GetInstanceForPlayer(m_player.PlayerIDX).ActiveActions.GetActionFromType(action).PressedDuration;
        }

        public bool KeyDown(GungeonActions.GungeonActionType action)
        {
            return BraveInput.GetInstanceForPlayer(m_player.PlayerIDX).ActiveActions.GetActionFromType(action).WasPressed;
        }

        public bool Key(GungeonActions.GungeonActionType action)
        {
            return BraveInput.GetInstanceForPlayer(m_player.PlayerIDX).ActiveActions.GetActionFromType(action).IsPressed;
        }
    }
}
