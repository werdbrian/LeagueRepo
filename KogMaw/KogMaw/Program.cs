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

namespace KogMaw
{
    class Program
    {
        public const string ChampionName = "KogMaw";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static bool attackNow = true;
        //ManaMenager
        public static float qRange = 1100;
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static bool Esmart = false;
        public static double WCastTime = 0;
        public static double OverKill = 0;
        public static double OverFarm = 0;
        public static double lag = 0;
        public static Stopwatch stopWatch = new Stopwatch();
        //AutoPotion
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);
        public static int Muramana = 3042;
        public static int Tear = 3070;
        public static int Manamune = 3004;
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
            Q = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 1200);
            R = new Spell(SpellSlot.R, 1800);


            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.5f, 225f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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


            Config.SubMenu("Iteams").AddItem(new MenuItem("mura", "Auto Muramana").SetValue(true));
            Config.SubMenu("Iteams").AddItem(new MenuItem("pots", "Use pots").SetValue(true));

            Config.SubMenu("E config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));

            Config.SubMenu("W config").AddItem(new MenuItem("autoW", "auto W").SetValue(true));
            Config.SubMenu("W config").AddItem(new MenuItem("harasW", "haras W").SetValue(true));

            Config.SubMenu("R option").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("comboStack", "Max combo stack R").SetValue(new Slider(2, 5, 0)));
            Config.SubMenu("R option").AddItem(new MenuItem("harasStack", "Max haras stack R").SetValue(new Slider(1, 5, 0)));
            Config.SubMenu("R option").AddItem(new MenuItem("Rcc", "R cc").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("Rslow", "R slow").SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("Raoe", "R aoe").SetValue(true));

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("orb", "Orbwalker target").SetValue(true));

            Config.AddItem(new MenuItem("Hit", "Hit Chance Skillshot").SetValue(new Slider(2, 3, 0)));
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#008aff\">K</font>og Maw full automatic AI ver 1.0 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();

            W.Range = 650 + 110 + 20 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                Farm = true;
            else
                Farm = false;

            if (Orbwalker.GetTarget() == null)
                attackNow = true;

            if (Config.Item("autoW").GetValue<bool>() && W.IsReady() && ObjectManager.Player.CountEnemiesInRange(W.Range) > 0 )
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    W.Cast();
                else if (Farm && Config.Item("harasW").GetValue<bool>())
                    W.Cast();
            }
            if (Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (t.IsValidTarget(W.Range) && qDmg + eDmg > t.Health)
                        CastSpell(Q, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + EMANA )
                        CastSpell(Q, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true) )
                    {
                        CastSpell(Q, t, Config.Item("Hit").GetValue<Slider>().Value);
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
            if (E.IsReady() && attackNow)
            {
                //W.Cast(ObjectManager.Player);
                ManaMenager();
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (eDmg > t.Health)
                        CastSpell(E, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (eDmg + qDmg > t.Health && Q.IsReady())
                        CastSpell(E, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(E, t, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                E.Cast(enemy, true);
                            }
                        }
                    }
                }
            }
            PotionMenager();

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && attackNow)
            {
                R.Range = 900 + 300 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (target.IsValidTarget(R.Range) && !target.HasBuffOfType(BuffType.PhysicalImmunity) &&
                    !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                {
                    float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                    double Rdmg = R.GetDamage(target);

                    if ( R.GetDamage(target) > target.Health)
                        CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && R.GetDamage(target) * 2 > target.Health && GetRStacks() < 3 && ObjectManager.Player.Mana > RMANA * 3)
                    {
                        CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                    }
                   
                    else if ((target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                        target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                        target.HasBuffOfType(BuffType.Taunt)) && Config.Item("Rcc").GetValue<bool>() && GetRStacks() < 4 && ObjectManager.Player.Mana > RMANA * 3)
                    {
                        R.Cast(target, true);
                    }
                    else if (target.HasBuffOfType(BuffType.Slow) && Config.Item("Rslow").GetValue<bool>() && GetRStacks() < 3 && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetRStacks() < Config.Item("comboStack").GetValue<Slider>().Value && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                    else if ( Farm && GetRStacks() < Config.Item("harasStack").GetValue<Slider>().Value && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                }
            }
        }

        private static void CastSpell(Spell QWER, Obj_AI_Hero target, int HitChanceNum)
        {
            //HitChance 0 - 3
            // example CastSpell(Q, target, 3);
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
                if (QWER.Delay < 0.3)
                    QWER.CastIfHitchanceEquals(target, HitChance.Dashing, true);
                QWER.CastIfHitchanceEquals(target, HitChance.Immobile, true);
                QWER.CastIfWillHit(target, 2, true);

                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 6 - QWER.Width;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)) * 2;
                if (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || ObjectManager.Player.Distance(target.Position) < SiteToSite)
                {

                    if (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) <= ObjectManager.Player.Distance(target.Position))
                    {
                        if (ObjectManager.Player.Distance(target.ServerPosition) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
                else if (target.Path.Count() < 2
                    && (target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(target.Position)) > BackToFront
                    || target.Position == target.ServerPosition))
                {

                    if (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) <= ObjectManager.Player.Distance(target.Position))
                    {
                        if (ObjectManager.Player.Distance(target.ServerPosition) < QWER.Range - ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed)))
                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                    else
                    {
                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
            }
        }

        private static int GetRStacks()
        {
            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }
            return 0;
        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;
            if (Config.Item("mura").GetValue<bool>())
            {
                int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                if (args.Target.IsEnemy && args.Target.IsValid<Obj_AI_Hero>() && Items.HasItem(Mur) && Items.CanUseItem(Mur) && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA)
                {
                    if (!ObjectManager.Player.HasBuff("Muramana"))
                        Items.UseItem(Mur);
                }
                else if (ObjectManager.Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                    Items.UseItem(Mur);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(Target, true);
                    debug("E AGC");
                }
            }
            return;
        }


        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                {

                    var dmg = unit.GetSpellDamage(target, args.SData.Name);
                    double HpLeft = target.Health - dmg;
                    if (HpLeft < 0 && target.IsValidTarget() && target.IsValidTarget(R.Range))
                    {
                        OverKill = Game.Time;
                        debug("OverKill detection " + target.ChampionName);
                    }
                    if (target.IsValidTarget(Q.Range) && Q.IsReady())
                    {
                        var qDmg = Q.GetDamage(target);
                        if (qDmg > HpLeft && HpLeft > 0)
                        {
                            CastSpell(Q, target, Config.Item("Hit").GetValue<Slider>().Value);
                            debug("Q ks OPS");
                        }
                    }
                    if (target.IsValidTarget(W.Range) && W.IsReady())
                    {
                        var wDmg = W.GetDamage(target);
                        if (wDmg > HpLeft && HpLeft > 0)
                        {
                            CastSpell(W, target, Config.Item("Hit").GetValue<Slider>().Value);
                            debug("W ks OPS");
                        }
                    }
                    if (Config.Item("autoR").GetValue<bool>() && target.IsValidTarget(R.Range) && R.IsReady() && ObjectManager.Player.CountEnemiesInRange(700) == 0)
                    {
                        double rDmg = R.GetDamage(target);
                        if (rDmg > HpLeft && HpLeft > 0)
                        {
                            debug("R OPS");
                            CastSpell(R, target, Config.Item("Hit").GetValue<Slider>().Value);
                        }
                    }
                }
            }
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;


            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost; ;

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
            Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange);
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && Q.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && W.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && E.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>() && E.IsReady())
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Green);
                    else
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Green);
            }
            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Pink);
            }
        }
    }
}