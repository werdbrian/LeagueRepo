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
    class Ezreal
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

        public int FarmId;
        public bool attackNow = true;
        public double lag = 0;
        public double WCastTime = 0;
        public double QCastTime = 0;
        public float DragonDmg = 0;
        public double DragonTime = 0;
        public bool Esmart = false;
        public double OverKill = 0;
        public double OverFarm = 0;
        public double diag = 0;
        public double diagF = 0;
        public int Muramana = 3042;
        public int Tear = 3070;
        public int Manamune = 3004;
        public Items.Item Potion = new Items.Item(2003, 0);
        public Items.Item ManaPotion = new Items.Item(2004, 0);
        public Items.Item Youmuu = new Items.Item(3142, 0);
        public string MsgDebug = "wait";
        public double NotTime = 0;
        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1200);

            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 3000f);


            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);


            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;

        }

        private void LoadMenuOKTW()
        {
            Config.SubMenu("E config").AddItem(new MenuItem("AGC", "AntiGapcloserE").SetValue(true));
            Config.SubMenu("E config").AddItem(new MenuItem("smartE", "SmartCast E key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E").SetValue(true));
            Config.SubMenu("E config").AddItem(new MenuItem("autoEwall", "Try E over wall BETA").SetValue(false));

            Config.SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rcc", "R cc").SetValue(true));
            Config.SubMenu("R Config").AddItem(new MenuItem("Raoe", "R aoe").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rred", "Red").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rblue", "Blue").SetValue(true));
            Config.SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rally", "Ally stealer").SetValue(false));
            Config.SubMenu("R Config").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
            Config.SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            Config.SubMenu("Farm").AddItem(new MenuItem("farmQ", "Farm Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("LC", "LaneClear").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana").SetValue(new Slider(60, 100, 20)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LCP", "LaneClear passiv stack & E,R CD").SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu("Haras").AddItem(new MenuItem("haras" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));
            Config.AddItem(new MenuItem("wPush", "W ally (push tower)").SetValue(true));
            Config.AddItem(new MenuItem("noob", "Noob KS bronze mode").SetValue(false));
            Config.AddItem(new MenuItem("Hit", "Hit Chance Skillshot").SetValue(new Slider(3, 4, 0)));
            Config.AddItem(new MenuItem("debug", "Debug").SetValue(false));
        }

        private void Game_OnUpdate(EventArgs args)
        {
            SetMana();
            if (E.IsReady())
            {
                if (Config.Item("smartE").GetValue<KeyBind>().Active)
                    Esmart = true;
                if (Esmart && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
            else
                Esmart = false;

            if (Program.LagFree(1) && E.IsReady() && Config.Item("autoE").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo )
                LogicE();

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && (Game.Time - QCastTime > 0.6))
                LogicW();

            if (Program.LagFree(4) && R.IsReady())
            {
                if (Config.Item("useR").GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
                LogicR();
            }
        }

        private void LogicQ()
        {

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (ObjectManager.Player.CountEnemiesInRange(900) == 0)
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            else
                t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                var wDmg = W.GetDamage(t);
                if (qDmg > t.Health)
                    Program.CastSpell(Q, t);
                if (qDmg * 3 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                    debug("Q noob mode");
                else if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                {
                    Program.CastSpell(Q, t);
                    OverKill = Game.Time;
                }
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if ((Farm && attackNow && ObjectManager.Player.Mana > RMANA + EMANA + QMANA + WMANA) && !ObjectManager.Player.UnderTurret(true))
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.BaseSkinName).GetValue<bool>()))
                    {
                        Program.CastSpell(Q, enemy);
                    }
                }

                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(Q.Range)))
                    {
                        if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                        {
                            Q.Cast(enemy, true);
                        }
                    }
                }
            }
            if (Farm && attackNow && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + QMANA * 3)
            {
                farmQ();
                lag = Game.Time;
            }
            else if (!Farm && Config.Item("stack").GetValue<bool>() && !ObjectManager.Player.HasBuff("Recall") && ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.95 && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo && !t.IsValidTarget() && (Items.HasItem(Tear) || Items.HasItem(Manamune)))
            {
                Q.Cast(ObjectManager.Player.ServerPosition);
            }
        }
        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                var wDmg = W.GetDamage(t);
                if (wDmg > t.Health)
                {
                    Program.CastSpell(W, t);
                    OverKill = Game.Time;
                }
                else if (wDmg + qDmg > t.Health && Q.IsReady())
                    Program.CastSpell(W, t);
                else if (qDmg * 2 > t.Health && Config.Item("noob").GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                    debug("W noob mode");
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    Program.CastSpell(W, t);
                else if (Farm && Config.Item("haras" + t.BaseSkinName).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true) && (ObjectManager.Player.Mana > ObjectManager.Player.MaxMana * 0.8 || W.Level > Q.Level) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                    Program.CastSpell(W, t);
                else if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                    {
                        if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                        {
                            W.Cast(enemy, true);
                        }
                    }
                }
            }
        }
        private void LogicE()
        {

            var t2 = TargetSelector.GetTarget(950, TargetSelector.DamageType.Physical);
            var t = TargetSelector.GetTarget(1400, TargetSelector.DamageType.Physical);

            if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA
                && ObjectManager.Player.CountEnemiesInRange(260) > 0
                && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 3
                && t.Position.Distance(Game.CursorPos) > t.Position.Distance(ObjectManager.Player.Position))
            {
                if (Config.Item("autoEwall").GetValue<bool>())
                    FindWall();
                E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
            }
            else if (ObjectManager.Player.Health > ObjectManager.Player.MaxHealth * 0.4
                && !ObjectManager.Player.UnderTurret(true)
                && (Game.Time - OverKill > 0.4)
                && ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(700) < 3)
            {
                if (t.IsValidTarget()
                 && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                 && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                 && Q.IsReady()
                 && Q.GetDamage(t) + E.GetDamage(t) > t.Health
                 && !Orbwalking.InAutoAttackRange(t)
                 && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                     )
                {
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                    debug("E kill Q");
                }
                else if (t2.IsValidTarget()
                 && t2.Position.Distance(Game.CursorPos) + 300 < t2.Position.Distance(ObjectManager.Player.Position)
                 && ObjectManager.Player.Mana > EMANA + RMANA
                 && ObjectManager.Player.GetAutoAttackDamage(t2) + E.GetDamage(t2) > t2.Health
                 && !Orbwalking.InAutoAttackRange(t2))
                {
                    var position = ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range);
                    if (W.IsReady())
                        W.Cast(position, true);
                    E.Cast(position, true);
                    debug("E kill aa");
                    OverKill = Game.Time;
                }
                else if (t.IsValidTarget()
                 && ObjectManager.Player.Mana > QMANA + EMANA + WMANA
                 && t.Position.Distance(Game.CursorPos) + 300 < t.Position.Distance(ObjectManager.Player.Position)
                 && W.IsReady()
                 && W.GetDamage(t) + E.GetDamage(t) > t.Health
                 && !Orbwalking.InAutoAttackRange(t)
                 && Q.WillHit(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), Q.GetPrediction(t).UnitPosition)
                     )
                {
                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos, E.Range), true);
                    debug("E kill W");
                }
            }
        }
        private void LogicR()
        {

            if (Config.Item("autoR").GetValue<bool>() && ObjectManager.Player.CountEnemiesInRange(800) == 0 && (Game.Time - OverKill > 0.6))
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(R.Range)))
                {
                    if (Program.ValidUlt(target))
                    {
                        float predictedHealth = target.Health + target.HPRegenRate * 2;
                        double Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                            Rdmg = getRdmg(target);
                        var qDmg = Q.GetDamage(target);
                        var wDmg = W.GetDamage(target);
                        if (Rdmg > predictedHealth && target.CountAlliesInRange(400) == 0)
                        {
                            castR(target);
                            debug("R normal");
                        }
                        else if (Rdmg > predictedHealth && target.HasBuff("Recall"))
                        {
                            R.Cast(target, true, true);
                            debug("R recall");
                        }
                        else if ((target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                         target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                         target.HasBuffOfType(BuffType.Taunt)) && Config.Item("Rcc").GetValue<bool>() &&
                            target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg * 4 > predictedHealth)
                        {
                            R.CastIfWillHit(target, 2, true);
                            R.Cast(target, true);
                        }
                        else if (target.IsValidTarget(R.Range) && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 3, true);
                        }
                        else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg + wDmg > predictedHealth && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("Raoe").GetValue<bool>())
                        {
                            R.CastIfWillHit(target, 2, true);
                        }
                    }
                }
            }
        }
        private void castR(Obj_AI_Hero target)
        {
            if (Config.Item("hitchanceR").GetValue<bool>())
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (target.Path.Count() < 2 && (ObjectManager.Player.Distance(waypoints.Last<Vector2>().To3D()) - ObjectManager.Player.Distance(target.Position)) > 400)
                {
                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                }
            }
            else
                R.Cast(target, true);
        }
        private double getRdmg(Obj_AI_Base target)
        {
            var rDmg = R.GetDamage(target);
            var dmg = 0;
            PredictionOutput output = R.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                PredictionOutput prediction = R.GetPrediction(enemy);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
            foreach (var minion in allMinionsR)
            {
                PredictionOutput prediction = R.GetPrediction(minion);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug").GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);
            if (dmg > 7)
                return rDmg * 0.7;
            else
                return rDmg - (rDmg * 0.1 * dmg);
        }

        private float GetPassiveTime()
        {
            return
                ObjectManager.Player.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "ezrealrisingspellforce")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }
        private void debug(string cos)
        {
        }

        private bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        private void FindWall()
        {
            var CircleLineSegmentN = 20;

            var outRadius = 700 / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
            var inRadius = 300 / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
            var bestPoint = ObjectManager.Player.Position;
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector2(ObjectManager.Player.Position.X + outRadius * (float)Math.Cos(angle), ObjectManager.Player.Position.Y + outRadius * (float)Math.Sin(angle)).To3D();
                var point2 = new Vector2(ObjectManager.Player.Position.X + inRadius * (float)Math.Cos(angle), ObjectManager.Player.Position.Y + inRadius * (float)Math.Sin(angle)).To3D();
                if (!point.IsWall() && point2.IsWall() && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestPoint))
                    bestPoint = point;
            }
            if (bestPoint != ObjectManager.Player.Position && bestPoint.Distance(Game.CursorPos) < bestPoint.Distance(ObjectManager.Player.Position) && bestPoint.CountEnemiesInRange(500) < 3)
                E.Cast(bestPoint);
        }

        public void farmQ()
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    Q.Cast(mob, true);
                }
            }

            if (!Config.Item("farmQ").GetValue<bool>())
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && !Orbwalker.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion)))
            {
                if (minion.IsMoving)
                    Q.Cast(minion);
                else
                    Q.Cast(minion.Position);
                FarmId = minion.NetworkId;
            }
            if (Config.Item("LC").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !Orbwalking.CanAttack() && (ObjectManager.Player.ManaPercentage() > Config.Item("Mana").GetValue<Slider>().Value || ObjectManager.Player.UnderTurret(false)))
            {
                foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && Orbwalker.InAutoAttackRange(minion)))
                {
                    if (minion.Health < Q.GetDamage(minion) * 0.8 && minion.Health > minion.FlatPhysicalDamageMod)
                    {
                        if (minion.IsMoving)
                            Q.Cast(minion);
                        else
                            Q.Cast(minion.Position);
                    }

                }
                if (Config.Item("LCP").GetValue<bool>() && ((!E.IsReady() || Game.Time - GetPassiveTime() > -1.5)) && !ObjectManager.Player.UnderTurret(false))
                {
                    foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && minion.Health > Q.GetDamage(minion) * 1.5 && Orbwalker.InAutoAttackRange(minion)))
                    {
                        if (minion.IsMoving)
                            Q.Cast(minion);
                        else
                            Q.Cast(minion.Position);
                    }
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

            if (Farm)
                RMANA = RMANA + ObjectManager.Player.CountEnemiesInRange(2500) * 20;

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
