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
    class Orianna
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        private Spell E, Q, R, W, QR;

        private float QMANA, WMANA, EMANA, RMANA;
        private float RCastTime = 0;
        private Vector3 BallPos;
        private int FarmId;
        private bool Rsmart = false;

        private Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 820);
            W = new Spell(SpellSlot.W, 210);
            E = new Spell(SpellSlot.E, 1095);
            R = new Spell(SpellSlot.R, 380);
            QR = new Spell(SpellSlot.Q, 825);

            Q.SetSkillshot(0f, 100f, 1000f, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 210f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 80f, 1700f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.6f, 375f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            QR.SetSkillshot(0.6f, 400f, 100f, false, SkillshotType.SkillshotCircle);

            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("autoW", "Auto E").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("hadrCC", "Auto E hard CC").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("poison", "Auto E poison").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Shield Config").AddItem(new MenuItem("Wdmg", "E dmg % hp").SetValue(new Slider(10, 100, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(60, 100, 20)));


            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("rCount", "Auto R x enemies").SetValue(new Slider(3, 0, 5)));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("smartR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press)));
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            //Interrupter.OnPossibleToInterrupt += OnInterruptableSpell;
            //Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        

        private void Drawing_OnDraw(EventArgs args)
        {
            if (BallPos.IsValid())
            {

                //Utility.DrawCircle(BallPos, Q.Range, System.Drawing.Color.Orange, 5, 1);

            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero best = Player;
            foreach (var ally in HeroManager.Allies.Where(ally => ally.IsValid))
            {
                if (ally.HasBuff("orianaghostself") || ally.HasBuff("orianaghost"))
                    BallPos = ally.ServerPosition;

                if (Program.LagFree(2) )
                {
                    if (E.IsReady() && Player.Mana > RMANA + EMANA && ally.Distance(Player.Position) < E.Range)
                    {
                        if (ally.Health < ally.CountEnemiesInRange(600) * ally.Level * 20)
                        {
                            E.CastOnUnit(ally);
                        }
                        else if (!Program.CanMove(ally) && Config.Item("hadrCC").GetValue<bool>())
                        {
                            E.CastOnUnit(ally);
                        }
                        else if (ally.HasBuffOfType(BuffType.Poison) && Config.Item("poison").GetValue<bool>())
                        {
                            E.CastOnUnit(ally);
                        }
                    }
                    if (W.IsReady() && Player.Mana > RMANA + WMANA && BallPos.Distance(ally.ServerPosition) < 240 && ally.Health < ally.CountEnemiesInRange(600) * ally.Level * 20)
                        W.Cast();
                }
                if (Program.LagFree(3) && ally.Health < best.Health && ally.Distance(Player.Position) < E.Range)
                    best = ally;
            }


            
            /*
            foreach (var ally in HeroManager.Allies.Where(ally => ally.IsValid && ally.Distance(Player.Position) < 1000))
            {
                foreach (var buff in ally.Buffs)
                {
                        Program.debug(buff.Name);
                }

            }
            */

            if ((Config.Item("smartR").GetValue<KeyBind>().Active || Rsmart) && R.IsReady())
            {
                Rsmart = true;
                var target = TargetSelector.GetTarget(Q.Range + 100, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {
                    if (Q.IsReady())
                    {
                        QR.Cast(target, true, true);
                    }
                    else if (CountEnemiesInRangeDeley(BallPos, R.Width, R.Delay) > 0)
                        R.Cast();
                }
                else
                    Rsmart = false;
            }
            else
                Rsmart = false;


            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (Program.LagFree(1) &&  Q.IsReady())
                LogicQ();
            if (Program.LagFree(2) && W.IsReady() )
                LogicW();
            if (Program.LagFree(3) && R.IsReady())
                LogicR();
            if (Program.LagFree(4) && BallPos.IsValid())
                LogicFarm();
            var ta = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
            
            if (Program.LagFree(4) && ta.IsValidTarget() && E.IsReady() && !W.IsReady() && CountEnemiesInRangeDeley(BallPos, 100, 0.1f) > 0 && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                E.CastOnUnit(best);
                Program.debug(best.ChampionName);
            }
        }


        private void LogicFarm()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            if (Program.Farm && Player.Mana > RMANA + QMANA + WMANA + EMANA)
            {
                foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(Q.Range) && !Orbwalker.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion) && minion.Health > minion.FlatPhysicalDamageMod))
                {
                    Q.Cast(minion);
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value)
            {

                var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (Q.IsReady())
                        Q.Cast(mob);
                    if (W.IsReady() && BallPos.Distance(mob.Position) < W.Width)
                        W.Cast();
                    else if (E.IsReady())
                        E.CastOnUnit(Player);
                }
                var Qfarm = Q.GetLineFarmLocation(allMinions, Q.Width);

                var QWfarm = Q.GetCircularFarmLocation(allMinions, W.Width);
                if (Qfarm.MinionsHit + QWfarm.MinionsHit == 0 )
                    return;
                
                if (Qfarm.MinionsHit / 3 > QWfarm.MinionsHit && BallPos.Distance(Player.Position) < 50 && Qfarm.MinionsHit > 2)
                {
                    if (Q.IsReady())
                        Q.Cast(Qfarm.Position);
                }
                else if (QWfarm.MinionsHit > 2 && Q.IsReady())
                     Q.Cast(QWfarm.Position);
                
                if (W.IsReady())
                {
                    foreach (var minion in allMinions.Where(minion => minion.Distance(BallPos) < W.Range && minion.Health < W.GetDamage(minion)))
                        W.Cast();
                }

                
            }
        }

       

        private void LogicR()
        {
            foreach (var t in HeroManager.Enemies.Where(t => t.IsValidTarget() && BallPos.Distance(Prediction.GetPrediction(t, R.Delay).CastPosition) < R.Width && t.Health <Q.GetDamage(t) + R.GetDamage(t)))
                R.Cast();

            if (CountEnemiesInRangeDeley(BallPos, R.Width, R.Delay) >= Config.Item("rCount").GetValue<Slider>().Value)
                R.Cast();
        }

        private void LogicW()
        {
            Wks();
            if (CountEnemiesInRangeDeley(BallPos, 220, 0f) > 0 && ObjectManager.Player.Mana > RMANA + WMANA)
            {
                W.Cast();
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (Q.GetDamage(t) + W.GetDamage(t) > t.Health)
                    CastQ(t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA - 10)
                    CastQ(t);
                else if (Program.Farm && Player.Mana > RMANA + QMANA + WMANA + EMANA)
                    CastQ(t);
            }
        }

        private void CastQ(Obj_AI_Hero target)
        {
            float distance = Vector3.Distance(BallPos, target.ServerPosition);

            float delay = (distance / Q.Speed + Q.Delay);

            var prepos = Prediction.GetPrediction(target, delay);
            

            if ((int)prepos.Hitchance > Config.Item("Hit").GetValue<Slider>().Value)
            {
                if (prepos.CastPosition.Distance(prepos.CastPosition) < Q.Range)
                {
                    Q.Cast(prepos.CastPosition);
                    
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
             if (args.Target == null 
                || !args.Target.IsValid 
                || !sender.IsEnemy 
                || !args.Target.IsAlly 
                || !Config.Item("autoW").GetValue<bool>() 
                || Player.Mana < EMANA + RMANA
                || args.Target.Position.Distance(Player.Position) > E.Range)
                return;

            foreach (var ally in HeroManager.Allies.Where(ally => ally.IsValid && ally.NetworkId == args.Target.NetworkId))
            {
                var dmg = sender.GetSpellDamage(ally, args.SData.Name);
                double HpLeft = ally.Health - dmg;
                if (E.IsReady())
                {
                    
                    
                    double HpPercentage = (dmg * 100) / ally.Health;
                    double shieldValue = 60 + E.Level * 40 + 0.4 * Player.FlatMagicDamageMod;
                    if (HpPercentage >= Config.Item("Wdmg").GetValue<Slider>().Value)
                        E.CastOnUnit(ally);
                    else if (dmg > shieldValue)
                        E.CastOnUnit(ally);
                }
                //Game.PrintChat("" + HpPercentage);
            }   
        }


        private void Wks()
        {
            foreach (var t in HeroManager.Enemies.Where(t => t.IsValidTarget() && BallPos.Distance(t.ServerPosition) < 250 && t.Health < W.GetDamage(t)))
                W.Cast();
        }

        private int CountEnemiesInRangeDeley(Vector3 position, float range, float delay)
        {
            int count = 0;
            foreach (var t in HeroManager.Enemies.Where(t => t.IsValidTarget()))
            {
                Vector3 prepos = Prediction.GetPrediction(t, delay).CastPosition;
                if (position.Distance(prepos) < range)
                    count++;
            }
            return count;
        }
        private void Obj_AI_Base_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid && obj.IsAlly && obj.Name == "TheDoomBall")
                    BallPos = obj.Position;
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }
    }
}
