using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace SorakaMoon
{
    internal class OneMoonToSoraka
    {
        public Spell E;
        public Spell Q;
        public Spell R;
        public Spell W;
        public Orbwalking.Orbwalker Orbwalker { get; set; }
        public Menu Menu { get; set; }

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public void Load()
        {
            // Create spells
            Q = new Spell(SpellSlot.Q, 950);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 925);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.5f, 300, 1750, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 70f, 1750, false, SkillshotType.SkillshotCircle);

            CreateMenu();

            Game.PrintChat("<font color=\"#7CFC00\"><b>OneMoonToSoraka:</b></font> Loaded");

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2OnOnInterruptableTarget;
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.OnUpdate += GameOnOnUpdate;
        }

        private void GameOnOnUpdate(EventArgs args)
        {


            // Patented Advanced Algorithms D321988
            var healthTarget =
                HeroManager.Allies.Where(x => x.IsValid)
                    .FirstOrDefault(
                        x =>
                             x.Health < x.CountEnemiesInRange(700) * 200 );

            if (healthTarget != null)
            {
                R.Cast();
            }
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawE = Menu.Item("DrawE").IsActive();
            var drawAxeLocation = Menu.Item("DrawAxeLocation").IsActive();
            var drawAxeRange = Menu.Item("DrawAxeRange").IsActive();

            if (drawE)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawAxeRange)
            {
                Render.Circle.DrawCircle(Game.CursorPos, Menu.Item("CatchAxeRange").GetValue<Slider>().Value,
                    Color.DodgerBlue);
            }
        }


        private void Interrupter2OnOnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Menu.Item("UseEInterrupt").IsActive() || !E.IsReady() || !sender.IsValidTarget(E.Range))
            {
                return;
            }

            if (args.DangerLevel == Interrupter2.DangerLevel.Medium || args.DangerLevel == Interrupter2.DangerLevel.High)
            {
                E.Cast(sender);
            }
        }

        private void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("UseEGapcloser").IsActive() || !E.IsReady() || !gapcloser.Sender.IsValidTarget(E.Range))
            {
                return;
            }

            E.Cast(gapcloser.Sender);
        }

        public float ManaPercent
        {
            get { return Player.Mana / Player.MaxMana * 100; }
        }

        private void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (Menu.Item("UseEHarass").IsActive() && E.IsReady())
            {
                E.Cast(target);
            }
        }

        private void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = Menu.Item("UseQCombo").IsActive();
            var useE = Menu.Item("UseECombo").IsActive();


            if (useQ  && Q.IsReady() )
            {
                Q.Cast(target);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target);
            }
        }

        private void LaneClear()
        {
            var useQ = Menu.Item("UseQWaveClear").IsActive();
            var useE = Menu.Item("UseEWaveClear").IsActive();

            if (ManaPercent < Menu.Item("WaveClearManaPercent").GetValue<Slider>().Value)
            {
                return;
            }

            if (useQ || Q.IsReady())
            {
                return;
            }

            var bestLocation = Q.GetCircularFarmLocation(MinionManager.GetMinions(Q.Range));

            if (bestLocation.MinionsHit > 1)
            {
                E.Cast(bestLocation.Position);
            }
            if (!useE || !E.IsReady())
            {
                return;
            }

            var bestLocation2 = E.GetCircularFarmLocation(MinionManager.GetMinions(E.Range));

            if (bestLocation2.MinionsHit > 1)
            {
                E.Cast(bestLocation2.Position);
            }
        }

        private void CreateMenu()
        {
            Menu = new Menu("OneMoonToSoraka", "cmOneMoonToSoraka", true);

            // Target Selector
            var tsMenu = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            // Orbwalker
            var orbwalkMenu = new Menu("Orbwalker", "orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            Menu.AddSubMenu(orbwalkMenu);

            // Combo
            var comboMenu = new Menu("Combo", "combo");
            comboMenu.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Menu.AddSubMenu(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "harass");
            harassMenu.AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            harassMenu.AddItem(
                new MenuItem("UseHarassToggle", "Harass! (Toggle)").SetValue(new KeyBind(84, KeyBindType.Toggle)));
            Menu.AddSubMenu(harassMenu);

            // Lane Clear
            var laneClearMenu = new Menu("Wave Clear", "waveclear");
            laneClearMenu.AddItem(new MenuItem("UseQWaveClear", "Use Q").SetValue(true));
            laneClearMenu.AddItem(new MenuItem("UseWWaveClear", "Use W").SetValue(true));
            laneClearMenu.AddItem(new MenuItem("UseEWaveClear", "Use E").SetValue(false));
            laneClearMenu.AddItem(new MenuItem("WaveClearManaPercent", "Mana Percent").SetValue(new Slider(50)));
            Menu.AddSubMenu(laneClearMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "draw");
            drawMenu.AddItem(new MenuItem("DrawE", "Draw E").SetValue(true));
            drawMenu.AddItem(new MenuItem("DrawAxeLocation", "Draw Axe Location").SetValue(true));
            drawMenu.AddItem(new MenuItem("DrawAxeRange", "Draw Axe Catch Range").SetValue(true));
            Menu.AddSubMenu(drawMenu);

            // Misc Menu
            var miscMenu = new Menu("Misc", "misc");
            miscMenu.AddItem(new MenuItem("UseWSetting", "Use W Instantly(When Available)").SetValue(false));
            miscMenu.AddItem(new MenuItem("UseEGapcloser", "Use E on Gapcloser").SetValue(true));
            miscMenu.AddItem(new MenuItem("UseEInterrupt", "Use E to Interrupt").SetValue(true));
            miscMenu.AddItem(new MenuItem("UseWManaPercent", "Use W Mana Percent").SetValue(new Slider(50)));
            miscMenu.AddItem(new MenuItem("UseWSlow", "Use W if Slowed").SetValue(true));
            miscMenu.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();
        }
    }
}
