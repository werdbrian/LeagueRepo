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
    class MissFortune
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        public Spell E;
        public Spell Q;
        public Spell Q1;
        public Spell R;
        public Spell W;

        public float QMANA;
        public float WMANA;
        public float EMANA;
        public float RMANA;
        public bool attackNow = true;
        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 655f);
            Q1 = new Spell(SpellSlot.Q, 1100f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 1200f);

            Q1.SetSkillshot(0.25f, 200f, 1400f, true, SkillshotType.SkillshotLine);
            Q.SetTargetted(0.29f, 1400f);
            E.SetSkillshot(0.5f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 200f, 2000f, false, SkillshotType.SkillshotCircle);

            //LoadMenuOKTW();
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "OneKeyToCast R").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += afterAttack;
           // Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            //Drawing.OnDraw += Drawing_OnDraw;
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;

            if (Player.IsChannelingImportantSpell())
            {
                Orbwalking.Attack = false;
                Orbwalking.Move = false;
                Program.debug("cast R");
                return;
            }
            if (W.IsReady())
            {
                var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && args.Target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA)
                    W.Cast();
                else if (args.Target is Obj_AI_Hero && ObjectManager.Player.Mana > RMANA + WMANA + QMANA)
                    W.Cast();
                
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsChannelingImportantSpell())
            {
                Orbwalking.Attack = false;
                Orbwalking.Move = false;
                Program.debug("cast R");
                return;
            }
            else
            {
                Orbwalking.Attack = true;
                Orbwalking.Move = true;
                if (R.IsReady() && Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
            }


            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (Program.LagFree(1)  && Q.IsReady())
                LogicQ();

            if (Program.LagFree(2) && attackNow && E.IsReady())
                LogicE();

            if (Program.LagFree(4) && attackNow && R.IsReady())
            {
                LogicR();
            }
        }
        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var t1 = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
            if ( t.IsValidTarget(Q.Range))
            {
                
                if (Q.GetDamage(t) + ObjectManager.Player.GetAutoAttackDamage(t) * 3 > t.Health)
                   Q.Cast(t);
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
                    Q.Cast(t);
                else if (Program.Farm && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA)
                    Q.Cast(t);
            }
            if (t1.IsValidTarget(Q1.Range))
            {

                var poutput = Q1.GetPrediction(t1);
                var col = poutput.CollisionObjects;
                if (col.Count() == 0)
                    return;

                var minionQ = col.Last();
                if (minionQ.IsValidTarget(Q.Range) )
                {
                    Program.debug("" + minionQ.Distance(t1.Position));
                    if (minionQ.Distance(poutput.CastPosition) < 420 && minionQ.Distance(poutput.CastPosition) > 100)
                    {
                        if (Q.GetDamage(t1) + ObjectManager.Player.GetAutoAttackDamage(t1) > t1.Health)
                            Q.Cast(col.Last());
                        else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
                            Q.Cast(col.Last());
                        else if (Program.Farm && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA + QMANA)
                            Q.Cast(col.Last());
                    }
                }
            }
        }
        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (E.GetDamage(t) > t.Health && !Orbwalking.InAutoAttackRange(t))
                {
                    E.Cast(t, true, true);
                    return;
                }
                else if (E.GetDamage(t) + Q.GetDamage(t) > t.Health && ObjectManager.Player.Mana > QMANA + EMANA + RMANA)
                    Program.CastSpell(E, t);
                else if (Program.Combo && ObjectManager.Player.Mana > RMANA + QMANA + EMANA + WMANA)
                    Program.CastSpell(E, t);
                else if (Program.Combo && ObjectManager.Player.Mana > RMANA + WMANA + QMANA + 5
                    && !Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(E, t);
                else if (Program.Combo && ObjectManager.Player.Mana > RMANA + QMANA + WMANA
                   && ObjectManager.Player.CountEnemiesInRange(300) > 0)
                    Program.CastSpell(E, t);
                else if (Program.Combo  && ObjectManager.Player.Mana > RMANA + WMANA + EMANA
                    && ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.4)
                    Program.CastSpell(E, t);
                else if ((Program.Combo|| Program.Farm) && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                    {
                        if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            E.Cast(enemy, true, true);
                    }
                }
            }
        }
        private void LogicR()
        {

            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget(R.Range))
            {
                var rDmg = R.GetDamage(t) + (W.GetDamage(t) * 10);
 
                if (ObjectManager.Player.CountEnemiesInRange(700) == 0 && t.CountAlliesInRange(400) == 0 && Program.ValidUlt(t))
                {
                    var tDis = Player.Distance(t.ServerPosition);
                    if (rDmg * 6 > t.Health && tDis < 800)
                        R.Cast(t,true,true);
                    else if (rDmg * 5 > t.Health && tDis < 900)
                        R.Cast(t, true, true);
                    else if (rDmg * 4 > t.Health && tDis < 1000)
                        R.Cast(t, true, true);
                    else if (rDmg * 3 > t.Health && tDis < 1100)
                        R.Cast(t, true, true);
                    else if (rDmg * 2 > t.Health && tDis < 1200)
                        R.Cast(t, true, true);
                    else if (rDmg > t.Health && tDis < 1300)
                        R.Cast(t, true, true);
                    
                }
                else if (rDmg * 8 > t.Health && t.CountEnemiesInRange(300) > 2 && ObjectManager.Player.CountEnemiesInRange(700) == 0 )
                    R.Cast(t, true, true);
            }

        }
        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.Level * 2;
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

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("watermark").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0f, System.Drawing.Color.Cyan, "OneKeyToWin AIO - " + Player.ChampionName +  " by Sebby");
            }
        }

        

        public static void drawText(string msg, Obj_AI_Base Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }
    }
}
