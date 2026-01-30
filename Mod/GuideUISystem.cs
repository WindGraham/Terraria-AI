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
    /// </summary>
    public class GuideUISystem : ModSystem
    {
        private UserInterface _interface = null!;
        private SimpleChatUI _ui = null!;
        private bool _wasKeyDown = false;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _ui = new SimpleChatUI();
                _ui.Activate();
                
                _interface = new UserInterface();
                _interface.SetState(_ui);
            }
        }

        public override void Unload()
        {
            _ui?.Deactivate();
            _interface = null!;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // H键切换 - 只在游戏界面有效
            if (Main.gameMenu || Main.drawingPlayerChat) return;
            
            bool keyDown = Main.keyState.IsKeyDown(Keys.H);
            if (keyDown && !_wasKeyDown)
            {
                _ui?.Toggle();
            }
            _wasKeyDown = keyDown;

            if (_ui?.IsVisible == true)
            {
                _interface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (index != -1)
            {
                layers.Insert(index, new LegacyGameInterfaceLayer(
                    "GuideAI: ChatUI",
                    delegate
                    {
                        if (_ui?.IsVisible == true)
                        {
                            _interface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}
