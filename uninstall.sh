#!/usr/bin/env bash
# Gesturing Mouse Gestures Daemon Uninstaller
# Author: Antigravity

set -e

# Terminals colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0;0m' # No Color

echo -e "${BLUE}==============================================${NC}"
echo -e "${BLUE}        Gesturing Daemon Uninstaller          ${NC}"
echo -e "${BLUE}==============================================${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}[Error] Please run this script with sudo:${NC}"
  echo -e "        sudo ./uninstall.sh"
  exit 1
fi

# Remove application files
echo -e "${GREEN}[Info] Removing application files...${NC}"
rm -rf /usr/share/gesturing

# Remove executable symlink
echo -e "${GREEN}[Info] Removing executable symlink...${NC}"
rm -f /usr/bin/gesturing

# Remove desktop application shortcut
echo -e "${GREEN}[Info] Removing system desktop entry...${NC}"
rm -f /usr/share/applications/gesturing.desktop

# Remove udev rules
echo -e "${GREEN}[Info] Removing udev rules...${NC}"
rm -f /etc/udev/rules.d/99-gesturing-uinput.rules

# Reload udev rules
echo -e "${GREEN}[Info] Reloading udev rules...${NC}"
udevadm control --reload-rules
udevadm trigger

echo -e "${BLUE}==============================================${NC}"
echo -e "${GREEN}[Success] Gesturing has been successfully uninstalled.${NC}"
echo -e "${BLUE}==============================================${NC}"
