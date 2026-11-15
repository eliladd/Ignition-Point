import json, re

d = json.load(open('Assets/Resources/mission_data.json', encoding='utf-8'))
LINK = re.compile(r'<link=\"([^\"]+)\"')

print("P1 nodes:")
for n in d['storyNodes'][:12]:
    links = sorted(set(LINK.findall(n['storyText'])))
    cflags = [c['checkFlag'] for c in n.get('consequences', [])]
    print(f"{n['nodeId']}: links={links} flags={cflags}")

print("\nP2 nodes:")
for n in d['storyNodes'][12:24]:
    links = sorted(set(LINK.findall(n['storyText'])))
    cflags = [c['checkFlag'] for c in n.get('consequences', [])]
    print(f"{n['nodeId']}: links={links} flags={cflags}")
