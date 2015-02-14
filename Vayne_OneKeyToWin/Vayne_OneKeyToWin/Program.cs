using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using System.Threading;

namespace Vayne_OneKeyToWin
{
    class Program
    {
        public const string ChampionName = "Vayne";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;

        public static Spell E;
        public static Spell R;

        //ManaMenager
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
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
            Q = new Spell(SpellSlot.Q, 300);
            E = new Spell(SpellSlot.E, 560);
            R = new Spell(SpellSlot.R, 3000);


            SpellList.Add(Q);

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
            Config.AddItem(new MenuItem("opsE", "OnProcessSpellCastW").SetValue(true));
            Config.AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.AddItem(new MenuItem("useE", "Dash E HotKeySmartcast").SetValue(new KeyBind('t', KeyBindType.Press)));
            Config.AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat("<font color=\"#7e62cc\">C</font>aitlyn full automatic SI ver 1.4 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;

            

            if (E.IsReady())
            {
                CondemnCheck(ObjectManager.Player.ServerPosition);
                var t = TargetSelector.GetTarget(200, TargetSelector.DamageType.Physical);
                if (E.IsReady() && !Q.IsReady() && t.IsValidTarget() && t.IsMelee())
                    Q.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, Q.Range), true);
            }


           if (Orbwalker.ActiveMode.ToString() == "Combo" && Q.IsReady() )
            {
                ManaMenager();

                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                var t2 = TargetSelector.GetTarget(200, TargetSelector.DamageType.Physical);
                if (Q.IsReady()
                    && t2.IsValidTarget() && t2.IsMelee()
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, Q.Range).CountEnemiesInRange(500) < 3)
                    Q.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, Q.Range), true);
                if (t.IsValidTarget() && Q.IsReady()
                      && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                      && !Orbwalking.InAutoAttackRange(t))
                {
                    Q.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, Q.Range), true);
                }
                
            }

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && !ObjectManager.Player.UnderTurret(true))
            {
               
            }
            PotionMenager();
        }

        public static void CondemnCheck(Vector3 fromPosition)
        {
            //VHReborn Condemn Code
            foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(E.Range)))
            {
                var pushDistance = 370;
                var targetPosition = E.GetPrediction(target).UnitPosition;
                var finalPosition = targetPosition.Extend(fromPosition, -pushDistance);
                var numberOfChecks = Math.Ceiling(pushDistance / target.BoundingRadius);
                for (var i = 0; i < numberOfChecks; i++)
                {
                    var extendedPosition = targetPosition.Extend(fromPosition, -(float)(numberOfChecks * target.BoundingRadius));
                    var extendedPosition2 = targetPosition.Extend(fromPosition, -(float)(numberOfChecks * target.BoundingRadius + target.BoundingRadius / 4));
                    var extendedPosition3 = targetPosition.Extend(fromPosition, -(float)(numberOfChecks * target.BoundingRadius - target.BoundingRadius / 4));
                    if (extendedPosition.IsWall() || extendedPosition2.IsWall() || extendedPosition3.IsWall() ||  finalPosition.IsWall())
                    {
                        E.Cast(target);
                    }
                }

            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() )
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                    E.Cast(Target, true);
                return;
            }
            return;
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {


        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {

        }



        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {

        }


        private static float GetRealRange(GameObject target)
        {
            return 680f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        public static float bonusRange()
        {
            return 680f + ObjectManager.Player.BoundingRadius;
        }
        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;

            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost;

            RMANA = RMANA + (ObjectManager.Player.CountEnemiesInRange(2500) * 20);

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA)
                        ManaPotion.Cast();
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
        }
    }
}
