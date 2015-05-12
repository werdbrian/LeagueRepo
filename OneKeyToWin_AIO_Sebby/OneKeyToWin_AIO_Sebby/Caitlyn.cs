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
    class Caitlyn
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        private Spell E, Q, Qc, R, W;

        private float QMANA, WMANA, EMANA, RMANA;
        private float RCastTime = 0, QCastTime = 0;

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1280f);
            Qc = new Spell(SpellSlot.Q, 1100f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 980f);
            R = new Spell(SpellSlot.R, 3000f);

            Q.SetSkillshot(0.65f, 90f, 2200f, false, SkillshotType.SkillshotLine);
            Qc.SetSkillshot(0.65f, 90f, 2200f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(1.5f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.7f, 200f, 1500f, false, SkillshotType.SkillshotCircle);

            LoadMenuOKTW();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            //Orbwalking.BeforeAttack += BeforeAttack;
            //Orbwalking.AfterAttack += afterAttack;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }


        private void LoadMenuOKTW()
        {
            Config.SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W on hard CC").SetValue(true));  
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("telE", "Auto E teleport").SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("AGC", "Anti Gapcloser E,W").SetValue(true));
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            if (unit.IsMe && (args.SData.Name == "CaitlynPiltoverPeacemaker" || args.SData.Name == "CaitlynEntrapment"))
            {
                QCastTime = Game.Time;
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + WMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (E.IsReady() && Target.IsValidTarget(E.Range) && ObjectManager.Player.Position.Extend(Game.CursorPos, 400).CountEnemiesInRange(500) < 3)
                    E.Cast(Target, true);
                else if (W.IsReady() && Target.IsValidTarget(W.Range))
                    W.Cast(ObjectManager.Player.ServerPosition, true);
                return;
            }
            return;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
                //debug("" + ObjectManager.Player.AttackRange);
            }

            if (Program.LagFree(1) && E.IsReady())
                LogicE();

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();
            
            if (Program.LagFree(3) && W.IsReady() && Program.attackNow)
                LogicW();

            if (Program.LagFree(4) && R.IsReady() && Config.Item("autoR").GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && Game.Time - QCastTime > 1)
                LogicR();
            
        }

        private void LogicR()
        {
            bool cast = false;
            R.Range = 500 * R.Level + 1500;
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R.Range) && Program.ValidUlt(target) && target.CountEnemiesInRange(500) == 1 && target.CountAlliesInRange(500) == 0))
            {
                float predictedHealth = target.Health + target.HPRegenRate * 2;
                var Rdmg = R.GetDamage(target);
                if (Rdmg > predictedHealth && GetRealDistance(target) > bonusRange() + 400 + target.BoundingRadius  && Orbwalker.GetTarget() == null)
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
                        if (length < (400 + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                            cast = false;
                    }
                    if (cast)
                        R.Cast(target, true);
                }
            }
        }

        private void LogicW()
        {
            if (Player.Mana > RMANA + WMANA)
            {
                if (Config.Item("autoW").GetValue<bool>())
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range) && !Program.CanMove(enemy)))
                        W.Cast(enemy, true);
                
                if (Config.Item("telE").GetValue<bool>())
                    foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < W.Range  && Obj.Team != Player.Team && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                        W.Cast(Object.Position, true);
            }
        }

        private void LogicQ()
        {
            throw new NotImplementedException();
        }

        private void LogicE()
        {
            throw new NotImplementedException();
        }
        private float GetRealRange(GameObject target)
        {
            return 680f + Player.BoundingRadius + target.BoundingRadius;
        }
        private float GetRealDistance(GameObject target)
        {
            return Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }
        public float bonusRange()
        {
            return 720f + Player.BoundingRadius;
        }
        private void SetMana()
        {
            if (Player.Health < Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("watermark").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0f, System.Drawing.Color.Cyan, "OneKeyToWin AIO - " + Player.ChampionName + " by Sebby");
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
