#!/usr/bin/env bash
# Gesturing Mouse Gestures Daemon Installer for Fedora and Arch Linux
# Author: Antigravity

set -e

# Terminals colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0;0m' # No Color

echo -e "${BLUE}==============================================${NC}"
echo -e "${BLUE}     Gesturing Daemon Installer (Fedora/Arch) ${NC}"
echo -e "${BLUE}==============================================${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}[Error] Please run this script with sudo:${NC}"
  echo -e "        sudo ./install.sh"
  exit 1
fi

# Detect package manager and install dependencies
if [ -x "$(command -v dnf)" ]; then
  echo -e "${GREEN}[Info] Fedora detected. Installing dependencies via dnf...${NC}"
  dnf install -y python3-evdev python3-pystray python3-pillow
elif [ -x "$(command -v pacman)" ]; then
  echo -e "${GREEN}[Info] Arch Linux detected. Installing dependencies via pacman...${NC}"
  pacman -S --needed --noconfirm python-evdev python-pystray python-pillow
else
  echo -e "${YELLOW}[Warning] Neither dnf nor pacman detected. Please ensure Python dependencies are installed:${NC}"
  echo -e "          python-evdev, python-pystray, python-pillow"
fi

# Create target directories and install files
echo -e "${GREEN}[Info] Installing application files to /usr/share/gesturing/...${NC}"
mkdir -p /usr/share/gesturing
cp gesturing.py /usr/share/gesturing/gesturing.py
cp config.json /usr/share/gesturing/config.json
cp app.png /usr/share/gesturing/app.png
chmod +x /usr/share/gesturing/gesturing.py

# Create bin symlink
echo -e "${GREEN}[Info] Creating executable symlink at /usr/bin/gesturing...${NC}"
ln -sf /usr/share/gesturing/gesturing.py /usr/bin/gesturing

# Create system application desktop shortcut
echo -e "${GREEN}[Info] Creating system desktop entry...${NC}"
mkdir -p /usr/share/applications
cat <<EOF > /usr/share/applications/gesturing.desktop
[Desktop Entry]
Type=Application
Name=Gesturing
Comment=Gesturing Mouse Gestures Daemon
Exec=/usr/bin/gesturing
Icon=/usr/share/gesturing/app.png
Terminal=false
Categories=Utility;
EOF

# Install udev rule for /dev/uinput
echo -e "${GREEN}[Info] Installing udev rules for /dev/uinput access...${NC}"
cat <<EOF > /etc/udev/rules.d/99-gesturing-uinput.rules
# Allow members of the input group to write to uinput (required for Gesturing daemon)
KERNEL=="uinput", GROUP="input", MODE="0660", OPTIONS+="static_node=uinput"
EOF

# Reload udev rules
echo -e "${GREEN}[Info] Reloading udev rules...${NC}"
udevadm control --reload-rules
udevadm trigger

# Configure input group for current user
if [ -n "$SUDO_USER" ]; then
  echo -e "${GREEN}[Info] Adding user '$SUDO_USER' to 'input' group...${NC}"
  usermod -aG input "$SUDO_USER"
  
  # Check if uinput module needs to be auto-loaded at boot
  if [ ! -f /etc/modules-load.d/uinput.conf ]; then
    echo -e "${GREEN}[Info] Configuring 'uinput' kernel module to load at boot...${NC}"
    echo "uinput" > /etc/modules-load.d/uinput.conf
    # Try loading it now
    modprobe uinput || true
  fi
  
  echo -e "${BLUE}==============================================${NC}"
  echo -e "${GREEN}[Success] Installation completed successfully!${NC}"
  echo -e "${YELLOW}[Important] Please REBOOT or LOG OUT and LOG IN again${NC}"
  echo -e "            for the 'input' group privileges to take effect."
  echo -e "${BLUE}==============================================${NC}"
else
  echo -e "${BLUE}==============================================${NC}"
  echo -e "${GREEN}[Success] Installation completed successfully!${NC}"
  echo -e "${YELLOW}[Warning] Could not detect original non-root user (SUDO_USER was empty).${NC}"
  echo -e "          Please manually add your user to the 'input' group:"
  echo -e "          sudo usermod -aG input <username>"
  echo -e "          And then log out and log in again."
  echo -e "${BLUE}==============================================${NC}"
fi
