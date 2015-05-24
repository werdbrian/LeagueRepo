using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Activator
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public static Items.Item 

            //REGEN
            Potion = new Items.Item(2003, 0),
            ManaPotion = new Items.Item(2004, 0),
            //attack
            Botrk = new Items.Item(3153, 450f),
            Cutlass = new Items.Item(3144, 450f),
            Youmuus = new Items.Item(3142, 650f),
            Hydra = new Items.Item(3144, 450f),
            Hydra2 = new Items.Item(3144, 450f);

        public void LoadOKTW()
        {
            Game.OnUpdate += Game_OnGameUpdate;
            //Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            //Drawing.OnDraw += Drawing_OnDraw;
            Config.SubMenu("Items").AddItem(new MenuItem("pots", "Use pots").SetValue(true));

            Config.SubMenu("Items").SubMenu("Botrk").AddItem(new MenuItem("Botrk", "Botrk").SetValue(true));
            Config.SubMenu("Items").SubMenu("Botrk").AddItem(new MenuItem("BotrkKS", "Botrk KS").SetValue(true));
            Config.SubMenu("Items").SubMenu("Botrk").AddItem(new MenuItem("BotrkLS", "Botrk LifeSaver").SetValue(true));
            Config.SubMenu("Items").SubMenu("Botrk").AddItem(new MenuItem("BotrkCombo", "Botrk always in combo").SetValue(false));

            Config.SubMenu("Items").SubMenu("Cutlass").AddItem(new MenuItem("Cutlass", "Cutlass").SetValue(true));
            Config.SubMenu("Items").SubMenu("Cutlass").AddItem(new MenuItem("CutlassKS", "Cutlass KS").SetValue(true));
            Config.SubMenu("Items").SubMenu("Cutlass").AddItem(new MenuItem("CutlassCombo", "Cutlass always in combo").SetValue(false));
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!Program.LagFree(0))
                return;

            if (Config.Item("pots").GetValue<bool>())
                PotionManagement();

            if (Botrk.IsReady() && Config.Item("Botrk").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Botrk.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (Config.Item("BotrkKS").GetValue<bool>() && Player.CalcDamage(t, Damage.DamageType.Physical, t.MaxHealth * 0.1) > t.Health)
                        Botrk.Cast(t);
                    if (Config.Item("BotrkLS").GetValue<bool>() && Player.Health < Player.CountEnemiesInRange(600) * Player.Level * 20)
                        Botrk.Cast(t);
                    if (Config.Item("BotrkCombo").GetValue<bool>() && Program.Combo)
                        Botrk.Cast(t);
                }
            }

            if (Cutlass.IsReady() && Config.Item("Cutlass").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Cutlass.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (Config.Item("CutlassKS").GetValue<bool>() && Player.CalcDamage(t, Damage.DamageType.Magical, 100) > t.Health)
                        Cutlass.Cast(t);
                    if (Config.Item("CutlassCombo").GetValue<bool>() && Program.Combo)
                        Cutlass.Cast(t);
                }
            }
        }
        private void PotionManagement()
        {
            if (!Player.InFountain() && !Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !Player.HasBuff("RegenerationPotion"))
                {
                    if (Player.CountEnemiesInRange(700) > 0 && Player.Health + 200 < Player.MaxHealth)
                        Potion.Cast();
                    else if (Player.Health < Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !Player.HasBuff("FlaskOfCrystalWater"))
                {
                    if (Player.CountEnemiesInRange(1200) > 0 && Player.Mana < 200)
                        ManaPotion.Cast();
                }
            }
        }
    }
}
