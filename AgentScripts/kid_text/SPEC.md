# Kid-Friendly Rewrite Spec: "Ares Sols: 2038" (target audience: ages 7-12)

Game: kids run Ares Base on Mars during a giant 2038 dust storm (based on the real 2018 storm that ended
NASA's Opportunity rover). Each Sol (Mars day) they pick 1 of 3 options that change Electricity (power)
and Crew Health. Two on-screen helper characters talk to the player:

* **Pip** - a small, cheerful, curious rover robot. Explains words and science in simple, fun language.
  Voice: excited, friendly, uses "Whoa!", "Look!", "Did you know?". Never babyish, never sarcastic.
* **Captain Nova** - a calm, kind astronaut commander. Gives a *strategy hint* (what to think about), never
  the exact answer. Voice: steady, encouraging, "Think about...", "Remember...".

## Writing rules (STRICT)
1. Reading level: US grade 2-4. Sentences **max 12 words**. Prefer 6-10. One idea per sentence.
2. Everyday words. If a science word is needed, say it simply first ("dust storm", "solar panels",
   "nuclear power plant" -> "a tiny nuclear power plant"). Do not stack two hard terms in one sentence.
3. Be **accurate**. You may only simplify and shorten. Do NOT invent new facts or new numbers. You may drop
   numbers that are not essential. Keep key real facts (e.g. 2018 storm covered the whole planet, Opportunity
   lost power, Curiosity kept going on nuclear power, Mars air is very thin, radio takes ~22 minutes).
4. Fun, vivid, second person ("you", "your crew"). Light humor is fine. Never scary or gloomy for its own sake.
5. **ASCII-friendly typography only**: use straight quotes ' and ", plain hyphen -, "..." avoided. NO em/en
   dashes, NO emoji, NO curly quotes. The degree sign (a literal degree symbol) is OK. Exclamation marks OK.
6. Rich text: the game uses TextMeshPro rich text. Glossary links look exactly like this and must be kept:
   `<b><color=#5CE1E6><u><link="ID">phrase</link></u></color></b>`
   Keep **every link ID that the original text used** (the IDs must still appear, same markup), but you MAY
   shorten/rewrite the visible phrase inside the link. 2-4 tappable links per Sol is plenty; keep the originals.
   In JSON the quotes inside the markup must be escaped as `\"`.
7. Do not change: nodeId, dayNumber, realDate, numbers/deltas, flags, nextNodeId, subLabel, subsystem*,
   nasaId, imageFile, imageCaption, nasaSource, sourceUrl, nasaFactId, checkFlag, skyColorHex, sunlight values.
8. Length budgets (hard maximums):
   * `title`: keep the "Sol N: " prefix, then a fun title of max 5 words.
   * `storyText`: max 3 sentences and max 38 words (not counting markup).
   * `nasaFactBanner`: ONE sentence, max 16 words, keep a short prefix like "2018 LESSON:" or "REAL NASA FACT:".
   * `consequences[].text`: ONE short sentence, max 16 words. Keep the "(+3% Electricity)" style stat hints
     only if the original had them; keep the numbers identical.
   * choice `buttonLabel`: max 5 words, verb first, a kid can read it at a glance.
   * choice `description`: ONE sentence, max 14 words.
   * choice `outcomeText`: max 2 sentences, max 26 words total. Say what happened and why it matters.
   * choice `nasaComparisonTitle`: max 7 words.
   * choice `nasaRealDecision`: max 2 sentences, max 30 words. Real NASA connection, simply put.
   * choice `nasaFactEmphasis`: ONE sentence, max 18 words. A "wow" fact.
9. NEW helper-character lines (write these for every node/choice you own):
   * node `guideTip` (Pip, max 16 words): explains the science/word behind this Sol in the simplest way.
   * node `novaHint` (Captain Nova, max 16 words): a strategy nudge. E.g. "Dust blocks sunlight. What
     could still make power?" Do not name the best option.
   * choice `guideReaction` (Pip, max 14 words): a reaction after the player picks this option, tied to what
     it does (power up/down, health up/down). Positive trade-offs get cheers; costly ones get a gentle,
     encouraging "Hmm" and a tiny lesson. Never scold the player.

## Patch file format (what you deliver)
Write ONE UTF-8 JSON file (no BOM) to the path given in your brief. Only include fields you rewrote/added.
Use exact IDs from the source so a script can merge by ID.

```json
{
  "nodes": [
    {
      "nodeId": "DAY_1",
      "title": "Sol 1: Dust Storm Alarm",
      "nasaFactBanner": "2018 LESSON: ...",
      "storyText": "... <b><color=#5CE1E6><u><link=\"2018_mro_warning\">tiny dust storm</link></u></color></b> ...",
      "guideTip": "...",
      "novaHint": "...",
      "consequences": [ { "checkFlag": "SOL1_OVERCHARGED", "text": "..." } ],
      "choiceA": { "buttonLabel": "...", "description": "...", "outcomeText": "...",
                   "nasaComparisonTitle": "...", "nasaRealDecision": "...", "nasaFactEmphasis": "...",
                   "guideReaction": "..." },
      "choiceB": { },
      "choiceC": { }
    }
  ]
}
```
`consequences` entries are matched by `checkFlag` (keep every checkFlag, rewrite only `text`).
Some nodes may have a legacy `consequenceText` field - rewrite it too if present (same budgets).

## How to read the source safely (Windows PowerShell mangles UTF-8, so use Python)
```
python -c "import json;d=json.load(open('Assets/Resources/mission_data.json',encoding='utf-8'));print(json.dumps([n for n in d['storyNodes'] if n['nodeId'] in ('DAY_1',)],indent=1,ensure_ascii=False))"
```
Work from the project root `c:\Users\elith\Documents\Ignition Point`. **Never edit
`Assets/Resources/mission_data.json` yourself** (a merge script will apply the patches), and do not touch
Unity, any `.cs` file, or other writers' files. After writing, validate with
`python -c "import json;json.load(open(r'<your file>',encoding='utf-8'))"` and run
`python AgentScripts/kid_text/check_patch.py <your file>` (a word/length checker) and fix every violation.
Reply to the coordinator with a 3-line summary only when the checker reports 0 violations.
