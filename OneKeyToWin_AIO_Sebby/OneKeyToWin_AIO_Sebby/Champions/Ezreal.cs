using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Ezreal
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell Q, W, E, R;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        Vector3 CursorPosition = Vector3.Zero;

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

        public string MsgDebug = "wait";
        public double NotTime = 0;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 3000f);
            
            Q.SetSkillshot(0.25f, 50f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.2f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Items").AddItem(new MenuItem("mura", "Auto Muramana", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Items").AddItem(new MenuItem("stack", "Stack Tear if full mana", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("AGC", "AntiGapcloserE", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("smartE", "SmartCast E key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("smartEW", "SmartCast E + W key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoEwall", "Try E over wall BETA", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Rcc", "R cc", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Raoe", "R aoe", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rred", "Red", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rblue", "Blue", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rally", "Ally stealer", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Farm Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LC", "LaneClear", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(60, 100, 20)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCP", "LaneClear passiv stack & E,R CD", true).SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("harrasW", "Harras W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("wPush", "W ally (push tower)", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("noob", "Noob KS bronze mode", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("debug", "Debug", true).SetValue(false));

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC", true).GetValue<bool>() && E.IsReady() && Player.Mana > RMANA + EMANA && Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(400) < 3)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    if (Config.Item("autoEwall", true).GetValue<bool>())
                        FindWall();
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range), true);
                    Program.debug("E AGC");
                }
            }
            return;
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;
            if (FarmId != target.NetworkId)
                FarmId = target.NetworkId;
            if (W.IsReady() && Config.Item("wPush", true).GetValue<bool>() && target.IsValid<Obj_AI_Turret>() && Player.Mana > RMANA + EMANA + QMANA + WMANA + WMANA + RMANA)
            {
                foreach (var ally in Program.Allies)
                {
                    if (!ally.IsMe && ally.IsAlly && ally.Distance(Player.Position) < 600)
                        W.Cast(ally);
                }
            }
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;
            if (FarmId != args.Target.NetworkId)
                FarmId = args.Target.NetworkId;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (R.IsReady() && Config.Item("Rjungle", true).GetValue<bool>())
            {
                KsJungle();
            }
            else
                DragonTime = 0;

            if (E.IsReady())
            {
                if (Program.LagFree(1) && Config.Item("autoE", true).GetValue<bool>() && Program.Combo)
                    LogicE();

                if (Config.Item("smartE", true).GetValue<KeyBind>().Active)
                    Esmart = true;
                if (Config.Item("smartEW", true).GetValue<KeyBind>().Active && W.IsReady())
                {
                    CursorPosition = Game.CursorPos;
                    W.Cast(CursorPosition);
                }
                if (Esmart && Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range), true);
                
                if (!CursorPosition.IsZero)
                    E.Cast(Player.Position.Extend(CursorPosition, E.Range), true);
            }
            else
            {
                CursorPosition = Vector3.Zero;
                Esmart = false;
            }

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && (Game.Time - QCastTime > 0.6))
                LogicW();

            if ( R.IsReady())
            {
                if (Config.Item("useR", true).GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }

                if (Program.LagFree(4))
                    LogicR();
            }
        }

        private void LogicQ()
        {
            if (Config.Item("mura", true).GetValue<bool>())
            {
                int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                if (Program.Combo && Items.HasItem(Mur) && Items.CanUseItem(Mur) && Player.Mana > RMANA + EMANA + QMANA + WMANA)
                {
                    if (!Player.HasBuff("Muramana"))
                        Items.UseItem(Mur);
                }
                else if (Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                    Items.UseItem(Mur);
            }
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(900) > 0)
                t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var qDmg = Q.GetDamage(t);
                var wDmg = W.GetDamage(t);
                if (qDmg > t.Health)
                    Program.CastSpell(Q, t);
                if (qDmg * 3 > t.Health && Config.Item("noob", true).GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                    Program.debug("Q noob mode");
                else if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                {
                    Program.CastSpell(Q, t);
                    OverKill = Game.Time;
                }
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if ((Farm && attackNow && Player.Mana > RMANA + EMANA + QMANA + WMANA) && !Player.UnderTurret(true) && OktwCommon.CanHarras())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                    {
                        Program.CastSpell(Q, enemy);
                    }
                }

                else if ((Program.Combo || Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
            if (Farm && attackNow && Player.Mana > RMANA + EMANA + WMANA + QMANA * 3)
            {
                farmQ();
                lag = Game.Time;
            }
            else if (Config.Item("stack", true).GetValue<bool>() && !Player.HasBuff("Recall") && Player.Mana > Player.MaxMana * 0.95 && Program.None && (Items.HasItem(Tear) || Items.HasItem(Manamune)))
            {
                Q.Cast(Player.ServerPosition);
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
                else if (qDmg * 2 > t.Health && Config.Item("noob", true).GetValue<bool>() && t.CountAlliesInRange(800) > 1)
                    Program.debug("W noob mode");
                else if (Program.Combo && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                    Program.CastSpell(W, t);
                else if (Farm && Config.Item("harrasW", true).GetValue<bool>() && Config.Item("haras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && (Player.Mana > Player.MaxMana * 0.8 || W.Level >= Q.Level) && Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA && OktwCommon.CanHarras())
                    Program.CastSpell(W, t);
                else if ((Program.Combo || Farm) && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
        }
        private void LogicE()
        {
            var t = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
            var dashPosition = Player.Position.Extend(Game.CursorPos, E.Range);

            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(270) && target.IsMelee))
            {
                if (Config.Item("autoEwall", true).GetValue<bool>())
                    FindWall();
                E.Cast(dashPosition);
            }

            if (t.IsValidTarget() && Player.HealthPercent > 40 && !Player.UnderTurret(true) && (Game.Time - OverKill > 0.3) && dashPosition.CountEnemiesInRange(700) < 3)
            {
                if ( t.Distance(Game.CursorPos) + 300 < t.Position.Distance(Player.Position) && !Orbwalking.InAutoAttackRange(t))
                {
                    var dmgCombo = 0f;

                    if (t.IsValidTarget(950))
                    {
                        dmgCombo = (float)Player.GetAutoAttackDamage(t) + E.GetDamage(t);
                    }

                    if (Q.IsReady() && Player.Mana > QMANA + EMANA && Q.WillHit(dashPosition, Q.GetPrediction(t).UnitPosition))
                        dmgCombo = Q.GetDamage(t);

                    if (W.IsReady() && Player.Mana > QMANA + EMANA + WMANA )
                    {
                        dmgCombo += W.GetDamage(t);
                    }

                    if (dmgCombo > t.Health)
                    {
                        E.Cast(dashPosition);
                        OverKill = Game.Time;
                        Program.debug("E ks combo");
                    }
                }
            }
        }

        private void LogicR()
        {

            if (Config.Item("autoR", true).GetValue<bool>() && Player.CountEnemiesInRange(800) == 0 && (Game.Time - OverKill > 0.6))
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && OktwCommon.ValidUlt(target)))
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
                        Program.debug("R normal");
                    }
                    if (!OktwCommon.CanMove(target) && Config.Item("Rcc", true).GetValue<bool>() &&
                        target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg * 4 > predictedHealth)
                    {
                        R.CastIfWillHit(target, 2, true);
                        R.Cast(target, true);
                    }
                    else if ( Program.Combo && Config.Item("Raoe", true).GetValue<bool>())
                    {
                        R.CastIfWillHit(target, 3, true);
                    }
                    else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg + wDmg > predictedHealth && Program.Combo && Config.Item("Raoe", true).GetValue<bool>())
                    {
                        R.CastIfWillHit(target, 2, true);
                    }
                }
            }
        }

        private void castR(Obj_AI_Hero target)
        {
            if (Config.Item("hitchanceR", true).GetValue<bool>())
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (target.Path.Count() < 2 && (Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > 400)
                {
                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                }
            }
            else
                R.Cast(target, true);
        }

        private bool DashCheck(Vector3 dash)
        {
            if ((!dash.UnderTurret(true) || Program.Combo))
                return true;
            else
                return false;
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
            //if (Config.Item("debug", true).GetValue<bool>())
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
        public void debug(string msg)
        {
            MsgDebug = msg;
            NotTime = Game.Time;
            if (Config.Item("debug", true).GetValue<bool>())
                Console.WriteLine(msg);
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

            if (!Config.Item("farmQ", true).GetValue<bool>())
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && !Orbwalker.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion)))
            {
                Q.Cast(minion);
                FarmId = minion.NetworkId;
            }
            if (Config.Item("LC", true).GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !Orbwalking.CanAttack() && (Player.ManaPercentage() > Config.Item("Mana", true).GetValue<Slider>().Value || Player.UnderTurret(false)))
            {
                foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && Orbwalker.InAutoAttackRange(minion)))
                {
                    if (minion.Health < Q.GetDamage(minion) * 0.8 && minion.Health > minion.FlatPhysicalDamageMod)
                    {
                        Q.Cast(minion);
                    }

                }
                if (Config.Item("LCP", true).GetValue<bool>() && ((!E.IsReady() || Game.Time - GetPassiveTime() > -1.5)) && !Player.UnderTurret(false))
                {
                    foreach (var minion in minions.Where(minion => FarmId != minion.NetworkId && minion.Health > Q.GetDamage(minion) * 1.5 && Orbwalker.InAutoAttackRange(minion)))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }

        private void KsJungle()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, float.MaxValue, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var mob in mobs)
            {
                //debug(mob.SkinName);
                if (((mob.SkinName == "SRU_Dragon" && Config.Item("Rdragon", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Baron" && Config.Item("Rbaron", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Red" && Config.Item("Rred", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Blue" && Config.Item("Rblue", true).GetValue<bool>()))
                    && (mob.CountAlliesInRange(1000) == 0 || Config.Item("Rally", true).GetValue<bool>())
                    && mob.Health < mob.MaxHealth
                    && mob.Distance(Player.Position) > 1000
                    )
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 3)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }
                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 3);
                        //Program.debug("DS  " + DmgSec);
                        if (DragonDmg - mob.Health > 0)
                        {
                            debug(mob.SkinName + " " + (DmgSec / 3) + " dmg per sec");
                            var timeTravel = GetUltTravelTime(Player, R.Speed, R.Delay, mob.Position);
                            var timeR = (mob.Health - R.GetDamage(mob)) / (DmgSec / 3);
                            //Program.debug("timeTravel " + timeTravel + "timeR " + timeR + "d " + R.GetDamage(mob));
                            if (timeTravel > timeR)
                                R.Cast(mob.Position);
                            
                        }
                        else
                            DragonDmg = mob.Health;
                        
                        //Program.debug("" + GetUltTravelTime(ObjectManager.Player, R.Speed, R.Delay, mob.Position));
                    }
                }
            }
        }

        private float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;

            return (distance / missilespeed + delay);
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable" ,true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
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

        public static void drawText(string msg, Obj_AI_Hero Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("wRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
            if (Config.Item("eRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }


            if (Config.Item("noti", true).GetValue<bool>())
            {
                if (Game.Time - NotTime < 10)
                {
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.Red, MsgDebug);
                }
                else
                {
                    MsgDebug = "wait";
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.GreenYellow, MsgDebug);
                }

                var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {

                    var poutput = Q.GetPrediction(target);
                    if ((int)poutput.Hitchance == 5)
                        Render.Circle.DrawCircle(poutput.CastPosition, 50, System.Drawing.Color.YellowGreen);
                    if (Q.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) + E.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W + E kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                }
            }
        }
    }
}
