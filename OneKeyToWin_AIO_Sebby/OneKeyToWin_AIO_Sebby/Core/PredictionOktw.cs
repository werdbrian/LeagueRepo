using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace OneKeyToWin_AIO_Sebby.Core
{
    class PredictionOktw
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        private Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        int HitChanceNum = 4;

        public void LoadOKTW()
        {
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("Hit", "Prediction OKTW©").SetValue(new Slider(4, 4, 0)));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("0", "0 - normal"));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("1", "1 - high"));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("2", "2 - high + max range fix"));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("3", "3 - high + max range fix + waypionts analyzer"));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("4", "4 - VeryHigh + max range fix + waypionts analyzer"));
            Config.SubMenu("Prediction OKTW©").AddItem(new MenuItem("debugPred", "Prediction Debug").SetValue(false));
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {

        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                HitChanceNum = Config.Item("Hit", true).GetValue<Slider>().Value;
            }
        }


        public void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            if (target.Path.Count() > 1)
                return;

            var poutput = QWER.GetPrediction(target);
            var col = poutput.CollisionObjects.Count(ColObj => ColObj.IsEnemy && ColObj.IsMinion && !ColObj.IsDead);

            if (col > 0)
            {
                return;
            }


            if (target.HasBuff("Recall") || poutput.Hitchance == HitChance.Immobile)
            {
                QWER.Cast(poutput.CastPosition);
                return;
            }

            if (poutput.Hitchance == HitChance.Dashing && QWER.Delay < 0.30f)
            {
                QWER.Cast(poutput.CastPosition);
                return;
            }

            var LastWaypiont = target.GetWaypoints().Last().To3D();

            float fixRange = (target.MoveSpeed * (Player.ServerPosition.Distance(target.ServerPosition) / QWER.Speed + QWER.Delay)) - (target.BoundingRadius * 2);

            if (target.Path.Count() == 0 && target.Position == target.ServerPosition && !target.IsWindingUp)
            {
                Program.debug("notMove " + fixRange);
                if (Player.Distance(target.ServerPosition) < QWER.Range - fixRange)
                    QWER.Cast(poutput.CastPosition);

                return;
            }

            if (HitChanceNum == 4)
            {
                if ((int)poutput.Hitchance < 5)
                    return;
                if (LastWaypiont.Distance(Player.ServerPosition) + fixRange <= target.ServerPosition.Distance(Player.ServerPosition))
                {
                    if (Player.Distance(target.ServerPosition) < QWER.Range - fixRange)
                    {
                        float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                        float SiteToSite = (BackToFront * 5) - QWER.Width;

                        if ((target.ServerPosition.Distance(LastWaypiont) > SiteToSite
                            || Math.Abs(Player.Distance(LastWaypiont) - Player.Distance(target.ServerPosition)) > BackToFront)
                            || Player.Distance(target.ServerPosition) < SiteToSite + target.BoundingRadius * 2
                            || Player.Distance(LastWaypiont) < BackToFront)
                        {
                            QWER.Cast(poutput.CastPosition, true);
                            Program.debug("good 2");
                        }
                        else
                            Program.debug("ignore 2");
                    }
                    else
                        Program.debug("fixed " + fixRange);
                }
                else
                {
                    QWER.Cast(poutput.CastPosition, true);
                    Program.debug("Run: " + target.BaseSkinName);
                }
            }
            else if (HitChanceNum == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if ((int)poutput.Hitchance < 5)
                    return;

                if ((int)target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) == 0)
                    return;

                float BackToFront = ((target.MoveSpeed * QWER.Delay) + (Player.Distance(target.ServerPosition) / QWER.Speed));
                float SiteToSite = (BackToFront * 5) - QWER.Width;

                if ((target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) > SiteToSite
                    || Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > BackToFront)
                    || Player.Distance(target.Position) < SiteToSite + target.BoundingRadius * 2
                    || Player.Distance(waypoints.Last<Vector2>().To3D()) < BackToFront
                    || (int)poutput.Hitchance == 6)
                {
                    //debug("STS " + (int)SiteToSite + " < " + (int)target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) + " BTF " + (int)Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) + " > " + (int)BackToFront);
                    if (waypoints.Last<Vector2>().To3D().Distance(Player.Position) <= target.Distance(Player.Position))
                    {
                        if (Player.Distance(target.ServerPosition) < QWER.Range - (poutput.CastPosition.Distance(target.ServerPosition)))
                        {

                            QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                        }
                    }
                    else
                    {

                        QWER.CastIfHitchanceEquals(target, HitChance.High, true);
                    }
                }
                else
                {
                    //debug("STS " + (int)SiteToSite + " > " + (int)target.ServerPosition.Distance(waypoints.Last<Vector2>().To3D()) + " BTF " + (int)Math.Abs(Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) + " > " + (int)BackToFront + " ignore");
                }
            }
            else if (HitChanceNum == 0)
                QWER.Cast(target, true);
            else if (HitChanceNum == 1)
            {
                if ((int)poutput.Hitchance > 4)
                    QWER.Cast(poutput.CastPosition);
            }
            else if (HitChanceNum == 2)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (waypoints.Last<Vector2>().To3D().Distance(poutput.CastPosition) > QWER.Width && (int)poutput.Hitchance > 4)
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
        }
    }
}
