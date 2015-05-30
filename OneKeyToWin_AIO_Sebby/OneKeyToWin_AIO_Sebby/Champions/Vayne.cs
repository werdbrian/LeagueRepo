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
            E = new Spell(SpellSlot.E, 670);
            R = new Spell(SpellSlot.R, 3000);

            E.SetTargetted(0f, 3000f);

            LoadMenuOKTW();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && target.Path.Count() < 2))
            {
                var poutput = E.GetPrediction(target);
                if ((int)poutput.Hitchance < 5)
                    return;

                var pushDistance = 350 + target.BoundingRadius;

                var finalPosition = poutput.CastPosition.Extend(Player.ServerPosition, -pushDistance);

                    Render.Circle.DrawCircle(finalPosition, 50, System.Drawing.Color.YellowGreen);
                
            }
        }
        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var Target = gapcloser.Sender;
            if (Q.IsReady() && Target.IsValidTarget(E.Range))
                Q.Cast();

            else if ( E.IsReady() && Target.IsValidTarget(E.Range) )
                E.CastOnUnit(Player);
            return;
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var t = args.Target as Obj_AI_Hero;
            if (t.IsValidTarget() && W.GetDamage(t) + (float)Player.GetAutoAttackDamage(t, true) > t.Health)
                return;

            if (Config.Item("visibleR").GetValue<bool>() && Player.HasBuff("vaynetumblefade"))
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

            if (t.IsValidTarget() && GetWStacks(t) == 1 && t.Position.Distance(Game.CursorPos)  < t.Position.Distance(Player.Position))
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
                CondemnCheck(Player.ServerPosition);
            }

            if ( Q.IsReady())
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
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(250) && target.IsMeele))
            {
                if (Q.IsReady())
                    Q.Cast(dashPosition, true);
                else if (E.IsReady() && Player.Health < Player.MaxHealth * 0.6)
                    E.Cast(target);
            }
            if (R.IsReady() )
            {
                if (Program.Combo && Config.Item("autoR").GetValue<bool>() && Player.CountEnemiesInRange(700) > 2)
                    R.Cast();
            }
        }



        private void LoadMenuOKTW()
        {


            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("visibleR", "Unvisable block AA ").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoQR", "Auto Q when R active ").SetValue(true));


        }

        private void CondemnCheck(Vector3 fromPosition)
        {
            //VHReborn Condemn Code
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && target.Path.Count() < 2))
            {
                var poutput = E.GetPrediction(target);
                if ((int)poutput.Hitchance < 5)
                    return;

                var pushDistance = 330 + target.BoundingRadius;
                
                var finalPosition = poutput.CastPosition.Extend(Player.ServerPosition, -pushDistance);

                if (finalPosition.IsWall())
                {
                    E.Cast(target);
                }
            }
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
    }
}
