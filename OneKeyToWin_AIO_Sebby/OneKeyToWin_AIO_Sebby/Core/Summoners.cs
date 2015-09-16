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
        private SpellSlot heal, barrier, ignite, smite, exhaust, flash;
        private Obj_AI_Hero Player { get { return ObjectManager.Player; }}
        private int smiteHero = 0;
        private bool TryUse = false;

        public void LoadOKTW()
        {
            heal = Player.GetSpellSlot("summonerheal");
            barrier = Player.GetSpellSlot("summonerbarrier");
            ignite = Player.GetSpellSlot("summonerdot");
            exhaust = Player.GetSpellSlot("summonerexhaust");
            flash = Player.GetSpellSlot("summonerflash");

            if (flash != SpellSlot.Unknown)
            {
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Flash").AddItem(new MenuItem("Flash", "Flash max range").SetValue(true));

            }
            if (exhaust != SpellSlot.Unknown)
            {
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Exhaust").AddItem(new MenuItem("Exhaust", "Exhaust").SetValue(true));
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Exhaust").AddItem(new MenuItem("Exhaust1", "Exhaust if Channeling Important Spell ").SetValue(true));
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Exhaust").AddItem(new MenuItem("Exhaust2", "Always in combo").SetValue(false));
            }
            if (heal != SpellSlot.Unknown)
            {
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Heal").AddItem(new MenuItem("Heal", "Heal").SetValue(true));
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").SubMenu("Heal").AddItem(new MenuItem("AllyHeal", "AllyHeal").SetValue(true));
            }
            if (barrier != SpellSlot.Unknown)
            {
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").AddItem(new MenuItem("Barrier", "Barrier").SetValue(true));

            }
            if (ignite != SpellSlot.Unknown)
            {
                Config.SubMenu("Activator OKTW©").SubMenu("Summoners").AddItem(new MenuItem("Ignite", "Ignite").SetValue(true));
            }

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnGameUpdate;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {

            if (flash != SpellSlot.Unknown && Config.Item("Flash").GetValue<bool>() && sender.ActiveSpellSlot == flash && flash.IsReady() && args.Slot == flash)
            {
                args.Process = false;
                Player.Spellbook.CastSpell(flash, ObjectManager.Player.Position.Extend(Game.CursorPos, 500), false);
            }
           
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
           
        if(!Program.LagFree(0))

            if (CanUse(ignite) && Config.Item("Ignite").GetValue<bool>())
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

            if (CanUse(exhaust) && Config.Item("Exhaust").GetValue<bool>() )
            {
                if (Config.Item("Exhaust1").GetValue<bool>())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(650) && enemy.IsChannelingImportantSpell()))
                    {
                        Player.Spellbook.CastSpell(exhaust, enemy);
                    }
                }

                if (Config.Item("Exhaust2").GetValue<bool>() && Program.Combo)
                {
                    var t = TargetSelector.GetTarget(650, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget() )
                    {
                        Player.Spellbook.CastSpell(exhaust, t);
                    }
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!CanUse(barrier) && !CanUse(heal) && !CanUse(exhaust))
                return;

            if (!sender.IsEnemy || !sender.IsValidTarget(1500))
                return;

            foreach (var ally in Program.Allies.Where(ally => ally.IsValid && !ally.IsDead && Player.Distance(ally.ServerPosition) < 700))
            {
                double dmg = 0;
                if (args.Target != null && args.Target.NetworkId == ally.NetworkId)
                {
                    dmg = dmg + sender.GetSpellDamage(ally, args.SData.Name);
                }
                else
                {
                    var castArea = ally.Distance(args.End) * (args.End - ally.ServerPosition).Normalized() + ally.ServerPosition;
                    if (castArea.Distance(ally.ServerPosition) < ally.BoundingRadius / 2)
                    {
                        dmg = dmg + sender.GetSpellDamage(ally, args.SData.Name);
                    }
                }

                if (CanUse(barrier) && Config.Item("Barrier").GetValue<bool>() && ally.IsMe)
                {
                    var value = 95 + Player.Level * 20;
                    if (dmg > value && Player.Health < Player.MaxHealth * 0.5)
                        Player.Spellbook.CastSpell(barrier, Player);
                    if (Player.Health - dmg < Player.CountEnemiesInRange(700) * Player.Level * 15)
                        Player.Spellbook.CastSpell(barrier, Player);
                
                }

                if (CanUse(exhaust) && Config.Item("Exhaust").GetValue<bool>() && dmg > 0)
                {
                    if (ally.Health - dmg < ally.CountEnemiesInRange(650) * ally.Level * 40)
                        Player.Spellbook.CastSpell(exhaust, sender);
                }

                if (CanUse(heal) && Config.Item("Heal").GetValue<bool>() && dmg > 0)
                {
                    if (!Config.Item("AllyHeal").GetValue<bool>() && !ally.IsMe)
                        return;

                    if (ally.Health - dmg < ally.CountEnemiesInRange(700) * ally.Level * 10)
                        Player.Spellbook.CastSpell(heal, ally);
                    else if (ally.Health - dmg <  ally.Level * 10)
                        Player.Spellbook.CastSpell(heal, ally);
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
