from PIL import Image

def tint_multiply(input_path, output_path, hex_color):
    img = Image.open(input_path).convert("RGBA")

    # 색상 코드 → RGB
    hex_color = hex_color.lstrip('#')
    tint_rgb = tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))

    r, g, b, a = img.split()

    # 픽셀별 곱셈
    r = r.point(lambda i: i * tint_rgb[0] // 255)
    g = g.point(lambda i: i * tint_rgb[1] // 255)
    b = b.point(lambda i: i * tint_rgb[2] // 255)

    tinted = Image.merge("RGBA", (r, g, b, a))
    tinted.save(output_path)

lst = list(range(0, 8))

for i in lst:
    tint_multiply(f"./images/Block{i}.png", f"./images/Block{i}_green.png", "#CAFFCA")
    tint_multiply(f"./images/Block{i}.png", f"./images/Block{i}_yellow.png", "#FEFCCD")
