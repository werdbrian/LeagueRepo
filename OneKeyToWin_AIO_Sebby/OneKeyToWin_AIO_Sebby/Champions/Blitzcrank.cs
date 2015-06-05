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
    class Blitzcrank
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        private Spell E, Q, R, W;

        private float QMANA, WMANA, EMANA, RMANA;

        public Obj_AI_Hero Player {get { return ObjectManager.Player; }}

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, 200);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 600);

            Q.SetSkillshot(0.25f, 110f, 1800f, true, SkillshotType.SkillshotLine);

            Config.AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
            Config.AddItem(new MenuItem("autoE", "Auto E").SetValue(true));


            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts", "Use common TargetSelector").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts1", "ON - only one target"));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts2", "OFF - all grab-able targets"));

            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("qCC", "Auto Q cc & dash enemy").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("minGrab", "Min range grab").SetValue(new Slider(250, 125, (int)Q.Range)));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("maxGrab", "Max range grab").SetValue(new Slider((int)Q.Range, 125, (int)Q.Range)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Q option").SubMenu("Grab").AddItem(new MenuItem("grab" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("rCount", "Auto R if enemies in range").SetValue(new Slider(3, 0, 5)));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("afterGrab", "Auto R after grab").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("rKs", "R ks").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("inter", "OnPossibleToInterrupt")).SetValue(true);
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("Gap", "OnEnemyGapcloser")).SetValue(true);

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy").SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnInterruptableSpell;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(Player.Position, (float)Config.Item("maxGrab").GetValue<Slider>().Value, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, (float)Config.Item("maxGrab").GetValue<Slider>().Value, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }

        private void OnInterruptableSpell(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (R.IsReady() && Config.Item("inter").GetValue<bool>() && unit.IsValidTarget(R.Range))
                R.Cast();
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && Config.Item("Gap").GetValue<bool>() && gapcloser.Sender.IsValidTarget(R.Range))
                R.Cast();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(1) && Q.IsReady())
                LogicQ();
            if (Program.LagFree(2) && R.IsReady())
                LogicR();
            if (Program.LagFree(3) && W.IsReady())
                LogicW();
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (E.IsReady() && args.Target.IsValid<Obj_AI_Hero>() && Config.Item("autoE").GetValue<bool>())
                E.Cast();   
        }

        private void LogicQ()
        {
            float maxGrab = Config.Item("maxGrab").GetValue<Slider>().Value;
            float minGrab =  Config.Item("minGrab").GetValue<Slider>().Value;

            if (Program.Combo && Config.Item("ts").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(maxGrab, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget(maxGrab) && Config.Item("grab" + t.ChampionName).GetValue<bool>() && Player.Distance(t.ServerPosition) > minGrab)
                    Program.CastSpell(Q, t);
            }
            foreach (var t in Program.Enemies.Where(t => t.IsValidTarget(maxGrab) && Config.Item("grab" + t.ChampionName).GetValue<bool>()))
            {
                if (!t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) && Player.Distance(t.ServerPosition) > minGrab)
                {
                    if (Program.Combo && !Config.Item("ts").GetValue<bool>())
                        Program.CastSpell(Q,t);

                    if (Config.Item("qCC").GetValue<bool>() )
                    {
                        if(!OktwCommon.CanMove(t))
                            Q.Cast(t, true);
                        Q.CastIfHitchanceEquals(t, HitChance.Dashing);
                        Q.CastIfHitchanceEquals(t, HitChance.Immobile);
                    }
                }
            }
        }

        private void LogicR()
        {
            bool rKs = Config.Item("rKs").GetValue<bool>();
            bool afterGrab = Config.Item("afterGrab").GetValue<bool>();
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && target.HasBuff("rocketgrab2")))
            {
                if (rKs && R.GetDamage(target) > target.Health)
                    R.Cast();
                if (afterGrab && target.IsValidTarget(400) && target.HasBuff("rocketgrab2"))
                    R.Cast();
            }
            if (Player.CountEnemiesInRange(R.Range) >= Config.Item("rCount").GetValue<Slider>().Value && Config.Item("rCount").GetValue<Slider>().Value > 0)
                R.Cast();
        }
        private void LogicW()
        {
            if (Config.Item("autoW").GetValue<bool>())
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && target.HasBuff("rocketgrab2")))
                    W.Cast();
            }
        }
    }
}
