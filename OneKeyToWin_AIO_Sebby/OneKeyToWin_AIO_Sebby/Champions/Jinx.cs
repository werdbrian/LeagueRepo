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
        public Spell Q, W, E, R;
        public float QMANA, WMANA, EMANA, RMANA;

        public double lag = 0, WCastTime = 0, QCastTime = 0, DragonTime = 0;
        public float DragonDmg = 0;

        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

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



        private void LoadMenuOKTW()
        {
            #region E
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            #endregion
            #region E
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E on CC").SetValue(true));
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
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "OneKeyToCast R").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            #endregion
            Config.SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("semi", "Semi-manual R target").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Q farm").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Q Mana").SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("debug", "Debug").SetValue(false));
        }
        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!Q.IsReady())
                return;
            var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
            
            if (FishBoneActive && t.IsValidTarget())
            {
                if (Program.Combo && GetRealDistance(t) < GetRealPowPowRange(t) && (Player.Mana < RMANA + WMANA + 20 || Player.GetAutoAttackDamage(t) * 2 < t.Health))
                    Q.Cast();
                else if (Program.Farm && (GetRealDistance(t) > bonusRange() || GetRealDistance(t) < GetRealPowPowRange(t) || Player.Mana < RMANA + EMANA + WMANA + WMANA))
                    Q.Cast();
            }

            if (Program.LaneClear && !FishBoneActive && Config.Item("farmQ").GetValue<bool>() && Player.ManaPercent > Config.Item("Mana").GetValue<Slider>().Value && Player.Mana > RMANA + EMANA + WMANA + 30)
            {
                Program.debug("mana "+ Player.ManaPercent);
                var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, bonusRange(), MinionTypes.All);
                foreach (var minion in allMinionsQ.Where(minion => args.Target.NetworkId != minion.NetworkId && minion.Distance(args.Target.Position) < 200))
                {
                    Q.Cast();
                }
            }
        }
        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                var Target = gapcloser.Sender;
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
            if (unit.IsEnemy && unit.IsValidTarget(E.Range) && Config.Item("opsE").GetValue<bool>() && ShouldUseE(args.SData.Name))
            {
                E.Cast(unit.ServerPosition, true);
                debug("E ope");
            }
            if (unit.IsMe && args.SData.Name == "JinxWMissile")
                WCastTime = Game.Time;
            
        }
        private void Game_OnUpdate(EventArgs args)
        {
            if (R.IsReady())
            {
                if (Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
                if (Config.Item("Rjungle").GetValue<bool>())
                {
                    KsJungle();
                }
            }

            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (Program.LagFree(1) && E.IsReady())
                LogicE();

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && !Player.IsWindingUp)
                LogicW();
            
            if (Program.LagFree(4) && R.IsReady())
                LogicR();
            
        }

        private void LogicQ()
        {
            if (Program.Farm && !Player.IsWindingUp && Config.Item("farmQ").GetValue<bool>() && (Game.Time - lag > 0.1) && Player.Mana > RMANA + WMANA + EMANA + 10 && !FishBoneActive )
            {
                farmQ();
                lag = Game.Time;
            }
            var t = TargetSelector.GetTarget(bonusRange() + 60, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var distance = GetRealDistance(t);
                if (!FishBoneActive && (!Orbwalking.InAutoAttackRange(t) || t.CountEnemiesInRange(250) > 2))
                {
                    if (Program.Combo && (Player.Mana > RMANA + WMANA + 20 || Player.GetAutoAttackDamage(t) * 2 > t.Health))
                        Q.Cast();
                    else if (Program.Farm && Orbwalker.GetTarget() == null && Player.Mana > RMANA + WMANA + EMANA + 20 && distance < bonusRange() + t.BoundingRadius + Player.BoundingRadius)
                        Q.Cast();
                }
            }
            else if (!FishBoneActive && Program.Combo && Player.Mana > RMANA + WMANA + 20 && Player.CountEnemiesInRange(2000) > 0)
                Q.Cast();
            else if (FishBoneActive && Program.Combo && Player.Mana < RMANA + WMANA + 20)
                Q.Cast();
            else if (FishBoneActive && Program.Combo && Player.CountEnemiesInRange(2000) == 0)
                Q.Cast();
            else if (FishBoneActive && Program.Farm)
                Q.Cast();
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget() )
            {
                foreach (var enemy in Program.Enemies.Where(enemy => Game.Time - QCastTime > 0.6 && enemy.IsValidTarget(W.Range) && Player.CountEnemiesInRange(400) == 0 && !Orbwalking.InAutoAttackRange(enemy) && W.GetDamage(enemy) > enemy.Health))
                {
                    Program.CastSpell(W, enemy);
                    return;
                }

                if (Program.Combo && Player.Mana > RMANA + WMANA + 10 && Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                {
                    Program.CastSpell(W, t);
                }
                else if (Program.Farm && Player.Mana > RMANA + EMANA + WMANA + WMANA + 40 && Config.Item("haras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && Player.CountEnemiesInRange(bonusRange()) == 0)
                {
                    Program.CastSpell(W, t);
                }
                else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + WMANA && Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);  
                }
            }
        }

        private void LogicE()
        {
            if (Player.Mana > RMANA + EMANA && Config.Item("autoE").GetValue<bool>())
            {
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                {
                    E.Cast(enemy.Position, true);
                    return;
                }

                if (Config.Item("telE").GetValue<bool>())
                {
                    foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < E.Range && Obj.Team != Player.Team && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                    {
                        E.Cast(Object.Position);
                    }
                }

                var ta = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (Player.IsMoving && ta.IsValidTarget(E.Range) && E.GetPrediction(ta).CastPosition.Distance(ta.Position) > 200 && (int)E.GetPrediction(ta).Hitchance == 5 && Program.Combo && Config.Item("comboE").GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA)
                {
                    if (ta.HasBuffOfType(BuffType.Slow) || OktwCommon.CountEnemiesInRangeDeley(E.GetPrediction(ta).CastPosition, 250, E.Delay) > 1 )
                    {
                        Program.CastSpell(E, ta);
                        debug("E slow");
                    }
                    else
                    {
                        var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (Program.Combo && t.IsValidTarget(W.Range) &&  E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                        {
                            if (ObjectManager.Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                            {
                                if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                                    Program.CastSpell(E, t);
                            }
                            else
                            {
                                if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                                    Program.CastSpell(E, t);
                            }
                        }
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Game.Time - WCastTime > 0.9 && Config.Item("autoR").GetValue<bool>())
            {
                bool cast = false;
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && Program.ValidUlt(target)))
                {
                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    var Rdmg = R.GetDamage(target,1);

                    if (Rdmg > predictedHealth)
                    {
                        cast = true;
                        PredictionOutput output = R.GetPrediction(target);
                        Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                        direction.Normalize();
                        List<Obj_AI_Hero> enemies = Program.Enemies.Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
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

        public void farmQ()
        {
            foreach (var minion in MinionManager.GetMinions(bonusRange() + 30).Where(
                minion => !Orbwalking.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion)))
                {
                    Orbwalker.ForceTarget(minion);
                    Q.Cast();
                    return;
                }
        }

        private float bonusRange() { return 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level; }

        private bool FishBoneActive {get { return Player.AttackRange > 525f; }}

        private  float GetRealPowPowRange(GameObject target) {return 630f + Player.BoundingRadius + target.BoundingRadius;}

        private float GetRealDistance(Obj_AI_Base target)
        {
            return Player.ServerPosition.Distance(target.ServerPosition) + Player.BoundingRadius +
                   target.BoundingRadius;
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

        public void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Console.WriteLine(msg);
        }

        private void SetMana()
        {
            QMANA = 10;
            WMANA = W.Level * 10 + 40;
            EMANA = 50;

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * 6;
            else
                RMANA = 100; 

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
            if (Config.Item("debug").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Height * 0.5f, Drawing.Height * 0.5f, System.Drawing.Color.GreenYellow, "ManaCost: Q " + QMANA + " W " + WMANA + " E " + EMANA + " R " + RMANA);
            }

            if (Config.Item("watermark").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0f, System.Drawing.Color.Cyan, "OneKeyToWin AIO - " + Player.ChampionName + " by Sebby");
            }
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
