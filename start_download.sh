#!/bin/bash
# Terraria Wiki 下载启动脚本
# 使用方法: ./start_download.sh [选项]
#   --fg      前台运行
#   --bg      后台运行（默认）
#   --retry   重试失败的页面
#   --reset   重置进度
#   --status  查看状态

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 目录设置
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

show_status() {
    echo -e "${GREEN}=== 下载状态 ===${NC}"
    
    if [ -f "download_progress.json" ]; then
        echo -e "${YELLOW}进度文件: ${NC}✓ 存在"
        
        # 解析JSON
        if command -v python3 &> /dev/null; then
            python3 -c "
import json
import sys
try:
    with open('download_progress.json', 'r') as f:
        d = json.load(f)
    total = d.get('total_pages', 0)
    downloaded = len(d.get('downloaded_titles', []))
    failed = len(d.get('failed_titles', []))
    percent = (downloaded / total * 100) if total > 0 else 0
    
    print(f'总页面数: {total}')
    print(f'已下载: {downloaded} ({percent:.1f}%)')
    print(f'失败: {failed}')
    
    start = d.get('start_time', 'N/A')
    last = d.get('last_update', 'N/A')
    print(f'开始时间: {start}')
    print(f'最后更新: {last}')
except Exception as e:
    print(f'读取进度文件出错: {e}')
"
        fi
    else
        echo -e "${YELLOW}进度文件: ${NC}✗ 不存在（首次运行）"
    fi
    
    if [ -d "wiki_full_data" ]; then
        count=$(ls -1 wiki_full_data/ 2>/dev/null | wc -l)
        size=$(du -sh wiki_full_data/ 2>/dev/null | cut -f1)
        echo -e "${YELLOW}数据目录: ${NC}$count 个文件, $size"
    else
        echo -e "${YELLOW}数据目录: ${NC}✗ 不存在"
    fi
    
    # 检查运行中的进程
    if pgrep -f "download_wiki_full.py" > /dev/null; then
        echo -e "${GREEN}下载进程: ${NC}✓ 正在运行 (PID: $(pgrep -f "download_wiki_full.py"))"
    else
        echo -e "${YELLOW}下载进程: ${NC}✗ 未运行"
    fi
    
    echo -e "${GREEN}================${NC}"
}

start_fg() {
    echo -e "${GREEN}前台启动下载...${NC}"
    echo "按 Ctrl+C 暂停（会自动保存进度）"
    echo ""
    python3 crawler/download_wiki_full.py
}

start_bg() {
    echo -e "${GREEN}后台启动下载...${NC}"
    
    # 检查是否已在运行
    if pgrep -f "download_wiki_full.py" > /dev/null; then
        echo -e "${YELLOW}警告: 下载进程已在运行！${NC}"
        echo "使用 tail -f download.log 查看日志"
        exit 1
    fi
    
    # 启动
    nohup python3 crawler/download_wiki_full.py > download.log 2>&1 &
    PID=$!
    
    echo -e "${GREEN}✓ 下载进程已启动 (PID: $PID)${NC}"
    echo ""
    echo "常用命令:"
    echo "  tail -f download.log    # 查看实时日志"
    echo "  ./start_download.sh --status  # 查看状态"
    echo "  kill $PID               # 停止下载"
    echo ""
    echo "等待3秒后开始输出日志..."
    sleep 3
    tail -f download.log
}

start_retry() {
    echo -e "${GREEN}重试失败的页面...${NC}"
    python3 crawler/download_wiki_full.py --retry
}

reset_progress() {
    echo -e "${RED}警告: 这将删除所有进度！${NC}"
    read -p "确定要重置吗？输入 'yes' 确认: " confirm
    if [ "$confirm" = "yes" ]; then
        rm -f download_progress.json
        echo -e "${GREEN}✓ 进度已重置${NC}"
    else
        echo "已取消"
    fi
}

show_help() {
    cat << EOF
Terraria Wiki 下载管理脚本

用法: ./start_download.sh [选项]

选项:
    --fg, -f       前台运行（实时显示输出）
    --bg, -b       后台运行（默认，推荐）
    --retry, -r    重试之前失败的页面
    --reset        重置进度（删除进度文件）
    --status, -s   查看当前状态
    --help, -h     显示此帮助

示例:
    ./start_download.sh              # 后台开始下载
    ./start_download.sh --fg         # 前台运行
    ./start_download.sh --status     # 查看状态
    tail -f download.log             # 查看后台日志

数据将保存在: wiki_full_data/
进度保存在: download_progress.json

EOF
}

# 主逻辑
case "${1:-}" in
    --fg|-f)
        start_fg
        ;;
    --bg|-b|"")
        start_bg
        ;;
    --retry|-r)
        start_retry
        ;;
    --reset)
        reset_progress
        ;;
    --status|-s)
        show_status
        ;;
    --help|-h)
        show_help
        ;;
    *)
        echo -e "${RED}未知选项: $1${NC}"
        show_help
        exit 1
        ;;
esac
