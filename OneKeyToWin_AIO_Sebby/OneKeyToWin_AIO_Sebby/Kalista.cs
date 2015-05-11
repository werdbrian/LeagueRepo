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
    class Kalista
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
        private int count = 0 , countE = 0;

        private static Obj_AI_Hero AllyR;

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1180);
            W = new Spell(SpellSlot.W, 5200);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1400f);

            Q.SetSkillshot(0.25f, 30f, 1700f, true, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
           // Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

        }

        private void Drawing_OnDraw(EventArgs args)
        {

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(E.Range + 500) && target.IsEnemy ))
            {
                int hp = (int)enemy.Health - (int)E.GetDamage(enemy);
                if (hp >0)
                    drawText("" + hp, enemy, System.Drawing.Color.GreenYellow);
                else
                    drawText("KILL E" + hp, enemy, System.Drawing.Color.Red);
            }
          
        }


        public static void drawText(string msg, Obj_AI_Hero Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
                countE = Config.Item("countE").GetValue<Slider>().Value;
            }

            if (Program.LagFree(1) && Q.IsReady() && Program.attackNow)
                LogicQ();
            if (Program.LagFree(2) && E.IsReady())
                JungleE();
            if (Program.LagFree(3) && E.IsReady() && Program.attackNow)
            {
                farm();
                LogicE();
            }
            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }

        private void JungleE()
        {

            if (!Config.Item("jungleE").GetValue<bool>())
                return;

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var mob in mobs)
            {
                if (mob.Health < E.GetDamage(mob))
                    E.Cast();
            } 
        }
        private void LogicQ()
        {

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t) + ObjectManager.Player.GetAutoAttackDamage(t);
                var eDmg = E.GetDamage(t);
                if (qDmg > t.Health && eDmg < t.Health && ObjectManager.Player.Mana > QMANA + EMANA && Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if ((qDmg * 1.1) + eDmg > t.Health && eDmg < t.Health && ObjectManager.Player.Mana > QMANA + EMANA && Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA && !Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if (Program.Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA && !Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Program.Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA && !Orbwalking.InAutoAttackRange(t))
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range) && !Program.CanMove(enemy)))
                    {
                        Q.Cast(enemy, true);
                    }
                }
            }
        }
        private void LogicR()
        {
            if (Player.IsRecalling() || Player.InFountain())
                return;

            if (AllyR == null)
            {
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsDead && !ally.IsMe && ally.HasBuff("kalistacoopstrikeally")))
                {

                    AllyR = ally;
                    break;
                }
            }
            else if (AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range)
            {
                if (AllyR.Health < AllyR.CountEnemiesInRange(600) * AllyR.Level * 15)
                {
                    R.Cast();
                }
            }
        }
        private void LogicE()
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(E.Range) && target.IsEnemy && Program.ValidUlt(target)))
            {
                var Edmg = E.GetDamage(target);
                if (target.Health + target.HPRegenRate < Edmg)
                {
                    E.Cast();
                    return;
                }
                if (0 < Edmg && count > 0)
                {
                    E.Cast();
                    return;
                }

                if (GetRStacks(target) >= countE 
                    && (Game.Time - GetPassiveTime(target) > -0.5 || Player.Distance(target.ServerPosition) > E.Range - 100)
                    && Player.Distance(target.ServerPosition) > Player.Distance(target.Position)
                    && Player.Mana > RMANA + QMANA + EMANA + WMANA
                    && Player.CountEnemiesInRange(900) == 0)
                {
                    E.Cast();
                    return;
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && count > 1)
            {
                E.Cast();
                return;
            }
        }

        private float GetPassiveTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "kalistaexpungemarker")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }
        private int GetRStacks(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "kalistaexpungemarker")
                    return buff.Count;
            }
            return 0;
        }

        private int farm()
        {
            count = 0;
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget(E.Range) && minion.IsEnemy))
            {
                if (minion.Health < E.GetDamage(minion) && minion.GetAutoAttackDamage(minion) * 3 < E.GetDamage(minion) && minion.Health > minion.GetAutoAttackDamage(minion) * 2)
                    count++;
            }
            return count;
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost + 20;

            if (!R.IsReady())
                RMANA = EMANA - Player.PARRegenRate * E.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;

            if (Player.Health < Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        private void LoadMenuOKTW()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Haras Q").AddItem(new MenuItem("haras" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("jungleE", "Jungle ks E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("countE", "auto E out AA stack").SetValue(new Slider(10, 30, 0)));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
        }
    }
}
