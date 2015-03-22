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
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;

        public static bool HaveStun = false;
        
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
            W = new Spell(SpellSlot.W, 600f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 625f);
            Q.SetTargetted(0.25f, 1400f);
            W.SetSkillshot(0.50f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.20f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            Config.AddItem(new MenuItem("autoE", "Auto E stack stun").SetValue(true));
            Config.AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.AddItem(new MenuItem("sup", "Support mode").SetValue(true));
            Config.AddItem(new MenuItem("rCount", "Auto R stun x enemies").SetValue(new Slider(3, 0, 5)));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu("R champions").AddItem(new MenuItem("ro" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));

            //Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Game.PrintChat("<font color=\"#ff00d8\">A</font>nie full automatic AI ver 1.3 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        static void Orbwalking_BeforeAttack(LeagueSharp.Common.Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("sup").GetValue<bool>() && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit))
            {
                if (((Obj_AI_Base)Orbwalker.GetTarget()).IsMinion) args.Process = false;
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (ObjectManager.Player.HasBuff("Recall"))
                return;


            ManaMenager();
            PotionMenager();
            HaveStun = GetPassiveStacks();
            
            
            
            if (Combo)
            {
                var t = TargetSelector.GetTarget(ObjectManager.Player.AttackRange + 150, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() && (ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health || ObjectManager.Player.Mana < RMANA))
                    Orbwalking.Attack = true;
                else
                    Orbwalking.Attack = false;
            }
            else
                Orbwalking.Attack = true;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                
                if (!HaveTibers && R.IsReady())
                {
                    if (Combo && HaveStun && target.CountEnemiesInRange(400) > 1)
                        R.Cast(target, true, true);
                    else if (Config.Item("rCount").GetValue<Slider>().Value > 0 && Config.Item("rCount").GetValue<Slider>().Value <= target.CountEnemiesInRange(300))
                        R.Cast(target, true, true);
                    else if (Combo && !W.IsReady() && !Q.IsReady()
                        && Q.GetDamage(target) < target.Health
                        && (target.CountEnemiesInRange(400) > 1 || R.GetDamage(target) + Q.GetDamage(target) > target.Health))
                        R.Cast(target, true, true);
                    else if (Combo && Q.GetDamage(target) < target.Health)
                        if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                                     target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||target.HasBuffOfType(BuffType.Taunt))
                        {
                            R.Cast(target, true, true);
                        }
                }
                if (W.IsReady() && (Farm || Combo))
                {
                    if (Combo && HaveStun && target.CountEnemiesInRange(250) > 1)
                        W.Cast(target, true, true);
                    else if (!Q.IsReady())
                        W.Cast(target, true, true);
                    else if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Charm) || 
                    target.HasBuffOfType(BuffType.Fear) ||target.HasBuffOfType(BuffType.Taunt))
                    {
                        W.Cast(target, true, true);
                    }
                }
                if (Q.IsReady() && (Farm || Combo))
                {
                    if (HaveStun && Combo && target.CountEnemiesInRange(400) > 1 && (W.IsReady() || R.IsReady()))
                    {
                        return;
                    }
                    else
                        Q.Cast(target, true);
                }
            }

            

            if (Config.Item("sup").GetValue<bool>())
            {
                if (Q.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ObjectManager.Player.Mana > RMANA + QMANA)
                    farmQ();
            }
            else
            {
                if (Q.IsReady() && (!HaveStun || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear))
                    farmQ();
            }

            if (Config.Item("autoE").GetValue<bool>() && E.IsReady() && !HaveStun && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                E.Cast();

            if (W.IsReady() && ObjectManager.Player.InFountain() && !HaveStun)
                W.Cast(ObjectManager.Player, true, true);
        }

        public static void farmQ()
        {
            if (!Config.Item("farmQ").GetValue<bool>())
                return;
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            if (Q.IsReady())
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    Q.Cast(mob, true);
                }
            }

            foreach (var minion in allMinionsQ)
            {
                if (minion.Health > ObjectManager.Player.GetAutoAttackDamage(minion) && minion.Health < Q.GetDamage(minion))
                {
                    Q.Cast(minion);
                }
            }

        }

        private static bool HaveTibers
        {
            get { return ObjectManager.Player.HasBuff("infernalguardiantimer"); }
        }

        private static bool Combo
        {
            get { return Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo; }
        }
        private static bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) ; }
        }
        public static bool GetPassiveStacks()
        {
            var buffs = Player.Buffs.Where(buff => (buff.Name.ToLower() == "pyromania" || buff.Name.ToLower() == "pyromania_particle"));
            if (buffs.Any())
            {
                var buff = buffs.First();
                if (buff.Name.ToLower() == "pyromania_particle")
                    return true;
                else
                    return false;
            }
            return false;
        }
        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost;

            if (Farm)
                RMANA = RMANA + ObjectManager.Player.CountEnemiesInRange(2500) * 20;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }
        public static void PotionMenager()
        {
            if (Config.Item("pots").GetValue<bool>() && !ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(700) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                        Potion.Cast();
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA + RMANA)
                        ManaPotion.Cast();
                }
            }
        }
    }
}
