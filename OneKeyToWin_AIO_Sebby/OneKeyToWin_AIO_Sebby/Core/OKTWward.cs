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
    class OKTWward
    {
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Menu Config = Program.Config;

        private Items.Item
            VisionWard = new Items.Item(2043, 550f),
            OracleLens = new Items.Item(3364, 550f);

        public void LoadOKTW()
        {
            Config.SubMenu("OneKeyToBrain©").SubMenu("Auto ward").AddItem(new MenuItem("AutoWardPink", "Auto VisionWard & OracleLens").SetValue(true));

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate +=GameObject_OnCreate;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Program.LagFree(4) && Player.HasBuff("rengarralertsound"))
            {
                CastVisionWards(Player.ServerPosition);
            }
        }

        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsEnemy && sender.Position.Distance(Player.Position) < 800)
            {
                switch (sender.Name)
                {
                    case "Rengar_LeapSound.troy":
                        CastVisionWards(sender.Position);
                        break;
                    case "Rengar_Base_R_Alert":
                        CastVisionWards(sender.Position);
                        break;
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsAlly && sender is Obj_AI_Hero && sender.Distance(Player.Position) < 800)
            {
                Program.debug(args.SData.Name);
            }
            if (sender.IsEnemy && sender is Obj_AI_Hero && sender.Distance(Player.Position) < 800)
            {

                switch (args.SData.Name)
                {
                    case "akalismokebomb":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "deceive":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "khazixr":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "khazixrlong":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "talonshadowassault":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "monkeykingdecoy":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "RengarR":
                        CastVisionWards(sender.ServerPosition);
                        break;
                    case "TwitchHideInShadows":
                        CastVisionWards(sender.ServerPosition);
                        break;
                }
            }
        }

        private void CastVisionWards(Vector3 position)
        {
            if (Config.Item("AutoWardPink").GetValue<bool>())
            {
                if (OracleLens.IsReady())
                    OracleLens.Cast(Player.Position.Extend(position, OracleLens.Range));
                else if (VisionWard.IsReady())
                    VisionWard.Cast(Player.Position.Extend(position, VisionWard.Range));
            }
        }
    }
}
