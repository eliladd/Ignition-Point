import subprocess
import time
import os

unity_exe = r"C:\Users\elith\AppData\Local\Unity\bin\unity.exe"

def run_eval(code):
    cmd = [unity_exe, "command", "script", "eval", code]
    res = subprocess.run(cmd, capture_output=True, text=True)
    out = res.stdout.strip()
    err = res.stderr.strip()
    return out if out else err

print("Checking GameManager and UI...")
info = run_eval("""
var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
var guide = UnityEngine.Object.FindAnyObjectByType<MissionGuide>();
return "GM: " + (gm != null) + ", Guide: " + (guide != null) + ", Sol: " + (gm != null ? gm.State.sol : -1);
""")
print(info)

print("Capturing title screenshot...")
run_eval('UnityEngine.ScreenCapture.CaptureScreenshot("snap_title.png"); return true;')
time.sleep(1)

print("Advancing to briefing...")
run_eval('var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>(); gm.OnTitleOrBriefingAdvance(); return true;')
time.sleep(1)
run_eval('UnityEngine.ScreenCapture.CaptureScreenshot("snap_briefing.png"); return true;')
time.sleep(1)

print("Advancing through briefings to Sol 1 Decision...")
run_eval('var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>(); gm.OnTitleOrBriefingAdvance(); return true;')
time.sleep(1)
run_eval('var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>(); gm.OnTitleOrBriefingAdvance(); return true;')
time.sleep(1)
run_eval('var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>(); gm.OnTitleOrBriefingAdvance(); return true;')
time.sleep(2)

print("Capturing decision screen screenshot...")
run_eval('UnityEngine.ScreenCapture.CaptureScreenshot("snap_sol1.png"); return true;')
time.sleep(1)

print("Done. Files created:")
for f in ["snap_title.png", "snap_briefing.png", "snap_sol1.png"]:
    print(f, os.path.exists(f), os.path.getsize(f) if os.path.exists(f) else 0)
