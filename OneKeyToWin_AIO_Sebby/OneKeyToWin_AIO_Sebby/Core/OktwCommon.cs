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
    class OktwCommon
    {
        public static bool CanMove(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) ||
                target.IsStunned || target.IsChannelingImportantSpell())
            {
                Program.debug("!canMov" + target.ChampionName);
                return false;
            }
            else
                return true;
        }

        public static bool ValidUlt(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity) || target.HasBuffOfType(BuffType.SpellImmunity)
            || target.IsZombie || target.HasBuffOfType(BuffType.Invulnerability) || target.HasBuffOfType(BuffType.SpellShield))
                return false;
            else
                return true;
        }

        public static int CountEnemiesInRangeDeley(Vector3 position, float range, float delay)
        {
            int count = 0;
            foreach (var t in Program.Enemies.Where(t => t.IsValidTarget()))
            {
                Vector3 prepos = Prediction.GetPrediction(t, delay).CastPosition;
                if (position.Distance(prepos) < range)
                    count++;
            }
            return count;
        }


        private List<Vector3> CirclePoints(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            var bestPoint = ObjectManager.Player.Position;
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
        }

        public static bool GetCollision(Obj_AI_Base target, Spell QWER, bool champion, bool minion)
        {
            var rDmg = QWER.GetDamage(target);
            int collision = 0;
            PredictionOutput output = QWER.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - ObjectManager.Player.Position.To2D();
            direction.Normalize();
            if (champion)
            {
                foreach (var enemy in Program.Enemies.Where(x => x.IsEnemy && x.IsValidTarget()))
                {
                    PredictionOutput prediction = QWER.GetPrediction(enemy);
                    Vector3 predictedPosition = prediction.CastPosition;
                    Vector3 v = output.CastPosition - ObjectManager.Player.ServerPosition;
                    Vector3 w = predictedPosition - ObjectManager.Player.ServerPosition;
                    double c1 = Vector3.Dot(w, v);
                    double c2 = Vector3.Dot(v, v);
                    double b = c1 / c2;
                    Vector3 pb = ObjectManager.Player.ServerPosition + ((float)b * v);
                    float length = Vector3.Distance(predictedPosition, pb);
                    if (length < QWER.Width )
                        return true;
                }
            }
            if (minion)
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, QWER.Range, MinionTypes.All);
                foreach (var enemy in allMinions.Where(x => x.IsEnemy && x.IsValidTarget()))
                {
                    PredictionOutput prediction = QWER.GetPrediction(enemy);
                    Vector3 predictedPosition = prediction.CastPosition;
                    Vector3 v = output.CastPosition - ObjectManager.Player.ServerPosition;
                    Vector3 w = predictedPosition - ObjectManager.Player.ServerPosition;
                    double c1 = Vector3.Dot(w, v);
                    double c2 = Vector3.Dot(v, v);
                    double b = c1 / c2;
                    Vector3 pb = ObjectManager.Player.ServerPosition + ((float)b * v);
                    float length = Vector3.Distance(predictedPosition, pb);
                    if (length < QWER.Width)
                        return true;
                }
            }
            return false;
        }

        public static int GetBuffCount(Obj_AI_Base target, String buffName)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == buffName)
                    return buff.Count;
            }
            return 0;
        }

        public static int WayPointAnalysis(Obj_AI_Base unit , Spell QWER)
        {
            int HC = 0;

            if (QWER.Delay < 0.25f)
                HC = 2;
            else
                HC = 1;

            if (unit.Path.Count() == 1)
                HC = 2;
            


            return HC;

        }
    }
}
