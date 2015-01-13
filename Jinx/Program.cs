using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Jinx
{
    class Program
    {
        public const string ChampionName = "Jinx";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
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
            Q = new Spell(SpellSlot.Q, float.MaxValue);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 1100f);
            R = new Spell(SpellSlot.R, 2500f);
            R1 = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.1f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.7f, 200f, 1500f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
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
            Config.AddItem(new MenuItem("Botrk", "Use Botrk").SetValue(true));
            Config.AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
            Config.AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            //Add the events we are going to use:
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.PrintChat("<font color=\"#ff00d8\">J</font>inx full automatic SI ver 1.7 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            PotionMenager();
           
            //Game.PrintChat(Game.Time.ToString());
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;
            
            if (ObjectManager.Player.Mana > RMANA + EMANA && E.IsReady())
            {
                var t = TargetSelector.GetTarget(900f, TargetSelector.DamageType.Physical);

                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (enemy.Distance(Player.ServerPosition) < 900f && enemy.HasBuff("zhonyasringshield"))
                        E.Cast(enemy, true);
                }
                foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < 900f && Obj.Team != Player.Team && Obj.HasBuff("teleport_target", true)))
                {
                    E.Cast(Object.Position, true);
                 }
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range)))
                {

                    if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.HasBuff("Recall") )
                        E.Cast(enemy, true);
                }
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range + 200)))
                {
                    if (enemy.HasBuffOfType(BuffType.Slow) && t.Path.Count() > 1)
                    {
                        E.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                    }
                }
            }

            if (Q.IsReady())
            {
                if (Farm)
                    if (ObjectManager.Player.Mana > RMANA + WMANA + EMANA && !FishBoneActive)
                        farmQ();
                var t = TargetSelector.GetTarget(bonusRange() + 50, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var distance = GetRealDistance(t);
                    var powPowRange = GetRealPowPowRange(t);
                    if (Youmuu.IsReady() && (ObjectManager.Player.GetAutoAttackDamage(t) * 6 > t.Health || ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4))
                        Youmuu.Cast();
                    if (!FishBoneActive && (distance > powPowRange) && (ObjectManager.Player.Mana > RMANA + WMANA || ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health))
                    {
                        if (Orbwalker.ActiveMode.ToString() == "Combo")
                            Q.Cast();
                        else if (Farm && haras() && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + WMANA && distance < bonusRange() + t.BoundingRadius)
                                Q.Cast();
                    }
                }
                else if (FishBoneActive && Farm)
                    Q.Cast();
                else if (!FishBoneActive && (Orbwalker.ActiveMode.ToString() == "Combo") && ObjectManager.Player.Mana > RMANA + WMANA)
                    Q.Cast();
            }

            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var wDmg = W.GetDamage(t);
                    if (GetRealDistance(t) > bonusRange() && wDmg + ObjectManager.Player.GetAutoAttackDamage(t) > t.Health)
                        W.Cast(t, true);
                    else if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0)
                        W.CastIfHitchanceEquals(t, HitChance.High, true);
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA) && CountEnemies(ObjectManager.Player, bonusRange()) == 0 && haras())
                    {
                        if (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8 )
                            W.CastIfHitchanceEquals(t, HitChance.High, true);
                        else if (t.Path.Count() > 1)
                            W.CastIfHitchanceEquals(t, HitChance.High, true);
                    }
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + WMANA && CountEnemies(ObjectManager.Player, GetRealPowPowRange(t)) == 0)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                                W.CastIfHitchanceEquals(t, HitChance.High, true);
                        }
                    }
                }
            }

            if (R.IsReady())
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R.Range)))
                {
                    if (target.IsValidTarget() && Config.Item("autoR").GetValue<bool>() && (Game.Time - WCastTime > 1)  && 
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        var Rdmg = R.GetDamage(target);
                        if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4)
                            Rdmg = R.GetDamage(target) * 1.4f;
                        if (Rdmg > predictedHealth && GetRealDistance(target) > bonusRange() + 100 + target.BoundingRadius
                            && (CountAlliesNearTarget(target, 500) == 0 || (CountAlliesNearTarget(target, 700) > 0 && ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4 && CountEnemies(target, 300) > 0)))
                        {
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
                                if (length < (R.Width + 50 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            if (cast && target.IsValidTarget())
                            {
                                if (Config.Item("hitchanceR").GetValue<bool>() && target.Path.Count() > 1)
                                    R.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                                else
                                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                            }

                        }
                        /*
                        var distance = GetRealDistance(t);
                        var rDamage = R.GetDamage(t);
                        var powPowRange = GetRealPowPowRange(t);
                        if (rDamage > t.Health && CountAlliesNearTarget(t, 600) == 0 && CountEnemies(ObjectManager.Player, 200f) == 0 && distance > bonusRange() + 70 && t.Path.Count() > 1)
                                R.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                        else 
                        else if (rDamage * 1.4 > t.Health && CountEnemies(t, 200) > 2)
                            R.CastIfHitchanceEquals(t, HitChance.VeryHigh, true);
                         * */
                    }
                }
            }
            if (Config.Item("Botrk").GetValue<bool>())
            {
                Botrk();
            }
        }

        public static void Botrk()
        {
            if (CountEnemies(ObjectManager.Player, 450) == 0)
                return;
            Obj_AI_Hero lowHP = TargetSelector.GetTarget(450, TargetSelector.DamageType.Physical);
            Obj_AI_Hero nearest = TargetSelector.GetTarget(450, TargetSelector.DamageType.Physical);
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(450)))
            {
                if (lowHP.MaxHealth < target.MaxHealth)
                    lowHP = target;
                if(Player.Distance(target.ServerPosition) < Player.Distance(nearest.ServerPosition))
                    nearest = target;
            }
            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4)
            {
                UseItem(3144, lowHP);
                UseItem(3153, lowHP);
                return;
            }
            else if (ObjectManager.Player.MaxHealth - ObjectManager.Player.Health > nearest.MaxHealth * 0.1 && 200 > Player.Distance(nearest.ServerPosition))
            {
                UseItem(3153, nearest);
                UseItem(3144, nearest);
                return;
            }
            return;
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            var t = TargetSelector.GetTarget(bonusRange() + 50, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var distance = GetRealDistance(t);
                var powPowRange = GetRealPowPowRange(t);
                if (Orbwalker.ActiveMode.ToString() == "Combo" && FishBoneActive && (distance < powPowRange) && (ObjectManager.Player.Mana < RMANA + WMANA || ObjectManager.Player.GetAutoAttackDamage(t) < t.Health))
                    Q.Cast();
                else if (Farm && FishBoneActive && (distance > bonusRange() || distance < powPowRange))
                    Q.Cast();
            }
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var t = TargetSelector.GetTarget(bonusRange() + 50, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var distance = GetRealDistance(t);
                var powPowRange = GetRealPowPowRange(t);
                if (Orbwalker.ActiveMode.ToString() == "Combo" && FishBoneActive && (distance < powPowRange) && (ObjectManager.Player.Mana < RMANA + WMANA || ObjectManager.Player.GetAutoAttackDamage(t) < t.Health))
                    Q.Cast();
                else if (Farm && FishBoneActive && (distance > bonusRange() || distance < powPowRange))
                    Q.Cast();
            }
        }

        public static void UseItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
            }
        }

        public static void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) && minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion)  && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion))
                {
                    Q.Cast();
                    return;
                }
                else if (Orbwalking.InAutoAttackRange(minion) && CountEnemies(minion, 150) > 0)
                {
                    Q.Cast();
                    return;
                }
            }
        }
    
        public static bool haras()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange(), MinionTypes.All);
            var haras = true;
            foreach (var minion in allMinionsQ)
            {
                if (minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) * 2  && bonusRange() > GetRealDistance(minion))
                    haras = false;
            }
            if (haras)
                return true;
            else
                return false;
        }
        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            double ShouldUse = ShouldUseE(args.SData.Name);
            if (unit.Team != ObjectManager.Player.Team && ShouldUse >= 0f && unit.IsValidTarget(E.Range))
                E.Cast(unit.ServerPosition, true);
            if (unit.IsMe && args.SData.Name == "JinxW")
            {
                WCastTime = Game.Time;
            }
        }

        public static double ShouldUseE(string SpellName)
        {
            if (SpellName == "ThreshQ")
                return 0;
            if (SpellName == "KatarinaR")
                return 0;
            if (SpellName == "AlZaharNetherGrasp")
                return 0;
            if (SpellName == "GalioIdolOfDurand")
                return 0;
            if (SpellName == "LuxMaliceCannon")
                return 0;
            if (SpellName == "MissFortuneBulletTime")
                return 0;
            if (SpellName == "RocketGrabMissile")
                return 0;
            if (SpellName == "CaitlynPiltoverPeacemaker")
                return 0;
            if (SpellName == "EzrealTrueshotBarrage")
                return 0;
            if (SpellName == "InfiniteDuress")
                return 0;
            if (SpellName == "VelkozR")
                return 0;
            return -1;
        }

        public static float bonusRange()
        {
            return 620f + ObjectManager.Player.BoundingRadius + 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level; 
        }

        private static bool FishBoneActive
        {
            get { return Math.Abs(ObjectManager.Player.AttackRange - 525f) > float.Epsilon; }
        }

        private static int PowPowStacks
        {
            get
            {
                return
                    ObjectManager.Player.Buffs.Where(buff => buff.DisplayName.ToLower() == "jinxqramp")
                        .Select(buff => buff.Count)
                        .FirstOrDefault();
            }
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

        private static float GetRealPowPowRange(GameObject target)
        {
            return 600f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        private static float GetSlowEndTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Slow)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
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

        public static void ManaMenager()
        {
            QMANA = 10;
            WMANA = 40 + 10 * W.Level;
            EMANA = 50;
            if (!R.IsReady())
                RMANA = WMANA - 10;
            else
                RMANA = 100;
            if (Farm)
                RMANA = RMANA + (CountEnemies(ObjectManager.Player, 2500) * 20);
        }

        public static void PotionMenager()
        {
            if (Config.Item("pots").GetValue<bool>() && Potion.IsReady() && !InFountain() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
            {
                if (CountEnemies(ObjectManager.Player, 600) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                    Potion.Cast();
                else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.5)
                    Potion.Cast();
            }
            if (Config.Item("pots").GetValue<bool>() && ManaPotion.IsReady() && !InFountain())
            {
                if (CountEnemies(ObjectManager.Player, 1000) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA)
                    ManaPotion.Cast();
            } 
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("noti").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                if (t.IsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(t);
                    if (rDamage > predictedHealth)
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    if (Config.Item("useR").GetValue<KeyBind>().Active)
                    {
                        R1.Cast(t,true,true);
                    }
                }
                var tw = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {
                    var wDmg = W.GetDamage(tw);
                    if (wDmg  > tw.Health)
                    {
                        Utility.DrawCircle(ObjectManager.Player.ServerPosition, W.Range, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "W can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                }
            }
        }
    }
}
