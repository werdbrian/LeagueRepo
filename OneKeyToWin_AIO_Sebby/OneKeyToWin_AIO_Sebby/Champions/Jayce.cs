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
            Q = new Spell(SpellSlot.Q, 1050);
            Qext = new Spell(SpellSlot.Q, 1650);
            QextCol = new Spell(SpellSlot.Q, 1650);
            Q2 = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            W2 = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 650);
            E2 = new Spell(SpellSlot.E, 240);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 80, 1200, true, SkillshotType.SkillshotLine);
            Qext.SetSkillshot(0.25f, 80, 1600, false, SkillshotType.SkillshotLine);
            QextCol.SetSkillshot(0.25f, 80, 1600, true, SkillshotType.SkillshotLine);
            Q2.SetTargetted(0.25f, float.MaxValue);
            E.SetSkillshot(0.1f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E2.SetTargetted(0.25f, float.MaxValue);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(false));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += OnUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
           
            if (args.Slot == SpellSlot.Q )
            {
                E.Cast(Player.ServerPosition .Extend(args.EndPosition, 150));
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

            if(Range)
            {

                if (Program.LagFree(2) && Q.IsReady() )
                    LogicQ();

                if (Program.LagFree(3) && W.IsReady() )
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

        private void LogicQ()
        {
            var Qtype = Q;
            if (E.IsReady())
                Qtype = Qext;

            var t = TargetSelector.GetTarget(Qtype.Range, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(900) > 0)
                t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);


            if (t.IsValidTarget())
            {
                var qDmg = Qtype.GetDamage(t);
                if (qDmg > t.Health)
                    CastQ(t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
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

        private void LogicW2()
        {
            if (Player.CountEnemiesInRange(500) > 0)
                W.Cast();
        }

        private void LogicE2()
        {
            var t = TargetSelector.GetTarget(E2.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() && !Player.HasBuff("jaycehyperchargevfx") && t.CountEnemiesInRange(900) <3)
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
                var qDmg = Q2.GetDamage(t);
                if (qDmg > t.Health)
                    Q2.Cast(t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Q2.Cast(t);
            }
        }

        private void LogicR()
        {
            if (Range)
            {
                var t = TargetSelector.GetTarget(Q2.Range + 300, TargetSelector.DamageType.Physical);

                if (Program.Combo && Range && Qcd > 1 && Wcd > 1 && !E.IsReady() && t.IsValidTarget() && t.CountEnemiesInRange(900) < 3)
                    R.Cast();
            }
            else if (Program.Combo )
            {

                var t = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Physical);
                if (!Q.IsReady() && !E.IsReady() )
                {
                    R.Cast();
                }   
            }
        }

        private void CastQ(Obj_AI_Base t)
        {
            if(!E.IsReady())
                Program.CastSpell(Q, t);

            var poutput = QextCol.GetPrediction(t);
            bool cast = true;
            foreach (var minion in poutput.CollisionObjects.Where(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead && t.Distance(poutput.CastPosition) > 150))
            {
                cast = false;
                break;

            }
            if (cast)
                Program.CastSpell(Qext, t);
            else
                Program.CastSpell(QextCol, t);

        }

        private void Drawing_OnDraw(EventArgs args)
        {

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

    }
}
