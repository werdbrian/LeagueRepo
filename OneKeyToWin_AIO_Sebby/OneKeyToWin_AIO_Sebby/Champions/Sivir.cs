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
    class Sivir
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell E, Q, Qc, W, R;
        public float QMANA, WMANA, EMANA, RMANA;

        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            
            Q = new Spell(SpellSlot.Q, 1180f);
            Qc = new Spell(SpellSlot.Q, 1180f);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, float.MaxValue);
            R = new Spell(SpellSlot.R, 25000f);

            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);
            Qc.SetSkillshot(0.25f, 90f, 1350f, true, SkillshotType.SkillshotLine);

            Config.SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Farm W").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(80, 100, 30)));
            
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("harasW", "Harras W").SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("forceW", "Force W (if dont work)").SetValue(false));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras Q").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("Edmg", "E dmg % hp").SetValue(new Slider(0, 100, 0)));

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalker_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        public void Orbwalker_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            
            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (Program.Combo && target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA)
                    W.Cast();
                else if (Config.Item("harasW").GetValue<bool>() && (target is Obj_AI_Hero || t.IsValidTarget()) && Player.Mana > RMANA + WMANA + QMANA)
                    W.Cast();
                else if (Config.Item("farmW").GetValue<bool>() && Program.LaneClear && Player.Mana > RMANA + WMANA + QMANA && (farmW() || t.IsValidTarget()))
                    W.Cast();
                
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!E.IsReady() || !sender.IsEnemy)
                return;

            if (args.SData.Name == "KalistaExpungeWrapper" && Player.HasBuff("kalistaexpungemarker"))
            {
                E.Cast();
            }
            
            if (!E.IsReady() || args.Target == null || !sender.IsEnemy || !args.Target.IsMe || !sender.IsValid<Obj_AI_Hero>() || args.SData.Name == "TormentedSoil")
                return;
            var dmg = sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            double HpLeft = ObjectManager.Player.Health - dmg;
            double HpPercentage = (dmg * 100) / Player.Health;
            if ( HpPercentage >= Config.Item("Edmg").GetValue<Slider>().Value && sender.IsEnemy && args.Target.IsMe && !args.SData.IsAutoAttack() && Config.Item("autoE").GetValue<bool>() )
            {
                E.Cast();
            }
            
        }
        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var Target = (Obj_AI_Hero)gapcloser.Sender;
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && Target.IsValidTarget(5000))
                E.Cast();
            return;
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            SetMana();
            if (Program.LagFree(1) && Q.IsReady() && !Player.IsWindingUp)
            {
                LogicQ();
            }
            if (Program.LagFree(3) && Config.Item("forceW").GetValue<bool>() && W.IsReady())
            {
                var target = Orbwalker.GetTarget();
                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (W.IsReady())
                {
                    if (Program.Combo && target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA)
                        Utility.DelayAction.Add(250, () => W.Cast());
                    else if (target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA + QMANA)
                        Utility.DelayAction.Add(250, () => W.Cast());
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Config.Item("farmW").GetValue<bool>() && Player.Mana > RMANA + WMANA + QMANA && (farmW() || t.IsValidTarget()))
                        Utility.DelayAction.Add(250, () => W.Cast());
                }
            }
            if (Program.LagFree(2) && R.IsReady() && Program.Combo && Config.Item("autoR").GetValue<bool>())
            {
                LogicR();
            }
        }
        private bool farmW()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 1300, MinionTypes.All);
            int num = 0;
            foreach (var minion in allMinions)
            {
                num++;
            }
            if (num > 4 )
                return true;
            else
                return false;
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                OktwCommon.WayPointAnalysis(t, Q);
                var qDmg = Q.GetDamage(t) * 1.9;
                if (Orbwalking.InAutoAttackRange(t))
                    qDmg = qDmg + Player.GetAutoAttackDamage(t) * 3;
                if (qDmg > t.Health)
                    Q.Cast(t, true);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if (Program.Farm && Config.Item("haras" + t.ChampionName).GetValue<bool>())
                {
                     if (Player.Mana > Player.MaxMana * 0.9)
                        Program.CastSpell(Q, t);
                     else if (ObjectManager.Player.Mana > RMANA + WMANA + QMANA + QMANA)
                        Program.CastSpell(Qc, t);
                     else if (Player.Mana > RMANA + WMANA + QMANA + QMANA)
                        Q.CastIfWillHit(t, 2, true);
                }
                if (Player.Mana > RMANA + QMANA + WMANA && Q.IsReady())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
            else if (Program.LaneClear && ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmQ").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
                var Qfarm = Q.GetLineFarmLocation(allMinionsQ, 100);
                if (Qfarm.MinionsHit > 5 && Q.IsReady())
                    Q.Cast(Qfarm.Position);
            }
        }
        private void LogicR()
        {
            var t = TargetSelector.GetTarget(800, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(800f) > 2)
                R.Cast();
            else if (t.IsValidTarget() && Orbwalker.GetTarget() == null && Program.Combo && Player.GetAutoAttackDamage(t) * 2 > t.Health && !Q.IsReady() && t.CountEnemiesInRange(800) < 3)
                R.Cast();
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
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

            if (Config.Item("noti").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {
                    if (Q.GetDamage(target) * 2 > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }

                }
            }
        }
    }
}
