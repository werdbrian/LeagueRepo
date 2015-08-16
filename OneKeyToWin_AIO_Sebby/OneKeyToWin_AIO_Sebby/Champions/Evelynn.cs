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
    class Evelynn
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W;
        private float QMANA, WMANA, EMANA, RMANA;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 500f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 250f);
            R = new Spell(SpellSlot.R, 650f);

            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("autoQ", "Auto Q").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W config").AddItem(new MenuItem("autoW", "Auto W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W config").AddItem(new MenuItem("slowW", "Auto W slow").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("rCount", "Auto R x enemies").SetValue(new Slider(3, 0, 5)));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "Clear Mana").SetValue(new Slider(20, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("laneQ", "Lane clear Q").SetValue(true));

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            if (Config.Item("useR").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    R.CastIfWillHit(t, 2, true);
                    R.Cast(t, true, true);
                }
            }
            if (Program.Combo)
            {
                if (Program.LagFree(1) && Q.IsReady() && Config.Item("autoQ").GetValue<bool>())
                    LogicQ();
                if (Program.LagFree(2) && E.IsReady() && Config.Item("autoE").GetValue<bool>())
                    LogicE();
                if (Program.LagFree(3) && W.IsReady())
                    LogicW();
                if (Program.LagFree(4) && R.IsReady())
                    LogicR();
            }
            else if (Program.LaneClear)
            {
                Jungle();
            }
        }

        private void LogicQ()
        {
            if (Player.CountEnemiesInRange(Q.Range) > 0)
                Q.Cast();
        }

        private void LogicE()
        {
           var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
           if (t.IsValidTarget())
           {
               E.CastOnUnit(t);
           }
        }

        private void LogicW()
        {
            if (Config.Item("autoW").GetValue<bool>() && Player.Mana > RMANA + EMANA + QMANA && Player.CountEnemiesInRange(W.Range) > 0)
                W.Cast();
            else if (Config.Item("slowW").GetValue<bool>() && Player.Mana > RMANA + EMANA + QMANA && Player.HasBuffOfType(BuffType.Slow))
                W.Cast();
        }

        private void LogicR()
        {
            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var poutput = R.GetPrediction(t,true);
               
                var aoeCount = poutput.AoeTargetsHitCount;
                if (aoeCount == 0)
                    aoeCount = 1;
                
                if (Config.Item("rCount").GetValue<Slider>().Value > 0 && Config.Item("rCount").GetValue<Slider>().Value <= aoeCount)
                    R.Cast(poutput.CastPosition);
                
                if (Player.Health < Player.MaxHealth * 0.3)
                    R.Cast(t);
            }
        }

        private void Jungle()
        {
            if (Player.ManaPercent < Config.Item("Mana").GetValue<Slider>().Value)
                return;
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (Config.Item("jungleE").GetValue<bool>() && E.IsReady())
                    E.CastOnUnit(mob);
                if (Config.Item("jungleQ").GetValue<bool>() && Q.IsReady())
                    Q.Cast();
            }

            if (Config.Item("laneQ").GetValue<bool>() && Q.IsReady())
                Q.Cast();
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
            if (Config.Item("wRange").GetValue<bool>())
            {
                if (Config.Item("onlyRdy").GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
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
    }

}
