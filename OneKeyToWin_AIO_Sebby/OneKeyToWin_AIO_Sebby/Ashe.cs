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
    class Ashe
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

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);
            E = new Spell(SpellSlot.E, 2500);
            R = new Spell(SpellSlot.R, 3000f);

            W.SetSkillshot(0.25f, 60f , 1700f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 299f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 130f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
        }

        private void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Config.Item("autoRinter").GetValue<bool>() && R.IsReady() && unit.IsValidTarget(R.Range))
                R.Cast(unit);
        }


        private void Drawing_OnDraw(EventArgs args)
        {
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
        }

        private void Game_OnUpdate(EventArgs args)
        {

            if (R.IsReady())
            {
                if (Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
            }

            if (Program.LagFree(1))
            {
                SetMana();
            }

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() )
                LogicW();

            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }

        private void LogicR()
        {
            if (Config.Item("autoR").GetValue<bool>())
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R.Range) && target.IsEnemy && Program.ValidUlt(target)))
                {
                    if (Config.Item("autoRinter").GetValue<bool>() && target.IsChannelingImportantSpell())
                        R.Cast(target);

                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    var Rdmg = R.GetDamage(target);
                    if (target.CountEnemiesInRange(250) > 2 && Config.Item("autoRaoe").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                        Program.CastSpell(R, target);
                    if (Rdmg > predictedHealth && target.CountAlliesInRange(600) == 0 && target.Distance(Player.Position) > 1200)
                    {
                        cast = true;
                        PredictionOutput output = R.GetPrediction(target);
                        Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                        direction.Normalize();
                        List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                        foreach (var enemy in enemies)
                        {
                            if (enemy.SkinName == target.SkinName || !cast)
                                continue;
                            PredictionOutput prediction = R.GetPrediction(enemy);
                            Vector3 predictedPosition = prediction.CastPosition;
                            Vector3 v = output.CastPosition - Player.ServerPosition;
                            Vector3 w = predictedPosition - Player.ServerPosition;
                            double c1 = Vector3.Dot(w, v);
                            double c2 = Vector3.Dot(v, v);
                            double b = c1 / c2;
                            Vector3 pb = Player.ServerPosition + ((float)b * v);
                            float length = Vector3.Distance(predictedPosition, pb);
                            if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                cast = false;
                        }
                        if (cast)
                            Program.CastSpell(R, target);
                    }
                }
            }
        }

        private void LogicQ()
        {
            if (Orbwalker.GetTarget() == null)
                return;
                 var target = Orbwalker.GetTarget();

                if (target == null)
                    Program.debug("ss");
                if (target.IsValid && !FrostShot && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target is Obj_AI_Hero && ObjectManager.Player.Mana > WMANA + QMANA && Config.Item("autoQ").GetValue<bool>())
                    Q.Cast();
                else if (target.IsValid && !FrostShot && Program.Farm && target is Obj_AI_Hero && ObjectManager.Player.Mana > WMANA + QMANA + EMANA + RMANA && Config.Item("autoQharas").GetValue<bool>())
                    Q.Cast();
                else if (FrostShot)
                    Q.Cast();
        }

        private void LogicW()
        {

            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (ObjectManager.Player.CountEnemiesInRange(700) > 0)
                t = TargetSelector.GetTarget(700, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var wDmg = W.GetDamage(t);
                if (wDmg > t.Health)
                {
                    Program.CastSpell(W, t);
                }
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA)
                    Program.CastSpell(W, t);
                else if (Program.Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + WMANA + QMANA + WMANA)
                    Program.CastSpell(W, t);
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Program.Farm) && ObjectManager.Player.Mana > RMANA + WMANA)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                    {
                        if (!Program.CanMove(enemy))
                        {
                            W.Cast(enemy, true);
                        }
                    }
                }
            }
        }

        public bool FrostShot
        {
            get { return ObjectManager.Player.HasBuff("FrostShot"); }
        }

        private void SetMana()
        {
            QMANA = 7;
            WMANA = W.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost; ;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
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
                Config.SubMenu(Player.ChampionName).SubMenu("Haras W").AddItem(new MenuItem("haras" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQharas", "Auto Q haras").SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoE", "Auto E").SetValue(true));

            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRaoe", "Auto R aoe").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRinter", "Auto R OnPossibleToInterrupt").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
        }
    }
}
