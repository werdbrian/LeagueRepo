using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Jinx
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        public Spell E;
        public Spell Q;
        public Spell R;
        public Spell W;
        
        public float QMANA;
        public float WMANA;
        public float EMANA;
        public float RMANA;

        public bool attackNow = true;
        public double lag = 0;
        public double WCastTime = 0;
        public double QCastTime = 0;
        public float DragonDmg = 0;
        public double DragonTime = 0;

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);
            
            W.SetSkillshot(0.6f, 75f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Game_OnUpdate(EventArgs args)
        {

            SetMana();
            
            if (E.IsReady() && Player.Mana > RMANA + EMANA && Config.Item("autoE").GetValue<bool>())
                LogicE();

            if (Q.IsReady())
                LogicQ();

            if (W.IsReady() && (Game.Time - QCastTime > 0.6))
                LogicW();
            if (R.IsReady())
            {
                if (Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
                if (Config.Item("Rjungle").GetValue<bool>())
                    KsJungle();

                LogicR();
            }
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
            if (Q.IsReady() && FishBoneActive && t.IsValidTarget())
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetRealDistance(t) < GetRealPowPowRange(t) && (Player.Mana < RMANA + WMANA + 20 || Player.GetAutoAttackDamage(t) * 2 < t.Health))
                    Q.Cast();
                else if (Farm && (GetRealDistance(t) > bonusRange() || GetRealDistance(t) < GetRealPowPowRange(t) || Player.Mana < RMANA + EMANA + WMANA + WMANA))
                    Q.Cast();
            }
            if (Q.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !FishBoneActive && Player.Mana < RMANA + EMANA + WMANA + 30)
            {
                var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
                foreach (var minion in allMinionsQ)
                {
                    if (Orbwalking.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion))
                    {
                        foreach (var minion2 in allMinionsQ)
                        {
                            if (minion2.Health < Player.GetAutoAttackDamage(minion2) && minion.ServerPosition.Distance(minion2.Position) < 150 && minion2.Position != minion.Position)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }
        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(Player.ServerPosition, true);
                    debug("E agc");
                }
                return;
            }
            return;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {

            if (Config.Item("opsE").GetValue<bool>() && unit.Team != Player.Team && ShouldUseE(args.SData.Name) && unit.IsValidTarget(E.Range))
            {
                E.Cast(unit.ServerPosition, true);
                debug("E ope");
            }
            if (unit.IsMe && args.SData.Name == "JinxW")
            {
                WCastTime = Game.Time;
            }
        }

        private void LogicQ()
        {
            if (Program.LagFree(1) && Farm && Config.Item("farmQ").GetValue<bool>() && Player.Mana > RMANA + WMANA + EMANA + 10 && !FishBoneActive)
            {
                farmQ();
            }
            var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var distance = GetRealDistance(t);
                var powPowRange = GetRealPowPowRange(t);
                if (!FishBoneActive && !Orbwalking.InAutoAttackRange(t))
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && (Player.Mana > RMANA + WMANA + 20 || Player.GetAutoAttackDamage(t) * 2 > t.Health))
                        Q.Cast();
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && Orbwalker.GetTarget() == null && Player.Mana > RMANA + WMANA + EMANA + 20 && distance < bonusRange() + t.BoundingRadius + Player.BoundingRadius)
                        Q.Cast();
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !Player.UnderTurret(true) && Player.Mana > RMANA + WMANA + EMANA + WMANA + 20 && distance < bonusRange())
                        Q.Cast();
                }
            }
            else if (!FishBoneActive && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.Mana > RMANA + WMANA + 20 && Player.CountEnemiesInRange(2000) > 0)
                Q.Cast();
            else if (FishBoneActive && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.Mana < RMANA + WMANA + 20)
                Q.Cast();
            else if (FishBoneActive && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.CountEnemiesInRange(2000) == 0)
                Q.Cast();
            else if (FishBoneActive && Farm)
                Q.Cast();
        }

        private void LogicW()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range) && ObjectManager.Player.CountEnemiesInRange(400) == 0 && !Orbwalking.InAutoAttackRange(enemy) && W.GetDamage(enemy) > enemy.Health))
            {
                Program.CastSpell(W, enemy);
            }

            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.Mana > RMANA + WMANA + 10 && Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                {
                     Program.CastSpell(W, t);
                }
                else if ((Farm && Player.Mana > RMANA + EMANA + WMANA + WMANA + 40) && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !Player.UnderTurret(true) && Player.CountEnemiesInRange(bonusRange()) == 0)
                {
                     Program.CastSpell(W, t);
                }
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && Player.Mana > RMANA + WMANA && Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range)))
                    {
                        if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                        {
                            W.Cast(enemy, true);
                        }
                    }
                }
            }
        }

        private void LogicE()
        {
            if (Config.Item("telE").GetValue<bool>())
            {
                foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < E.Range && E.IsReady() && Obj.Team != Player.Team && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                {
                    E.Cast(Object.Position, true);
                    return;
                }
            }
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range) && !enemy.HasBuff("rocketgrab2") && E.IsReady()))
            {
                if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                     enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                     enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                     enemy.IsStunned || enemy.HasBuff("Recall"))
                    E.Cast(enemy, true);
                else
                    E.CastIfHitchanceEquals(enemy, HitChance.Immobile, true);
            }

            var ta = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (Player.IsMoving && ta.IsValidTarget(E.Range) && E.GetPrediction(ta).CastPosition.Distance(ta.Position) > 200 && (int)E.GetPrediction(ta).Hitchance == 5 && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && E.IsReady() && Config.Item("comboE").GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA && ta.Path.Count() == 1)
            {
                if (ta.HasBuffOfType(BuffType.Slow))
                    E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                else if (ta.CountEnemiesInRange(250) > 2)
                    E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                else
                {
                    if (Player.Position.Distance(ta.ServerPosition) > Player.Position.Distance(ta.Position))
                    {
                        if (ta.Position.Distance(Player.ServerPosition) < ta.Position.Distance(Player.Position))
                        {
                            Program.CastSpell(E, ta);
                            debug("E run");
                            return;
                        }
                    }
                    else
                    {
                        if (ta.Position.Distance(Player.ServerPosition) > ta.Position.Distance(Player.Position))
                        {
                            Program.CastSpell(E, ta);
                            debug("E escape");
                            return;
                        }
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Config.Item("autoR").GetValue<bool>())
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(R.Range) && (Game.Time - WCastTime > 1) && Program.ValidUlt(target))
                    {

                        float predictedHealth = target.Health + target.HPRegenRate * 2;
                        var Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
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
                                if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }

                            if (cast && GetRealDistance(target) > bonusRange() + 300 + target.BoundingRadius && target.CountAlliesInRange(600) == 0 && Player.CountEnemiesInRange(400) == 0)
                            {
                                castR(target);
                                debug("R normal High");

                            }
                            else if (cast && target.CountEnemiesInRange(200) > 2 && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius)
                            {
                                R.Cast(target, true, true);
                                debug("R aoe 1");
                            }
                        }
                    }
                }
            }
        }

        private void castR(Obj_AI_Hero target)
        {
            if (Config.Item("hitchanceR").GetValue<bool>())
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if ((Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > 400)
                {
                    R.Cast(target, true);
                }
            }
            else
                R.Cast(target, true);
        }

        private void KsJungle()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, float.MaxValue, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var mob in mobs)
            {
                //debug(mob.SkinName);
                if (((mob.SkinName == "SRU_Dragon" && Config.Item("Rdragon").GetValue<bool>())
                    || (mob.SkinName == "SRU_Baron" && Config.Item("Rbaron").GetValue<bool>()))
                    && mob.CountAlliesInRange(1000) == 0
                    && mob.Health < mob.MaxHealth
                    && mob.Distance(Player.Position) > 1000)
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 4)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }

                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 4);
                        //debug("DS  " + DmgSec);
                        if (DragonDmg - mob.Health > 0)
                        {
                            var timeTravel = GetUltTravelTime(Player, R.Speed, R.Delay, mob.Position);
                            var timeR = (mob.Health - Player.CalcDamage(mob, Damage.DamageType.Physical, (250 + (100 * R.Level)) + Player.FlatPhysicalDamageMod + 300)) / (DmgSec / 4);
                            //debug("timeTravel " + timeTravel + "timeR " + timeR + "d " + ((150 + (100 * R.Level + 200) + Player.FlatPhysicalDamageMod)));
                            if (timeTravel > timeR)
                                R.Cast(mob.Position);
                        }
                        else
                        {
                            DragonDmg = mob.Health;
                        }
                        //debug("" + GetUltTravelTime(Player, R.Speed, R.Delay, mob.Position));
                    }
                }
            }
        }

        private float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;
            if (source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second
                var acceldifference = distance - 1350f;
                if (acceldifference > 150f) //it only accelerates 150 units
                    acceldifference = 150f;
                var difference = distance - 1500f;
                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) + difference * 2200f) / distance;
            }
            return (distance / missilespeed + delay);
        }

        private void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion))
                {
                    Q.Cast();
                    return;
                }
            }
        }

        public bool ShouldUseE(string SpellName)
        {
            if (SpellName == "ThreshQ")
                return true;
            if (SpellName == "KatarinaR")
                return true;
            if (SpellName == "AlZaharNetherGrasp")
                return true;
            if (SpellName == "GalioIdolOfDurand")
                return true;
            if (SpellName == "LuxMaliceCannon")
                return true;
            if (SpellName == "MissFortuneBulletTime")
                return true;
            if (SpellName == "RocketGrabMissile")
                return true;
            if (SpellName == "CaitlynPiltoverPeacemaker")
                return true;
            if (SpellName == "EzrealTrueshotBarrage")
                return true;
            if (SpellName == "InfiniteDuress")
                return true;
            if (SpellName == "VelkozR")
                return true;
            return false;
        }

        private bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        private float bonusRange()
        {
            return 620f + Player.BoundingRadius + 50 + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private bool FishBoneActive
        {
            get { return Player.AttackRange > 525f; }
        }

        private  float GetRealPowPowRange(GameObject target)
        {
            return 630f + Player.BoundingRadius + target.BoundingRadius;
        }

        private float GetRealDistance(Obj_AI_Base target)
        {
            return Player.ServerPosition.Distance(target.ServerPosition) + Player.BoundingRadius +
                   target.BoundingRadius;
        }

        private void LoadMenuOKTW()
        {
            #region E
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("Haras").AddItem(new MenuItem("haras" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("humanzier", "Humanizer W (0.5 delay)").SetValue(false));
            #endregion
            #region E
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("comboE", "Auto E in Combo BETA").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("opsE", "OnProcessSpellCastE").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("telE", "Auto E teleport").SetValue(true));
            #endregion
            #region R
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            #endregion
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("semi", "Semi-manual R target").SetValue(false));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("farmQ", "Q farm").SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("debug", "Debug").SetValue(false));
        }

        public void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Console.WriteLine(msg);
        }

        private void SetMana()
        {
            QMANA = 10;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost; ;

            if (Player.Health < Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (FishBoneActive)
                    Utility.DrawCircle(Player.Position, 590f + Player.BoundingRadius, System.Drawing.Color.DeepPink, 1, 1);
                else
                    Utility.DrawCircle(Player.Position, bonusRange() - 40, System.Drawing.Color.DeepPink, 1, 1);
            }
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }
    }
}
