using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using System.Diagnostics;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using System.Threading;

namespace OneKeyToBrain
{
    class Program
    {

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;

        public static Vector3 positionWard;
        private static Obj_AI_Hero WardTarget;
        private static float WardTime= 0;

        public static Items.Item WardS = new Items.Item(2043, 600f);
        public static Items.Item WardN = new Items.Item(2044, 600f);
        public static Items.Item TrinketN = new Items.Item(3340, 600f);
        public static Items.Item SightStone = new Items.Item(2049, 600f);
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);

        public static Menu Config;

        private static Obj_AI_Hero myHero;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            myHero = ObjectManager.Player;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Config = new Menu("OKTW Brain", "OKTW Brain", true);
            Config.AddToMainMenu();

            Config.SubMenu("Iteams").AddItem(new MenuItem("pots", "Use pots").SetValue(true));
            Config.AddItem(new MenuItem("click", "Show enemy click").SetValue(true));
            Config.AddItem(new MenuItem("infoCombo", "Show info combo").SetValue(true));
            Config.SubMenu("Wards").AddItem(new MenuItem("ward", "Auto ward enemy in Grass").SetValue(false));
            Config.SubMenu("Wards").AddItem(new MenuItem("wardC", "Only Combo").SetValue(false));
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));

            Config.SubMenu("Combo Key").AddItem(new MenuItem("Combo", "Combo").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            
            Drawing.OnDraw += Drawing_OnDraw;
            
            Game.OnGameUpdate += Game_OnGameUpdate;
            
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("ward").GetValue<bool>() || (Config.Item("Combo").GetValue<KeyBind>().Active && Config.Item("wardC").GetValue<bool>()))
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(1300)))
                {


                        bool WallOfGrass = NavMesh.IsWallOfGrass(Prediction.GetPrediction(enemy, 0.3f).CastPosition, 0);
                        if (WallOfGrass)
                        {
                            positionWard = Prediction.GetPrediction(enemy, 0.3f).CastPosition;
                            WardTarget = enemy;
                            WardTime = Game.Time;
                        }
                    
                }
                if (myHero.Distance(positionWard) < 600 && !WardTarget.IsValidTarget() && Game.Time - WardTime < 5)
                {
                    WardTime = Game.Time - 6;
                    if (TrinketN.IsReady())
                        TrinketN.Cast(positionWard);
                    else if (SightStone.IsReady())
                        SightStone.Cast(positionWard);
                    else if (WardS.IsReady())
                        WardS.Cast(positionWard);
                    else if (WardN.IsReady())
                        WardN.Cast(positionWard);
                }
            }
            


        }
        public static void drawText(string msg, Obj_AI_Hero Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            
            var tw = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);

            if (tw.IsValidTarget())
            {
                if (Config.Item("click").GetValue<bool>())
                {
                    List<Vector2> waypoints = tw.GetWaypoints();
                    Render.Circle.DrawCircle(waypoints.Last<Vector2>().To3D(), 50, System.Drawing.Color.Red);
                }
            }
           if (Config.Item("infoCombo").GetValue<bool>())
            {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(2000)))
            {
                string combo;
                var hpCombo = Q.GetDamage(enemy) + W.GetDamage(enemy) + E.GetDamage(enemy);
                var hpLeft = enemy.Health - Q.GetDamage(enemy) + W.GetDamage(enemy) + E.GetDamage(enemy) + R.GetDamage(enemy);
                if (Q.GetDamage(enemy) > enemy.Health)
                    combo = "Q";
                else if (Q.GetDamage(enemy) + W.GetDamage(enemy)> enemy.Health)
                    combo = "QW";
                else if (Q.GetDamage(enemy) + W.GetDamage(enemy) + E.GetDamage(enemy) > enemy.Health)
                    combo = "QWE";
                else if (Q.GetDamage(enemy) + W.GetDamage(enemy) + E.GetDamage(enemy) + R.GetDamage(enemy) > enemy.Health)
                    combo = "QWER";
                else 
                {
                    if (myHero.FlatPhysicalDamageMod > myHero.FlatMagicDamageMod)
                        combo = "QWER+" + (int)(hpLeft / (myHero.Crit * myHero.GetAutoAttackDamage(enemy) + myHero.GetAutoAttackDamage(enemy))) + " AA";
                    else
                        combo = "QWER+" + (int)(hpLeft / hpCombo) + "QWE";
                }
                if (hpLeft > hpCombo)
                    drawText(combo, enemy, System.Drawing.Color.Red);
                else if (hpLeft < 0)
                    drawText(combo, enemy, System.Drawing.Color.Red);
                else if (hpLeft > 0)
                    drawText(combo, enemy, System.Drawing.Color.Yellow);
            }
            }

        }
        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                
                Game.PrintChat(msg);
        }
        public static void PotionMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost; ;   
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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA )
                        ManaPotion.Cast();
                }
            }
        }

    }
}
