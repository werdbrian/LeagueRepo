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

        public Spell E;
        public Spell Q;
        public Spell Qc;
        public Spell R;
        public Spell W;

        public float QMANA;
        public float WMANA;
        public float EMANA;
        public float RMANA;

        public bool attackNow = true;
        public double lag = 0;
        public double WCastTime = 0;
        public double QCastTime = 0;
        public float DragonDmg = 0;
        public double DragonTime = 0;

        public Obj_AI_Hero Player { get { return Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1240f);
            Qc = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, float.MaxValue);
            R = new Spell(SpellSlot.R, 25000f);

            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);
            Qc.SetSkillshot(0.25f, 90f, 1350f, true, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalker_AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        public  void Orbwalker_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            
            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target is Obj_AI_Hero && Player.Mana > RMANA + WMANA)
                    W.Cast();
                else if (target is Obj_AI_Hero && Player.Mana > RMANA + WMANA + QMANA)
                    W.Cast();
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.Mana > RMANA + WMANA + QMANA && (farmW() || t.IsValidTarget()))
                    W.Cast();
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target == null)
                return;
            var dmg = sender.GetSpellDamage(Player, args.SData.Name);
            double HpLeft = Player.Health - dmg;
            double HpPercentage = (dmg * 100) / Player.Health;
            if (sender.IsValid<Obj_AI_Hero>() && HpPercentage >= Config.Item("Edmg").GetValue<Slider>().Value && !sender.IsValid<Obj_AI_Turret>() && sender.IsEnemy && args.Target.IsMe && !args.SData.IsAutoAttack() && Config.Item("autoE").GetValue<bool>() && E.IsReady())
            {
                E.Cast();
                //Game.PrintChat("" + HpPercentage);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var Target = (Obj_AI_Hero)gapcloser.Sender;
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && Target.IsValidTarget(1000))
                E.Cast();
            return;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            SetMana();
            if (Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t) * 1.9;
                    if (Orbwalking.InAutoAttackRange(t))
                        qDmg = qDmg + Player.GetAutoAttackDamage(t) * 3;
                    if (qDmg > t.Health)
                        Q.Cast(t, true);
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.Mana > RMANA + QMANA)
                        Program.CastSpell(Q, t);
                    else if (Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>())
                        if (Player.Mana > RMANA + WMANA + QMANA + QMANA && t.Path.Count() > 1)
                            Program.CastSpell(Qc, t);
                        else if (Player.Mana > Player.MaxMana * 0.9)
                            Program.CastSpell(Q, t);
                        else if (Player.Mana > RMANA + WMANA + QMANA + QMANA)
                            Q.CastIfWillHit(t, 2, true);
                    if (Player.Mana > RMANA + QMANA + WMANA && Q.IsReady())
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                                Q.Cast(enemy, true);
                            else
                                Q.CastIfHitchanceEquals(enemy, HitChance.Immobile, true);
                        }
                    }
                }
                else if (Program.LagFree(1) && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmQ").GetValue<bool>() && Player.Mana > RMANA + QMANA + WMANA)
                {
                    var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All);
                    var Qfarm = Q.GetLineFarmLocation(allMinionsQ, 100);
                    if (Qfarm.MinionsHit > 5 && Q.IsReady())
                        Q.Cast(Qfarm.Position);
                }
            }

            if (Config.Item("forceW").GetValue<bool>() && W.IsReady())
            {
                var target = Orbwalker.GetTarget();
                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (W.IsReady())
                {
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && target is Obj_AI_Hero && Player.Mana > RMANA + WMANA)
                        Utility.DelayAction.Add(250, () => W.Cast());
                    else if (target is Obj_AI_Hero && Player.Mana > RMANA + WMANA + QMANA)
                        Utility.DelayAction.Add(250, () => W.Cast());
                    else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Player.Mana > RMANA + WMANA + QMANA && (farmW() || t.IsValidTarget()))
                        Utility.DelayAction.Add(250, () => W.Cast());
                }
            }

            if (R.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("autoR").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(800, TargetSelector.DamageType.Physical);
                if (Player.CountEnemiesInRange(800f) > 2)
                    R.Cast();
                else if (t.IsValidTarget() && Orbwalker.GetTarget() == null && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.GetAutoAttackDamage(t) * 2 > t.Health && !Q.IsReady() && t.CountEnemiesInRange(800) < 3)
                    R.Cast();
            }
        }

        private bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        public bool farmW()
        {
            if (!Program.LagFree(2))
                return false;
            var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, 1300, MinionTypes.All);
            int num = 0;
            foreach (var minion in allMinionsW)
            {
                num++;
            }
            if (num > 4 && Config.Item("farmW").GetValue<bool>())
                return true;
            else
                return false;
        }
        private void SetMana()
        {
            QMANA = 10;
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
                        Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
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
        private void LoadMenuOKTW()
        {
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Farm W").SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("forceW", "Force W (if dont work)").SetValue(false));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Haras Q").AddItem(new MenuItem("haras" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R").SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("Edmg", "E dmg % hp").SetValue(new Slider(0, 100, 0)));
        }
    }
}
