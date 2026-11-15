import json

d = json.load(open('Assets/Resources/mission_data.json', encoding='utf-8'))
for nid in [f'DAY_{s}{c}' for s in [5, 6, 7] for c in ['A', 'B', 'C', 'D']]:
    n = next(x for x in d['storyNodes'] if x['nodeId'] == nid)
    for ck in ['choiceA', 'choiceB', 'choiceC']:
        c = n[ck]
        print(nid + '.' + ck + ' -> ' + c.get('buttonLabel', '') + ' | ' + c.get('description', '') + ' | ' + c.get('outcomeText', ''))
