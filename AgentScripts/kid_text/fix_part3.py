import json

d = json.load(open('AgentScripts/kid_text/part3.json', encoding='utf-8'))

fixes = {
  ('DAY_8A', 'SOL4_SAVED_FUEL'): 'Sol 4 cold fuel uses power (-5% Power), but lifts morale (+3% Crew Health).',
  ('DAY_8A', 'SOL7_RATIONED_FOOD'): 'Sol 7 cold ration bars saved power (0% Electricity Conserved!), but tired everyone (-5% Crew Health).',
  ('DAY_8A', 'SOL7D_DOCK_ROVERS'): 'Clean rover plugs help power (0% Electricity Conserved!), but suits tired the crew (-3% Crew Health).',
  ('DAY_8B', 'SOL7_DUST_WALK'): 'Sol 7 skylight sweeping gives power (+3% Electricity), but sore muscles slow everyone (-4% Crew Health).',
  ('DAY_8B', 'SOL7A_HOT_SOUP'): 'Sol 7 hot meals gave energy (+4% Crew Health), but used power (-3% Power).',
  ('DAY_8C', 'SOL7D_RADAR_HEAT'): 'Heated radar gears stay ice-free (0% Electricity Conserved!), but chilly rooms stiffen joints (-3% Crew Health).',
  ('DAY_9A', 'SOL8_SWEPT_ARRAYS'): 'Swept Wing 1 gives morning power (+3% Electricity), but sore workers need rest (-4% Crew Health).',
  ('DAY_9B', 'SOL3_COLD_CORRIDORS'): 'Sealed corridors save power (0% Electricity Conserved!), but icy doorways stiffen joints (-4% Crew Health).',
  ('DAY_9C', 'SOL8C_STORM_BALLOON'): 'The storm balloon timed skies (0% Electricity Conserved!), but outdoor duties tired everyone (-4% Crew Health).',
  ('DAY_9D', 'SOL8_ROVER_CLEANING'): 'Cleared arrays give morning power (+3% Electricity), but scrubbing rover plugs tired everyone (-3% Crew Health).',
  ('DAY_9D', 'SOL8_AUX_BYPASS'): 'The bypass kept the reactor on (0% Electricity Conserved!), but loud pumps rattled bunks (-4% Crew Health).',
  ('DAY_10A', 'SOL9B_RADIATOR_CHIP'): 'Clear radiators work better (0% Electricity Conserved!), but icy work left crew sore (-3% Crew Health).',
  ('DAY_10A', 'SOL9D_SOLAR_CALIB'): 'Tuned trackers catch strong sun (+3% Electricity), but dune hikes tired the crew (-3% Crew Health).',
  ('DAY_10B', 'SOL9_FUEL_CHILL_TEST'): 'Tested chillers locked power buses (0% Electricity Conserved!), but late shifts tired everyone (-3% Crew Health).',
  ('DAY_10D', 'SOL9A_RIDGE_PANELS'): 'Ridge panels bring morning power (+3% Electricity), but dune hikes tired the crew (-3% Crew Health).'
}

for n in d['nodes']:
    nid = n['nodeId']
    if nid == 'DAY_8B' and n['choiceC']['buttonLabel'] == 'Use Chariot power and dust shields':
        n['choiceC']['buttonLabel'] = 'Use Chariot dust shields'
    if nid == 'DAY_10A' and n['choiceC']['buttonLabel'] == 'Link the core and solar grid':
        n['choiceC']['buttonLabel'] = 'Link core and solar'
    if nid == 'DAY_10C' and n['choiceC']['buttonLabel'] == 'Zap dust in the dawn light':
        n['choiceC']['buttonLabel'] = 'Zap dust at dawn'
    if nid == 'DAY_9D':
        n['storyText'] = 'No need to wait for <b><color=#5CE1E6><u><link="2018_cleaning_events">dust devils</link></u></color></b>! You can revive <b><color=#5CE1E6><u><link="2018_greenhouse_crops">greenhouse crops</link></u></color></b> and <b><color=#5CE1E6><u><link="2018_water_loss">water tanks</link></u></color></b>.'
    for c in n.get('consequences', []):
        k = (nid, c['checkFlag'])
        if k in fixes:
            c['text'] = fixes[k]

with open('AgentScripts/kid_text/part3.json', 'w', encoding='utf-8', newline='\n') as f:
    json.dump(d, f, indent=2, ensure_ascii=False)
print('Fixed part3.json successfully.')
