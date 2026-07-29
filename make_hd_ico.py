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

# High resolution 1024x1024 master canvas
size = 1024
s = min(src.size)
crop = src.crop((
    (src.width - s) // 2,
    (src.height - s) // 2,
    (src.width + s) // 2,
    (src.height + s) // 2,
)).resize((size, size), Image.Resampling.LANCZOS)

# Create smooth anti-aliased circle mask
mask = Image.new('L', (size, size), 0)
draw = ImageDraw.Draw(mask)
draw.ellipse([2, 2, size - 3, size - 3], fill=255)
crop.putalpha(mask)

# Save 256x256 PNG for high-res logo
crop_256 = crop.resize((256, 256), Image.Resampling.LANCZOS)
crop_256.save(
    r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\bundled_assets\logo.png',
    format='PNG',
)


def create_bmp_icon_data(img):
  w, h = img.size
  # 32bpp BGRA bottom-to-top
  xor_data = bytearray()
  and_data = bytearray()

  # AND mask row size padded to 4 bytes
  and_row_bytes = ((w + 31) // 32) * 4

  for y in range(h - 1, -1, -1):
    and_row = bytearray(and_row_bytes)
    for x in range(w):
      r, g, b, a = img.getpixel((x, y))
      if a < 128:
        # Transparent pixel: alpha 0, AND mask bit 1
        xor_data += bytes([0, 0, 0, 0])
        byte_idx = x // 8
        bit_idx = 7 - (x % 8)
        and_row[byte_idx] |= 1 << bit_idx
      else:
        # Opaque/semi-transparent pixel: BGRA, AND mask bit 0
        xor_data += bytes([b, g, r, a])
    and_data += and_row

  # BITMAPINFOHEADER (40 bytes)
  header = struct.pack(
      '<IIIHHIIIIII',
      40,  # biSize
      w,  # biWidth
      h * 2,  # biHeight (XOR + AND)
      1,  # biPlanes
      32,  # biBitCount
      0,  # biCompression (BI_RGB)
      len(xor_data) + len(and_data),  # biSizeImage
      0,
      0,
      0,
      0,
  )

  return header + xor_data + and_data


# Build ICO entries
sizes = [256, 128, 64, 48, 32, 16]
image_frames = []

for sz in sizes:
  resized = crop.resize((sz, sz), Image.Resampling.LANCZOS)
  if sz == 256:
    # 256x256 uses PNG format inside ICO
    bio = BytesIO()
    resized.save(bio, format='PNG')
    image_frames.append(bio.getvalue())
  else:
    # 128, 64, 48, 32, 16 use 32bpp BMP + 1-bit AND mask inside ICO
    image_frames.append(create_bmp_icon_data(resized))

num_images = len(sizes)
ico = bytearray()
ico += struct.pack('<HHH', 0, 1, num_images)  # ICO Header

offset = 6 + 16 * num_images
for i, sz in enumerate(sizes):
  w = 0 if sz == 256 else sz
  h = 0 if sz == 256 else sz
  data_len = len(image_frames[i])
  ico += struct.pack(
      '<BBBBHHII', w, h, 0, 0, 1, 32, data_len, offset
  )  # Directory entry
  offset += data_len

for frame in image_frames:
  ico += frame

ico_path = r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\icon.ico'
with open(ico_path, 'wb') as f:
  f.write(ico)

print('HD Clean ICO file created with proper 1-bit AND transparency masks!')
