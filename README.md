# Gesturing 🐭

轻量级、面向 KDE Wayland 的后台鼠标手势守护进程 (A lightweight background mouse gestures daemon for KDE Wayland)。

目前支持 Arch Linux (EndeavourOS) 和 Fedora。

---

## 功能特性

- **Wayland 原生支持**：使用 Linux `evdev` 直接读取输入事件，并模拟键盘/鼠标操作，无需依赖 X11 (XWayland)。
- **KDE 窗口检测**：利用 KWin DBus 脚本，能获取当前激活的窗口类名（如 `chrome`, `konsole`），从而实现仅在特定应用中启用手势。
- **系统托盘**：使用 `pystray` 提供了系统托盘菜单，支持快捷关闭、重启配置和开关开机自启。
- **静默启动**：默认启动时无弹窗通知骚扰（适合放入开机自启），仅在重载配置、启用/禁用手势或退出时提供通知反馈。
- **IPC 通信**：支持从终端通过命令行发送 `--reload`, `--enable`, `--disable`, `--stop` 指令来控制已在运行的后台实例。

---

## 依赖要求

本程序依赖以下 Python 库：
- `python-evdev`
- `python-pystray`
- `python-pillow`

### 包管理器安装

#### Fedora
```bash
sudo dnf install python3-evdev python3-pystray python3-pillow
```

#### Arch Linux (EndeavourOS)
```bash
sudo pacman -S python-evdev python-pystray python-pillow
```

---

## 安装方法

我们提供了多种安装选项，以便于迁移与维护。

### 选项 1：使用通用安装脚本（推荐，最快）

该脚本将自动检测你的发行版（Fedora/Arch），安装依赖项，设置 `/dev/uinput` 权限，并全局安装本软件：

```bash
# 赋予执行权限并运行
chmod +x install.sh
sudo ./install.sh
```

> [!IMPORTANT]
> 安装完成后，你必须**注销当前会话或重启电脑**，以便让你所属的 `input` 用户组权限生效。

若要卸载，请运行：
```bash
sudo ./uninstall.sh
```

---

### 选项 2：在 Fedora 下构建并安装 RPM 包

如果你在 Fedora 下偏好使用 RPM 包管理器来管理软件：

1. **安装构建工具**：
   ```bash
   sudo dnf install rpm-build rpmdevtools
   ```

2. **准备打包目录**：
   ```bash
   rpmdev-setuptree
   ```

3. **打包源码**：
   在项目根目录下，运行以下指令创建源码归档：
   ```bash
   tar --transform 's,^,gesturing-1.0.0/,' -czf ~/rpmbuild/SOURCES/gesturing-1.0.0.tar.gz gesturing.py config.json app.png
   ```

4. **构建 RPM**：
   ```bash
   rpmbuild -ba gesturing.spec
   ```

5. **安装构建好的 RPM**：
   ```bash
   sudo dnf install ~/rpmbuild/RPMS/noarch/gesturing-1.0.0-1*.rpm
   ```

---

### 选项 3：在 Arch Linux / EndeavourOS 下构建安装

你可以继续使用原有的 `PKGBUILD` 构建：

```bash
makepkg -si
```

---

## 权限配置说明 (Uinput)

手势守护进程通过模拟 `/dev/uinput` 来发送全局快捷键。默认情况下，Fedora 对 `/dev/uinput` 有严格的 root 权限限制。

我们的 `install.sh` 脚本和 RPM 包会自动安装以下 udev 规则文件：
`/etc/udev/rules.d/99-gesturing-uinput.rules`

其内容为：
```udev
KERNEL=="uinput", GROUP="input", MODE="0660", OPTIONS+="static_node=uinput"
```

只要确保你的用户已被加入 `input` 组，就可以直接作为普通用户启动 Gesturing：
```bash
sudo usermod -aG input $USER
```
*(注意：组更改在重新登录后生效)*

---

## 配置文件说明

默认配置文件保存在 `~/.config/Gesturing/config.json`。

常见配置字段：
- `auto_start`: `true`/`false` - 是否开机自动启动。
- `is_enabled`: `true`/`false` - 默认是否启用鼠标手势。
- `target_processes`: 字符串列表 - 仅在此列表内的进程中（例如 `konsole`, `firefox`, `chrome`）才拦截右键并触发手势。
- `rules`: 手势动作和绑定快捷键的映射表。
  - `gesture_sequence`: 手势方向序列。`L` (左), `R` (右), `U` (上), `D` (下)。
  - `hotkey`: 绑定的键盘按键。支持 `modifiers`（修饰键，如 `Ctrl`, `Shift`, `Alt`, `Win`）与 `key`（如 `W`, `T`, `BrowserBack` 等）。
