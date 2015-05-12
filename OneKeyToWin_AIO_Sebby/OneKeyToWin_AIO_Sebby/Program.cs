using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;

namespace OneKeyToWin_AIO_Sebby
{

    class RecallInfo
    {
        public int RecallID{ get; set; }
        public float RecallStart{ get; set; }
        public int RecallNum { get; set; }
    }
    class VisableInfo
    {
        public int VisableID { get; set; }
        public Vector3 LastPosition { get; set; }
        public float time { get; set; }
        public Vector3 PredictedPos { get; set; }
    }

    class champions
    {
        public Obj_AI_Hero Player;
    }
    internal class Program
    {
        public static string AnnieVer = "1.6.0.0";
        public static string JinxVer = "3.1.3.0";
        public static string EzrealVer = "2.7.3.0";
        public static string KogMawVer = "1.3.0.0";
        public static string SivirVer = "2.0.0.0";

        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static string championMsg;
        public static float JungleTime;
        public static Obj_AI_Hero jungler = ObjectManager.Player;
        public static int timer;
        public static Obj_SpawnPoint enemySpawn;
        public static int HitChanceNum= 4;
        public static int tickNum = 4;
        public static int tickIndex = 0;
        public static bool tickSkip = true, attackNow = true;

        public static List<RecallInfo> RecallInfos = new List<RecallInfo>();

        public static List<VisableInfo> VisableInfo = new List<VisableInfo>();


