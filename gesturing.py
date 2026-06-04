#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import sys
import os
import time
import math
import json
import socket
import select
import shutil
import re
import threading
import subprocess

try:
    import evdev
except ImportError:
    print("[Error] 缺少 'evdev' 库，无法运行。")
    if os.path.exists("/usr/bin/dnf"):
        print("        请运行: sudo dnf install python3-evdev")
    elif os.path.exists("/usr/bin/pacman"):
        print("        请运行: sudo pacman -S python-evdev")
    else:
        print("        请运行: pip install evdev")
    sys.exit(1)

# 尝试加载 pystray 和 PIL 库以支持系统托盘，失败时回退到纯命令行模式
try:
    import pystray
    from PIL import Image
    HAS_TRAY = True
except Exception:
    HAS_TRAY = False

# 全局映射常量
DEBOUNCE_THRESHOLD = 10
SHORT_DISTANCE_THRESHOLD = 15
TIMEOUT_SECONDS = 3.0

KEY_MAP = {
    "ctrl": evdev.ecodes.KEY_LEFTCTRL,
    "control": evdev.ecodes.KEY_LEFTCTRL,
    "shift": evdev.ecodes.KEY_LEFTSHIFT,
    "alt": evdev.ecodes.KEY_LEFTALT,
    "win": evdev.ecodes.KEY_LEFTMETA,
    "windows": evdev.ecodes.KEY_LEFTMETA,
    "cmd": evdev.ecodes.KEY_LEFTMETA,
    "browserback": evdev.ecodes.KEY_BACK,
    "browserforward": evdev.ecodes.KEY_FORWARD,
    "browserrefresh": evdev.ecodes.KEY_REFRESH,
    "browserstop": evdev.ecodes.KEY_STOP,
    "browsersearch": evdev.ecodes.KEY_SEARCH,
    "browserfavorites": evdev.ecodes.KEY_BOOKMARKS,
    "browserhome": evdev.ecodes.KEY_HOMEPAGE,
    "esc": evdev.ecodes.KEY_ESC,
    "escape": evdev.ecodes.KEY_ESC,
    "space": evdev.ecodes.KEY_SPACE,
    "tab": evdev.ecodes.KEY_TAB,
    "enter": evdev.ecodes.KEY_ENTER,
    "return": evdev.ecodes.KEY_ENTER,
    "backspace": evdev.ecodes.KEY_BACKSPACE,
    "back": evdev.ecodes.KEY_BACKSPACE,
    "delete": evdev.ecodes.KEY_DELETE,
    "del": evdev.ecodes.KEY_DELETE,
    "insert": evdev.ecodes.KEY_INSERT,
    "ins": evdev.ecodes.KEY_INSERT,
    "home": evdev.ecodes.KEY_HOME,
    "end": evdev.ecodes.KEY_END,
    "pageup": evdev.ecodes.KEY_PAGEUP,
    "pagedown": evdev.ecodes.KEY_PAGEDOWN,
    "left": evdev.ecodes.KEY_LEFT,
    "right": evdev.ecodes.KEY_RIGHT,
    "up": evdev.ecodes.KEY_UP,
    "down": evdev.ecodes.KEY_DOWN,
    "printscreen": evdev.ecodes.KEY_PRINT,
    "pause": evdev.ecodes.KEY_PAUSE,
    "capslock": evdev.ecodes.KEY_CAPSLOCK,
    "numlock": evdev.ecodes.KEY_NUMLOCK,
    "scrolllock": evdev.ecodes.KEY_SCROLLLOCK,
}

# 动态填充 A-Z, 0-9, F1-F24 键映射
for c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ":
    KEY_MAP[c.lower()] = getattr(evdev.ecodes, f"KEY_{c}")
for i in range(10):
    KEY_MAP[str(i)] = getattr(evdev.ecodes, f"KEY_{i}")
for i in range(1, 25):
    KEY_MAP[f"f{i}"] = getattr(evdev.ecodes, f"KEY_F{i}")

def is_portable():
    script_dir = os.path.dirname(os.path.realpath(__file__))
    return os.path.exists(os.path.join(script_dir, "portable.marker"))

def get_config_dir():
    if is_portable():
        return os.path.dirname(os.path.realpath(__file__))
    return os.path.expanduser("~/.config/Gesturing")

def get_config_path():
    d = get_config_dir()
    os.makedirs(d, exist_ok=True)
    return os.path.join(d, "config.json")

def get_socket_path():
    return os.path.join(get_config_dir(), "gesturing.sock")

def show_notification(title, body):
    try:
        subprocess.Popen(["notify-send", "-a", "Gesturing", title, body])
    except Exception:
        pass

