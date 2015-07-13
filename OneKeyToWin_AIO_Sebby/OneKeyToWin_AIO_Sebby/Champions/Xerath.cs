using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Xerath
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, W, E, R;
        private float QMANA, WMANA, EMANA, RMANA;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1550);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 1150);
            R = new Spell(SpellSlot.R, 675);

            Q.SetSkillshot(0.6f, 100f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.7f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 60f, 1400f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.7f, 120f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 800, 1550, 1.5f);

            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("maxQ", "Cast Q only max range").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("fastQ", "Fast cast Q").SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harras W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("rCount", "Auto R if enemies in range (combo mode)").SetValue(new Slider(3, 0, 5)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W").SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.Distance(gapcloser.Sender.Position) < E.Range)
            {
                E.Cast(gapcloser.Sender);
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Player.Distance(sender.Position) < E.Range)
            {
                E.Cast(sender);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            //Program.debug(""+OktwCommon.GetPassiveTime(Player, "XerathArcanopulseChargeUp"));

            if (IsCastingR)
            {
                OktwCommon.blockAttack = true;
                OktwCommon.blockMove = true;
            }
            else
            {
                OktwCommon.blockAttack = false;
                OktwCommon.blockMove = false;
            }

            if (Program.LagFree(1))
            {
                SetMana();
                Jungle();
            }

            if (E.IsReady() && Config.Item("autoQ").GetValue<bool>())
                LogicE();
            if (Program.LagFree(2) && W.IsReady() && Config.Item("autoW").GetValue<bool>())
                LogicW();
            if (Program.LagFree(3) && Q.IsReady() && Config.Item("autoE").GetValue<bool>())
                LogicQ();
            if (Program.LagFree(4) && R.IsReady() && Config.Item("autoR").GetValue<bool>())
                LogicR();
        }

        private void LogicR()
        {
            R.Range = 1850 + R.Level * 1050;
            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(900) == 0 && t.IsValidTarget() && !t.IsValidTarget(W.Range) && t.CountAlliesInRange(500) == 0)
            {
                if (R.GetDamage(t) * 2 > t.Health)
                {
                    Program.CastSpell(R, t);
                }
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                var wDmg = W.GetDamage(t);
                if (wDmg > t.Health)
                {
                    Program.CastSpell(W, t);
                }
                else if (wDmg + qDmg > t.Health && Player.Mana > WMANA  + QMANA)
                    Program.CastSpell(W, t);
                else if (Program.Combo && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    Program.CastSpell(W, t);
                else if (Program.Farm && Config.Item("harrasW").GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && (Player.Mana > Player.MaxMana * 0.8 || W.Level > Q.Level) && Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA && OktwCommon.CanHarras())
                    Program.CastSpell(W, t);
                else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
            else if (Program.LaneClear && Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmW").GetValue<bool>() && Player.Mana > RMANA + QMANA + WMANA)
            {
                var allMinions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All);
                var farmPos = W.GetCircularFarmLocation(allMinions, W.Width);
                if (farmPos.MinionsHit > 3)
                    W.Cast(farmPos.Position);
            }
        }

        private void LogicQ()
        {

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var t2 = TargetSelector.GetTarget(1550, TargetSelector.DamageType.Physical);
            
            if (t.IsValidTarget())
            {
                if (Q.IsCharging)
                {
                    if (OktwCommon.GetPassiveTime(Player, "XerathArcanopulseChargeUp") > 2)
                        Program.CastSpell(Q, t);
                    else
                        Q.Cast(Q.GetPrediction(t).CastPosition);
                    return;
                }
                else if (t.IsValidTarget(Q.Range - 300))
                {
                    if (Program.Combo && Player.Mana > EMANA + QMANA)
                    {
                        Q.StartCharging();
                    }
                    else if (Program.Farm && Player.Mana > RMANA + EMANA + QMANA + QMANA && Config.Item("harras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && OktwCommon.CanHarras())
                    {
                        Q.StartCharging();
                    }
                    else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + WMANA)
                    {
                        foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                            Q.StartCharging();
                    }
                }
            }
            if (!t2.IsValidTarget() && Q.Range > 1500 && Program.LaneClear && (Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmQ").GetValue<bool>() && Player.Mana > RMANA + QMANA + WMANA))
            {
                var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All);
                var Qfarm = Q.GetLineFarmLocation(allMinionsQ, Q.Width);
                if (Qfarm.MinionsHit > 0)
                    Q.Cast(Qfarm.Position);
            }
        }

        private void LogicE()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && E.GetDamage(enemy) + Q.GetDamage(enemy) + W.GetDamage(enemy) > enemy.Health))
            {
                Program.CastSpell(E, enemy);
            }
            var t = Orbwalker.GetTarget() as Obj_AI_Hero;
            if (!t.IsValidTarget())
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if ( Player.Mana > RMANA + EMANA)
                {
                    if (Program.Combo )
                        Program.CastSpell(E, t);
                    
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                        E.Cast(enemy);
                }
            }
        }

        private bool IsCastingR
        {
            get
            {
                return Player.HasBuff("XerathLocusOfPower2", true) ||
                       (ObjectManager.Player.LastCastedSpellName() == "XerathLocusOfPower2" &&
                        Utils.TickCount - ObjectManager.Player.LastCastedSpellT() < 500);
            }
        }

        private void Jungle()
        {
            if (Player.Mana > RMANA + WMANA + RMANA + WMANA)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (W.IsReady() && Config.Item("jungleW").GetValue<bool>())
                    {
                        W.Cast(mob);
                        return;
                    }
                    if (Q.IsReady() && Config.Item("jungleQ").GetValue<bool>())
                    {
                        Q.Cast(mob);
                        return;
                    }
                }
            }
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;

            if (Player.Health < Player.MaxHealth * 0.2 || Q.IsCharging)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }
    }
}