        public static Items.Item WardS = new Items.Item(2043, 600f);
        public static Items.Item WardN = new Items.Item(2044, 600f);
        public static Items.Item TrinketN = new Items.Item(3340, 600f);
        public static Items.Item SightStone = new Items.Item(2049, 600f);
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            Config = new Menu("OneKeyToWin AIO", "OneKeyToWin_AIO" + ObjectManager.Player.ChampionName, true);
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("watermark", "Watermark").SetValue(true));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("debug", "Debug").SetValue(false));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("0", "OneKeyToWin© by Sebby"));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("1", "visit joduska.me"));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("2", "Supported champions:"));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("3", "Annie " + AnnieVer));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("4", "Jinx " + JinxVer));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("5", "Ezreal " + EzrealVer));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("6", "KogMaw " + KogMawVer));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("7", "Sivir " + SivirVer));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("8", "Ashe " ));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("9", "Miss Fortune "));
            Config.SubMenu("About OKTW©").AddItem(new MenuItem("10", "Quinn "));

            Config.SubMenu("OneKeyToBrain©").AddItem(new MenuItem("aio", "Disable AIO champions (need F5)").SetValue(false));

            Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").AddItem(new MenuItem("timer", "GankTimer").SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").SubMenu("Custome jungler (select one)").AddItem(new MenuItem("ro" + enemy.ChampionName, enemy.ChampionName).SetValue(false));
            }
            Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").AddItem(new MenuItem("1", "RED - be careful"));
            Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").AddItem(new MenuItem("2", "ORANGE - you have time"));
            Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").AddItem(new MenuItem("3", "GREEN - jungler visable"));
            Config.SubMenu("OneKeyToBrain©").SubMenu("GankTimer").AddItem(new MenuItem("4", "CYAN jungler dead - take objectives"));

            Config.SubMenu("OneKeyToBrain©").AddItem(new MenuItem("championInfo", "Game Info").SetValue(true));

            Config.SubMenu("OneKeyToBrain©").SubMenu("Auto ward").AddItem(new MenuItem("AutoWard", "Auto Ward").SetValue(true));
            Config.SubMenu("OneKeyToBrain©").SubMenu("Auto ward").AddItem(new MenuItem("AutoWardCombo", "Only combo mode").SetValue(true));


            Q = new Spell(SpellSlot.Q);
            E = new Spell(SpellSlot.E);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);

            if (!Config.Item("aio").GetValue<bool>())
            {
                var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
                TargetSelector.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);

                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

                switch (Player.ChampionName)
                {
                    case "Jinx":
                        new Jinx().LoadOKTW();
                        break;
                    case "Sivir":
                        new Sivir().LoadOKTW();
                        break;
                    case "Ezreal":
                        new Ezreal().LoadOKTW();
                        break;
                    case "KogMaw":
                        new KogMaw().LoadOKTW();
                        break;
                    case "Annie":
                        new Annie().LoadOKTW();
                        break;
                    case "Ashe":
                        new Ashe().LoadOKTW();
                        break;
                    case "MissFortune":
                        new MissFortune().LoadOKTW();
                        break;
                    case "Quinn":
                        new Quinn().LoadOKTW();
                        break;
                    case "Kalista":
                        new Kalista().LoadOKTW();
                        break;
                    case "Caitlyn":
                        new Caitlyn().LoadOKTW();
                        break;
                }
                
                Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("OrbDraw", "Draw AAcirlce OKTW© style").SetValue(false));
                Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("orb", "Orbwalker target OKTW© style").SetValue(true));
                Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("1", "pls disable Orbwalking > Drawing > AAcirlce"));
                Config.SubMenu("Draw").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("2", "My HP: 0-30 red, 30-60 orange,60-100 green"));

                Config.SubMenu("Items").AddItem(new MenuItem("pots", "Use pots").SetValue(true));

                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("Hit", "Prediction OKTW©").SetValue(new Slider(4, 4, 0)));
                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("0", "0 - normal"));
                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("1", "1 - high"));
                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("2", "2 - high + max range fix"));
                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("3", "3 - normal + max range fix + waypionts analyzer"));
                Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("4", "4 - high + max range fix + waypionts analyzer"));

                Config.SubMenu("Performance OKTW©").AddItem(new MenuItem("pre", "OneSpellOneTick©").SetValue(true));
                Config.SubMenu("Performance OKTW©").AddItem(new MenuItem("0", "OneSpellOneTick© is tick management"));
                Config.SubMenu("Performance OKTW©").AddItem(new MenuItem("1", "ON - increase fps"));
                Config.SubMenu("Performance OKTW©").AddItem(new MenuItem("2", "OFF - normal mode"));
            }
            

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (IsJungler(enemy) && enemy.IsEnemy)
                {
                    jungler = enemy;
                }
            }
            Config.AddToMainMenu();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Drawing.OnDraw += OnDraw;
        }
        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            attackNow = true;

        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;

        }
        private static void OnUpdate(EventArgs args)
        {
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
            if (LagFree(0))
            {
                var target = Orbwalker.GetTarget();
                if (!(target is Obj_AI_Hero))
                    attackNow = true;
                HitChanceNum = Config.Item("Hit").GetValue<Slider>().Value;
                if (Config.Item("pots").GetValue<bool>())
                    PotionManagement();
                tickSkip = Config.Item("pre").GetValue<bool>();

                JunglerTimer();
                if(!Player.IsRecalling())
                    AutoWard();
            }
        }


        public static void AutoWard()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                if (enemy.IsVisible && enemy.IsValid && !enemy.IsDead && enemy != null)
                {
                    
                    if (Prediction.GetPrediction(enemy, 0.4f).CastPosition != null)
                    {
                        var prepos = Prediction.GetPrediction(enemy, 0.4f).CastPosition;
                        VisableInfo.RemoveAll(x => x.VisableID == enemy.NetworkId);
                        VisableInfo.Add(new VisableInfo() { VisableID = enemy.NetworkId, LastPosition = enemy.Position, time = Game.Time, PredictedPos = prepos });
                    }
                }
                else if (enemy.IsDead)
                {
                    VisableInfo.RemoveAll(x => x.VisableID == enemy.NetworkId);
                }
                else
                {
                    var need = VisableInfo.Find(x => x.VisableID == enemy.NetworkId);
                    if (need == null || need.PredictedPos == null)
                        return;

                    if (W.IsReady() && Player.ChampionName == "Quinn" && Game.Time - need.time > 0.5 && Game.Time - need.time < 4 && need.PredictedPos.Distance(Player.Position) < 1500 && Config.Item("autoW").GetValue<bool>())
                    {
                        W.Cast();
                    }
                    if (E.IsReady() && Game.Time - need.time > 0.5 && Game.Time - need.time < 4 && Player.ChampionName == "Ashe" && Config.Item("autoE").GetValue<bool>())
                    {
                        var eRange = 1750 + 750 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
                        if (need.PredictedPos.Distance(Player.Position) < eRange)
                        {
                            E.Cast(ObjectManager.Player.Position.Extend(need.PredictedPos, eRange));
                        }
                    }
                    if (E.IsReady() && Game.Time - need.time > 0.5 && Game.Time - need.time < 4 && Player.ChampionName == "MissFortune" && Combo && Player.Mana > 150f)
                    {
                        if (need.PredictedPos.Distance(Player.Position) < 800)
                        {
                            E.Cast(ObjectManager.Player.Position.Extend(need.PredictedPos, 800));
                        }
                    }
                    if (W.IsReady() && Game.Time - need.time > 3 && Game.Time - need.time < 4 && Player.ChampionName == "Kalista" && !Combo && Config.Item("autoW").GetValue<bool>() && ObjectManager.Player.Mana > 300f)
                    {
                        if (need.PredictedPos.Distance(Player.Position) > 1500 && need.PredictedPos.Distance(Player.Position) < 4000)
                        {
                            W.Cast(ObjectManager.Player.Position.Extend(need.PredictedPos, 5500));
                        }
                    }
                    if (W.IsReady() && Game.Time - need.time > 0.5 && Game.Time - need.time < 3 && Player.ChampionName == "Caitlyn" && Player.Mana > 150f)
                    {
                        if (need.PredictedPos.Distance(Player.Position) < 800)
                        {
                            E.Cast(ObjectManager.Player.Position.Extend(need.PredictedPos, 800));
                        }
                    }
                    if (Game.Time - need.time < 4 && need.PredictedPos.Distance(Player.Position) < 600 && Config.Item("AutoWard").GetValue<bool>() )
                    {
                        if (Config.Item("AutoWardCombo").GetValue<bool>() && Combo)
                            return;
                        if (NavMesh.IsWallOfGrass(need.PredictedPos, 0))
                        {
                            if (TrinketN.IsReady())
                            {
                                TrinketN.Cast(need.PredictedPos);
                                need.time = Game.Time - 5;
                            }
                            else if (SightStone.IsReady())
                            {
                                SightStone.Cast(need.PredictedPos);
                                    need.time = Game.Time - 5;
                            }
                            else if (WardS.IsReady())
                            {
                                WardS.Cast(need.PredictedPos);
                                    need.time = Game.Time - 5;
                            }
                            else if (WardN.IsReady())
                            {
                                WardN.Cast(need.PredictedPos);
                                need.time = Game.Time - 5;
                            }
                        }
                    }
                }
            }
        }

        public static void JunglerTimer()
        {
            if (Config.Item("timer").GetValue<bool>() && jungler != null && jungler.IsValid)
            {

                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsValid))
                {
                    if (Config.Item("ro" + enemy.ChampionName) != null && Config.Item("ro" + enemy.ChampionName).GetValue<bool>())
                        jungler = enemy;
                }
                

                if (jungler.IsDead)
                {
                    enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
                    timer = (int)(enemySpawn.Position.Distance(ObjectManager.Player.Position) / 370);
                }
                else if (jungler.IsVisible && jungler.IsValid)
                {
                    float Way = 0;
                    var JunglerPath = ObjectManager.Player.GetPath(ObjectManager.Player.Position, jungler.Position);
                    var PointStart = ObjectManager.Player.Position;
                    if (JunglerPath == null)
                        return;
                    foreach (var point in JunglerPath)
                    {
                        if (PointStart.Distance(point) > 0)
                        {
                            Way += PointStart.Distance(point);
                            PointStart = point;
                        }
                    }
                    timer = (int)(Way / jungler.MoveSpeed);
                }
            }
        }
        public static bool LagFree(int offset)
        {
            if (!tickSkip)
                return true;
            if (tickIndex == offset)
                return true;
            else
                return false;
        }

        public static bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        public static bool Combo
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo); }
        }

        private static bool IsJungler(Obj_AI_Hero hero)
        {
            return hero.Spellbook.Spells.Any(spell => spell.Name.ToLower().Contains("smite"));
        }

        private static void PotionManagement()
        {
            if (!ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(700) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                        Potion.Cast();
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < 200)
                        ManaPotion.Cast();
                }
            }
        }

        public static bool ValidUlt(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
            || target.HasBuffOfType(BuffType.SpellImmunity)
            || target.IsZombie
            || target.HasBuffOfType(BuffType.Invulnerability)
            || target.HasBuffOfType(BuffType.SpellShield)
            )
                return false;
            else
                return true;
        }

        public static bool CanMove(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || target.IsRecalling() || target.IsChannelingImportantSpell() ||
                !target.CanMove
            )
                return false;
            else
                return true;
        }


        public static float GetRealDmg(Spell QWER, Obj_AI_Hero target)
        {
            if (Orbwalking.InAutoAttackRange(target) || target.CountAlliesInRange(300) > 0)
                return QWER.GetDamage(target) + (float)ObjectManager.Player.GetAutoAttackDamage(target) * 2;
            else
                return QWER.GetDamage(target);
        }

        public static void CastSpell(Spell QWER, Obj_AI_Hero target)
        {
            
            //HitChance 0 - 2
            // example CastSpell(Q, ts, 2);
            var poutput = QWER.GetPrediction(target);
            var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead);
            if (target.IsDead || col > 0 || target.Path.Count() > 1)
                return;
            if ((target.Path.Count() == 0 && target.Position == target.ServerPosition) || target.HasBuff("Recall") || poutput.Hitchance == HitChance.Immobile)
            {
                QWER.Cast(poutput.CastPosition);
                return;
            }

            if (QWER.Delay < 0.30f && poutput.Hitchance == HitChance.Dashing)
            {
                QWER.Cast(poutput.CastPosition);
                return;
            }

            if (HitChanceNum == 0)
                QWER.Cast(target, true);
            else if (HitChanceNum == 1)
            {
                if ((int)poutput.Hitchance > 4)
                    QWER.Cast(poutput.CastPosition);
            }
            else if (HitChanceNum == 2)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (waypoints.Last<Vector2>().To3D().Distance(poutput.CastPosition) > QWER.Width  && (int)poutput.Hitchance == 5)
                {
                    if (waypoints.Last<Vector2>().To3D().Distance(Player.Position) <= target.Distance(Player.Position) || (target.Path.Count() == 0 && target.Position == target.ServerPosition))
                    {
                        if (Player.Distance(target.ServerPosition) < QWER.Range - (poutput.CastPosition.Distance(target.ServerPosition) + target.BoundingRadius))
                        {
                            QWER.Cast(poutput.CastPosition);
                        }
                    }
                    else if ((int)poutput.Hitchance == 5)
                    {
                        QWER.Cast(poutput.CastPosition);
                    }
                }
            }
            else if (HitChanceNum == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed) - QWER.Width) * 6;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                if (Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || Player.Distance(target.Position) < SiteToSite)
                    QWER.Cast(poutput.CastPosition);
                else if ((target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > BackToFront))
                {
                    if (waypoints.Last<Vector2>().To3D().Distance(Player.Position) <= target.Distance(Player.Position))
                    {
                        if (Player.Distance(target.ServerPosition) < QWER.Range - (poutput.CastPosition.Distance(target.ServerPosition)))
                        {
                            QWER.Cast(poutput.CastPosition);
                        }
                    }
                    else
                    {
                        QWER.Cast(poutput.CastPosition);
                    }
                }
            }
            else if (HitChanceNum == 4 && (int)poutput.Hitchance > 4)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                float SiteToSite = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed) - QWER.Width) * 6;
                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));

                if (Player.Distance(waypoints.Last<Vector2>().To3D()) < SiteToSite || Player.Distance(target.Position) < SiteToSite)
                    QWER.Cast(poutput.CastPosition);
                else if ((target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > BackToFront))
                {
                    if (waypoints.Last<Vector2>().To3D().Distance(Player.Position) <= target.Distance(Player.Position))
                    {
                        if (Player.Distance(target.ServerPosition) < QWER.Range - (poutput.CastPosition.Distance(target.ServerPosition)))
                        {
                            QWER.Cast(poutput.CastPosition);
                        }
                    }
                    else
                    {
                        QWER.Cast(poutput.CastPosition);
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            var unit = sender as Obj_AI_Hero;

            if (unit == null || !unit.IsValid || unit.IsAlly)
            {
                return;
            }
           
            var recall = Packet.S2C.Teleport.Decoded(unit, args);

            if (recall.Type == Packet.S2C.Teleport.Type.Recall)
            {
                switch (recall.Status)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        RecallInfos.RemoveAll(x => x.RecallID == sender.NetworkId);
                        RecallInfos.Add(new RecallInfo() { RecallID = sender.NetworkId, RecallStart = Game.Time , RecallNum = 0});

                        break;
                    case Packet.S2C.Teleport.Status.Abort:
                        RecallInfos.RemoveAll(x => x.RecallID == sender.NetworkId);
                        RecallInfos.Add(new RecallInfo() { RecallID = sender.NetworkId, RecallStart = Game.Time, RecallNum = 1 });

                        break;
                    case Packet.S2C.Teleport.Status.Finish:
                        RecallInfos.RemoveAll(x => x.RecallID == sender.NetworkId);
                        if (jungler.NetworkId == sender.NetworkId)
                        {
                            enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
                            timer = (int)(enemySpawn.Position.Distance(ObjectManager.Player.Position) / 370);
                        }
                        RecallInfos.Add(new RecallInfo() { RecallID = sender.NetworkId, RecallStart = Game.Time, RecallNum = 2 });
                        break;
                }
            }
        }

        public static void drawText(string msg, Obj_AI_Hero Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Console.WriteLine(msg);
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config.Item("timer").GetValue<bool>() && jungler != null)
            {
                if (jungler.IsDead)
                    drawText(" " + timer, ObjectManager.Player, System.Drawing.Color.Cyan);
                else if (jungler.IsVisible)
                    drawText(" " + timer, ObjectManager.Player, System.Drawing.Color.GreenYellow);
                else
                {
                    if (timer > 0)
                        drawText(" " + timer, ObjectManager.Player, System.Drawing.Color.Orange);
                    else
                        drawText(" " + timer, ObjectManager.Player, System.Drawing.Color.Red);
                    if (Game.Time - JungleTime >= 1)
                    {
                        timer = timer - 1;
                        JungleTime = Game.Time;
                    }
                }
            }
            if (Config.Item("championInfo").GetValue<bool>())
            {
                float positionDraw = 0.1f;

                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                {
                    var kolor = System.Drawing.Color.GreenYellow;
                    if (enemy.IsDead)
                        kolor = System.Drawing.Color.Gray;
                    else if (!enemy.IsVisible)
                        kolor = System.Drawing.Color.OrangeRed;

                    positionDraw += 0.015f;
                    
                    Drawing.DrawText(100, Drawing.Height * positionDraw, kolor, " " + enemy.ChampionsKilled + "/" + enemy.Deaths + "/" + enemy.Assists + " " + enemy.MinionsKilled);
                    //Drawing.DrawText(Drawing.Width * 0.11f, Drawing.Height * positionDraw, kolor, (int)enemy.HealthPercent );
                    foreach (RecallInfo rerecall in RecallInfos)
                    {
                        if (rerecall.RecallID == enemy.NetworkId && Game.Time - rerecall.RecallStart < 8)
                       {
                           var time = (Game.Time - rerecall.RecallStart) * 10;
                           if (rerecall.RecallNum == 2)
                           {
                               Drawing.DrawText(400, Drawing.Height * positionDraw, System.Drawing.Color.GreenYellow, "rerecall finish");
                           }
                           else if (rerecall.RecallNum == 1)
                           {
                               Drawing.DrawText(400, Drawing.Height * positionDraw, System.Drawing.Color.Yellow, "rerecall stop");
                           }
                           else if (rerecall.RecallNum == 0)
                           {
                               Drawing.DrawLine(400, Drawing.Height * positionDraw, 480 - time, Drawing.Height * positionDraw, 12, System.Drawing.Color.Red);
                               Drawing.DrawLine(480 - time, Drawing.Height * positionDraw, 480, Drawing.Height * positionDraw, 12, System.Drawing.Color.Black);
                           }
                       }
                    }
                    var kolorHP = System.Drawing.Color.GreenYellow;
                    if (enemy.IsDead)
                        kolorHP = System.Drawing.Color.GreenYellow;
                    else if ((int)enemy.HealthPercent < 30)
                        kolorHP = System.Drawing.Color.Red;
                    else if ((int)enemy.HealthPercent < 60)
                        kolorHP = System.Drawing.Color.Orange;
                    if ((int)enemy.HealthPercent > 0)
                        Drawing.DrawLine(200, Drawing.Height * positionDraw, (200 + ((int)enemy.HealthPercent) / 2)+1, Drawing.Height * positionDraw, 12, kolorHP);
                    if ((int)enemy.HealthPercent<100)
                        Drawing.DrawLine((200 + ((int)enemy.HealthPercent)/2), Drawing.Height * positionDraw, 200 + 50, Drawing.Height * positionDraw, 12, System.Drawing.Color.Black);
                    Drawing.DrawText(260, Drawing.Height * positionDraw, kolor, enemy.ChampionName + " " + enemy.Level + "lvl");
                }
            }

            if (Config.Item("OrbDraw").GetValue<bool>())
            {
                if (Player.HealthPercentage() > 60)
                    Utility.DrawCircle(ObjectManager.Player.Position, Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.GreenYellow, 2, 1);
                else if (Player.HealthPercentage() > 30)
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Orange, 3, 1);
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Red, 4, 1);
            }

            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();

                if (orbT.IsValidTarget())
                {
                    if (orbT.Health > orbT.MaxHealth * 0.6)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.GreenYellow, 5, 1);
                    else if (orbT.Health > orbT.MaxHealth * 0.3)
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Orange, 5, 1);
                    else
                        Utility.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Red, 5, 1);
                }
            }
        }
    }
}
