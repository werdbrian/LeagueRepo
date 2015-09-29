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
    class Jayce
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, Q2, Qext, QextCol, W, W2, E, E2, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, QMANA2 = 0, WMANA2 = 0, EMANA2 = 0, RMANA = 0;
        private float Qcd, Wcd, Ecd, Q2cd, W2cd, E2cd;
        private float Qcdt, Wcdt, Ecdt, Q2cdt, W2cdt, E2cdt;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            #region SET SKILLS
            Q = new Spell(SpellSlot.Q, 1030);
            Qext = new Spell(SpellSlot.Q, 1650);
            QextCol = new Spell(SpellSlot.Q, 1650);
            Q2 = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            W2 = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 650);
            E2 = new Spell(SpellSlot.E, 240);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 80, 1200, true, SkillshotType.SkillshotLine);
            Qext.SetSkillshot(0.25f, 100, 1600, false, SkillshotType.SkillshotLine);
            QextCol.SetSkillshot(0.25f, 100, 1600, true, SkillshotType.SkillshotLine);
            Q2.SetTargetted(0.25f, float.MaxValue);
            E.SetSkillshot(0.1f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E2.SetTargetted(0.25f, float.MaxValue);
            #endregion

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification & line", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("gapE", "Gapcloser R + E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("intE", "Interrupt spells R + Q + E", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("flee", "FLEE MODE", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space


            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += OnUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item("gapE", true).GetValue<bool>() || E2cd > 0.1)
                return;

            if(Range && !R.IsReady())
                return;

            var t = gapcloser.Sender;

            if (t.IsValidTarget(400))
            {
                if (Range)
                {
                    R.Cast();
                }
                else
                    E.Cast(t);
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero t, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Config.Item("intE", true).GetValue<bool>() || E2cd > 0.1)
                return;

            if (Range && !R.IsReady())
                return;

            if (t.IsValidTarget(300))
            {
                if (Range)
                {
                    R.Cast();
                }
                else 
                    E.Cast(t);

            }
            else if (Q2cd < 0.2 && t.IsValidTarget(Q2.Range))
            {
                if (Range)
                {
                    R.Cast();
                }
                else
                {
                    Q.Cast(t);
                    if(t.IsValidTarget(E2.Range))
                        E.Cast(t);
                }
            }
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q )
            {
                E.Cast(Player.ServerPosition .Extend(args.EndPosition, 120));
            }

        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (W.IsReady() && Range && args.Target is Obj_AI_Hero)
            {
                if(Program.Combo)
                    W.Cast();
                else if (args.Target.Position.Distance(Player.Position)< 500)
                    W.Cast();
            }
        }

        private void OnUpdate(EventArgs args)
        {
            SetValue();
            if (Config.Item("flee", true).GetValue<KeyBind>().Active)
            {
                FleeMode();
            }

            if (Range)
            {

                if (Program.LagFree(1) && Q.IsReady())
                    LogicQ();

                if (Program.LagFree(2) && W.IsReady())
                    LogicW();
            }
            else
            {
                if (Program.LagFree(1) && E.IsReady())
                    LogicE2();

                if (Program.LagFree(2) && Q.IsReady())
                    LogicQ2();

                if (Program.LagFree(3) && W.IsReady())
                    LogicW2();
            }

            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }

        private void FleeMode()
        {
            if (Range)
            {
                if (E.IsReady())
                    E.Cast(Player.Position.Extend(Game.CursorPos, 150));
                else if (R.IsReady())
                    R.Cast();
            }
            else
            {
                if (Q2.IsReady())
                {
                    var mobs = MinionManager.GetMinions(Player.ServerPosition, Q2.Range);
                    

                    if (mobs.Count > 0)
                    {
                        Obj_AI_Base best;
                        best = mobs[0];

                        foreach (var mob in mobs.Where(mob => mob.IsValidTarget(Q2.Range)))
                        {
                            if (mob.Distance(Game.CursorPos) < best.Distance(Game.CursorPos))
                                best = mob;
                        }

                        Q2.Cast(best);
                    }
                    else if (R.IsReady())
                        R.Cast();
                }
                else if (R.IsReady())
                    R.Cast();
            }
        }

        private void LogicQ()
        {
            var Qtype = Q;
            if (CanUseQE())
                Qtype = Qext;

            var t = TargetSelector.GetTarget(Qtype.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = Qtype.GetDamage(t);

                if (CanUseQE())
                {
                    qDmg = qDmg * 1.4f;
                }

                if (qDmg > t.Health)
                    CastQ(t);
                else if (Program.Combo && Player.Mana > EMANA + QMANA)
                    CastQ(t);
                else if (Program.Farm && Player.Mana > RMANA + EMANA + QMANA + WMANA && !Player.UnderTurret(true) && OktwCommon.CanHarras())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Qtype.Range) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                    {
                        CastQ(t);
                    }
                }
                else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Qtype.Range) && !OktwCommon.CanMove(enemy)))
                        CastQ(t);
                }
            }
        }

        private void LogicW()
        {
            if (Program.Combo && R.IsReady() && Range && Orbwalker.GetTarget().IsValidTarget() && Orbwalker.GetTarget() is Obj_AI_Hero)
            {
                W.Cast();
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = E2.GetDamage(t);
                if (qDmg > t.Health)
                    E2.Cast(t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    E2.Cast(t);
            }
        }

        private void LogicQ2()
        {
            var t = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                if (Q2.GetDamage(t) > t.Health)
                    Q2.Cast(t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Q2.Cast(t);
            }
        }

        private void LogicW2()
        {
            if (Player.CountEnemiesInRange(300) > 0 && Player.Mana > 80)
                W.Cast();
        }

        private void LogicE2()
        {
            var t = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (E2.GetDamage(t) > t.Health)
                    E2.Cast(t);
                else if (Program.Combo && !Player.HasBuff("jaycehyperchargevfx"))
                    E2.Cast(t);
            }
        }

        private void LogicR()
        {
            if (Range)
            {
                var t = TargetSelector.GetTarget(Q2.Range + 300, TargetSelector.DamageType.Physical);
                if (Program.Combo && Qcd > 0.5 && !W.IsReady() && t.IsValidTarget() )
                {
                    if (Q2cd < 0.5 && t.CountEnemiesInRange(800) < 3)
                        R.Cast();
                    else if (Player.CountEnemiesInRange(300) > 0 && E2cd < 0.5)
                        R.Cast();
                }
            }
            else if (Program.Combo )
            {

                var t = TargetSelector.GetTarget(1400, TargetSelector.DamageType.Physical);
                if(t.IsValidTarget()&& !t.IsValidTarget(Q2.Range + 200) && Q.GetDamage(t) * 1.4 > t.Health && Qcd < 0.5 && Ecd < 0.5)
                {
                    R.Cast();
                }

                if (!Q.IsReady() && !E.IsReady())
                {
                    R.Cast();
                }   
            }
        }

        private void CastQ(Obj_AI_Base t)
        {
            if (!CanUseQE())
            {
                Program.CastSpell(Q, t);
                return; 
            }

            var poutput = QextCol.GetPrediction(t);
            bool cast = true;

            foreach (var minion in poutput.CollisionObjects.Where(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead && (t.Distance(poutput.CastPosition) > 100 || t.Distance(t.ServerPosition) > 100)))
            {
                cast = false;
                break;
            }

            if (cast)
                Program.CastSpell(Qext, t);
            else
                Program.CastSpell(QextCol, t);

        }

        private float GetComboDMG(Obj_AI_Base t)
        {
            float comboDMG = 0;

            if (Qcd < 1 && Ecd < 1)
                comboDMG = Q.GetDamage(t) * 1.4f;
            else if (Qcd < 1)
                comboDMG = Q.GetDamage(t);

            if (Q2cd < 1)
                comboDMG = Q.GetDamage(t, 1);

            if (Wcd < 1)
                comboDMG += (float)Player.GetAutoAttackDamage(t) * 3;

            if (W2cd < 1)
                comboDMG += W.GetDamage(t) * 2;

            if (E2cd < 1)
                comboDMG += E.GetDamage(t) * 3;
            return comboDMG;
        }

        private bool CanUseQE()
        {
            if(E.IsReady() && Player.Mana > QMANA + EMANA)
                return true;
            else
                return false;
        }

        private float SetPlus(float valus)
        {
            if (valus < 0)
                return 0;
            else
                return valus;
        }

        private void SetValue()
        {
            if (Range)
            {
                Qcdt = Q.Instance.CooldownExpires;
                Wcdt = W.Instance.CooldownExpires;
                Ecd = E.Instance.CooldownExpires;

                QMANA = Q.Instance.ManaCost;
                WMANA = W.Instance.ManaCost;
                EMANA = E.Instance.ManaCost;
            }
            else
            {
                Q2cdt = Q.Instance.CooldownExpires;
                W2cdt = W.Instance.CooldownExpires;
                E2cdt = E.Instance.CooldownExpires;

                QMANA2 = Q.Instance.ManaCost;
                WMANA2 = W.Instance.ManaCost;
                EMANA2 = E.Instance.ManaCost;
            }

            Qcd = SetPlus(Qcdt - Game.Time);
            Wcd = SetPlus(Wcdt - Game.Time);
            Ecd = SetPlus(Ecdt - Game.Time);
            Q2cd = SetPlus(Q2cdt - Game.Time);
            W2cd = SetPlus(W2cdt - Game.Time);
            E2cd = SetPlus(E2cdt - Game.Time);
        }

        private bool Range { get { return Q.Instance.Name.Contains("jayceshockblast"); } }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                    {
                        if (Range)
                        {
                            if (CanUseQE())
                                Utility.DrawCircle(Player.Position, Qext.Range, System.Drawing.Color.Cyan, 1, 1);
                            else
                                Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                        }
                        else
                            Utility.DrawCircle(Player.Position, Q2.Range, System.Drawing.Color.Orange, 1, 1);
                    }
                }
                else
                {
                    if (Range)
                    {
                        if (CanUseQE())
                            Utility.DrawCircle(Player.Position, Qext.Range, System.Drawing.Color.Cyan, 1, 1);
                        else
                            Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                    }
                    else
                        Utility.DrawCircle(Player.Position, Q2.Range, System.Drawing.Color.Orange, 1, 1);
                }
            }

            if (Config.Item("noti", true).GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(1600, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var damageCombo = GetComboDMG(t);
                    if (damageCombo > t.Health)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Combo deal  " + damageCombo + " to " + t.ChampionName);
                        drawLine(t.Position, Player.Position, 10, System.Drawing.Color.Yellow);
                    }

                }
            }
        }
    }
}