def sync_autostart(enabled):
    if is_portable():
        return
    config_dir = os.path.expanduser("~/.config")
    autostart_dir = os.path.join(config_dir, "autostart")
    os.makedirs(autostart_dir, exist_ok=True)
    autostart_path = os.path.join(autostart_dir, "gesturing.desktop")
    
    # 清理旧的 systemd 服务文件以防冲突
    systemd_path = os.path.expanduser("~/.config/systemd/user/gesturing.service")
    if os.path.exists(systemd_path):
        try:
            subprocess.run(["systemctl", "--user", "disable", "gesturing"], capture_output=True)
            os.remove(systemd_path)
        except Exception:
            pass

    if enabled:
        # 优先使用 /usr/bin/gesturing，如果不存在则使用当前运行的脚本
        exec_path = "/usr/bin/gesturing"
        if not os.path.exists(exec_path):
            script_path = os.path.abspath(sys.argv[0])
            exec_path = f"{sys.executable} {script_path}"
            
        real_script_dir = os.path.dirname(os.path.realpath(__file__))
        icon_path = os.path.join(real_script_dir, "app.png")
        if not os.path.exists(icon_path):
            icon_path = "/home/oltra/Projects/gesturing/app.png"

        content = f"""[Desktop Entry]
Type=Application
Name=Gesturing
Comment=Gesturing Mouse Gestures Daemon
Exec={exec_path}
Icon={icon_path}
Terminal=false
Categories=Utility;
X-GNOME-Autostart-enabled=true
"""
        with open(autostart_path, "w", encoding="utf-8") as f:
            f.write(content)
        print("[Info] Enabled XDG autostart by writing desktop entry.")
    else:
        if os.path.exists(autostart_path):
            try:
                os.remove(autostart_path)
            except Exception:
                pass
            print("[Info] Disabled XDG autostart by removing desktop entry.")

def register_desktop_entry():
    """将应用注册到系统应用列表中 (KDE 应用启动器菜单)"""
    apps_dir = os.path.expanduser("~/.local/share/applications")
    os.makedirs(apps_dir, exist_ok=True)
    desktop_path = os.path.join(apps_dir, "gesturing.desktop")
    
    # 优先使用 /usr/bin/gesturing，如果不存在则使用当前运行的 python 脚本
    exec_path = "/usr/bin/gesturing"
    if not os.path.exists(exec_path):
        script_path = os.path.abspath(sys.argv[0])
        exec_path = f"{sys.executable} {script_path}"
    
    # 图标路径优先使用实际安装路径 /usr/share/gesturing/app.png
    real_script_dir = os.path.dirname(os.path.realpath(__file__))
    icon_path = os.path.join(real_script_dir, "app.png")
    if not os.path.exists(icon_path):
        icon_path = "/home/oltra/Projects/gesturing/app.png"

    content = f"""[Desktop Entry]
Type=Application
Name=Gesturing
Comment=Gesturing Mouse Gestures Daemon
Exec={exec_path}
Icon={icon_path}
Terminal=false
Categories=Utility;
"""
    try:
        with open(desktop_path, "w", encoding="utf-8") as f:
            f.write(content)
        subprocess.run(["update-desktop-database", apps_dir], capture_output=True)
        print(f"[Info] Registered application shortcut at: {desktop_path}")
    except Exception as e:
        print(f"[Warning] Failed to register desktop entry: {e}")

