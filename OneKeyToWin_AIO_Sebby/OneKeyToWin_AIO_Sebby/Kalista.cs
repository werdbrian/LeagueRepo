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
        public Spell Q, W, E, R;
        public float QMANA, WMANA, EMANA, RMANA;

        private int count = 0 , countE = 0;

        private static Obj_AI_Hero AllyR;

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1130);
            W = new Spell(SpellSlot.W, 5200);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1400f);

            Q.SetSkillshot(0.25f, 30f, 1700f, true, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
           // Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
        }

        private void LoadMenuOKTW()
        {
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Haras Q").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("jungleE", "Jungle ks E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("countE", "auto E out AA stack").SetValue(new Slider(10, 30, 0)));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if ( Player.Mana > QMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Q.IsReady() && Target.IsValidTarget(Q.Range) )
                    Q.Cast(Target, true);
            }
            
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            foreach (var enemy in Program.Enemies.Where(target => target.IsValidTarget(E.Range + 500) && target.IsEnemy ))
            {
                float hp = enemy.Health - E.GetDamage(enemy);
                int stack = GetRStacks(enemy);
                float dmg = (float)Player.GetAutoAttackDamage(enemy) * 2f;
                if (stack > 0)
                    dmg = (float)Player.GetAutoAttackDamage(enemy) + (E.GetDamage(enemy) / (float)stack);
                
                if (hp >0)
                    drawText((int)((hp/dmg) + 1) + "hit", enemy, System.Drawing.Color.GreenYellow);
                else
                    drawText("KILL E", enemy, System.Drawing.Color.Red);
            }

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
            
            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
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

            if (Program.LagFree(1) && Q.IsReady() && !Player.IsWindingUp)
                LogicQ();
            if (Program.LagFree(2) && E.IsReady())
                JungleE();
            if (Program.LagFree(3) && E.IsReady())
            {
                farm();
                LogicE();
            }
            if (Program.LagFree(4) && R.IsReady() && Config.Item("autoR").GetValue<bool>())
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
                bool back = false;
                List<Vector2> waypoints = t.GetWaypoints();
                if ((ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(t.Position)) > 100)
                {
                    back = true;
                }
                var qDmg = Q.GetDamage(t) + ObjectManager.Player.GetAutoAttackDamage(t);
                var eDmg = E.GetDamage(t);

                if (qDmg > t.Health && eDmg < t.Health && ObjectManager.Player.Mana > QMANA + EMANA)
                    Program.CastSpell(Q, t);
                else if ((qDmg * 1.1) + eDmg > t.Health && eDmg < t.Health && ObjectManager.Player.Mana > QMANA + EMANA && Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA && ((!Orbwalking.InAutoAttackRange(t) && back) || Player.CountEnemiesInRange(400) > 0))
                    Program.CastSpell(Q, t);
                else if (Program.Farm && Config.Item("haras" + t.ChampionName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA && !Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Program.Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA )
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !Program.CanMove(enemy)))
                        Q.Cast(enemy, true);
                    
                }
            }
        }
        private void LogicR()
        {
            if (Player.IsRecalling() || Player.InFountain())
                return;

            if (AllyR == null)
            {
                foreach (var ally in Program.Allies.Where(ally => !ally.IsDead && !ally.IsMe && ally.HasBuff("kalistacoopstrikeally")))
                {
                    AllyR = ally;
                    break;
                }
            }
            else if (AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range)
            {
                if (AllyR.Health < AllyR.CountEnemiesInRange(600) * AllyR.Level * 20)
                {
                    R.Cast();
                }
            }
        }
        private void LogicE()
        {
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && target.IsEnemy && Program.ValidUlt(target)))
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
                    && (Game.Time - GetPassiveTime(target) > -0.5 || Player.Distance(target.ServerPosition) > E.Range - 100 || Player.Health < Player.MaxHealth * 0.4)
                    && Player.Mana > RMANA + QMANA + EMANA + WMANA
                    && Player.CountEnemiesInRange(900) == 0)
                {
                    E.Cast();
                    return;
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && (count > 1 || ((Player.UnderTurret(false) && !Player.UnderTurret(true)) && count > 0 && Player.Mana > RMANA + QMANA + EMANA)))
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
            int outRange = 0;
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget(E.Range) && minion.IsEnemy))
            {
                if (minion.Health < E.GetDamage(minion) && minion.GetAutoAttackDamage(minion) * 3 < E.GetDamage(minion) && minion.Health > minion.GetAutoAttackDamage(minion) * 2)
                {
                    count++;
                    if (!Orbwalking.InAutoAttackRange(minion))
                    {
                        outRange++;
                    }
                }
            }
            if (Program.Farm && outRange > 0)
            {
                E.Cast();
                return 0;
            }
            return count;
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = 60;
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
    }
}
