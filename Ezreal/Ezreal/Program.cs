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
namespace Ezreal
{
    class Program
    {
        public const string ChampionName = "Ezreal";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        //ManaMenager
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static double WCastTime = 0;
        public static double QCastTime = 0;
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
            Q = new Spell(SpellSlot.Q, 1200);
            Q2 = new Spell(SpellSlot.Q, 1600);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 2500f);
            R1 = new Spell(SpellSlot.R, 2500f);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(1f, 200f, 2000f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q); 
            SpellList.Add(Q2);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(R1);
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
            Config.AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            #region Combo
                Config.SubMenu("R option").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
                Config.SubMenu("R option").AddItem(new MenuItem("Rcc", "R cc").SetValue(true));
                Config.SubMenu("R option").AddItem(new MenuItem("Raoe", "R aoe").SetValue(true));
                Config.SubMenu("R option").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
                Config.SubMenu("R option").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            #endregion
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#008aff\">E</font>zreal full automatic AI ver 1.5 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        public static void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            var t = TargetSelector.GetTarget(Q.Range + 300, TargetSelector.DamageType.Physical);
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion) )
                    Q.Cast(minion);
            }

            if (mobs.Count > 0 && Q.IsReady() && Orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                var mob = mobs[0];
                    Q.Cast(mob, true);
            }
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;
            if (Orbwalker.ActiveMode.ToString() == "Combo" && E.IsReady() && Config.Item("autoE").GetValue<bool>())
            {
                ManaMenager();
                var t2 = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
                var t = TargetSelector.GetTarget( Q.Range, TargetSelector.DamageType.Physical);
                if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA 
                    && ObjectManager.Player.CountEnemiesInRange(250) > 0 
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3)
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                else if (ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4 
                    && !ObjectManager.Player.UnderTurret(true) 
                    && (Game.Time - QCastTime > 0.6)
                    && t2.Position.Distance(Game.CursorPos) < t2.Position.Distance(ObjectManager.Player.Position)
                     && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3)
                {
                    if (t.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                     && Q.IsReady() && W.IsReady()
                     && Q.GetDamage(t) + W.GetDamage(t) + E.GetDamage(t) > t.Health
                     && !Orbwalking.InAutoAttackRange(t)
                     && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                         )
                     {
                         E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                         if (Config.Item("debug").GetValue<bool>())
                             Game.PrintChat("E kill Q");
                     }
                    else if (t2.IsValidTarget()
                     && ObjectManager.Player.Mana > EMANA + RMANA
                     && ObjectManager.Player.GetAutoAttackDamage(t2)  + E.GetDamage(t2) > t2.Health
                     && !Orbwalking.InAutoAttackRange(t2))

                    {
                        E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                        if (Config.Item("debug").GetValue<bool>())
                            Game.PrintChat("E kill aa");
                        QCastTime = Game.Time;
                    }
                }
            }
            
            if (Q.IsReady() )
            {
                ManaMenager();
                bool cast = false;
                bool wait = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(Q.Range + 100) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(Q.Delay + (Player.Distance(target.ServerPosition) / Q.Speed) * 1000));
                        var Qdmg = Q.GetDamage(target);
                        if (Qdmg > predictedHealth)
                        {
                            cast = true;
                            wait = true;
                            PredictionOutput output = R.GetPrediction(target);
                            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                            direction.Normalize();
                            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == target.SkinName || !cast)
                                    continue;
                                PredictionOutput prediction = R.GetPrediction(enemy);
                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - Player.ServerPosition;
                                Vector3 w = predictedPosition - Player.ServerPosition;
                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;
                                Vector3 pb = Player.ServerPosition + ((float)b * v);
                                float length = Vector3.Distance(predictedPosition, pb);
                                if (length < (Q.Width + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            if ( cast && target.IsValidTarget(Q.Range + 100))
                            {
                                Q.Cast(target, true);
                                if (Config.Item("debug").GetValue<bool>() && !Q.IsReady())
                                    Game.PrintChat("Q ks");
                            }
                        }
                    }
                }
                var t = TargetSelector.GetTarget(Q.Range - 50, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && Q.IsReady() && !wait)
                {
                    var wDmg = W.GetDamage(t);
                    var qDmg = Q.GetDamage(t);
                    if (wDmg + qDmg > t.Health && ObjectManager.Player.Mana > WMANA + QMANA)
                        W.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA )
                    {
                        Q.Cast(t, true);
                    }
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true) )
                    {
                        if (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.7)
                            Q.Cast(t, true);
                        else if (t.Path.Count() == 1)
                            Q.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                    }

                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + QMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                Q.CastIfHitchanceEquals(t, HitChance.High, true);
                            }
                        }
                    }
                }
                if ((Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear") && Config.Item("farmQ").GetValue<bool>() && Q.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA  + WMANA + QMANA * 3)
                    farmQ();
            }
            if (W.IsReady())
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var wDmg = W.GetDamage(t);
                    var qDmg = Q.GetDamage(t);
                    if (wDmg > t.Health)
                        W.Cast(t, true);
                    else if (wDmg + qDmg> t.Health && ObjectManager.Player.Mana > WMANA + QMANA )
                        W.Cast(t, true);
                    else if (t.Path.Count() == 1 && Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    {
                        W.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                    }
                    else if (t.Path.Count() == 1 &&Farm && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8)
                    {
                            W.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                    }
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + WMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                W.CastIfHitchanceEquals(t, HitChance.High, true);
                            }
                        }
                    }

                }
            }
            PotionMenager();

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && ObjectManager.Player.CountEnemiesInRange(800) == 0 && (Game.Time - QCastTime > 0.6))
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(R.Range)  &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && 
                        !target.HasBuffOfType(BuffType.SpellImmunity) && 
                        !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        double Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                            Rdmg = getRdmg(target);

                        if (target.IsValidTarget(R.Range) && Rdmg > predictedHealth && target.CountAlliesInRange(400) == 0)
                        {
                            if (Config.Item("hitchanceR").GetValue<bool>() && target.Path.Count() == 1)
                                R.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                            else
                                R.Cast(target, true);
                        }

                        else if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                             target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                             target.HasBuffOfType(BuffType.Taunt))
                        {
                            if (target.IsValidTarget(R.Range) && Rdmg * target.CountAlliesInRange(1100) > predictedHealth)
                                R.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                        }
                        else if (target.IsValidTarget(R.Range) && Orbwalker.ActiveMode.ToString() == "Combo")
                        {
                            R.CastIfWillHit(target, 3, true);
                        }
                        else if (Rdmg > predictedHealth && target.HasBuff("Recall"))
                        {
                            R.Cast(target, true, true);
                            if (Config.Item("debug").GetValue<bool>())
                                Game.PrintChat("R recall");
                        }
                        else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg * 2 > predictedHealth)
                        {
                            R.CastIfWillHit(target, 2, true);
                            if (Config.Item("debug").GetValue<bool>())
                                Game.PrintChat("R aoe 2");
                        }
                      
                    }
                }
            }
            
        }


        private static double getRdmg(Obj_AI_Hero target)
        {
                var rDmg = R.GetDamage(target);
                var dmg = 0;
                PredictionOutput output = R.GetPrediction(target);
                Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                direction.Normalize();
                List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                foreach (var enemy in enemies)
                {
                    PredictionOutput prediction = R.GetPrediction(enemy);
                    Vector3 predictedPosition = prediction.CastPosition;
                    Vector3 v = output.CastPosition - Player.ServerPosition;
                    Vector3 w = predictedPosition - Player.ServerPosition;
                    double c1 = Vector3.Dot(w, v);
                    double c2 = Vector3.Dot(v, v);
                    double b = c1 / c2;
                    Vector3 pb = Player.ServerPosition + ((float)b * v);
                    float length = Vector3.Distance(predictedPosition, pb);
                    if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                        dmg ++;
                }
                var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
                foreach (var minion in allMinionsR)
                {
                    PredictionOutput prediction = R.GetPrediction(minion);
                    Vector3 predictedPosition = prediction.CastPosition;
                    Vector3 v = output.CastPosition - Player.ServerPosition;
                    Vector3 w = predictedPosition - Player.ServerPosition;
                    double c1 = Vector3.Dot(w, v);
                    double c2 = Vector3.Dot(v, v);
                    double b = c1 / c2;
                    Vector3 pb = Player.ServerPosition + ((float)b * v);
                    float length = Vector3.Distance(predictedPosition, pb);
                    if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                        dmg++;
                }
                //if (Config.Item("debug").GetValue<bool>())
                //    Game.PrintChat("R collision" + dmg);
                if (dmg > 7)
                    return rDmg * 0.7;
                else
                    return rDmg - (rDmg * 0.1 * dmg);
                
        }


        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            
            
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode.ToString() == "LaneClear" && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA * 3 && Q.IsReady())
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
                var t = TargetSelector.GetTarget(Q.Range + 300, TargetSelector.DamageType.Physical);
                foreach (var minion in allMinionsQ)
                {
                    if ( minion.Health > ObjectManager.Player.GetAutoAttackDamage(minion) && minion.Health < Q.GetDamage(minion) && Orbwalker.GetTarget() != minion && (!t.IsValidTarget() || ObjectManager.Player.UnderTurret(false)))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range),true); 
                return;
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
                        QCastTime = Game.Time;
                    }
                    if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(Q.Range) && Q.IsReady())
                    {
                        var qDmg = Q.GetDamage(target);
                        if (qDmg > HpLeft && HpLeft > 0)
                        {
                            Q.Cast(target, true);
                            if (Config.Item("debug").GetValue<bool>())
                                Game.PrintChat("Q ks OPS");
                        }
                    }
                    if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(W.Range) && W.IsReady())
                    {
                        var wDmg = W.GetDamage(target);
                        if (wDmg > HpLeft && HpLeft > 0)
                        {
                            W.Cast(target, true);
                            if (Config.Item("debug").GetValue<bool>())
                                Game.PrintChat("W ks OPS");
                        }
                    }
                    if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(R.Range) && R.IsReady() && ObjectManager.Player.CountEnemiesInRange(700) == 0)
                    {
                        var rDmg = R.GetDamage(target);
                        
                        if (rDmg > HpLeft && HpLeft > 0  && target.CountAlliesInRange(500) == 0)
                        {
                            R.Cast(target, true);
                            if (Config.Item("debug").GetValue<bool>())
                                Game.PrintChat("R OPS");
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

            if (Config.Item("noti").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Aqua);
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                if (t.IsValidTarget() && R.IsReady())
                {
                    double rDamage = R.GetDamage(t);
                    if (rDamage > predictedHealth)
                        rDamage = getRdmg(t);
                    if (rDamage > predictedHealth)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        Render.Circle.DrawCircle(t.ServerPosition, 200, System.Drawing.Color.Red);
                    }
                    if (Config.Item("useR").GetValue<KeyBind>().Active)
                    {
                        R1.Cast(t, true, true);
                    }
                }
                var tw = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {

                    Render.Circle.DrawCircle(tw.ServerPosition, 100, System.Drawing.Color.Gold);
                    if (Q.GetDamage(tw) > tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                    else if (Q.GetDamage(tw) + W.GetDamage(tw) > tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                    else if (Q.GetDamage(tw) + Q.GetDamage(tw)+ E.GetDamage(tw)> tw.Health)
                    {
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W + E kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}