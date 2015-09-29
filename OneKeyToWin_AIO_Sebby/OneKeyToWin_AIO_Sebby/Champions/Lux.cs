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
    class Lux
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W, Qcol;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Vector3 Epos = Vector3.Zero;
        private float DragonDmg = 0;
        private double DragonTime = 0;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1175);
            Qcol = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 1075);
            E = new Spell(SpellSlot.E, 1075);
            R = new Spell(SpellSlot.R, 3000);

            Qcol.SetSkillshot(0.25f, 80f, 1200f, true, SkillshotType.SkillshotLine);
            Q.SetSkillshot(0.25f, 80f, 1200f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 110f, 1200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.3f, 250f, 1050f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(1.25f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRangeMini", "R range minimap", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("gapQ", "Auto Q Gap Closer", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harrasQ", "Harras Q", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Q Config").SubMenu("Use on:").AddItem(new MenuItem("Qon" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("harrasE", "Harras E", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoEcc", "Auto E only CC enemy", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoEslow", "Auto E slow logic detonate", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoEdet", "Only detonate if target in E ", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").AddItem(new MenuItem("Wdmg", "W dmg % hp", true).SetValue(new Slider(10, 100, 0)));
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team == Player.Team))
            {
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("skillshot" + ally.ChampionName, "skillshot", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("targeted" + ally.ChampionName, "targeted", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("HardCC" + ally.ChampionName, "Hard CC", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("Poison" + ally.ChampionName, "Poison", true).SetValue(true));
            }

            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Rcc", "R fast KS combo", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Raoe", "R aoe", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("hitchanceR", "Hit Chance R", true).SetValue(new Slider(2, 3, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space   

            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rred", "Red", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rblue", "Blue", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rally", "Ally stealer", true).SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 30)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle clear E", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Q.IsReady() && gapcloser.Sender.IsValidTarget(Q.Range) && Config.Item("gapQ", true).GetValue<bool>())
                Q.Cast(gapcloser.Sender);
        }


        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "LuxLightStrikeKugel")
            {
                Program.debug(args.SData.Name);
                Epos = args.End;
            }

            if (!W.IsReady() || !sender.IsEnemy || Player.Distance(sender.ServerPosition) > 2000)
                return;
            
            foreach (var ally in Program.Allies.Where(ally => ally.IsValid  && Player.Distance(ally.ServerPosition) < W.Range))
            {
                double dmg = 0;

                if (Config.Item("targeted" + ally.ChampionName, true).GetValue<bool>() && args.Target != null && args.Target.NetworkId == ally.NetworkId)
                {
                    
                    dmg = sender.GetSpellDamage(Player, args.SData.Name);
                }
                else if (Config.Item("skillshot" + ally.ChampionName, true).GetValue<bool>())
                {
                    var castArea = ally.Distance(args.End) * (args.End - ally.ServerPosition).Normalized() + ally.ServerPosition;
                    if (castArea.Distance(ally.ServerPosition) > ally.BoundingRadius / 2)
                        continue;
                    dmg = sender.GetSpellDamage(Player, args.SData.Name);
                }

                if (dmg > 0)
                {
                    double HpLeft = ally.Health - dmg;

                    double HpPercentage = (dmg * 100) / ally.Health;
                    double shieldValue = 65 + W.Level * 25 + 0.35 * Player.FlatMagicDamageMod;

                    if (HpPercentage >= Config.Item("Wdmg", true).GetValue<Slider>().Value)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                    else if (dmg > shieldValue)
                        W.Cast(W.GetPrediction(ally).CastPosition);
                }
            }   
        }

        private void Game_OnGameUpdate(EventArgs args)
        {

            if (R.IsReady() )
            {
                if (Config.Item("Rjungle", true).GetValue<bool>())
                {
                    KsJungle();
                }
                
                if (Config.Item("useR", true).GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }
            }
            else
                DragonTime = 0; 


            if (Program.LagFree(0))
            {
                SetMana();
                Jungle();
            }
            if (Program.LagFree(1) && Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();
            if (Program.LagFree(2) && E.IsReady() && Config.Item("autoE", true).GetValue<bool>())
                LogicE();
            if (Program.LagFree(3) && R.IsReady())
                LogicR();
            if (Program.LagFree(4) && W.IsReady())
                LogicW();

        }

        private void LogicW()
        {
            foreach (var ally in Program.Allies.Where(ally => ally.IsValid && !ally.IsDead && ally.Distance(Player.Position) < W.Range))
            {
                if (Config.Item("HardCC" + ally.ChampionName,true).GetValue<bool>() && HardCC(ally))
                {
                    W.CastOnUnit(ally);
                }
                else if (Config.Item("Poison" + ally.ChampionName, true).GetValue<bool>() && ally.HasBuffOfType(BuffType.Poison))
                {
                    W.CastOnUnit(ally);
                }
            }
        }

        private void LogicQ()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && E.GetDamage(enemy) + Q.GetDamage(enemy) + BonusDmg(enemy) > enemy.Health))
            {
                CastQ(enemy);
                return;
            }

            var t = Orbwalker.GetTarget() as Obj_AI_Hero;
            if (!t.IsValidTarget())
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget() && Config.Item("Qon" + t.ChampionName).GetValue<bool>())
            {
                if (Program.Combo && Player.Mana > RMANA + QMANA)
                    CastQ(t);
                if (Program.Farm && Config.Item("harrasQ", true).GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    CastQ(t);
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                    CastQ(enemy);
            }
        }
        
        private void CastQ(Obj_AI_Base t)
        {
            var poutput = Qcol.GetPrediction(t);
            var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead); 
     
            if ( col < 4)
                Program.CastSpell(Q, t);
        }

        private void LogicE()
        {
            if (Player.HasBuff("LuxLightStrikeKugel") && !Program.None)
            {
                int eBig = Epos.CountEnemiesInRange(330);
                if (Config.Item("autoEslow", true).GetValue<bool>())
                {
                    int detonate = eBig - Epos.CountEnemiesInRange(150);

                    if (detonate > 0 || eBig > 1)
                        E.Cast();
                }
                else if (Config.Item("autoEdet", true).GetValue<bool>())
                {
                    if (eBig > 0)
                        E.Cast();
                }
                else
                {
                    E.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() )
                {
                    if (!Config.Item("autoEcc", true).GetValue<bool>())
                    {
                        if (E.GetDamage(t) > t.Health)
                            Program.CastSpell(E, t);
                        else if (Program.Combo && Player.Mana > RMANA + EMANA)
                            Program.CastSpell(E, t);
                        else if (Config.Item("harrasE", true).GetValue<bool>() && Player.Mana > RMANA + EMANA + EMANA + RMANA && Program.Farm)
                            Program.CastSpell(E, t);
                    }

                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                        E.Cast(enemy, true);
                }
                else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmE", true).GetValue<bool>() && Player.Mana > RMANA + WMANA)
                {
                    var minionList = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All);
                    var farmPosition = E.GetCircularFarmLocation(minionList, E.Width);

                    if (farmPosition.MinionsHit > Config.Item("LCminions", true).GetValue<Slider>().Value)
                        E.Cast(farmPosition.Position);
                }
            }
        }

        private void LogicR()
        {
            if (Config.Item("autoR", true).GetValue<bool>() )
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && target.CountAlliesInRange(700) < 2 && OktwCommon.ValidUlt(target)))
                {
                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    float Rdmg = R.GetDamage(target);

                    if (target.HasBuff("luxilluminatingfraulein"))
                    {
                        Rdmg +=  (float)Player.CalcDamage(target, Damage.DamageType.Magical,10 + (8 * Player.Level) + 0.2 * Player.FlatMagicDamageMod);
                    }

                    if (Player.HasBuff("itemmagicshankcharge"))
                    {
                        if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                        {
                            Rdmg += (float)Player.CalcDamage(target, Damage.DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                        }
                    }

                    if (Rdmg > predictedHealth )
                    {
                        castR(target);
                        Program.debug("R normal");
                    }
                    else if (!OktwCommon.CanMove(target) && Config.Item("Rcc", true).GetValue<bool>() && target.IsValidTarget(E.Range))
                    {
                        float dmgCombo = Rdmg;

                        if (E.IsReady())
                        {
                            var eDmg = E.GetDamage(target);
                            
                            if (eDmg > predictedHealth)
                                return;
                            else
                                dmgCombo += eDmg;
                        }

                        if (target.IsValidTarget(800))
                            dmgCombo += BonusDmg(target);

                        if (dmgCombo > predictedHealth)
                        {
                            R.CastIfWillHit(target, 2);
                            R.Cast(target);
                        }

                    }
                    else if (Program.Combo && Config.Item("Raoe", true).GetValue<bool>())
                    {
                        R.CastIfWillHit(target, 3, true);
                    }
                }
            }
        }

        private float BonusDmg(Obj_AI_Hero target)
        {
            float damage = 10 + (Player.Level) * 8 + 0.2f * Player.FlatMagicDamageMod;
            if (Player.HasBuff("lichbane"))
            {
                damage += (Player.BaseAttackDamage * 0.75f) + ((Player.BaseAbilityDamage + Player.FlatMagicDamageMod) * 0.5f);
            }

            return (float)(Player.GetAutoAttackDamage(target) + Player.CalcDamage(target, Damage.DamageType.Magical, damage));
        }

        private void castR(Obj_AI_Hero target)
        {
            var inx = Config.Item("hitchanceR", true).GetValue<Slider>().Value;
            if (inx == 0)
            {
                R.Cast(R.GetPrediction(target).CastPosition);
            }
            else if (inx == 1)
            {
                R.Cast(target);
            }
            else if (inx == 2)
            {
                Program.CastSpell(R, target);
            }
            else if (inx == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if ((Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > 400)
                {
                    Program.CastSpell(R, target);
                }
            }
        }

        private void Jungle()
        {
            if (Program.LaneClear && Player.Mana > RMANA + WMANA + RMANA + WMANA)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && Config.Item("jungleQ", true).GetValue<bool>())
                    {
                        Q.Cast(mob.ServerPosition);
                        return;
                    }
                    if (E.IsReady() && Config.Item("jungleE", true).GetValue<bool>())
                    {
                        E.Cast(mob.ServerPosition);
                        return;
                    }
                }
            }
        }

        private void KsJungle()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, R.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
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
                            var timeTravel = R.Delay;
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


        private bool HardCC(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned)
            {
                return true;

            }
            else
                return false;
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable", true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
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

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnEndScene(EventArgs args)
        {

            if (Config.Item("rRangeMini", true).GetValue<bool>())
            {
                if (R.IsReady())
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);
            }
            else
                Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);


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
            if (R.IsReady() && Config.Item("noti", true).GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if ( t.IsValidTarget() && R.GetDamage(t) > t.Health)
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    drawLine(t.Position, Player.Position, 5, System.Drawing.Color.Red);
                }
            }
        }
    }
}