class ActiveWindowTracker:
    def __init__(self):
        self.active_class = None
        self.script_path = "/tmp/gesturing_active_window.js"
        self.script_id = -1
        self.running = True
        self.reader_thread = None
        self.journal_proc = None
        self.watchdog_thread = None
        
        self.backend = "none"
        self.supported = False
        self.detect_backend()

    def detect_backend(self):
        session_type = os.environ.get("XDG_SESSION_TYPE", "").lower()
        desktop = os.environ.get("XDG_CURRENT_DESKTOP", "").lower()
        wayland_display = os.environ.get("WAYLAND_DISPLAY", "")
        
        is_wayland = (session_type == "wayland") or bool(wayland_display)
        
        # 1. 检测 Wayland 模式
        if is_wayland:
            # 检测 KDE Wayland
            if "kde" in desktop:
                self.backend = "kde_wayland"
                self.supported = True
                print("[Info] ActiveWindowTracker: Detected KDE Wayland, using KWin Script backend.")
                return

            # 检测 GNOME Wayland
            if "gnome" in desktop:
                self.backend = "gnome_wayland"
                # 测试 FocusedWindow D-Bus 扩展是否存在
                if self.check_gnome_extension():
                    self.supported = True
                    print("[Info] ActiveWindowTracker: Detected GNOME Wayland, using Focused Window D-Bus extension backend.")
                else:
                    self.supported = False
                    print("[Warning] ActiveWindowTracker: Detected GNOME Wayland, but 'Focused Window D-Bus' extension is not installed or enabled.")
                    print("          To restrict gestures to specific target apps, please install: https://extensions.gnome.org/extension/5592/focused-window-d-bus/")
                    print("          Gesturing will run in GLOBAL mode (apply to all windows) as a fallback.")
                return

        # 2. 检测 X11 模式
        if session_type == "x11" or os.environ.get("DISPLAY"):
            if shutil.which("xprop"):
                self.backend = "x11"
                self.supported = True
                print("[Info] ActiveWindowTracker: Detected X11 environment, using xprop backend.")
                return

        # 3. 兜底检测（如果上面环境变量不完整）
        # 尝试检测 KDE 相关的 dbus 接口是否存在
        try:
            res = subprocess.run([
                "busctl", "--user", "status", "org.kde.KWin"
            ], capture_output=True, timeout=0.5)
            if res.returncode == 0:
                self.backend = "kde_wayland"
                self.supported = True
                print("[Info] ActiveWindowTracker: Detected KWin DBus service, using KWin Script backend.")
                return
        except Exception:
            pass

        # 尝试检测 GNOME extension
        if self.check_gnome_extension():
            self.backend = "gnome_wayland"
            self.supported = True
            print("[Info] ActiveWindowTracker: Detected Focused Window D-Bus extension, using it.")
            return

        # 都没有检测到，则不支持窗口过滤，退化为全局生效
        self.backend = "none"
        self.supported = False
        print("[Warning] ActiveWindowTracker: No active window tracking backend is available.")
        print("          Gesturing will run in GLOBAL mode (apply to all windows) as a fallback.")

    def check_gnome_extension(self):
        try:
            res = subprocess.run([
                "gdbus", "call", "--session",
                "--dest", "org.gnome.Shell",
                "--object-path", "/org/gnome/shell/extensions/FocusedWindow",
                "--method", "org.gnome.shell.extensions.FocusedWindow.Get"
            ], capture_output=True, text=True, timeout=0.5)
            return res.returncode == 0
        except Exception:
            return False

    def get_active_window_gnome(self):
        try:
            res = subprocess.run([
                "gdbus", "call", "--session",
                "--dest", "org.gnome.Shell",
                "--object-path", "/org/gnome/shell/extensions/FocusedWindow",
                "--method", "org.gnome.shell.extensions.FocusedWindow.Get"
            ], capture_output=True, text=True, timeout=0.2)
            if res.returncode == 0:
                out = res.stdout.strip()
                # 典型的输出格式为: (true, '{"wm_class": "firefox", ...}') 或 ('{"wm_class": "firefox"}',)
                start = out.find('{')
                end = out.rfind('}')
                if start != -1 and end != -1:
                    data = json.loads(out[start:end+1])
                    val = data.get("wm_class") or data.get("wm_class_instance")
                    if val:
                        return val
        except Exception:
            pass
        return None

    def get_active_window_x11(self):
        try:
            res = subprocess.run(["xprop", "-root", "_NET_ACTIVE_WINDOW"], capture_output=True, text=True, timeout=0.2)
            if res.returncode == 0:
                m = re.search(r"_NET_ACTIVE_WINDOW\(WINDOW\):\s*window\s*id\s*#\s*(0x[0-9a-fA-F]+)", res.stdout)
                if m:
                    win_id = m.group(1)
                    res2 = subprocess.run(["xprop", "-id", win_id, "WM_CLASS"], capture_output=True, text=True, timeout=0.2)
                    if res2.returncode == 0:
                        m2 = re.findall(r'"([^"]+)"', res2.stdout)
                        if m2:
                            return m2[-1]
        except Exception:
            pass
        return None

    def get_active_class(self):
        if self.backend == "kde_wayland":
            return self.active_class
        elif self.backend == "gnome_wayland":
            return self.get_active_window_gnome()
        elif self.backend == "x11":
            return self.get_active_window_x11()
        return None

    def is_supported(self):
        return self.supported

    def write_script_file(self):
        script_content = """
        workspace.windowActivated.connect(function(win) {
            if (win) {
                console.log("GESTURING_ACTIVE_WINDOW:" + win.resourceClass);
            } else {
                console.log("GESTURING_ACTIVE_WINDOW:null");
            }
        });
        """
        try:
            with open(self.script_path, "w", encoding="utf-8") as f:
                f.write(script_content)
            return True
        except Exception as e:
            print(f"[Warning] Failed to write KWin script file: {e}")
            return False

    def reload_script(self):
        if self.backend != "kde_wayland" or not self.running:
            return
        print("[Info] KWin script missing or KWin restarted. Reloading KWin script...")
        
        if self.script_id >= 0:
            try:
                subprocess.run([
                    "busctl", "--user", "call", "org.kde.KWin", 
                    f"/Scripting/Script{self.script_id}", "org.kde.kwin.Script", "stop"
                ], timeout=0.5, capture_output=True)
            except Exception:
                pass
            self.script_id = -1

        if not self.write_script_file():
            return

        try:
            res = subprocess.run([
                "busctl", "--user", "call", "org.kde.KWin", "/Scripting", 
                "org.kde.kwin.Scripting", "loadScript", "ss", 
                self.script_path, "gesturing_active_window"
            ], capture_output=True, text=True, timeout=1.0)
            
            if res.returncode == 0:
                parts = res.stdout.strip().split()
                if len(parts) >= 2 and parts[0] == "i":
                    self.script_id = int(parts[1])
                    print(f"[Info] KWin active window script reloaded with ID {self.script_id}")
                    
                    subprocess.run([
                        "busctl", "--user", "call", "org.kde.KWin", 
                        f"/Scripting/Script{self.script_id}", "org.kde.kwin.Script", "run"
                    ], timeout=1.0, capture_output=True)
            else:
                print(f"[Warning] Failed to reload KWin script: {res.stderr.strip()}")
        except Exception as e:
            print(f"[Warning] Exception reloading KWin script: {e}")

    def watchdog_loop(self):
        while self.running:
            time.sleep(5.0)
            if not self.running:
                break
                
            need_reload = False
            if self.script_id < 0:
                need_reload = True
            else:
                try:
                    res = subprocess.run([
                        "busctl", "--user", "call", "org.kde.KWin", 
                        f"/Scripting/Script{self.script_id}", "org.freedesktop.DBus.Peer", "Ping"
                    ], capture_output=True, timeout=1.0)
                    if res.returncode != 0:
                        need_reload = True
                except Exception:
                    need_reload = True
            
            if need_reload and self.running:
                self.reload_script()

    def start(self):
        if self.backend != "kde_wayland":
            return
            
        self.write_script_file()

        try:
            res = subprocess.run([
                "busctl", "--user", "call", "org.kde.KWin", "/Scripting", 
                "org.kde.kwin.Scripting", "loadScript", "ss", 
                self.script_path, "gesturing_active_window"
            ], capture_output=True, text=True, timeout=1.0)
            
            if res.returncode == 0:
                parts = res.stdout.strip().split()
                if len(parts) >= 2 and parts[0] == "i":
                    self.script_id = int(parts[1])
                    print(f"[Info] KWin active window script loaded with ID {self.script_id}")
                    
                    subprocess.run([
                        "busctl", "--user", "call", "org.kde.KWin", 
                        f"/Scripting/Script{self.script_id}", "org.kde.kwin.Script", "run"
                    ], timeout=1.0, capture_output=True)
            else:
                print(f"[Warning] Failed to load KWin active window script: {res.stderr.strip()}")
                self.supported = False
        except Exception as e:
            print(f"[Warning] Exception loading KWin script: {e}")
            self.supported = False

        if self.supported:
            self.running = True
            self.reader_thread = threading.Thread(target=self.journal_reader, daemon=True)
            self.reader_thread.start()
            
            self.watchdog_thread = threading.Thread(target=self.watchdog_loop, daemon=True)
            self.watchdog_thread.start()

    def journal_reader(self):
        try:
            self.journal_proc = subprocess.Popen(
                ["journalctl", "--user", "--since", "now", "-f", "-o", "cat"],
                stdout=subprocess.PIPE,
                stderr=subprocess.DEVNULL,
                text=True,
                bufsize=1
            )
            for line in self.journal_proc.stdout:
                if not self.running:
                    break
                line = line.strip()
                if "GESTURING_ACTIVE_WINDOW:" in line:
                    parts = line.split("GESTURING_ACTIVE_WINDOW:")
                    if len(parts) >= 2:
                        val = parts[1].strip()
                        self.active_class = None if val == "null" else val
        except Exception as e:
            if self.running:
                print(f"[Warning] Journalctl reader exception: {e}")

    def stop(self):
        self.running = False
        if self.journal_proc:
            try:
                self.journal_proc.terminate()
            except Exception:
                pass
        
        if self.script_id >= 0:
            try:
                subprocess.run([
                    "busctl", "--user", "call", "org.kde.KWin", 
                    f"/Scripting/Script{self.script_id}", "org.kde.kwin.Script", "stop"
                ], timeout=1.0, capture_output=True)
                print(f"[Info] KWin active window script stopped.")
            except Exception as e:
                pass
            self.script_id = -1
            
        try:
            if os.path.exists(self.script_path):
                os.remove(self.script_path)
        except Exception:
            pass

