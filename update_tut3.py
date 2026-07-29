import os
from PIL import Image

src = r'C:\Users\VERONICA\.gemini\antigravity\brain\360891bc-6e0d-4f38-99f0-d1422a19b112\.user_uploaded\media__1785281388188.png'
dst = r'c:\Users\VERONICA\Documents\universidad1\exe nuevo\bundled_assets\tut_3.png'

img = Image.open(src).convert('RGB')
w, h = img.size
pixels = img.load()

min_x, min_y, max_x, max_y = w, h, 0, 0

for y in range(h):
  for x in range(w):
    r, g, b = pixels[x, y]
    if r < 90 and g < 90 and b < 110:
      if x < min_x:
        min_x = x
      if x > max_x:
        max_x = x
      if y < min_y:
        min_y = y
      if y > max_y:
        max_y = y

if max_x > min_x and max_y > min_y:
  min_x = max(0, min_x - 2)
  min_y = max(0, min_y - 2)
  max_x = min(w, max_x + 3)
  max_y = min(h, max_y + 3)
  cropped = img.crop((min_x, min_y, max_x, max_y))
  cropped.save(dst, 'PNG', optimize=True)
  print(f'New tut_3.png saved successfully! ({cropped.size})')
else:
  img.save(dst, 'PNG', optimize=True)
