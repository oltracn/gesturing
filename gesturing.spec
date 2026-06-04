Name:           gesturing
Version:        1.0.0
Release:        1%{?dist}
Summary:        A lightweight background mouse gestures daemon for KDE Wayland

License:        MIT
URL:            https://github.com/oltra/gesturing
Source0:        %{name}-%{version}.tar.gz

BuildArch:      noarch
BuildRequires:  python3-devel

Requires:       python3
Requires:       python3-evdev
Requires:       python3-pystray
Requires:       python3-pillow
Requires:       systemd-udev

%description
A lightweight background mouse gestures daemon for KDE Wayland, supporting global and application-specific gestures.

%prep
%setup -q

%build
# No compilation required (Python script)

%install
# 1. Create target directories
install -d %{buildroot}%{_datadir}/%{name}
install -d %{buildroot}%{_bindir}
install -d %{buildroot}%{_datadir}/applications
install -d %{buildroot}%{_sysconfdir}/udev/rules.d

# 2. Install core application files
install -m 0755 gesturing.py %{buildroot}%{_datadir}/%{name}/gesturing.py
install -m 0644 config.json %{buildroot}%{_datadir}/%{name}/config.json
install -m 0644 app.png %{buildroot}%{_datadir}/%{name}/app.png

# 3. Create executable wrapper symlink in /usr/bin
ln -sf %{_datadir}/%{name}/gesturing.py %{buildroot}%{_bindir}/gesturing

# 4. Install system desktop entry
cat <<EOF > %{buildroot}%{_datadir}/applications/gesturing.desktop
[Desktop Entry]
Type=Application
Name=Gesturing
Comment=Gesturing Mouse Gestures Daemon
Exec=%{_bindir}/gesturing
Icon=%{_datadir}/%{name}/app.png
Terminal=false
Categories=Utility;
EOF

# 5. Install udev rule for non-root /dev/uinput write access
cat <<EOF > %{buildroot}%{_sysconfdir}/udev/rules.d/99-gesturing-uinput.rules
# Allow members of the input group to write to uinput (required for Gesturing daemon)
KERNEL=="uinput", GROUP="input", MODE="0660", OPTIONS+="static_node=uinput"
EOF

%post
# Reload udev rules to apply /dev/uinput permissions immediately
udevadm control --reload-rules || :
udevadm trigger || :

%postun
# Reload udev rules after uninstalling
udevadm control --reload-rules || :
udevadm trigger || :

%files
%{_datadir}/%{name}/
%{_bindir}/gesturing
%{_datadir}/applications/gesturing.desktop
%{_sysconfdir}/udev/rules.d/99-gesturing-uinput.rules

%changelog
* Fri Jun 05 2026 Oltra <oltra@example.com> - 1.0.0-1
- Initial packaging migration to Fedora RPM spec format