class GesturingDaemon:
    def __init__(self):
        self.config = {}
        self.is_enabled = True
        self.target_processes = set()
        self.rules = []
        
        self.devices = []
        self.ui_mouse = None
        self.ui_kbd = None
        self.running = True
        self.write_lock = threading.Lock()
        
        self.tracker = ActiveWindowTracker()
        
        self.in_gesture = False
        self.current_x = 0
        self.current_y = 0
        self.path_points = []
        self.directions = []
        self.gesture_start_time = 0
        self.timeout_timer = None
        self.tray_icon = None

    def load_config(self):
        path = get_config_path()
        if not os.path.exists(path):
            local_config = os.path.join(os.path.dirname(os.path.realpath(__file__)), "config.json")
            if os.path.exists(local_config):
                shutil.copy(local_config, path)
            else:
                default_conf = {
                    "auto_start": False,
                    "is_enabled": True,
                    "target_processes": ["chrome", "firefox", "msedge", "google-chrome", "konsole", "ghostty"],
                    "rules": [
                        {"gesture_sequence": ["L"], "hotkey": {"modifiers": [], "key": "BrowserBack"}},
                        {"gesture_sequence": ["R"], "hotkey": {"modifiers": [], "key": "BrowserForward"}},
                        {"gesture_sequence": ["D", "R"], "hotkey": {"modifiers": ["Ctrl"], "key": "W"}},
                        {"gesture_sequence": ["D", "L"], "hotkey": {"modifiers": ["Ctrl", "Shift"], "key": "T"}}
                    ]
                }
                with open(path, "w", encoding="utf-8") as f:
                    json.dump(default_conf, f, indent=2)

        try:
            with open(path, "r", encoding="utf-8") as f:
                self.config = json.load(f)
            self.is_enabled = self.config.get("is_enabled", True)
            self.target_processes = {p.lower() for p in self.config.get("target_processes", [])}
            self.target_processes = {p[:-4] if p.endswith(".exe") else p for p in self.target_processes}
            self.rules = self.config.get("rules", [])
            print(f"[Info] Config loaded. Rules: {len(self.rules)}, Targets: {list(self.target_processes)}")
        except Exception as e:
            print(f"[Error] Failed to load config: {e}")

    def reload_config(self):
        self.load_config()
        sync_autostart(self.config.get("auto_start", False))

    def set_enabled(self, enabled):
        self.is_enabled = enabled
        self.config["is_enabled"] = enabled
        self.save_config()
        print(f"[Info] Gesturing enabled set to: {enabled}")

    def save_config(self):
        try:
            with open(get_config_path(), "w", encoding="utf-8") as f:
                json.dump(self.config, f, indent=2)
        except Exception as e:
            print(f"[Error] Failed to save config: {e}")

    def find_mouse_devices(self):
        devices = []
        try:
            for path in evdev.list_devices():
                try:
                    dev = evdev.InputDevice(path)
                    if dev.name.startswith("Gesturing"):
                        continue
                    
                    caps = dev.capabilities()
                    if evdev.ecodes.EV_REL in caps and evdev.ecodes.EV_KEY in caps:
                        keys = caps[evdev.ecodes.EV_KEY]
                        if evdev.ecodes.BTN_RIGHT in keys:
                            devices.append(dev)
                except Exception:
                    pass
        except Exception as e:
            print(f"[Error] Scanning devices failed: {e}")
        return devices

    def setup_uinput(self):
        mouse_cap = {
            evdev.ecodes.EV_REL: [
                evdev.ecodes.REL_X,
                evdev.ecodes.REL_Y,
                evdev.ecodes.REL_WHEEL,
                evdev.ecodes.REL_HWHEEL
            ],
            evdev.ecodes.EV_KEY: [
                evdev.ecodes.BTN_LEFT,
                evdev.ecodes.BTN_RIGHT,
                evdev.ecodes.BTN_MIDDLE,
                evdev.ecodes.BTN_SIDE,
                evdev.ecodes.BTN_EXTRA,
                evdev.ecodes.BTN_FORWARD,
                evdev.ecodes.BTN_BACK
            ]
        }
        
        kbd_cap = {
            evdev.ecodes.EV_KEY: [
                *range(1, 256),       
                *range(183, 195)     
            ]
        }

        try:
            self.ui_mouse = evdev.UInput(mouse_cap, name="Gesturing Virtual Mouse", vendor=0x1234, product=0x5678)
            self.ui_kbd = evdev.UInput(kbd_cap, name="Gesturing Virtual Keyboard", vendor=0x1234, product=0x5679)
            print("[Info] Virtual mouse and keyboard created successfully.")
        except PermissionError:
            print("[Error] 无权限写入 /dev/uinput。")
            print("        请运行: sudo gpasswd -a $USER input 并重启会话。")
            sys.exit(1)

    def start(self):
        self.load_config()
        self.setup_uinput()
        
        sync_autostart(self.config.get("auto_start", False))
        register_desktop_entry()
        
        self.tracker.start()
        
        if not self.tracker.is_supported():
            desktop = os.environ.get("XDG_CURRENT_DESKTOP", "").lower()
            if "gnome" in desktop:
                show_notification(
                    "Gesturing 运行提示", 
                    "未检测到 Focused Window D-Bus 扩展，手势将全局生效。您可以安装该扩展以支持应用过滤。"
                )
            else:
                show_notification(
                    "Gesturing 运行提示", 
                    "未检测到活动窗口跟踪器，手势将全局生效。"
                )

        self.devices = self.find_mouse_devices()
        if not self.devices:
            print("[Warning] 未检测到物理鼠标事件设备！")
            print("          请确保您的用户已在 input 组中: groups 并检查 /dev/input/ 的权限。")
            
        for dev in self.devices:
            print(f"[Info] Hooking and grabbing: {dev.name} ({dev.path})")
            t = threading.Thread(target=self.device_loop, args=(dev,), daemon=True)
            t.start()

    def stop(self):
        self.running = False
        self.tracker.stop()
        if self.ui_mouse:
            self.ui_mouse.close()
            self.ui_mouse = None
        if self.ui_kbd:
            self.ui_kbd.close()
            self.ui_kbd = None
        if self.tray_icon:
            try:
                self.tray_icon.stop()
            except Exception:
                pass
        print("[Info] Stopping gesturing daemon...")

    def simulate_right_click(self):
        with self.write_lock:
            if self.ui_mouse:
                self.ui_mouse.write(evdev.ecodes.EV_KEY, evdev.ecodes.BTN_RIGHT, 1)
                self.ui_mouse.syn()
                self.ui_mouse.write(evdev.ecodes.EV_KEY, evdev.ecodes.BTN_RIGHT, 0)
                self.ui_mouse.syn()
        print("[Info] Simulated right-click.")

    def trigger_hotkey(self, hotkey):
        mods = [KEY_MAP.get(m.lower()) for m in hotkey.get("modifiers", []) if m.lower() in KEY_MAP]
        key_code = KEY_MAP.get(hotkey.get("key", "").lower())
        if not key_code:
            return

        print(f"[Info] Triggering hotkey: {hotkey.get('modifiers', [])} + {hotkey.get('key')}")

        with self.write_lock:
            if self.ui_kbd:
                for mod in mods:
                    if mod:
                        self.ui_kbd.write(evdev.ecodes.EV_KEY, mod, 1)
                if mods:
                    self.ui_kbd.syn()

                self.ui_kbd.write(evdev.ecodes.EV_KEY, key_code, 1)
                self.ui_kbd.syn()
                
                self.ui_kbd.write(evdev.ecodes.EV_KEY, key_code, 0)
                self.ui_kbd.syn()

                for mod in reversed(mods):
                    if mod:
                        self.ui_kbd.write(evdev.ecodes.EV_KEY, mod, 0)
                if mods:
                    self.ui_kbd.syn()

    def handle_timeout(self):
        if self.in_gesture:
            print("[Info] Gesture timeout (3s). Triggering fallback right-click.")
            self.in_gesture = False
            self.simulate_right_click()

    def analyze_path(self, points):
        if not points:
            return []

        directions = []
        
        # 宽容度阈值设定
        START_THRESHOLD = 30  # 建立第一段方向所需的最小位移
        TURN_THRESHOLD = 30   # 触发转向所需的垂直/反向位移
        
        stroke_dir = None
        ext_idx = 0
        
        # 1. 寻找第一个确定的方向段
        i = 0
        n = len(points)
        while i < n:
            pt = points[i]
            dx = pt[0] - points[0][0]
            dy = pt[1] - points[0][1]
            abs_dx = abs(dx)
            abs_dy = abs(dy)
            
            if abs_dx >= START_THRESHOLD or abs_dy >= START_THRESHOLD:
                # 引入 1.3 倍的轴向主导限制，防止轻微斜向抖动误判
                if abs_dx > abs_dy * 1.3:
                    stroke_dir = "R" if dx > 0 else "L"
                elif abs_dy > abs_dx * 1.3:
                    stroke_dir = "D" if dy > 0 else "U"
                
                if stroke_dir:
                    directions.append(stroke_dir)
                    ext_idx = i
                    break
            i += 1
            
        if not stroke_dir:
            return []

        # 2. 遍历后续点，更新极值点并进行转向判定
        i = ext_idx + 1
        while i < n:
            pt = points[i]
            ext_pt = points[ext_idx]
            
            # 更新当前方向的极值点 (最远点)
            if stroke_dir == "R":
                if pt[0] > ext_pt[0]:
                    ext_idx = i
            elif stroke_dir == "L":
                if pt[0] < ext_pt[0]:
                    ext_idx = i
            elif stroke_dir == "D":
                if pt[1] > ext_pt[1]:
                    ext_idx = i
            elif stroke_dir == "U":
                if pt[1] < ext_pt[1]:
                    ext_idx = i

            # 使用更新后的极值点计算相对位移
            ext_pt = points[ext_idx]
            ex = pt[0] - ext_pt[0]
            ey = pt[1] - ext_pt[1]
            
            new_dir = None
            if stroke_dir in ("R", "L"):
                # 横向移动时：检测纵向位移是否超限 (90度转向)
                if abs(ey) >= TURN_THRESHOLD:
                    new_dir = "D" if ey > 0 else "U"
                # 检测反向移动是否超限 (180度转向)
                elif stroke_dir == "R" and ex <= -TURN_THRESHOLD:
                    new_dir = "L"
                elif stroke_dir == "L" and ex >= TURN_THRESHOLD:
                    new_dir = "R"
            else:
                # 纵向移动时：检测横向位移是否超限 (90度转向)
                if abs(ex) >= TURN_THRESHOLD:
                    new_dir = "R" if ex > 0 else "L"
                # 检测反向移动是否超限 (180度转向)
                elif stroke_dir == "D" and ey <= -TURN_THRESHOLD:
                    new_dir = "U"
                elif stroke_dir == "U" and ey >= TURN_THRESHOLD:
                    new_dir = "D"

            if new_dir:
                directions.append(new_dir)
                stroke_dir = new_dir
                ext_idx = i
                if len(directions) >= 4:
                    break
            i += 1

        return directions

    def process_event(self, event):
        forward = True

        if event.type == evdev.ecodes.EV_KEY:
            if event.code == evdev.ecodes.BTN_RIGHT:
                if event.value == 1: # Down
                    self.in_gesture = False
                    if self.is_enabled:
                        active_win = self.tracker.get_active_class()
                        is_target = False
                        if self.tracker.is_supported():
                            if active_win and active_win.lower() in self.target_processes:
                                is_target = True
                        else:
                            is_target = True
                            
                        if is_target:
                            self.in_gesture = True
                            forward = False

                            self.current_x = 0
                            self.current_y = 0
                            self.path_points = [(0, 0)]
                            self.directions = []
                            self.gesture_start_time = time.time()
                            
                            if self.timeout_timer:
                                self.timeout_timer.cancel()
                            self.timeout_timer = threading.Timer(TIMEOUT_SECONDS, self.handle_timeout)
                            self.timeout_timer.start()
                
                elif event.value == 0: # Up
                    if self.in_gesture:
                        self.in_gesture = False
                        forward = False
                        
                        if self.timeout_timer:
                            self.timeout_timer.cancel()

                        elapsed = time.time() - self.gesture_start_time
                        
                        # 释放时进行离线路径 analysis
                        self.directions = self.analyze_path(self.path_points)

                        if elapsed > TIMEOUT_SECONDS or not self.directions:
                            self.simulate_right_click()
                        else:
                            matched = None
                            for rule in self.rules:
                                seq = rule.get("gesture_sequence", [])
                                if [x.lower() for x in seq] == [x.lower() for x in self.directions]:
                                    matched = rule
                                    break
                            
                            if matched:
                                self.trigger_hotkey(matched["hotkey"])
                            else:
                                print(f"[Info] Unknown gesture sequence: {' → '.join(self.directions)}")
                                self.simulate_right_click()
                                
        elif event.type == evdev.ecodes.EV_REL:
            if event.code in (evdev.ecodes.REL_X, evdev.ecodes.REL_Y):
                if self.in_gesture:
                    val = event.value
                    if event.code == evdev.ecodes.REL_X:
                        self.current_x += val
                    else:
                        self.current_y += val
                    
                    self.path_points.append((self.current_x, self.current_y))

        if forward:
            if event.type == evdev.ecodes.EV_SYN:
                with self.write_lock:
                    if self.ui_mouse:
                        self.ui_mouse.syn()
                    if self.ui_kbd:
                        self.ui_kbd.syn()
            elif event.type == evdev.ecodes.EV_REL:
                if event.code in (evdev.ecodes.REL_X, evdev.ecodes.REL_Y, evdev.ecodes.REL_WHEEL, evdev.ecodes.REL_HWHEEL):
                    with self.write_lock:
                        if self.ui_mouse:
                            self.ui_mouse.write(event.type, event.code, event.value)
            elif event.type == evdev.ecodes.EV_KEY:
                is_key = event.code < 0x110
                target_ui = self.ui_kbd if is_key else self.ui_mouse
                with self.write_lock:
                    if target_ui:
                        target_ui.write(event.type, event.code, event.value)

    def device_loop(self, device):
        try:
            device.grab()
            for event in device.read_loop():
                if not self.running:
                    break
                self.process_event(event)
        except Exception as e:
            print(f"[Warning] Device loop exception on {device.path}: {e}")
        finally:
            try:
                device.ungrab()
            except Exception:
                pass

