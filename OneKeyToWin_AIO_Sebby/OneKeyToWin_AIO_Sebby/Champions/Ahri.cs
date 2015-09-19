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
    class Ahri
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, W, E, R;
        private float QMANA, WMANA, EMANA, RMANA;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static GameObject QMissile = null, EMissile = null;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 895);
            W = new Spell(SpellSlot.W, 580);
            E = new Spell(SpellSlot.E, 920);
            R = new Spell(SpellSlot.R, 600);

            Q.SetSkillshot(0.25f, 100, 1600, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 60, 1550, true, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification & line").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("Qhelp", "Show Q helper").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harrasQ", "Harras Q").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harras W").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("harrasE", "Harras E").SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("E Config").SubMenu("Use on:").AddItem(new MenuItem("Eon" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W").SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_SpellMissile.OnCreate += SpellMissile_OnCreateOld;
            Obj_SpellMissile.OnDelete += Obj_SpellMissile_OnDelete;
        }

        private void Obj_SpellMissile_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<MissileClient>())
                return;
            MissileClient missile = (MissileClient)sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null)
            {
                if(missile.SData.Name == "AhriOrbReturn")
                    QMissile = null;
                if (missile.SData.Name == "AhriSeduceMissile")
                    EMissile = null;
            }
        }

        private void SpellMissile_OnCreateOld(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<MissileClient>())
                return;

            MissileClient missile = (MissileClient)sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null )
            {
                if (missile.SData.Name == "AhriOrbMissile" || missile.SData.Name == "AhriOrbReturn")
                {
                    QMissile = sender;
                }
                if (missile.SData.Name == "AhriSeduceMissile")
                {
                    EMissile = sender;
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.Distance(gapcloser.Sender.ServerPosition) < E.Range)
            {
                E.Cast(gapcloser.Sender);
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Player.Distance(sender.ServerPosition) < E.Range)
            {
                E.Cast(sender);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
                Jungle();
            }
            
            if (E.IsReady() && Config.Item("autoE").GetValue<bool>())
                LogicE();
            if (Program.LagFree(2) && W.IsReady() && Config.Item("autoW").GetValue<bool>())
                LogicW();
            if (Program.LagFree(3) && Q.IsReady() && Config.Item("autoQ").GetValue<bool>())
                LogicQ();
            if (Program.LagFree(4) && R.IsReady() && Config.Item("autoR").GetValue<bool>() && Program.Combo)
                LogicR();
        }

        private void LogicR()
        {
            var dashPosition = Player.Position.Extend(Game.CursorPos, 450);
            if (Player.Distance(Game.CursorPos) < 450)
                dashPosition = Game.CursorPos;

            if (dashPosition.CountEnemiesInRange(800) > 2)
                return;

            if (Player.HasBuff("AhriTumble"))
            {
                var BuffTime = OktwCommon.GetPassiveTime(Player, "AhriTumble");
                if (BuffTime < 3)
                {
                    R.Cast(dashPosition);
                }
            }
            
            var t = TargetSelector.GetTarget(450 + R.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                var comboDmg = R.GetDamage(t) * 3;
                if (Q.IsReady())
                {
                    comboDmg += Q.GetDamage(t) * 2;
                }
                if (W.IsReady())
                {
                    comboDmg += W.GetDamage(t) + W.GetDamage(t,1);
                }
                if (t.CountAlliesInRange(600) < 2 && comboDmg > t.Health && t.Position.Distance(Game.CursorPos) < t.Position.Distance(Player.Position)  && dashPosition.Distance(t.ServerPosition) < 500)
                {
                    R.Cast(dashPosition);
                }

                foreach (var target in Program.Enemies.Where(target => target.IsMelee && target.IsValidTarget(300)))
                {
                    R.Cast(dashPosition);
                }
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (W.GetDamage(t) + W.GetDamage(t, 1) + Q.GetDamage(t) * 2 > t.Health)
                    W.Cast();
                else if (Program.Combo && Player.Mana > RMANA + WMANA)
                    W.Cast();
                else if (Program.Farm && Player.Mana > RMANA + QMANA + WMANA && Config.Item("harrasW").GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>())
                    W.Cast();
            }
            else if (Program.LaneClear && QMissile == null && (Player.ManaPercent > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmW").GetValue<bool>() && Player.Mana > RMANA + WMANA))
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                foreach (var minion in minionList.Where(minion =>  minion.Health < W.GetDamage(minion)))
                {
                    W.Cast();
                }
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (EMissile == null || !EMissile.IsValid)
                {
                    if (Q.GetDamage(t) * 2 + OktwCommon.GetEchoLudenDamage(t) > t.Health)
                        Q.Cast(t, true);
                    else if (Program.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                        Program.CastSpell(Q, t);
                    else if (Program.Farm && Player.Mana > RMANA + WMANA + QMANA + QMANA && Config.Item("harrasQ").GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>())
                        Program.CastSpell(Q, t);
                }
                if (Player.Mana > RMANA + QMANA )
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmQ").GetValue<bool>() && Player.Mana > RMANA + QMANA)
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All);
                var farmPosition = Q.GetLineFarmLocation(minionList, Q.Width);
                if (farmPosition.MinionsHit > Config.Item("LCminions", true).GetValue<Slider>().Value)
                    Q.Cast(farmPosition.Position);
            }
        }

        private void LogicE()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && E.GetDamage(enemy) + Q.GetDamage(enemy) + W.GetDamage(enemy) + OktwCommon.GetEchoLudenDamage(enemy) > enemy.Health))
            {
                Program.CastSpell(E, enemy);
            }
            var t = Orbwalker.GetTarget() as Obj_AI_Hero;
            if (!t.IsValidTarget())
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget() && Config.Item("Eon" + t.ChampionName).GetValue<bool>())
            {
                if (Program.Combo && Player.Mana > RMANA + EMANA)
                    Program.CastSpell(E, t);
                if (Program.Farm && Config.Item("harrasE").GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    Program.CastSpell(E, t);
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                    E.Cast(enemy);
            }
        }

        private void Jungle()
        {
            if (Program.LaneClear && Player.Mana > QMANA + RMANA)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (W.IsReady() && Config.Item("jungleW").GetValue<bool>())
                    {
                        W.Cast();
                        return;
                    }
                    if (Q.IsReady() && Config.Item("jungleQ").GetValue<bool>())
                    {
                        Q.Cast(mob.Position);
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

            RMANA = RMANA - (30 + Player.Level * 3 + Player.Level);

            if (Player.Health < Player.MaxHealth * 0.2 || Q.IsCharging)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (QMissile != null && QMissile.IsValid && Config.Item("Qhelp").GetValue<bool>())
                OktwCommon.DrawLineRectangle(QMissile.Position, Player.Position, (int)Q.Width, 1, System.Drawing.Color.White);

            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }

            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }

            if (Config.Item("eRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }

            if (Config.Item("noti").GetValue<bool>())
            {

                var t = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    var comboDmg = 0f;
                    if (R.IsReady())
                    {
                        comboDmg += R.GetDamage(t) * 3;
                    }
                    if (Q.IsReady())
                    {
                        comboDmg += Q.GetDamage(t) * 2;
                    }
                    if (W.IsReady())
                    {
                        comboDmg += W.GetDamage(t) + W.GetDamage(t, 1);
                    }
                    if (comboDmg > t.Health)
                    {

                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "COMBO KILL " + t.ChampionName + " have: " + t.Health + "hp");
                        drawLine(t.Position, Player.Position, 10, System.Drawing.Color.Yellow);
                    }
                }
            }
        }
    }
}
