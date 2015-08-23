using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;

namespace OKTWprediction
{
    public class Prediction
    {
        public static Menu Config;
        private static Spell qwer;

        public class PredictionResult
        {
            public int Hitchance = 0;
            public Vector3 CastPosition;
            public Vector3 Position;

        }

        static Prediction()
        {
            Config = new Menu("OneKeyToWin Prediction", "PredictionOKTW" + ObjectManager.Player.ChampionName, true);
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            
        }

        private static void OnDraw(EventArgs args)
        {
            
        }

        private static PredictionResult GetPrediction(Obj_AI_Base unit, float delay, float width, float range, float speed, Vector3 from, int spelltype, bool collision)
        {
            Vector3 CastPosition = unit.ServerPosition;
            int hitChance = 0;

            delay = delay + (0.07f + Game.Ping / 2000f);

            var result = CalculateTargetPosition(unit, delay, width, range, speed, from, spelltype, collision);
            
            return new PredictionResult
            {
                CastPosition = CastPosition,
                Hitchance = 0
            };
        }
        private static PredictionResult CalculateTargetPosition(Obj_AI_Base unit, float delay, float radius, float range, float speed, Vector3 from, int spelltype, bool collision)
        {
            var Waypoints = unit.GetWaypoints();

            var Waypointslength = Waypoints.PathLength();

            //Skillshots with only a delay
            if (Waypointslength - delay * unit.MoveSpeed + radius >= 0)
            {
                Waypoints = Waypoints.CutPath(delay * unit.MoveSpeed - radius);
                if (speed!= float.MaxValue)
                {
                    var tT = 0f;
                    for (var i = 0; i < Waypoints.Count - 1; i++)
                    {
                        var a = Waypoints[i];
                        var b = Waypoints[i + 1];
                        var tB = a.Distance(b) / unit.MoveSpeed;
                        var direction = (b - a).Normalized();
                        a = a - unit.MoveSpeed * tT * direction;
                        var sol = Geometry.VectorMovementCollision(a, b, unit.MoveSpeed, from.To2D(), speed, tT);
                        var t = (float)sol[0];
                        var pos = (Vector2)sol[1];

                        if (pos.IsValid() && t >= tT && t <= tT + tB)
                        {
                            if (pos.Distance(b, true) < 20)
                                break;
                            var p = pos + radius * direction;

                            if (spelltype == 0 && false)
                            {
                                var alpha = (from.To2D() - p).AngleBetween(a - b);
                                if (alpha > 30 && alpha < 180 - 30)
                                {
                                    var beta = (float)Math.Asin(radius/ p.Distance(from));
                                    var cp1 = from.To2D() + (p - from.To2D()).Rotated(beta);
                                    var cp2 = from.To2D() + (p - from.To2D()).Rotated(-beta);

                                    pos = cp1.Distance(pos, true) < cp2.Distance(pos, true) ? cp1 : cp2;
                                }
                            }

                            return new PredictionResult
                            {
                                CastPosition = pos.To3D(),
                                Position = p.To3D(),
                                Hitchance = 2
                            };
                        }
                        tT += tB;
                    }
                }
            }
            return new PredictionResult
            {
                CastPosition = unit.ServerPosition,
                Position = unit.ServerPosition,
                Hitchance = 2
            };
        }
    }
}