def ipc_server_loop(socket_path, daemon):
    if os.path.exists(socket_path):
        try:
            os.remove(socket_path)
        except Exception:
            pass

    server = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    try:
        server.bind(socket_path)
        server.listen(5)
    except Exception as e:
        print(f"[Error] Failed to bind Unix IPC socket: {e}")
        return

    while daemon.running:
        r, _, _ = select.select([server], [], [], 0.5)
        if server in r:
            conn, _ = server.accept()
            try:
                data = conn.recv(1024)
                if data:
                    cmd = data.decode("utf-8").strip().lower()
                    print(f"[IPC] Received command: {cmd}")
                    if cmd in ("reload", "--reload"):
                        daemon.reload_config()
                        show_notification("Gesturing", "配置文件已重新加载")
                    elif cmd in ("enable", "--enable"):
                        daemon.set_enabled(True)
                        show_notification("Gesturing", "鼠标手势已启用")
                    elif cmd in ("disable", "--disable"):
                        daemon.set_enabled(False)
                        show_notification("Gesturing", "鼠标手势已禁用")
                    elif cmd in ("stop", "--stop", "exit", "--exit"):
                        show_notification("Gesturing", "鼠标手势服务已退出")
                        daemon.stop()
            except Exception as e:
                print(f"[Warning] IPC connection error: {e}")
            finally:
                conn.close()

    server.close()
    try:
        os.remove(socket_path)
    except Exception:
        pass

