using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;

namespace OneKeyToWin_AIO_Sebby
{
    internal class Program
    {
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;

        public static int HitChanceNum= 4;
        public static int tickNum = 4;
        public static int tickIndex = 0;
        public static bool tickSkip = true;
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            Config = new Menu("OneKeyToWin AIO", "OneKeyToWin_AIO" + ObjectManager.Player.ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            
            switch (Player.ChampionName)
            {
                case "Jinx":
                    new Jinx().LoadOKTW();
                    break;
                case "Sivir":
                    new Sivir().LoadOKTW();
                    break;
                case "Ezreal":
                    new Ezreal().LoadOKTW();
                    break;
            }
            Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("OrbDraw", "Draw AAcirlce OKTW© style").SetValue(false));
            Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("orb", "Orbwalker target OKTW© style").SetValue(true));
            Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("1", "pls disable Orbwalking > Drawing > AAcirlce"));
            Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("2", "My HP: 0-30 red, 30-60 orange,60-100 green"));

            Config.SubMenu("Items").AddItem(new MenuItem("pots", "Use pots").SetValue(true));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("Hit", "Prediction OKTW©").SetValue(new Slider(4, 4, 0)));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("0", "0 - normal"));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("1", "1 - high"));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("2", "2 - high + max range fix"));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("3", "3 - normal + max range fix + waypionts analyzer"));
            Config.SubMenu("Prediction OKTW").AddItem(new MenuItem("4", "4 - high + max range fix + waypionts analyzer"));

            Config.SubMenu("Performance OKTW").AddItem(new MenuItem("pre", "OneSpellOneTick©").SetValue(true));
            Config.SubMenu("Performance OKTW").AddItem(new MenuItem("0", "OneSpellOneTick© is tick management"));
            Config.SubMenu("Performance OKTW").AddItem(new MenuItem("1", "ON - increase fps"));
            Config.SubMenu("Performance OKTW").AddItem(new MenuItem("2", "OFF - normal mode"));

            Config.SubMenu("About OKTW").AddItem(new MenuItem("0", "OneKeyToWin by Sebby"));
            Config.SubMenu("About OKTW").AddItem(new MenuItem("1", "Supported champions:"));
            Config.SubMenu("About OKTW").AddItem(new MenuItem("2", "Jinx, Sivir"));
            Config.SubMenu("About OKTW").AddItem(new MenuItem("3", "visit joduska.me"));

            
            Config.AddItem(new MenuItem("watermark", "Disabe Watermark").SetValue(false));
            Config.AddToMainMenu();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnDamage += Obj_AI_Base_OnDamage;
            Drawing.OnDraw += OnDraw;
        }

        private static void Obj_AI_Base_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            
        }

        private static void OnUpdate(EventArgs args)
        {
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
            if (LagFree(0))
            {
                if (Config.Item("pots").GetValue<bool>())
                    PotionManagement();
                tickSkip = Config.Item("pre").GetValue<bool>();
            }
        }


        public static bool LagFree(int offset)
        {
            if (!tickSkip)
            {
                return true;
            }
            
            //Console.WriteLine(tickIndex);
            
            if (tickIndex == offset)
            {
                return true;
            }
            else
                return false;
        }

        public static void PotionManagement()
        {
            if (!ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < 200)
                        ManaPotion.Cast();
                }
            }
        }

        public static bool ValidUlt(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
            || target.HasBuffOfType(BuffType.SpellImmunity)
            || target.IsZombie
            || target.HasBuffOfType(BuffType.Invulnerability)
            || target.HasBuffOfType(BuffType.SpellShield)
            )
                return false;
            else
                return true;
        }

        public static void CastSpell(Spell QWER, Obj_AI_Hero target)
        {

            HitChanceNum = Config.Item("Hit").GetValue<Slider>().Value;
            //HitChance 0 - 2
            // example CastSpell(Q, ts, 2);
            var poutput = QWER.GetPrediction(target);
            var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead);
            if (QWER.Collision && col > 0)
                return;
            if (HitChanceNum == 0)
                QWER.Cast(target, true);
            else if (HitChanceNum == 1)
            {
                if ((int)poutput.Hitchance > 4)
                    QWER.Cast(poutput.CastPosition);
            }
            else if (HitChanceNum == 2)
            {
                if ((target.IsFacing(Player) && (int)poutput.Hitchance == 5) || (target.Path.Count() == 0 && target.Position == target.ServerPosition))
                {
                    if (Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius * 2)))
                    {
                        QWER.Cast(poutput.CastPosition);
                    }
                }
                else if ((int)poutput.Hitchance == 5)
                {
                    QWER.Cast(poutput.CastPosition);
                }
            }
            else if (HitChanceNum == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 6 - QWER.Width;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                if (Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || Player.Distance(target.Position) < SiteToSite)
                    QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                else if (target.Path.Count() < 2
                    && (target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > BackToFront
                    || target.HasBuffOfType(BuffType.Slow) || target.HasBuff("Recall")
                    || (target.Path.Count() == 0 && target.Position == target.ServerPosition)
                    ))
                {
                    if (target.IsFacing(Player) || target.Path.Count() == 0)
                    {
                        if (Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius * 2)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
            }
            else if (HitChanceNum == 4 && (int)poutput.Hitchance > 4)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 6 - QWER.Width;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                if (Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || Player.Distance(target.Position) < SiteToSite)
                    QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                else if (target.Path.Count() < 2
                    && (target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > BackToFront
                    || target.HasBuffOfType(BuffType.Slow) || target.HasBuff("Recall")
                    || (target.Path.Count() == 0 && target.Position == target.ServerPosition)
                    ))
                {
                    if (target.IsFacing(Player) || target.Path.Count() == 0)
                    {
                        if (Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius * 2)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Config.Item("watermark").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0f, System.Drawing.Color.Cyan, "OneKeyToWin AIO - " + Player.ChampionName + " by Sebby");
            }

            if (Config.Item("OrbDraw").GetValue<bool>())
            {
                if (Player.HealthPercentage() > 60)
                    Utility.DrawCircle(ObjectManager.Player.Position, Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.GreenYellow, 2, 1);
                else if (Player.HealthPercentage() > 30)
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Orange, 3, 1);
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Red, 4, 1);
            }

            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();

                if (orbT.IsValidTarget())
                {
                    if (orbT.Health > orbT.MaxHealth * 0.6)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.GreenYellow, 5, 1);
                    else if (orbT.Health > orbT.MaxHealth * 0.3)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Orange, 5, 1);
                    else
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Red, 5, 1);
                }
            }
        }
    }
}
