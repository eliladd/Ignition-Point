"""Validate a kid-text patch against SPEC.md budgets. Usage: python check_patch.py <patch.json>"""
import json, re, sys

SRC = 'Assets/Resources/mission_data.json'
TAG = re.compile(r'<[^>]+>')
LINK = re.compile(r'<link=\\?"([^"\\]+)\\?"')
BAD_CHARS = re.compile(r"[^\x00-\x7f\u00b0]")

def plain(s):
    return TAG.sub('', s or '')

def words(s):
    return len(plain(s).split())

def sentences(s):
    return [x for x in re.split(r'(?<=[.!?])\s+', plain(s).strip()) if x]

violations = []

def v(where, msg):
    violations.append(f'{where}: {msg}')

def check_text(where, s, max_words, max_sent_words=12, max_sentences=None, one_sentence=False):
    if s is None:
        return
    if BAD_CHARS.search(s):
        v(where, 'non-ASCII char (em dash / curly quote / emoji?): ' + ''.join(sorted(set(BAD_CHARS.findall(s)))))
    if words(s) > max_words:
        v(where, f'{words(s)} words > {max_words}')
    sents = sentences(s)
    if max_sentences and len(sents) > max_sentences:
        v(where, f'{len(sents)} sentences > {max_sentences}')
    if one_sentence and len(sents) > 1:
        v(where, 'must be ONE sentence')
    for x in sents:
        if len(x.split()) > max_sent_words:
            v(where, f'sentence has {len(x.split())} words > {max_sent_words}: "{x[:60]}..."')

def main(path):
    patch = json.load(open(path, encoding='utf-8'))
    src = json.load(open(SRC, encoding='utf-8'))
    src_nodes = {n['nodeId']: n for n in src['storyNodes']}
    for n in patch.get('nodes', []):
        nid = n['nodeId']
        if nid not in src_nodes:
            v(nid, 'unknown nodeId'); continue
        s = src_nodes[nid]
        if 'title' in n:
            if not re.match(r'^Sol \d+: ', n['title']): v(nid + '.title', 'missing "Sol N: " prefix')
            check_text(nid + '.title', re.sub(r'^Sol \d+: ', '', n['title']), 5, 5)
        if 'storyText' in n:
            check_text(nid + '.storyText', n['storyText'], 38, 14, 3)
            old = set(LINK.findall(s.get('storyText', '')))
            new = set(LINK.findall(n['storyText']))
            if old - new: v(nid + '.storyText', 'missing link ids: ' + ', '.join(sorted(old - new)))
            if new - old: v(nid + '.storyText', 'unknown link ids: ' + ', '.join(sorted(new - old)))
        check_text(nid + '.nasaFactBanner', n.get('nasaFactBanner'), 16, 16, one_sentence=True)
        check_text(nid + '.guideTip', n.get('guideTip'), 16, 16)
        check_text(nid + '.novaHint', n.get('novaHint'), 16, 16)
        flags = {c['checkFlag'] for c in s.get('consequences', [])}
        for c in n.get('consequences', []):
            if c['checkFlag'] not in flags: v(nid + '.consequences', 'unknown checkFlag ' + c['checkFlag'])
            check_text(nid + '.consequence.' + c['checkFlag'], c.get('text'), 16, 16, one_sentence=True)
        if 'consequenceText' in n:
            check_text(nid + '.consequenceText', n['consequenceText'], 16, 16, one_sentence=True)
        for key in ('choiceA', 'choiceB', 'choiceC'):
            c = n.get(key)
            if not c: continue
            w = f'{nid}.{key}'
            check_text(w + '.buttonLabel', c.get('buttonLabel'), 5, 5)
            check_text(w + '.description', c.get('description'), 14, 14, one_sentence=True)
            check_text(w + '.outcomeText', c.get('outcomeText'), 26, 14, 2)
            check_text(w + '.nasaComparisonTitle', c.get('nasaComparisonTitle'), 7, 7)
            check_text(w + '.nasaRealDecision', c.get('nasaRealDecision'), 30, 16, 2)
            check_text(w + '.nasaFactEmphasis', c.get('nasaFactEmphasis'), 18, 18, one_sentence=True)
            check_text(w + '.guideReaction', c.get('guideReaction'), 14, 14)
    # extras: intro / glossary / endings / comparison / guide lines (used by the "content" writer)
    for i in patch.get('introScreens', []):
        check_text(f'intro{i.get("screenNumber")}.headline', i.get('headline'), 8, 8)
        check_text(f'intro{i.get("screenNumber")}.bodyText', i.get('bodyText'), 55, 14, 5)
        check_text(f'intro{i.get("screenNumber")}.guideLine', i.get('guideLine'), 18, 18)
    for g in patch.get('kidGlossary', []):
        check_text(f'gloss.{g["id"]}.headline', g.get('headline'), 8, 8)
        check_text(f'gloss.{g["id"]}.simpleDefinition', g.get('simpleDefinition'), 40, 13, 4)
        check_text(f'gloss.{g["id"]}.termMeaning', g.get('termMeaning'), 28, 13, 3)
    for e in patch.get('endings', []):
        check_text(f'ending.{e["endingId"]}.headline', e.get('headline'), 9, 9)
        check_text(f'ending.{e["endingId"]}.storyText', e.get('storyText'), 55, 14, 5)
        check_text(f'ending.{e["endingId"]}.guideLine', e.get('guideLine'), 20, 20)
    rl = patch.get('realLifeComparison')
    if rl:
        check_text('comparison.title', rl.get('title'), 9, 9)
        for c in rl.get('cards', []):
            check_text('comparison.' + c.get('topic', '?') + '.topic', c.get('topic'), 8, 8)
            check_text('comparison.' + c.get('topic', '?') + '.what', c.get('whatHappenedInRealLife'), 40, 13, 4)
            check_text('comparison.' + c.get('topic', '?') + '.how', c.get('howItConnectsToGame'), 40, 13, 4)
    for line in violations:
        print('VIOLATION', line)
    print(f'{len(violations)} violation(s)')
    sys.exit(1 if violations else 0)

if __name__ == '__main__':
    main(sys.argv[1])
