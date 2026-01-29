using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using GuideAIMod.UI;

namespace GuideAIMod
{
    /// <summary>
    /// UI 系统管理
    /// 处理界面的显示/隐藏和快捷键
    /// </summary>
    public class GuideUISystem : ModSystem
    {
        private UserInterface _guideInterface = null!;
        private GuideUI _guideUI = null!;
        private bool _lastKeyState = false;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _guideUI = new GuideUI();
                _guideUI.Activate();
                
                _guideInterface = new UserInterface();
                _guideInterface.SetState(_guideUI);
            }
        }

        public override void Unload()
        {
            _guideUI?.Deactivate();
            _guideInterface = null!;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // H 键切换界面
            bool currentKeyState = Main.keyState.IsKeyDown(Keys.H);
            if (currentKeyState && !_lastKeyState && !Main.drawingPlayerChat && !Main.editSign)
            {
                _guideUI?.Toggle();
            }
            _lastKeyState = currentKeyState;

            if (_guideUI?.Visible == true)
            {
                _guideInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "GuideAI: GuideUI",
                    delegate
                    {
                        if (_guideUI?.Visible == true)
                        {
                            _guideInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}
