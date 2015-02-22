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
        public static bool attackNow = true;
        //ManaMenager
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static bool Esmart = false;
        public static double secoundDmgR = 0.65;
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
            Q = new Spell(SpellSlot.Q, 950f);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 450f);
            R = new Spell(SpellSlot.R, 1000f);
            R1 = new Spell(SpellSlot.R, 1600f);

            Q.SetSkillshot(0.26f, 50f, 1950f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 120f, 2100f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.26f, 20f * (float)Math.PI / 180, 2100f, false, SkillshotType.SkillshotCone);

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
            Config.AddItem(new MenuItem("smartE", "SmartCast E key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.AddItem(new MenuItem("Hit", "Hit Chance Q").SetValue(new Slider(2, 2, 0)));
            Config.AddItem(new MenuItem("HitR", "Hit Chance R").SetValue(new Slider(2, 2, 0)));
            Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += afterAttack;
            Game.PrintChat("<font color=\"#9c3232\">G</font>raves full automatic AI ver 1.4 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;

            if (Orbwalker.GetTarget() == null)
                attackNow = true;

            if (E.IsReady())
            {
                if (Config.Item("smartE").GetValue<KeyBind>().Active)
                {
                    Esmart = true;
                }
                if (Esmart && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                {
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                }

            }
            else
                Esmart = false;
            
            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                {
                    if (W.GetDamage(t) > t.Health)
                    {
                        W.Cast(t, true, true);
                        debug("W ks");
                        OverKill = Game.Time;
                        return;
                    }
                    else if (W.GetDamage(t) + Q.GetDamage(t) > t.Health && ObjectManager.Player.Mana >  QMANA + EMANA + RMANA )
                        W.Cast(t, true, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA)
                        W.Cast(t, true, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA + QMANA + 5
                        && !Orbwalking.InAutoAttackRange(t))
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
                var t = TargetSelector.GetTarget(E.Range + Q.Range, TargetSelector.DamageType.Physical);
                var t2 = TargetSelector.GetTarget(900f, TargetSelector.DamageType.Physical);
                if ( ObjectManager.Player.Mana > RMANA + EMANA
                    && ObjectManager.Player.CountEnemiesInRange(250) > 0
                    && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3
                    && t.Position.Distance(Game.CursorPos) > t.Position.Distance(ObjectManager.Player.Position))
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                else if (E.IsReady() && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3 && Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4 && !ObjectManager.Player.UnderTurret(true))
                {
                    if (t.IsValidTarget()
                    && Q.IsReady()
                    && ObjectManager.Player.Mana > RMANA + EMANA
                    && Q.GetDamage(t) + ObjectManager.Player.GetAutoAttackDamage(t2) * 2 > t.Health
                    && t.Position.Distance(Game.CursorPos) + 200 < t.Position.Distance(ObjectManager.Player.Position) 
                    && !Orbwalking.InAutoAttackRange(t)
                    )
                    {
                        E.Cast(Game.CursorPos, true);
                        debug("E + aa + Q");
                    }
                    else if (t2.IsValidTarget()
                     && ObjectManager.Player.Mana > QMANA + RMANA
                     && ObjectManager.Player.GetAutoAttackDamage(t2) * 2 > t2.Health
                     && !Orbwalking.InAutoAttackRange(t2)
                     && t2.Position.Distance(Game.CursorPos) + 200 < t2.Position.Distance(ObjectManager.Player.Position) 
                     )
                    {
                        E.Cast(Game.CursorPos, true);
                        debug("E + aa");
                    }
                    else if (t.IsValidTarget()
                     && Q.IsReady() && R.IsReady()
                     && t.Position.Distance(Game.CursorPos) + 200 < t.Position.Distance(ObjectManager.Player.Position)
                     && ObjectManager.Player.Mana > RMANA + EMANA + RMANA
                     && Q.GetDamage(t) + R.GetDamage(t) < t.Health
                     && !Orbwalking.InAutoAttackRange(t2))
                    {
                        E.Cast(Game.CursorPos, true);
                        debug("E + Q + R");
                    }
                }
            }

            if (Q.IsReady())
            {
                ManaMenager();
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (Q.GetDamage(t) > t.Health)
                    {
                        Q.Cast(t, true);
                        OverKill = Game.Time;
                        debug("Q ks");
                    }
                    else if (Q.GetDamage(t) + R.GetDamage(t) > t.Health && R.IsReady())
                    {
                        Q.Cast(t, true);
                        debug("Q + R ks");
                    }
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + QMANA && attackNow)
                        castQ(t);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA + QMANA) && t.IsValidTarget(Q.Range - 100) && attackNow)
                        castQ(t);
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA && attackNow)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                                Q.CastIfHitchanceEquals(enemy, HitChance.High, true);
                        }
                    }
                }
            }

            if (R.IsReady() && Config.Item("autoR").GetValue<bool>() && (Game.Time - OverKill > 0.5))
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
                            && (!Orbwalking.InAutoAttackRange(target) || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6))
                        {

                            castR(target);
                            debug("Rdmg");
                        }
                        else if (cast
                            && Rdmg * secoundDmgR > predictedHealth
                            && target.IsValidTarget(R1.Range)
                            && target.CountAlliesInRange(300) == 0 && (!Orbwalking.InAutoAttackRange(target) || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6))
                        {
                            R1.Cast(target, true, true);
                            debug("Rdmg 0.7");
                        }
                        else if (!cast && Rdmg * secoundDmgR > predictedHealth
                            && target.IsValidTarget(GetRealDistance(collisionTarget) + 700))
                        {
                            R1.Cast(target, true, true);
                            debug("Rdmg 0.7 collision");
                        }
                    }
                }
            }
            PotionMenager();
        }

        private static void castQ(Obj_AI_Hero target)
        {
            if (Config.Item("Hit").GetValue<Slider>().Value == 0)
                Q.Cast(target, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 1)
                Q.CastIfHitchanceEquals(target, HitChance.High, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                Q.CastIfHitchanceEquals(target, HitChance.High, true);
        }

        private static void castR(Obj_AI_Hero target)
        {
            if (Config.Item("HitR").GetValue<Slider>().Value == 0)
                R.Cast(target, true);
            else if (Config.Item("HitR").GetValue<Slider>().Value == 1)
                R.CastIfHitchanceEquals(target, HitChance.High, true);
            else if (Config.Item("HitR").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                R.CastIfHitchanceEquals(target, HitChance.High, true);
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                {
                    var dmg = unit.GetSpellDamage(target, args.SData.Name);
                    double HpLeft = target.Health - dmg;
                    var qDmg = Q.GetDamage(target);
                    var rDmg = R.GetDamage(target);
                    if (HpLeft < 0 && target.IsValidTarget())
                    {
                        OverKill = Game.Time;
                        debug("OverKill detection " + target.ChampionName);
                    }
                    if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(Q.Range) && Q.IsReady() && qDmg > HpLeft && HpLeft > 0)
                    {
                            Q.Cast(target, true);
                            debug("Q ops");
                    }
                    else if (
                        HpLeft > 0 
                        && rDmg * secoundDmgR > HpLeft 
                        && target.IsValidTarget(R1.Range) 
                        && R1.IsReady()
                        && (!Orbwalking.InAutoAttackRange(target) || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        && target.CountAlliesInRange(300) == 0)
                    {
                        var cast = true;

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
                            if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                cast = false;
                        }
                        if (rDmg > HpLeft && cast && target.IsValidTarget(R.Range))
                        {
                            castR(target);
                            debug("R ops");
                        }
                        else if (rDmg * secoundDmgR > HpLeft)
                        {
                            R1.Cast(target, true);
                            debug("R2 ops");
                        }
                        else if (rDmg + qDmg > HpLeft && cast && target.IsValidTarget(Q.Range))
                        {
                            castR(target);
                            debug("R + Q ops");
                        }
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
                {
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                    debug("E agc");
                }
                return;
            }
            return;
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
        }

        public static void UseItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
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
                    var rDamage = R.GetDamage(t) * secoundDmgR;
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
                    if (Q.GetDamage(tw) > tw.Health)
                    {
                        Utility.DrawCircle(ObjectManager.Player.ServerPosition, Q.Range, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}