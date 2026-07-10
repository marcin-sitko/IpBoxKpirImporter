#!/usr/bin/env python3
"""Grid-line detector for KPiR table pages (cross-platform backend).

Reproduces the OpenCvSharp algorithm used on Windows so the importer can run
on macOS / Linux without the Windows-only OpenCvSharp native libraries.
Reads an image path, prints JSON: {width,height,horizontal[],vertical[]}.
"""
import sys, json, cv2

def cluster(vals, tol):
    res = []
    if not vals:
        return res
    g = [vals[0]]
    for v in vals[1:]:
        if v - g[-1] <= tol:
            g.append(v)
        else:
            res.append(int(round(sum(g) / len(g))))
            g = [v]
    res.append(int(round(sum(g) / len(g))))
    return res

def main():
    img = cv2.imread(sys.argv[1], cv2.IMREAD_GRAYSCALE)
    if img is None:
        sys.stderr.write("cannot read " + sys.argv[1])
        sys.exit(2)
    h, w = img.shape
    _, bw = cv2.threshold(img, 0, 255, cv2.THRESH_BINARY_INV | cv2.THRESH_OTSU)
    hkw = max(40, w // 18)
    vkh = max(25, h // 45)
    hor = cv2.morphologyEx(bw, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (hkw, 1)))
    ver = cv2.morphologyEx(bw, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, vkh)))
    ys = [y for y in range(h) if cv2.countNonZero(hor[y:y + 1, :]) > w * 0.35]
    xs = [x for x in range(w) if cv2.countNonZero(ver[:, x:x + 1]) > h * 0.15]
    print(json.dumps({"width": w, "height": h,
                      "horizontal": cluster(ys, 6), "vertical": cluster(xs, 8)}))

if __name__ == "__main__":
    main()
