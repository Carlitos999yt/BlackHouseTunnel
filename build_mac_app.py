import os
import shutil

base_dir = r'c:\Users\VERONICA\Documents\universidad1\exe nuevo'


def create_mac_app(src_dir, app_name, zip_name):
  app_dir = os.path.join(base_dir, app_name)
  contents_dir = os.path.join(app_dir, 'Contents')
  macos_dir = os.path.join(contents_dir, 'MacOS')
  resources_dir = os.path.join(contents_dir, 'Resources')

  if os.path.exists(app_dir):
    shutil.rmtree(app_dir)

  os.makedirs(macos_dir, exist_ok=True)
  os.makedirs(resources_dir, exist_ok=True)

  # Copy binaries to MacOS dir
  for item in os.listdir(src_dir):
    s = os.path.join(src_dir, item)
    d = os.path.join(macos_dir, item)
    if os.path.isdir(s):
      shutil.copytree(s, d)
    else:
      shutil.copy2(s, d)

  # Write Info.plist
  plist = (
      '<?xml version="1.0" encoding="UTF-8"?>\n'
      '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"'
      ' "http://www.apple.com/DTDs/PropertyList-1.0.dtd">\n'
      '<plist version="1.0">\n'
      '<dict>\n'
      '    <key>CFBundleExecutable</key>\n'
      '    <string>NepTunnelMac</string>\n'
      '    <key>CFBundleIdentifier</key>\n'
      '    <string>com.neptunnel.app</string>\n'
      '    <key>CFBundleName</key>\n'
      '    <string>Nep Tunnel</string>\n'
      '    <key>CFBundlePackageType</key>\n'
      '    <string>APPL</string>\n'
      '    <key>CFBundleShortVersionString</key>\n'
      '    <string>2.3</string>\n'
      '    <key>LSMinimumSystemVersion</key>\n'
      '    <string>10.15</string>\n'
      '</dict>\n'
      '</plist>'
  )

  with open(os.path.join(contents_dir, 'Info.plist'), 'w') as f:
    f.write(plist)

  # Zip the .app bundle
  zip_out = os.path.join(base_dir, zip_name)
  if os.path.exists(zip_out):
    os.remove(zip_out)

  shutil.make_archive(
      zip_out.replace('.zip', ''),
      'zip',
      base_dir,
      os.path.basename(app_dir),
  )
  print(f'Mac App Bundle {app_name} created successfully!')


create_mac_app(
    os.path.join(base_dir, 'bin', 'Release', 'net8.0', 'osx-arm64', 'publish'),
    'NepTunnel_AppleSilicon.app',
    'NepTunnel_Mac_AppleSilicon_App.zip',
)
create_mac_app(
    os.path.join(base_dir, 'bin', 'Release', 'net8.0', 'osx-x64', 'publish'),
    'NepTunnel_Intel.app',
    'NepTunnel_Mac_Intel_App.zip',
)
