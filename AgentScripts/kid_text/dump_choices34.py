import json

d = json.load(open('Assets/Resources/mission_data.json', encoding='utf-8'))
for nid in ['DAY_3A', 'DAY_3B', 'DAY_3C', 'DAY_3D', 'DAY_4A', 'DAY_4B', 'DAY_4C', 'DAY_4D']:
    n = next(x for x in d['storyNodes'] if x['nodeId'] == nid)
    for ck in ['choiceA', 'choiceB', 'choiceC']:
        c = n[ck]
        print(nid + '.' + ck + ' -> ' + c.get('buttonLabel', '') + ' | ' + c.get('description', '') + ' | ' + c.get('outcomeText', ''))
