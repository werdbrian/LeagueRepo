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
    class Vayne
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W;
        private float QMANA, WMANA, EMANA, RMANA;

        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 300);
            E = new Spell(SpellSlot.E, 680);
            R = new Spell(SpellSlot.R, 3000);

            E.SetTargetted(0.1f, 3000f);

            LoadMenuOKTW();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Interrupter.OnPossibleToInterrupt += OnInterruptableSpell;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        private void LoadMenuOKTW()
        {
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange2", "E push position").SetValue(false));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("QE", "try Q + E ").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").AddItem(new MenuItem("gapQ", "Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").AddItem(new MenuItem("gapE", "E").SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").SubMenu("Use on").AddItem(new MenuItem("gap" + enemy.ChampionName, enemy.ChampionName).SetValue(true));


            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("visibleR", "Unvisable block AA ").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoQR", "Auto Q when R active ").SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("E config").SubMenu("Use E ").AddItem(new MenuItem("stun" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

        }

        private void OnInterruptableSpell(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (E.IsReady() && unit.IsValidTarget(E.Range))
                E.Cast(unit);
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var target = gapcloser.Sender;

            if (!target.IsValidTarget(E.Range) && Config.Item("gap" + target.ChampionName).GetValue<bool>())
                return;
            if (E.IsReady() && Config.Item("gapE").GetValue<bool>() )
                E.Cast(target);
            if (Q.IsReady() && Config.Item("gapQ").GetValue<bool>() )
                Q.Cast();
            return;
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("visibleR").GetValue<bool>() && Player.HasBuff("vaynetumblefade") && Player.CountEnemiesInRange(800)>1)
                args.Process = false;

            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(800) && GetWStacks(target) > 0))
            {
                if (Orbwalking.InAutoAttackRange(target))
                    Orbwalker.ForceTarget(target);
            }
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            var t = target as Obj_AI_Hero;

            var dashPosition = Player.Position.Extend(Game.CursorPos, Q.Range);

            if (Q.IsReady() && t.IsValidTarget() && GetWStacks(t) == 1 && t.Position.Distance(Game.CursorPos) < t.Position.Distance(Player.Position))
            {
                Q.Cast(dashPosition, true);
                Program.debug("" + t.Name + GetWStacks(t));
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            var dashPosition = Player.Position.Extend(Game.CursorPos, Q.Range);
            if (E.IsReady())
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && target.Path.Count() < 2 && Config.Item("stun" + target.ChampionName).GetValue<bool>()))
                {
                    if (CondemnCheck(Player.Position, target) )
                        E.Cast(target);
                    else if (Q.IsReady() && !dashPosition.IsWall() && Config.Item("QE").GetValue<bool>() && CondemnCheck(dashPosition, target))
                    {
                        Q.Cast(dashPosition, true);
                        Program.debug("Q + E");
                    }
                }
            }

            if (Program.LagFree(1) && Q.IsReady())
            {
                if (Config.Item("autoQR").GetValue<bool>() && Player.HasBuff("vayneinquisition"))
                {
                    Q.Cast(dashPosition, true);
                }
                if (Program.Combo && !dashPosition.IsWall())
                {
                    var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

                    if (t.IsValidTarget() && !Orbwalking.InAutoAttackRange(t) && t.Position.Distance(Game.CursorPos)  < t.Position.Distance(Player.Position) && dashPosition.CountEnemiesInRange(800) < 3)
                    {
                        Q.Cast(dashPosition, true);
                    }
                }
            }

            if (Program.LagFree(2))
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(270) && target.IsMeele))
                {
                    if (Q.IsReady() && !dashPosition.IsWall())
                        Q.Cast(dashPosition, true);
                    else if (E.IsReady() && Player.Health < Player.MaxHealth * 0.5)
                    {
                        E.Cast(target);
                        Program.debug("push");
                    }
                }
            }

            if (Program.LagFree(3) && R.IsReady() )
            {
                if ( Config.Item("autoR").GetValue<bool>())
                {
                    if (Player.CountEnemiesInRange(700) > 2)
                        R.Cast();
                    else if (Program.Combo && Player.CountEnemiesInRange(600) > 1)
                        R.Cast();
                    else if (Player.Health < Player.MaxHealth * 0.5 && Player.CountEnemiesInRange(500) > 0)
                        R.Cast();
                }
            }
        }

        private bool CondemnCheck(Vector3 fromPosition, Obj_AI_Hero target)
        {
            var poutput = E.GetPrediction(target);
            if ((int)poutput.Hitchance < 5)
                return false;
            float pushDistance;
            if (Player.Position == fromPosition )
                pushDistance = 325 + target.BoundingRadius;
            else
                pushDistance = 230 + target.BoundingRadius;

            var finalPosition = poutput.CastPosition.Extend(fromPosition, -pushDistance);
                
            if (finalPosition.IsWall())
                return true;
            else
                return false;
        }

        private int GetWStacks(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "vaynesilvereddebuff")
                    return buff.Count;
            }
            return 0;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("watermark").GetValue<bool>())
                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0f, System.Drawing.Color.Cyan, "OneKeyToWin AIO - " + Player.ChampionName + " by Sebby");

            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range + E.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range + E.Range, System.Drawing.Color.Cyan, 1, 1);
            }

            if (E.IsReady() && Config.Item("eRange2").GetValue<bool>())
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(800)))
                {
                    var poutput = E.GetPrediction(target);

                    var pushDistance = 350 + target.BoundingRadius;

                    var finalPosition = poutput.CastPosition.Extend(Player.ServerPosition, -pushDistance);
                    if (finalPosition.IsWall())
                        Render.Circle.DrawCircle(finalPosition, 50, System.Drawing.Color.Red);
                    else
                        Render.Circle.DrawCircle(finalPosition, 50, System.Drawing.Color.YellowGreen);


                }
            }
        }
    }
}
