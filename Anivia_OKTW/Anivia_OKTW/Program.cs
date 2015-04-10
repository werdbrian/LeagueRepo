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
        public const string ChampionName = "Anivia";

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

        public static GameObject QMissile;
        public static GameObject RMissile;

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
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 625);

            Q.SetSkillshot(.25f, 110f, 850f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(.5f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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

            Config.SubMenu("Farm").AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W").SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(60, 100, 30)));

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy").SetValue(true));

            Config.AddItem(new MenuItem("pots", "Use pots").SetValue(true));
            Config.AddItem(new MenuItem("AACombo", "AA in combo").SetValue(false));
            Config.AddItem(new MenuItem("Hit", "Hit Chance Skillshot").SetValue(new Slider(3, 3, 0)));


            //Add the events we are going to use:
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnDelete += Obj_AI_Base_OnDelete;
            Obj_AI_Base.OnCreate +=Obj_AI_Base_OnCreate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat("<font color=\"#ff00d8\">A</font>nivia full automatic AI ver 1.0 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }


        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Q.IsReady())
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(Q.Range))
                {
                    Q.Cast(Target);
                }
            }
            else if (W.IsReady())
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    W.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, 150), true);
                }
            }
            return;
        }

        static void Orbwalking_BeforeAttack(LeagueSharp.Common.Orbwalking.BeforeAttackEventArgs args)
        {
         
         
        }

        private static void Obj_AI_Base_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid)
            {
                if (obj.Name == "cryo_FlashFrost_Player_mis.troy")
                    QMissile = obj;
                if (obj.Name.Contains("cryo_storm"))
                    RMissile = obj;
            }
        }

        private static void Obj_AI_Base_OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.IsValid)
            {
                if (obj.Name == "cryo_FlashFrost_Player_mis.troy")
                    QMissile = null;
                if (obj.Name.Contains("cryo_storm"))
                    RMissile = null;
            }
        }
        
        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            PotionMenager();
            if (Combo && !Config.Item("AACombo").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(ObjectManager.Player.AttackRange + 150, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() && (ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health || ObjectManager.Player.Mana < RMANA))
                    Orbwalking.Attack = true;
                else
                    Orbwalking.Attack = false;
            }
            else
                Orbwalking.Attack = true;


            if (Q.IsReady() && QMissile == null)
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    if (t.HasBuffOfType(BuffType.Slow))
                        qDmg = 2 * qDmg;
                    if (qDmg > t.Health)
                        Q.Cast(t, true);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                        CastSpell(Q, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ((Farm &&  ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true))
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.BaseSkinName).GetValue<bool>()))
                        {
                            CastSpell(Q, enemy, Config.Item("Hit").GetValue<Slider>().Value);
                        }
                    }

                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                Q.Cast(enemy, true);
                            }
                        }
                    }
                }
            }
            if (Q.IsReady() && QMissile != null)
            {
                if (QMissile.Position.CountEnemiesInRange(220) > 0)
                    Q.Cast();
            }
            if (E.IsReady() )
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var eDmg = E.GetDamage(t);

                    if (eDmg > t.Health)
                        E.Cast(t, true);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA && QMissile == null)
                        E.Cast(t, true);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true) && QMissile == null)
                    {
                        E.Cast(t, true);
                    }
                }
            }
            if (R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range + 400 , TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (RMissile == null && ObjectManager.Player.Mana > RMANA + QMANA && Q.GetDamage(t) * 2 + R.GetDamage(t) > t.Health)
                        R.Cast(t, true, true);
                    if (RMissile == null && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                    {
                        R.Cast(t, true, true);
                    }
                }
                if (RMissile != null && (RMissile.Position.CountEnemiesInRange(450) == 0 || ObjectManager.Player.Mana < EMANA + QMANA))
                {
                    R.Cast();
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmW").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA * 2)
                {
                    var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
                    var Wfarm = W.GetCircularFarmLocation(allMinionsQ, R.Width);
                    if (Wfarm.MinionsHit > 4 && RMissile == null)
                        R.Cast(Wfarm.Position);
                    else if (RMissile != null )
                    {
                        R.Cast();
                    }
                }
            }
            if (W.IsReady())
            {
                var ta = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ta.IsValidTarget(W.Range) && ObjectManager.Player.Mana > RMANA + EMANA + WMANA && ta.Path.Count() == 1 && W.GetPrediction(ta).CastPosition.Distance(ta.Position) > 100)
                {
                    if (ObjectManager.Player.Position.Distance(ta.ServerPosition) > ObjectManager.Player.Position.Distance(ta.Position))
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) < ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(W.Range - 200))
                        {

                            CastSpell(W, ta, Config.Item("Hit").GetValue<Slider>().Value);

                        }
                    }
                    else
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) > ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(W.Range))
                        {
                            CastSpell(W, ta, Config.Item("Hit").GetValue<Slider>().Value);

                        }
                    }
                }
            }

        }

        private static void CastSpell(Spell QWER, Obj_AI_Hero target, int HitChanceNum)
        {
            //HitChance 0 - 2
            // example CastSpell(Q, ts, 2);
            if (HitChanceNum == 0)
                QWER.Cast(target, true);
            else if (HitChanceNum == 1)
                QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            else if (HitChanceNum == 2)
            {
                if (QWER.Delay < 0.3)
                    QWER.CastIfHitchanceEquals(target, HitChance.Dashing, true);
                QWER.CastIfHitchanceEquals(target, HitChance.Immobile, true);
                QWER.CastIfWillHit(target, 2, true);
                if (target.Path.Count() < 2)
                    QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
            }
            else if (HitChanceNum == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                //debug("" + target.Path.Count() + " " + (target.Position == target.ServerPosition) + (waypoints.Last<Vector2>().To3D() == target.ServerPosition));
                if (QWER.Delay < 0.3)
                    QWER.CastIfHitchanceEquals(target, HitChance.Dashing, true);
                QWER.CastIfHitchanceEquals(target, HitChance.Immobile, true);
                QWER.CastIfWillHit(target, 2, true);

                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 6 - QWER.Width;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                if (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || ObjectManager.Player.Distance(target.Position) < SiteToSite)
                    QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                else if (target.Path.Count() < 2
                    && (target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(target.Position)) > BackToFront
                    || target.HasBuffOfType(BuffType.Slow) || target.HasBuff("Recall")
                    || (target.Path.Count() == 0 && target.Position == target.ServerPosition)
                    ))
                {
                    if (target.IsFacing(ObjectManager.Player) || target.Path.Count() == 0)
                    {
                        if (ObjectManager.Player.Distance(target.Position) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.Position) / QWER.Speed) + (target.BoundingRadius)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
            }
        }

        private static bool Combo
        {
            get { return Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo; }
        }
        private static bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
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
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (RMissile != null)
            Render.Circle.DrawCircle(RMissile.Position, 200, System.Drawing.Color.Cyan);

            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && Q.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && R.IsReady())
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Red);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Red);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && W.IsReady())
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Yellow);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Yellow);
            }
        }
    }
}
