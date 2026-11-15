import os
import shutil
import json
import urllib.request

src_dir = "Nasa pictures"
res_bg_dir = os.path.join("Assets", "Resources", "NasaBackgrounds")
stream_bg_dir = os.path.join("Assets", "StreamingAssets", "NasaBackgrounds")

os.makedirs(res_bg_dir, exist_ok=True)
os.makedirs(stream_bg_dir, exist_ok=True)

# 1. Copy user's attached images and videos
local_images = [
    ("41667_Zurek-3-DustStorm_Jun2018_ScienceObservations-01.png", "nasa_local_01.png"),
    ("atmospheric opacity- how conditions have changed around the NASA rover as a huge dust storm has intensified (from left to right) throughout June 2018.jpg", "nasa_local_02.jpg"),
    ("onset and expansion- Two images from the surface of Mars show the atmospheric effects of the June 2018 dust storm..jpg", "nasa_local_03.jpg"),
]

local_videos = [
    ("https-_science.nasa.gov_resource_the-martian-dust-storm-of-june-2018.mp4", "nasa_vid_01.mp4"),
    ("onset and expansion- A timeseries of global images of Mars show the spread of the June 2018 dust storm..mp4", "nasa_vid_02.mp4"),
    ("onset and expansion- An animation of optical depth during the 2018 Mars global dust storm..webm", "nasa_vid_03.webm"),
    ("onset and expansion- Rotating globes from May 28 and July 1 show a global dust storm completely obscuring the surface of Mars.webm", "nasa_vid_04.webm"),
]

for src_name, dst_name in local_images:
    s = os.path.join(src_dir, src_name)
    d = os.path.join(res_bg_dir, dst_name)
    if os.path.exists(s):
        shutil.copy2(s, d)
        print(f"Copied image: {dst_name} ({os.path.getsize(d)} bytes)")
    else:
        print(f"MISSING image: {s}")

for src_name, dst_name in local_videos:
    s = os.path.join(src_dir, src_name)
    d = os.path.join(stream_bg_dir, dst_name)
    if os.path.exists(s):
        shutil.copy2(s, d)
        print(f"Copied video: {dst_name} ({os.path.getsize(d)} bytes)")
    else:
        print(f"MISSING video: {s}")

# 2. Download official NASA images via images-api.nasa.gov
nasa_ids = [
    ("PIA22519", "nasa_web_PIA22519.jpg"),
    ("PIA23513", "nasa_web_PIA23513.jpg"),
    ("PIA15691", "nasa_web_PIA15691.jpg"),
    ("PIA24937", "nasa_web_PIA24937.jpg"),
    ("PIA26680", "nasa_web_PIA26680.jpg"),
]

for nasa_id, dst_name in nasa_ids:
    dst_path = os.path.join(res_bg_dir, dst_name)
    if os.path.exists(dst_path) and os.path.getsize(dst_path) > 10000:
        print(f"Already exists: {dst_name}")
        continue
    try:
        api_url = f"https://images-api.nasa.gov/asset/{nasa_id}"
        req = urllib.request.Request(api_url, headers={"User-Agent": "AresSols2038/1.0"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            data = json.loads(resp.read().decode("utf-8"))
        items = data.get("collection", {}).get("items", [])
        hrefs = [it.get("href", "") for it in items]
        # Prefer medium or large jpg (not ~orig tif/huge 40MB files, ~large or ~medium is ideal for 1080p)
        chosen = None
        for tag in ["~large.jpg", "~medium.jpg", "~orig.jpg"]:
            for h in hrefs:
                if h.lower().endswith(tag):
                    chosen = h
                    break
            if chosen:
                break
        if not chosen:
            for h in hrefs:
                if h.lower().endswith(".jpg"):
                    chosen = h
                    break
        if chosen:
            chosen_safe = chosen.replace(" ", "%20")
            req2 = urllib.request.Request(chosen_safe, headers={"User-Agent": "AresSols2038/1.0"})
            with urllib.request.urlopen(req2, timeout=25) as r2:
                img_bytes = r2.read()
            with open(dst_path, "wb") as f:
                f.write(img_bytes)
            print(f"Downloaded {nasa_id} -> {dst_name} ({len(img_bytes)} bytes)")
        else:
            print(f"No JPG found for {nasa_id}")
    except Exception as e:
        print(f"Failed {nasa_id}: {e}")
