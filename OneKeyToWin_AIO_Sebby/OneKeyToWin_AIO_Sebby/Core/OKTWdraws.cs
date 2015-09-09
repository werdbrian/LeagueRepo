using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace OneKeyToWin_AIO_Sebby.Core
{
    class OKTWdraws
    {
        private Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Menu Config = Program.Config;
        private Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell Q, W, E, R, DrawSpell;

        private static Font Tahoma13, Tahoma13B;

        public void LoadOKTW()
        {
            Config.SubMenu("Utility, Draws OKTW©").AddItem(new MenuItem("disableDraws", "Disable Utility draws").SetValue(false));
            //Config.SubMenu("Utility, Draws OKTW©").AddItem(new MenuItem("disableChampion", "Disable AIO champion (utility mode only) need F5").SetValue(false));
            Config.SubMenu("Utility, Draws OKTW©").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("OrbDraw", "Draw AAcirlce OKTW© style").SetValue(false));
            Config.SubMenu("Utility, Draws OKTW©").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("orb", "Orbwalker target OKTW© style").SetValue(true));
            Config.SubMenu("Utility, Draws OKTW©").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("1", "pls disable Orbwalking > Drawing > AAcirlce"));
            Config.SubMenu("Utility, Draws OKTW©").SubMenu("Draw AAcirlce OKTW© style").AddItem(new MenuItem("2", "My HP: 0-30 red, 30-60 orange,60-100 green"));

            Tahoma13B = new Font( Drawing.Direct3DDevice, new FontDescription
               { FaceName = "Tahoma", Height = 14, Weight = FontWeight.Bold, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            Tahoma13 = new Font( Drawing.Direct3DDevice, new FontDescription
                { FaceName = "Tahoma", Height = 14, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });


            Q = new Spell(SpellSlot.Q);
            E = new Spell(SpellSlot.E);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private void Game_OnUpdate(EventArgs args)
        {

        }

        public void drawText(string msg, Vector3 Hero, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] + weight, color, msg);
        }

        private void DrawFontTextScreen(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }

        private void DrawFontTextMap(Font vFont, string vText, Vector3 Pos, ColorBGRA vColor)
        {
            var wts = Drawing.WorldToScreen(Pos);
            vFont.DrawText(null, vText, (int)wts[0] , (int)wts[1], vColor);
        }

        public void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void OnDraw(EventArgs args)
        {
            if (Config.Item("disableDraws").GetValue<bool>())
                return;

            var HpBar = Config.Item("HpBar").GetValue<bool>();

            var championInfo = Config.Item("championInfo").GetValue<bool>();
            var GankAlert = Config.Item("GankAlert").GetValue<bool>();
            var ShowKDA = Config.Item("ShowKDA").GetValue<bool>();
            var ShowClicks = Config.Item("ShowClicks").GetValue<bool>();
            float posY = ((float)Config.Item("posY").GetValue<Slider>().Value * 0.01f) * Drawing.Height;
            float posX = ((float)Config.Item("posX").GetValue<Slider>().Value * 0.01f) * Drawing.Width;
            float positionDraw = 0;
            float positionGang = 500;

            int Width = 103;
            int Height = 8;
            int XOffset = 10;
            int YOffset = 20;

            var FillColor = System.Drawing.Color.GreenYellow;
            var Color = System.Drawing.Color.Azure;

            foreach (var enemy in Program.Enemies)
            {
                if (enemy.IsValidTarget())
                {
                    if (ShowClicks)
                    {
                        var lastWaypoint = enemy.GetWaypoints().Last().To3D();
                        if (lastWaypoint.IsValid())
                        {
                            drawLine(enemy.Position, lastWaypoint, 1, System.Drawing.Color.Red);
                            if (enemy.GetWaypoints().Count() > 1)
                                DrawFontTextMap(Tahoma13, enemy.ChampionName, lastWaypoint, SharpDX.Color.WhiteSmoke);
                        }
                    }
                }

                if (HpBar && enemy.IsHPBarRendered && Render.OnScreen(Drawing.WorldToScreen(enemy.Position)))
                {

                    var barPos = enemy.HPBarPosition;

                    float QdmgDraw = 0, WdmgDraw = 0, EdmgDraw = 0, RdmgDraw = 0, damage = 0; ;

                    if (Q.IsReady())
                        damage = damage + Q.GetDamage(enemy);

                    if (W.IsReady() && Player.ChampionName != "Kalista")
                        damage = damage + W.GetDamage(enemy);

                    if (E.IsReady())
                        damage = damage + E.GetDamage(enemy);

                    if (R.IsReady())
                        damage = damage + R.GetDamage(enemy);

                    if (Q.IsReady())
                        QdmgDraw = (Q.GetDamage(enemy) / damage);

                    if (W.IsReady() && Player.ChampionName != "Kalista")
                        WdmgDraw = (W.GetDamage(enemy) / damage);

                    if (E.IsReady())
                        EdmgDraw = (E.GetDamage(enemy) / damage);

                    if (R.IsReady())
                        RdmgDraw = (R.GetDamage(enemy) / damage);

                    var percentHealthAfterDamage = Math.Max(0, enemy.Health - damage) / enemy.MaxHealth;

                    var yPos = barPos.Y + YOffset;
                    var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                    var xPosCurrentHp = barPos.X + XOffset + Width * enemy.Health / enemy.MaxHealth;

                    float differenceInHP = xPosCurrentHp - xPosDamage;
                    var pos1 = barPos.X + XOffset + (107 * percentHealthAfterDamage);

                    for (int i = 0; i < differenceInHP; i++)
                    {
                        if (Q.IsReady() && i < QdmgDraw * differenceInHP)
                            Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, System.Drawing.Color.Cyan);
                        else if (W.IsReady() && i < (QdmgDraw + WdmgDraw) * differenceInHP)
                            Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, System.Drawing.Color.Orange);
                        else if (E.IsReady() && i < (QdmgDraw + WdmgDraw + EdmgDraw) * differenceInHP)
                            Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, System.Drawing.Color.Yellow);
                        else if (R.IsReady() && i < (QdmgDraw + WdmgDraw + EdmgDraw + RdmgDraw) * differenceInHP)
                            Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, System.Drawing.Color.YellowGreen);
                    }
                }

                var kolor = System.Drawing.Color.GreenYellow;

                if (enemy.IsDead)
                    kolor = System.Drawing.Color.Gray;
                else if (!enemy.IsVisible)
                    kolor = System.Drawing.Color.OrangeRed;

                var kolorHP = System.Drawing.Color.GreenYellow;

                if (enemy.IsDead)
                    kolorHP = System.Drawing.Color.GreenYellow;
                else if ((int)enemy.HealthPercent < 30)
                    kolorHP = System.Drawing.Color.Red;
                else if ((int)enemy.HealthPercent < 60)
                    kolorHP = System.Drawing.Color.Orange;

                if (championInfo)
                {
                    positionDraw += 15;
                    /*
                    if (ShowKDA)
                        Drawing.DrawText(posX - 30, posY + positionDraw, kolor, " " + enemy.ChampionsKilled + "/" + enemy.Deaths + "/" + enemy.Assists + " " + enemy.MinionsKilled);
                    */

                    Drawing.DrawText(posX + 60, posY + positionDraw, kolor, enemy.ChampionName);
                    Drawing.DrawText(posX - 70, posY + positionDraw, kolor, enemy.Level + " lvl");
                }

                var Distance = Player.Distance(enemy.Position);
                if (GankAlert && !enemy.IsDead && Distance > 1200)
                {

                    var wts = Drawing.WorldToScreen(ObjectManager.Player.Position.Extend(enemy.Position, positionGang));

                    wts[0] = wts[0];
                    wts[1] = wts[1] + 15;

                    if ((int)enemy.HealthPercent > 0)
                        Drawing.DrawLine(wts[0], wts[1], (wts[0] + ((int)enemy.HealthPercent) / 2) + 1, wts[1], 8, kolorHP);

                    if ((int)enemy.HealthPercent < 100)
                        Drawing.DrawLine((wts[0] + ((int)enemy.HealthPercent) / 2), wts[1], wts[0] + 50, wts[1], 8, System.Drawing.Color.White);

                    if (Distance > 3500 && enemy.IsVisible)
                    {
                        DrawFontTextMap(Tahoma13, enemy.ChampionName,Player.Position.Extend(enemy.Position, positionGang) , SharpDX.Color.White);
                        //drawText(enemy.ChampionName, Player.Position.Extend(enemy.Position, positionGang), System.Drawing.Color.GreenYellow);
                    }
                    else if (!enemy.IsVisible)
                    {
                        var ChampionInfoOne = Core.OKTWtracker.ChampionInfoList.Find(x => x.NetworkId == enemy.NetworkId);
                        if (ChampionInfoOne != null )
                        {
                            if (Game.Time - ChampionInfoOne.LastVisableTime > 3 && Game.Time - ChampionInfoOne.LastVisableTime < 7)
                            {
                                if ((int)(Game.Time * 10) % 2 == 0)
                                {
                                    //drawText("SS " + enemy.ChampionName, Player.Position.Extend(enemy.Position, positionGang), System.Drawing.Color.Yellow);
                                    DrawFontTextMap(Tahoma13, "SS " + enemy.ChampionName + " " + (int)(Game.Time - ChampionInfoOne.LastVisableTime), Player.Position.Extend(enemy.Position, positionGang), SharpDX.Color.Yellow);
                                }
                            }
                            else
                            {
                                DrawFontTextMap(Tahoma13, "SS " + enemy.ChampionName + " " + (int)(Game.Time - ChampionInfoOne.LastVisableTime), Player.Position.Extend(enemy.Position, positionGang), SharpDX.Color.Yellow);
                            }
                        }
                        else
                            DrawFontTextMap(Tahoma13, "SS " + enemy.ChampionName, Player.Position.Extend(enemy.Position, positionGang), SharpDX.Color.Yellow);
                    }
                    else
                    {
                        if ((int)(Game.Time * 10) % 2 == 0)
                        {
                            DrawFontTextMap(Tahoma13B, enemy.ChampionName, Player.Position.Extend(enemy.Position, positionGang), SharpDX.Color.OrangeRed);
                        }
                    }

                    if (Distance < 3500 && enemy.IsVisible && !Render.OnScreen(Drawing.WorldToScreen(Player.Position.Extend(enemy.Position, Distance + 500))))
                    {
                        
                        drawLine(Player.Position.Extend(enemy.Position, 100), Player.Position.Extend(enemy.Position, positionGang - 100), (int)((3500 - Distance) / 300), System.Drawing.Color.OrangeRed);

                    }
                    else if (Distance < 3500 && !enemy.IsVisible && !Render.OnScreen(Drawing.WorldToScreen(Player.Position.Extend(enemy.Position, Distance + 500))))
                    {
                        var need = Core.OKTWtracker.ChampionInfoList.Find(x => x.NetworkId == enemy.NetworkId);
                        if (need != null && Game.Time - need.LastVisableTime < 5)
                        {
                            drawLine(Player.Position.Extend(enemy.Position, 100), Player.Position.Extend(enemy.Position, positionGang - 100), (int)((3500 - Distance) / 300), System.Drawing.Color.Gray);
                        }
                    }
                }
                positionGang = positionGang + 100;
            }

            if (Config.Item("OrbDraw").GetValue<bool>())
            {
                if (Player.HealthPercent > 60)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.GreenYellow, 1);
                else if (Player.HealthPercent > 30)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Orange, 1);
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius * 2, System.Drawing.Color.Red, 1);
            }

            DrawOrbwalkerTarget();
        }

        private void DrawOrbwalkerTarget()
        {
            if (Config.Item("orb").GetValue<bool>())
            {
                var orbT = Program.Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                {
                    if (orbT.Health > orbT.MaxHealth * 0.6)
                        Render.Circle.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.GreenYellow, 1);
                    else if (orbT.Health > orbT.MaxHealth * 0.3)
                        Render.Circle.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Orange, 1);
                    else
                        Render.Circle.DrawCircle(orbT.Position, orbT.BoundingRadius, System.Drawing.Color.Red, 1);
                }
            }
        }
    }
}
