import struct
import urllib.request
from io import BytesIO
from PIL import Image, ImageDraw

req = urllib.request.Request(
    'https://i.imgur.com/68Bdv5u_d.webp?maxwidth=760&fidelity=grand',
    headers={'User-Agent': 'Mozilla/5.0'},
)
data = urllib.request.urlopen(req, timeout=12).read()
src = Image.open(BytesIO(data)).convert('RGBA')

size = 1024
s = min(src.size)
crop = src.crop((
    (src.width - s) // 2,
    (src.height - s) // 2,
    (src.width + s) // 2,
    (src.height + s) // 2,
)).resize((size, size), Image.Resampling.LANCZOS)

mask = Image.new('L', (size, size), 0)
draw = ImageDraw.Draw(mask)
draw.ellipse([2, 2, size - 3, size - 3], fill=255)
crop.putalpha(mask)

sizes = [256, 128, 64, 48, 32, 16]
png_datas = []
for sz in sizes:
  r = crop.resize((sz, sz), Image.Resampling.LANCZOS)
  bio = BytesIO()
  r.save(bio, format='PNG')
  png_datas.append(bio.getvalue())

num_images = len(sizes)
ico = bytearray()
ico += struct.pack('<HHH', 0, 1, num_images)
offset = 6 + 16 * num_images

for i, sz in enumerate(sizes):
  w = 0 if sz == 256 else sz
  h = 0 if sz == 256 else sz
  data_len = len(png_datas[i])
  ico += struct.pack('<BBBBHHII', w, h, 0, 0, 1, 32, data_len, offset)
  offset += data_len

for pd in png_datas:
  ico += pd

with open(r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\icon.ico', 'wb') as f:
  f.write(ico)

import os
os.makedirs(r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\bundled_assets', exist_ok=True)
crop_256 = crop.resize((256, 256), Image.Resampling.LANCZOS)
crop_256.save(r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\bundled_assets\logo.png', format='PNG')

print('Clean transparent icon.ico and logo.png created!')
