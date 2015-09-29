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
    class OKTWlab
    {
        private GameObject obj;
        private float time = 0;
        private Vector3 from;
        public void LoadOKTW()
        {
            Obj_AI_Base.OnDelete += Obj_AI_Base_OnDelete;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            //foreach (var buff in ObjectManager.Player.Buffs)
               //Program.debug(buff.Name);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            return;

            if (obj != null &&  obj.IsValid)
            {
                //Utility.DrawCircle(obj.Position, 100, System.Drawing.Color.Orange, 1, 1);
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            return;
            if (sender.IsMe)
            {
                //Program.debug("speed: " +args.SData.MissileSpeed);
                //Program.debug("name: " + args.SData.Name);
                //Program.debug("" + args.SData.DelayTotalTimePercent);
                //time = Game.Time;
            }
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            return;
            if (sender.IsValid )
            {
                //obj = sender;
                //Program.debug(sender.Name);
                //Program.debug(""+);
                //Program.debug("cast time" +(time - Game.Time));
            }
        }

        private void Obj_AI_Base_OnDelete(GameObject sender, EventArgs args)
        {
            return;
            if (sender.IsValid)
            {
                
            }
        }
    }
}
