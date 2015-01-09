using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;

namespace Annie
{
    class Program
    {
        public const string ChampionName = "Annie";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        //ManaMenager
        public static int QMANA;
        public static int WMANA;
        public static int EMANA;
        public static int RMANA;
        public static bool Farm = false;
        public static double WCastTime = 0;
        //AutoPotion
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);
        //Menu
        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 625f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 600f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Load the orbwalker and add it to the submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();
            Config.AddItem(new MenuItem("noti", "Show notification").SetValue(true));
            Config.AddItem(new MenuItem("pots", "Use pots").SetValue(true));
            Config.AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.PrintChat("<font color=\"#ff00d8\">J</font>inx full automatic SI ver 1.5 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
            Orbwalking.BeforeAttack += args =>
            {
                try
                {
                    if (args.Target.IsValid<Obj_AI_Minion>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    {
                        switch (ConfigValue<StringList>("AttackMinions").SelectedIndex)
                        {
                            case 0: // Smart
                                args.Process = AttackMinion;
                                break;

                            case 1: // Never
                                args.Process = false;
                                break;
                        }
                    }

                    if (args.Target.IsValid<Obj_AI_Hero>() && !ConfigValue<bool>("AttackChampions") &&
                        Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None)
                    {
                        args.Process = false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            };
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode.ToString() == "Combo")
                Orbwalking.Attack = false;

        }

        public static bool InFountain()
        {
            float fountainRange = 750;
            if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.SummonersRift)
                fountainRange = 1050;
            return ObjectManager.Get<Obj_SpawnPoint>()
                    .Where(spawnPoint => spawnPoint.IsAlly)
                    .Any(spawnPoint => Vector2.Distance(ObjectManager.Player.ServerPosition.To2D(), spawnPoint.Position.To2D()) < fountainRange);
        }
        private static int CountEnemies(Obj_AI_Base target, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.IsValidTarget() && hero.Team != ObjectManager.Player.Team &&
                            hero.ServerPosition.Distance(target.ServerPosition) <= range);
        }
        private static int CountAlliesNearTarget(Obj_AI_Base target, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Count(
                        hero =>
                            hero.Team == ObjectManager.Player.Team &&
                            hero.ServerPosition.Distance(target.ServerPosition) <= range);
        }
        public static int GetPassiveStacks()
        {
            var buffs = Player.Buffs.Where(buff => (buff.Name.ToLower() == "pyromania" || buff.Name.ToLower() == "pyromania_particle"));
            if (buffs.Any())
            {
                var buff = buffs.First();
                if (buff.Name.ToLower() == "pyromania_particle")
                    return 4;
                else
                    return buff.Count;
            }
            return 0;
        }
    }
}
