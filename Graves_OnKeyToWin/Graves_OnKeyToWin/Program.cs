using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Graves_OnKeyToWin
{
    class Program
    {
        public const string ChampionName = "Graves";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell Q1;
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
        public static double OverKill = 0;
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
            Q = new Spell(SpellSlot.Q, 840f);
            Q1 = new Spell(SpellSlot.Q, 940f);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 450f);
            R = new Spell(SpellSlot.R, 1000f);
            R1 = new Spell(SpellSlot.R, 1600f);

            Q.SetSkillshot(0.26f, 10f * 2 * (float)Math.PI / 180, 1800f, false, SkillshotType.SkillshotCone);
            Q1.SetSkillshot(0.26f, 50f, 1950f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 2100f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.26f, 30f * (float)Math.PI / 180, 2100f, false, SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(Q1);
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
            Config.AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += afterAttack;
            Game.PrintChat("<font color=\"#9c3232\">G</font>raves full automatic AI ver 1.3 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;

            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                {
                    if (W.GetDamage(t) > t.Health)
                    {
                        W.Cast(t, true, true);
                        OverKill = Game.Time;
                        return;
                    }
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA)
                        W.Cast(t, true, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA + QMANA + 5
                        && GetRealDistance(t) > GetRealRange(t))
                        W.Cast(t, true, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA + WMANA
                       && ObjectManager.Player.CountEnemiesInRange(300) > 0)
                        W.Cast(t, true, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA + EMANA
                        && ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4)
                        W.Cast(t, true, true);
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                                W.Cast(enemy, true, true);
                        }
                    }
                }
            }
            if (E.IsReady() && Config.Item("autoE").GetValue<bool>())
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(E.Range + Q.Range - 100f, TargetSelector.DamageType.Physical);
                var t2 = TargetSelector.GetTarget(900f, TargetSelector.DamageType.Physical);

                if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4 && !ObjectManager.Player.UnderTurret(true))
                {
                    if (t.IsValidTarget()
                    && Q.IsReady()
                    && ObjectManager.Player.Mana > RMANA + EMANA
                    && Q.GetDamage(t) > t.Health
                    && GetRealDistance(t) > GetRealRange(t)
                    && t.CountEnemiesInRange(800) < 3)
                    {
                        E.Cast(Game.CursorPos, true);
                        return;
                    }
                    else if (t2.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + RMANA
                     && ObjectManager.Player.GetAutoAttackDamage(t2) * 2 > t2.Health
                     && GetRealDistance(t2) > GetRealRange(t2)
                     && t2.CountEnemiesInRange(800) < 3)
                    {
                        E.Cast(Game.CursorPos, true);
                        return;
                    }
                    else if (t.IsValidTarget()
                     && Q.IsReady() && R.IsReady()
                     && ObjectManager.Player.Mana > RMANA + EMANA + RMANA
                     && Q.GetDamage(t) + R.GetDamage(t) > t.Health
                     && R.GetDamage(t) < t.Health
                     && GetRealDistance(t) > GetRealRange(t)
                     && t.CountEnemiesInRange(800) < 3)
                    {
                        E.Cast(Game.CursorPos, true);
                        return;
                    }
                }
            }

            if (Q.IsReady())
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (Q.GetDamage(t) > t.Health)
                    {
                        Q1.Cast(t, true);
                        OverKill = Game.Time;
                        return;
                    }
                    if (R.GetDamage(t) + Q.GetDamage(t) > t.Health && R.IsReady() && ObjectManager.Player.Mana > RMANA + QMANA)
                        Q1.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                        Q.Cast(t, true, true);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA + QMANA) && Orbwalker.GetTarget() == null)
                    {
                        if (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.9)
                            Q.Cast(t, true, true);
                        else if (t.Path.Count() > 1)
                            Q.Cast(t, true, true);
                    }
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + QMANA)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                                Q1.CastIfHitchanceEquals(enemy, HitChance.High, true);
                        }
                    }
                }
            }

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && (Game.Time - OverKill > 0.7))
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R1.Range)))
                {
                    if (target.IsValidTarget()
                    && R.GetDamage(target) > target.Health
                    && !target.HasBuffOfType(BuffType.PhysicalImmunity)
                    && !target.HasBuffOfType(BuffType.SpellImmunity)
                    && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        var Rdmg = R.GetDamage(target);
                        var collisionTarget = target;
                        cast = true;
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
                            if (length < (120 + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                            {
                                cast = false;
                                collisionTarget = enemy;
                            }
                        }
                        if (cast
                            && target.IsValidTarget()
                            && Rdmg > predictedHealth
                            && target.IsValidTarget(R.Range)
                            && ((GetRealDistance(target) > GetRealRange(target) && target.CountAlliesInRange(300) == 0) || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.7))
                            R.Cast(target, true);
                        else if (cast
                            && Rdmg * 0.7 > predictedHealth
                            && target.IsValidTarget(R1.Range)
                            && ((GetRealDistance(target) > GetRealRange(target) && target.CountAlliesInRange(300) == 0) || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.7))
                            R1.Cast(target, true, true);

                        else if (!cast && Rdmg * 0.7 > predictedHealth
                            && target.IsValidTarget(GetRealDistance(collisionTarget) + 700))
                            R1.Cast(target, true, true);
                    }
                }
            }
            PotionMenager();
        }
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                    E.Cast(Game.CursorPos, true);
                return;
            }
            return;
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (E.IsReady() && Config.Item("autoE").GetValue<bool>() && Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + EMANA && ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.7 && ObjectManager.Player.CountEnemiesInRange(400) > 0)
                E.Cast(Game.CursorPos);
        }


        public static void UseItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
            }
        }

        private static float GetRealRange(GameObject target)
        {
            return 600f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        public static float bonusRange()
        {
            return 600 + ObjectManager.Player.BoundingRadius;
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
                RMANA = R.Instance.ManaCost;

            RMANA = RMANA + (ObjectManager.Player.CountEnemiesInRange(2500) * 20);

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
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA)
                        ManaPotion.Cast();
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("noti").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(1800, TargetSelector.DamageType.Physical);
                float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                if (t.IsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(t) * 0.7;
                    if (rDamage > predictedHealth)
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    if (Config.Item("useR").GetValue<KeyBind>().Active)
                    {
                        R1.Cast(t, true, true);
                    }
                }
                var tw = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(tw);
                    if (qDmg > tw.Health)
                    {
                        Utility.DrawCircle(ObjectManager.Player.ServerPosition, Q.Range, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}