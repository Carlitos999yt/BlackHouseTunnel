import os
from PIL import Image

base_user_dir = r'C:\Users\VERONICA\.gemini\antigravity\brain\360891bc-6e0d-4f38-99f0-d1422a19b112\.user_uploaded'
output_dir = r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\bundled_assets'

files = [
    'media__1785280810035.png',  # 1: Playit Download
    'media__1785280816231.png',  # 2: Claim & Agent Setup
    'media__1785280823038.png',  # 3: New Tunnel Form
    'media__1785280830214.png',  # 4: Tunnel Address & Port
    'media__1785280836563.png',  # 5: Main Menu
    'media__1785280877069.png',  # 6: Host Session Config
    'media__1785280890042.png',  # 7: Server Console
    'media__1785280899358.png',  # 8: Join Button
    'media__1785280904633.png',  # 9: Join Session Config
]


def crop_ui_window_only(img_path, out_path):
  img = Image.open(img_path).convert('RGB')
  w, h = img.size
  pixels = img.load()

  # Find rows and columns containing dark UI pixels (R < 100 and G < 100 and B < 100)
  min_x, min_y, max_x, max_y = w, h, 0, 0

  for y in range(h):
    for x in range(w):
      r, g, b = pixels[x, y]
      # UI elements are dark backgrounds (R < 90 and G < 90 and B < 110)
      if r < 90 and g < 90 and b < 110:
        if x < min_x:
          min_x = x
        if x > max_x:
          max_x = x
        if y < min_y:
          min_y = y
        if y > max_y:
          max_y = y

  # Ensure valid crop rectangle
  if max_x > min_x and max_y > min_y:
    min_x = max(0, min_x - 2)
    min_y = max(0, min_y - 2)
    max_x = min(w, max_x + 3)
    max_y = min(h, max_y + 3)
    cropped = img.crop((min_x, min_y, max_x, max_y))
    cropped.save(out_path, 'PNG')
    print(
        f'UI Window Cropped {os.path.basename(img_path)} -> {out_path}'
        f' ({cropped.size})'
    )
  else:
    img.save(out_path, 'PNG')


for i, filename in enumerate(files, 1):
  src = os.path.join(base_user_dir, filename)
  dst = os.path.join(output_dir, f'tut_{i}.png')
  crop_ui_window_only(src, dst)