def send_command(socket_path, cmd):
    try:
        client = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        client.connect(socket_path)
        client.sendall(cmd.encode("utf-8"))
        client.close()
        print(f"[IPC] Sent command '{cmd}' to running instance.")
        return True
    except Exception as e:
        print(f"[Error] Failed to send command: {e}")
        return False

def check_single_instance(socket_path):
    if os.path.exists(socket_path):
        try:
            client = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            client.connect(socket_path)
            client.close()
            return True
        except Exception:
            try:
                os.remove(socket_path)
            except Exception:
                pass
    return False

def create_tray_icon(daemon):
    """创建 pystray 系统托盘图标及其右键菜单"""
    icon_path = os.path.join(os.path.dirname(os.path.realpath(__file__)), "app.png")
    image = Image.open(icon_path)

    def on_toggle_enable(icon, item):
        daemon.set_enabled(not daemon.is_enabled)

    def on_toggle_autostart(icon, item):
        auto_start = not daemon.config.get("auto_start", False)
        daemon.config["auto_start"] = auto_start
        daemon.save_config()
        sync_autostart(auto_start)

    def on_open_config(icon, item):
        config_path = get_config_path()
        try:
            subprocess.Popen(["xdg-open", config_path])
        except Exception:
            try:
                subprocess.Popen(["kate", config_path])
            except Exception:
                pass

    def on_reload(icon, item):
        daemon.reload_config()

    def on_exit(icon, item):
        daemon.stop()

    menu = pystray.Menu(
        pystray.MenuItem("启用", on_toggle_enable, checked=lambda item: daemon.is_enabled),
        pystray.MenuItem("开机自启", on_toggle_autostart, checked=lambda item: daemon.config.get("auto_start", False)),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("打开配置文件", on_open_config),
        pystray.MenuItem("刷新配置", on_reload),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("退出", on_exit)
    )

    icon = pystray.Icon("Gesturing", image, "Gesturing", menu)
    daemon.tray_icon = icon
    return icon

