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
    class AutoLvlUp
    {
        private Menu Config = Program.Config;
        public void LoadOKTW()
        {
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("AutoLvl", "ENABLE").SetValue(true));
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("1", "1",true).SetValue(new StringList(new[] { "Q", "W", "E","R" }, 3)));
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("2", "2", true).SetValue(new StringList(new[] { "Q", "W", "E", "R" }, 1)));
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("3", "3", true).SetValue(new StringList(new[] { "Q", "W", "E", "R" }, 1)));
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("4", "4", true).SetValue(new StringList(new[] { "Q", "W", "E", "R" }, 1)));
            Config.SubMenu("AutoLvlUp").AddItem(new MenuItem("LvlStart", "Auto LVL start", true).SetValue(new Slider(2, 6, 1)));

           Obj_AI_Base.OnLevelUp +=Obj_AI_Base_OnLevelUp;
           Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("AutoLvl").GetValue<bool>())
            {
                var lvl2 = Config.Item("2", true).GetValue<StringList>().SelectedIndex;
                var lvl3 = Config.Item("3", true).GetValue<StringList>().SelectedIndex;
                var lvl4 = Config.Item("4", true).GetValue<StringList>().SelectedIndex;
                if ((lvl2 == lvl3 || lvl2 == lvl4 || lvl3 == lvl4) && (int)Game.Time % 2 == 0)
                {
                    drawText("PLEASE SET ABILITY SEQENCE", ObjectManager.Player.Position, System.Drawing.Color.OrangeRed, -200);
                }
            }
        }

        public static void drawText(string msg, Vector3 Hero, System.Drawing.Color color, int weight = 0)
        {
            var wts = Drawing.WorldToScreen(Hero);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] + weight, color, msg);
        }

        private void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, EventArgs args)
        {
            if (!sender.IsMe || !Config.Item("AutoLvl").GetValue<bool>() || ObjectManager.Player.Level < Config.Item("LvlStart", true).GetValue<Slider>().Value)
                return;
            var lvl1 = Config.Item("1", true).GetValue<StringList>().SelectedIndex;
            var lvl2 = Config.Item("2", true).GetValue<StringList>().SelectedIndex;
            var lvl3 = Config.Item("3", true).GetValue<StringList>().SelectedIndex;
            var lvl4 = Config.Item("4", true).GetValue<StringList>().SelectedIndex;

            if (lvl1 == 0) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
            if (lvl1 == 1) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
            if (lvl1 == 2) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
            if (lvl1 == 3) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);

            if (ObjectManager.Player.Level > 3 || ObjectManager.Player.Level == 1)
            {
                if (lvl2 == 0) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                if (lvl2 == 1) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                if (lvl2 == 2) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                if (lvl2 == 3) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }
            if (ObjectManager.Player.Level > 3 || ObjectManager.Player.Level == 2)
            {
                if (lvl3 == 0) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                if (lvl3 == 1) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                if (lvl3 == 2) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                if (lvl3 == 3) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }

            if (lvl4 == 0) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
            if (lvl4 == 1) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
            if (lvl4 == 2) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
            if (lvl4 == 3) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            
            }

    }
}
