"""Merge kid-text patches into Assets/Resources/mission_data.json. Usage: python merge_patches.py part1.json ..."""
import json, sys, shutil, os

SRC = 'Assets/Resources/mission_data.json'
BACKUP = 'AgentScripts/kid_text/mission_data.original.json'

def merge_dict(dst, src, keys=None):
    for k, val in src.items():
        if keys is not None and k not in keys:
            continue
        dst[k] = val

def main(paths):
    if not os.path.exists(BACKUP):
        shutil.copyfile(SRC, BACKUP)
    base = BACKUP  # always merge from the pristine original so this script is re-runnable
    data = json.load(open(base, encoding='utf-8'))
    nodes = {n['nodeId']: n for n in data['storyNodes']}
    for p in paths:
        patch = json.load(open(p, encoding='utf-8'))
        for pn in patch.get('nodes', []):
            n = nodes[pn['nodeId']]
            for k, val in pn.items():
                if k in ('nodeId', 'choiceA', 'choiceB', 'choiceC', 'consequences'):
                    continue
                n[k] = val
            for ck in ('choiceA', 'choiceB', 'choiceC'):
                if ck in pn and n.get(ck) is not None:
                    merge_dict(n[ck], pn[ck])
            if 'consequences' in pn:
                by_flag = {c['checkFlag']: c for c in n.get('consequences', [])}
                for pc in pn['consequences']:
                    if pc['checkFlag'] in by_flag:
                        by_flag[pc['checkFlag']]['text'] = pc['text']
        for pi in patch.get('introScreens', []):
            for i in data['introScreens']:
                if i['screenNumber'] == pi['screenNumber']:
                    merge_dict(i, pi)
        for pg in patch.get('kidGlossary', []):
            for g in data['kidGlossary']:
                if g['id'] == pg['id']:
                    merge_dict(g, pg)
        for pe in patch.get('endings', []):
            for e in data['endings']:
                if e['endingId'] == pe['endingId']:
                    merge_dict(e, pe)
        rl = patch.get('realLifeComparison')
        if rl:
            if 'title' in rl:
                data['realLifeComparison']['title'] = rl['title']
            # cards are matched by position (same order as the source file)
            for idx, pc in enumerate(rl.get('cards', [])):
                if idx < len(data['realLifeComparison']['cards']):
                    merge_dict(data['realLifeComparison']['cards'][idx], pc)
    with open(SRC, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print('merged', len(paths), 'patch file(s) into', SRC)

if __name__ == '__main__':
    main(sys.argv[1:])