def main():
    socket_path = get_socket_path()

    if len(sys.argv) > 1:
        cmd = sys.argv[1].strip().lower()
        if cmd in ("--help", "-h"):
            print("Gesturing Linux Daemon (Python version)")
            print("Usage:")
            print("  python gesturing.py           Start the daemon in foreground")
            print("  python gesturing.py --enable  Enable gesturing globally")
            print("  python gesturing.py --disable Disable gesturing globally")
            print("  python gesturing.py --reload  Reload configuration from file")
            print("  python gesturing.py --stop    Stop the running daemon cleanly")
            return
        
        send_command(socket_path, cmd)
        return

    if check_single_instance(socket_path):
        print("[Error] Gesturing daemon is already running.")
        show_notification("Gesturing", "Gesturing 已经在运行中。")
        sys.exit(1)

    daemon = GesturingDaemon()
    daemon.start()

    ipc_thread = threading.Thread(target=ipc_server_loop, args=(socket_path, daemon), daemon=True)
    ipc_thread.start()

    # show_notification("Gesturing", "鼠标手势服务已启动")
    print("[Info] Gesturing daemon started successfully. Press Ctrl+C to exit.")

    if HAS_TRAY:
        print("[Info] Starting system tray icon...")
        icon = create_tray_icon(daemon)
        icon.run() # 阻塞主线程直到托盘退出
    else:
        print("[Warning] pystray 或 Pillow 库未安装，运行在纯命令行模式。")
        if os.path.exists("/usr/bin/dnf"):
            print("          请运行: sudo dnf install python3-pystray python3-pillow 安装托盘库。")
        elif os.path.exists("/usr/bin/pacman"):
            print("          请运行: sudo pacman -S python-pystray python-pillow 安装托盘库。")
        else:
            print("          请运行: pip install pystray Pillow")
        try:
            while daemon.running:
                time.sleep(0.5)
        except KeyboardInterrupt:
            print("[Info] Ctrl+C received.")
            show_notification("Gesturing", "鼠标手势服务已退出")
        finally:
            daemon.stop()
            time.sleep(0.2)

if __name__ == "__main__":
    main()
