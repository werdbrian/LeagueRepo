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
    class LifeSaver
    {
        private SpellSlot heal;
        private Obj_AI_Hero Player { get { return ObjectManager.Player; }}

        public void LoadOKTW()
        {
            heal = ObjectManager.Player.GetSpellSlot("summonerheal");
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnDamage +=Obj_AI_Base_OnDamage;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (heal == SpellSlot.Unknown)
                return;
            //if (Player.Health < ObjectManager.Player.CountEnemiesInRange(600) * Player.Level * 20)
                //Player.Spellbook.CastSpell(heal, ObjectManager.Player);
        }

        private void Obj_AI_Base_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy || sender.IsMinion)
                return;
            var heal = ObjectManager.Player.GetSpellSlot("summonerheal");
            if (ObjectManager.Player.Spellbook.CanUseSpell(heal) != SpellState.Ready)
                return;
            if (heal == SpellSlot.Unknown)
                return;

            double dmg = 0;

            if (args.SData.IsAutoAttack() && args.Target.IsMe)
            {
                //Program.debug( "aa");
                dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            }
            else if (args.Target != null && args.Target.IsMe)
            {
                Program.debug("targeted");

                dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            }
            else if ( ObjectManager.Player.Distance(args.End) <= 20f)
            {
                Program.debug(args.SData.Name);
                if (!Program.CanMove(ObjectManager.Player) || ObjectManager.Player.Distance(sender.Position) < 300f)
                    dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
                else if (ObjectManager.Player.Distance(args.End) < 100f)
                    dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            }

             if (ObjectManager.Player.Health - dmg > ObjectManager.Player.Level * 20 && ObjectManager.Player.CountEnemiesInRange(800) > 0)
             {
                 
                 if (dmg > ObjectManager.Player.Health)
                 {
                     //ObjectManager.Player.Spellbook.CastSpell(heal, ObjectManager.Player);
                     
                 }
             }
        }
    }
}
