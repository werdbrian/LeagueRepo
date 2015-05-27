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
    class Summoners
    {
        private Menu Config = Program.Config;
        private SpellSlot heal, barrier, ignite;
        private Obj_AI_Hero Player { get { return ObjectManager.Player; }}

        public void LoadOKTW()
        {
            heal = Player.GetSpellSlot("summonerheal");
            barrier = Player.GetSpellSlot("summonerbarrier");
            ignite = Player.GetSpellSlot("summonerdot");

            if (heal != SpellSlot.Unknown)
            {
                Config.SubMenu("Summoners").SubMenu("Heal").AddItem(new MenuItem("Heal", "Heal").SetValue(true));
                Config.SubMenu("Summoners").SubMenu("Heal").AddItem(new MenuItem("AllyHeal", "AllyHeal").SetValue(true));
            }
            if (barrier != SpellSlot.Unknown)
            {
                Config.SubMenu("Summoners").SubMenu("Barrier").AddItem(new MenuItem("Barrier", "Barrier").SetValue(true));

            }
            if (ignite != SpellSlot.Unknown)
            {
                Config.SubMenu("Summoners").SubMenu("Ignite").AddItem(new MenuItem("Ignite", "Ignite").SetValue(true));
            }
            
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(4) && CanUse(ignite) && Config.Item("Ignite").GetValue<bool>())
            {
                foreach(var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(600)))
                {
                    var IgnDmg = Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
                    if (enemy.Health <= IgnDmg && Player.Distance(enemy.ServerPosition) > 500 && enemy.CountAlliesInRange(500) < 2)
                        Player.Spellbook.CastSpell(ignite, enemy);

                    if (enemy.Health <= 2 * IgnDmg )
                    {
                        if (enemy.PercentLifeStealMod > 10)
                            Player.Spellbook.CastSpell(ignite, enemy);

                        if (enemy.HasBuff("RegenerationPotion") || enemy.HasBuff("ItemMiniRegenPotion") || enemy.HasBuff("ItemCrystalFlask"))
                            Player.Spellbook.CastSpell(ignite, enemy);

                        if (enemy.Health > Player.Health)
                            Player.Spellbook.CastSpell(ignite, enemy);
                    } 
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy || sender.IsMinion || !sender.IsValidTarget(1000))
                return;

            double dmg = 0;

            if (args.SData.IsAutoAttack() && args.Target.IsMe)
            {
                //Program.debug( "aa");
                dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
            }
            else if (args.Target != null && args.Target.IsMe)
            {
                Program.debug("targeted");

                dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
            }
            else if ( Player.Distance(args.End) <= 300f)
            {
                Program.debug(args.SData.Name);
                if (!Program.CanMove(ObjectManager.Player) || ObjectManager.Player.Distance(sender.Position) < 300f)
                    dmg = dmg + sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);
                else if (Player.Distance(args.End) < 100f)
                    dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
            }

            if (CanUse(barrier) && Config.Item("Barrier").GetValue<bool>() && Player.Health - dmg > Player.Level * 20 && Player.CountEnemiesInRange(800) > 0)
            {

                if (Player.Health - dmg < Player.CountEnemiesInRange(600) * Player.Level * 15)
                    Player.Spellbook.CastSpell(barrier, Player);
                
            }

            if (CanUse(heal) && Config.Item("Heal").GetValue<bool>())
            {
                bool AllyHeal = Config.Item("AllyHeal").GetValue<bool>();
                if (AllyHeal)
                {
                    foreach (var ally in Program.Allies.Where(ally => ally.IsValid && !ally.IsDead && Player.Distance(ally.ServerPosition) < 700))
                    {
                        if (ally.Health - dmg < ally.CountEnemiesInRange(600) * ally.Level * 15)
                            Player.Spellbook.CastSpell(heal, ally);
                    }
                }
                else if (Player.Health - dmg < Player.CountEnemiesInRange(600) * Player.Level * 15 && dmg > 0)
                {
                    Player.Spellbook.CastSpell(heal, Player);
                }
            }
        }
        private bool CanUse(SpellSlot sum)
        {
            if (sum != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(sum) == SpellState.Ready)
                return true;
            else
                return false;
        }
    }
}
