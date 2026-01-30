#!/bin/bash
# GuideAI Mod 快速启动脚本

echo "════════════════════════════════════════════════════════"
echo "           🎮 GuideAI Mod 启动脚本"
echo "════════════════════════════════════════════════════════"

# 检查 Mod 文件
if [ ! -f "$HOME/.local/share/Terraria/tModLoader/Mods/GuideAIMod.tmod" ]; then
    echo "复制 Mod 文件..."
    cp "$HOME/.local/share/Terraria/tModLoader/ModSources/GuideAIMod/bin/Debug/net8.0/GuideAIMod.tmod" \
       "$HOME/.local/share/Terraria/tModLoader/Mods/" 2>/dev/null || echo "⚠️  Mod 文件未找到，请先编译"
fi

# 检查 Python 知识库索引
cd "$HOME/Projects/TerrariaWiki/terraria_wiki"
if [ ! -f "search_index.pkl" ]; then
    echo "🔧 构建知识库索引..."
    python3 knowledge_search.py
fi

# 检查配置
if [ ! -f "$HOME/.local/share/Terraria/GuideAIMod/config.json" ]; then
    echo "📝 创建默认配置..."
    mkdir -p "$HOME/.local/share/Terraria/GuideAIMod"
    cat > "$HOME/.local/share/Terraria/GuideAIMod/config.json" << 'CONFIG'
{
  "ApiKey": "",
  "ApiUrl": "https://api.deepseek.com/v1/chat/completions",
  "Model": "deepseek-chat",
  "MaxTokens": 1000,
  "Temperature": 0.7,
  "EnableCache": true,
  "CacheSize": 100,
  "ShowWelcomeMessage": true
}
CONFIG
    echo "⚠️  请编辑配置文件添加 DeepSeek API Key:"
    echo "   $HOME/.local/share/Terraria/GuideAIMod/config.json"
fi

echo ""
echo "✅ 检查完成！"
echo ""
echo "启动 tModLoader..."
echo ""
echo "游戏中操作："
echo "  - 按 H 键打开 AI 向导"
echo "  - 输入问题，按发送或回车"
echo "  - 按 ESC 关闭界面"
echo ""

# 启动 tModLoader
steam steam://rungameid/1281930 2>/dev/null || echo "请手动启动 Steam → tModLoader"
