# Maintainer: Oltra <oltra@example.com>
pkgname=gesturing
pkgver=1.0.0
pkgrel=1
pkgdesc="A lightweight background mouse gestures daemon for KDE Wayland"
arch=('x86_64')
url="https://github.com/oltra/gesturing"
license=('MIT')
depends=('python' 'python-evdev' 'python-pystray' 'python-pillow')
source=('gesturing.py' 'config.json' 'app.png')
sha256sums=('SKIP' 'SKIP' 'SKIP')

package() {
  # 1. 安装主守护进程、默认配置和图标到 /usr/share/gesturing/
  install -d "${pkgdir}/usr/share/gesturing"
  install -m755 "${srcdir}/gesturing.py" "${pkgdir}/usr/share/gesturing/gesturing.py"
  install -m644 "${srcdir}/config.json" "${pkgdir}/usr/share/gesturing/config.json"
  install -m644 "${srcdir}/app.png" "${pkgdir}/usr/share/gesturing/app.png"

  # 2. 创建 /usr/bin/gesturing 可执行软链接
  install -d "${pkgdir}/usr/bin"
  ln -sf /usr/share/gesturing/gesturing.py "${pkgdir}/usr/bin/gesturing"

  # 3. 安装系统应用快捷方式 (.desktop)
  install -d "${pkgdir}/usr/share/applications"
  cat <<EOF > "${pkgdir}/usr/share/applications/gesturing.desktop"
[Desktop Entry]
Type=Application
Name=Gesturing
Comment=Gesturing Mouse Gestures Daemon
Exec=/usr/bin/gesturing
Icon=/usr/share/gesturing/app.png
Terminal=false
Categories=Utility;
EOF
}
