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
    class Ekko
    {

        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        private Spell E, Q, Q1,  R, W;
        private Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private float QMANA, WMANA, EMANA, RMANA;
        private static GameObject RMissile, WMissile;
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 700); 
            Q1 = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, 1400);
            E = new Spell(SpellSlot.E, 330f);
            R = new Spell(SpellSlot.R, 1200f);

            Q.SetSkillshot(0.25f, 50f, 2000f, false, SkillshotType.SkillshotLine);
            Q1.SetSkillshot(0.5f, 150f, 1000f, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(2.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 375f, float.MaxValue, false, SkillshotType.SkillshotCircle);



            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells").SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Haras Q").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Farm W").SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("Rdmg", "R dmg % hp").SetValue(new Slider(20, 100, 0)));

            //LoadMenuOKTW();

            Game.OnUpdate += Game_OnGameUpdate;

            //Drawing.OnDraw += Drawing_OnDraw;
            //Orbwalking.AfterAttack += afterAttack;
           // Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target == null || !sender.IsEnemy || !args.Target.IsMe || !Config.Item("autoR").GetValue<bool>() || R.IsReady() )
                return;
            var dmg = sender.GetSpellDamage(Player, args.SData.Name);
            double HpLeft = Player.Health - dmg;
            double HpPercentage = (dmg * 100) / Player.Health;
            if (Player.Health - dmg < dmg)
            {
                if (HpPercentage >= Config.Item("Rdmg").GetValue<Slider>().Value)
                    R.Cast();

                //Game.PrintChat("" + HpPercentage);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {

            if (Program.LagFree(1) && Q.IsReady() )
                LogicQ();
            if (Program.LagFree(2) && W.IsReady())
                LogicW();
            if (Program.LagFree(3) && E.IsReady() )
                LogicE();
            
            if (Program.LagFree(4) && R.IsReady() )
                LogicR();

        }

        private void LogicR()
        {
            if (Config.Item("autoR").GetValue<bool>())
            {
                foreach (var t in Program.Enemies.Where(t =>RMissile != null && RMissile.IsValid && t.IsValidTarget() && RMissile.Position.Distance(Prediction.GetPrediction(t, R.Delay).CastPosition) < 350 && RMissile.Position.Distance(t.ServerPosition) < 350))
                {

                    var comboDmg = GetRdmg(t) + GetWdmg(t);
                    if (Q.IsReady())
                        comboDmg += GetQdmg(t);
                    if (E.IsReady())
                        comboDmg += GetEdmg(t);
                    if (t.Health < comboDmg)
                        R.Cast();
                    Program.debug("ks");



                }

                if (Player.Health < Player.CountEnemiesInRange(600) * Player.Level * 15)
                {
                    R.Cast();
                }
            }
        }

        private void LogicE()
        {
            if (Program.Combo && WMissile != null && WMissile.IsValid)
            {
                if (WMissile.Position.CountEnemiesInRange(200) > 0 && WMissile.Position.Distance(Player.ServerPosition) < 100)
                {
                    E.Cast(Player.Position.Extend(WMissile.Position, E.Range), true);
                }
            }

            var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Magical);

            if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA
                 && ObjectManager.Player.CountEnemiesInRange(260) > 0
                 && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3
                 && t.Position.Distance(Game.CursorPos) > t.Position.Distance(ObjectManager.Player.Position))
            {
                E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
            else if (Program.Combo && ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4
                && ObjectManager.Player.Mana > RMANA + EMANA
                && !ObjectManager.Player.UnderTurret(true)
                && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3)
            {
                if (t.IsValidTarget() && Player.Mana > QMANA + EMANA + WMANA && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(Player.Position))
                {
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range), true);
                }
            }
            else if (t.IsValidTarget() && Program.Combo  && GetEdmg(t) + GetWdmg(t) > t.Health)
            {
                E.Cast(Player.Position.Extend(t.Position, E.Range), true);
            }
        }
        private void Obj_AI_Base_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid )
            {
                if (obj.Name == "Ekko")
                    RMissile = obj;
                if (obj.Name == "Ekko_Base_W_Cas.troy")
                    WMissile = obj;
            }     
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var t1 = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                Program.debug("" + GetQdmg(t));
                var qDmg = GetQdmg(t);
                if (qDmg > t.Health)
                    Q.Cast(t, true);
                else if (Program.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if (Program.Farm && Config.Item("haras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + WMANA + QMANA + QMANA)
                        Program.CastSpell(Q, t);
                if (Player.Mana > RMANA + QMANA + WMANA )
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !Program.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }

            }
            else if (t1.IsValidTarget())
            {
                Program.debug("" + Q.GetDamage(t1));
                var qDmg = GetQdmg(t1);
                if (qDmg > t1.Health)
                    Q1.Cast(t1, true);
                else if (Program.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q1, t1);
                else if (Program.Farm && Config.Item("haras" + t1.ChampionName).GetValue<bool>() && Player.Mana > RMANA + WMANA + QMANA + QMANA)
                    Program.CastSpell(Q1, t1);
                if (Player.Mana > RMANA + QMANA + WMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q1.Range) && !Program.CanMove(enemy)))
                        Q1.Cast(enemy, true);
                }
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value && Config.Item("farmQ").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + QMANA + WMANA)
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q1.Range, MinionTypes.All);
                var Qfarm = Q.GetLineFarmLocation(allMinionsQ, 100);
                if (Qfarm.MinionsHit > 5 && Q1.IsReady())
                    Q.Cast(Qfarm.Position);
            }
        }


        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var qDmg = GetQdmg(t);
                if (t.HasBuffOfType(BuffType.Slow) || t.CountEnemiesInRange(250) > 1)
                {
                    Program.CastSpell(E, t);

                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    Program.CastSpell(W, t);
                else if (Program.Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8  && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                    Program.CastSpell(W, t);
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Program.Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !Program.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
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

        private double GetQdmg( Obj_AI_Base t)
        {
            double dmg = 90 + (30 * Q.Level) + Player.FlatMagicDamageMod * 0.8;
            return Player.CalcDamage(t, Damage.DamageType.Magical, dmg);
        }
        private double GetEdmg(Obj_AI_Base t)
        {
            double dmg = 20 + (30 * E.Level) + (Player.FlatMagicDamageMod * 0.2);
            return Player.CalcDamage(t, Damage.DamageType.Magical, dmg);
        }
        private double GetWdmg(Obj_AI_Base t)
        {
            if (t.Health < t.MaxHealth * 0.3)
            {
                double hp = t.MaxHealth - t.Health;
                double dmg = ((Player.FlatMagicDamageMod / 45) + 5) * 0.01;
                double dmg2 = hp * dmg;
                return Player.CalcDamage(t, Damage.DamageType.Magical, dmg2);

            }
            else
                return 0;

        }
        private double GetRdmg(Obj_AI_Base t)
        {
            double dmg = 50 + (150 * R.Level) + Player.FlatMagicDamageMod * 1.3;
            return Player.CalcDamage(t, Damage.DamageType.Magical, dmg);
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
                        Utility.DrawCircle(ObjectManager.Player.Position, 925, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, 925, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange").GetValue<bool>())
            {
                if (RMissile != null && RMissile.IsValid)
                {

                    if (Config.Item("rRange").GetValue<bool>())
                    {
                        if (Config.Item("onlyRdy").GetValue<bool>())
                        {
                            if (R.IsReady())
                                Utility.DrawCircle(RMissile.Position, R.Width, System.Drawing.Color.Gray, 1, 1);
                        }
                        else
                            Utility.DrawCircle(RMissile.Position, R.Width, System.Drawing.Color.Gray, 1, 1);
                    }
                }
            }
        }
    }
}
