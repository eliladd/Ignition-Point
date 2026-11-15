import json

# All URLs below are verified HTTP 200 official NASA articles, mission pages, and science reports.
URL_MRO_EARLY = "https://svs.gsfc.nasa.gov/30983"  # MRO MARCI spotted initial 2018 dust storm May 30
URL_JUNE_2018_STORM = "https://science.nasa.gov/resource/the-martian-dust-storm-of-june-2018/"  # Planet-encircling June 2018 dust storm
URL_STORM_WATCH_2018 = "https://mars.nasa.gov/weather/storm-watch-2018/"  # 2018 Mars dust storm watch / Opportunity blackout
URL_OPP_END = "https://science.nasa.gov/missions/mer/nasas-opportunity-rover-mission-on-mars-comes-to-end/"  # Opportunity mission end after 1,000+ recovery attempts
URL_OPP_MISSION = "https://science.nasa.gov/mission/mer-opportunity/"  # Opportunity Mars Exploration Rover mission
URL_CURIOSITY_SELFIE = "https://science.nasa.gov/photojournal/curiositys-dusty-selfie/"  # Curiosity June 15, 2018 dusty selfie in Gale Crater
URL_CURIOSITY_STORM = "https://www.nasa.gov/missions/martian-dust-storm-grows-global-curiosity-captures-photos-of-thickening-haze/"  # Curiosity MMRTG nuclear power & thickening haze
URL_DUST_TEMP = "https://www.nasa.gov/missions/how-a-tiny-curiosity-motor-identified-a-massive-martian-dust-storm/"  # Mars dust storm heating/cooling & atmosphere
URL_FSP = "https://www.nasa.gov/exploration-systems-development-mission-directorate/fission-surface-power/"  # Fission Surface Power / Kilopower
URL_KILOPOWER = "https://www.nasa.gov/directorates/stmd/fission-surface-power/"  # NASA STMD Kilopower / Fission Surface Power
URL_MOXIE = "https://www.nasa.gov/space-technology-mission-directorate/tdm/mars-oxygen-in-situ-resource-utilization-experiment-moxie/"  # MOXIE oxygen maker
URL_EDS = "https://www.nasa.gov/dust-mitigation/"  # Electrodynamic Dust Shield (EDS) & static Martian dust mitigation
URL_COMM_DELAY = "https://ntrs.nasa.gov/api/citations/20220013418/downloads/ASCEND-Communication%20Delays%2C%20Disruptions%2C%20and%20Blackouts%20for%20Crewed%20Mars%20Missions.pdf"  # 6-22 min comm delay
URL_MARS_SOL = "https://descanso.jpl.nasa.gov/DPSummary/Descanso14_MSL_Telecom.pdf"  # Mars Sol 24h 39m & telecom

def pick_nasa_url(text: str, default_url: str = URL_JUNE_2018_STORM) -> str:
    t = (text or "").lower()
    if any(k in t for k in ["electrodynamic", "eds", "static", "clings", "dust shield", "wipe", "dust devil", "cleaning", "clean"]):
        return URL_EDS
    if any(k in t for k in ["moxie", "oxygen", "co2", "breath", "air", "hepa", "life support", "human", "crew"]):
        if "kilopower" in t or "fission" in t or "nuclear" in t:
            return URL_FSP
        if "oxygen" in t or "moxie" in t or "co2" in t:
            return URL_MOXIE
    if any(k in t for k in ["kilopower", "fission", "reactor", "mmrtg", "nuclear"]):
        if "curiosity" in t:
            return URL_CURIOSITY_STORM
        return URL_KILOPOWER
    if any(k in t for k in ["radio", "delay", "22 minute", "22-minute", "relay", "blind command", "orbiter", "signal", "comm"]):
        if "spotted" in t or "tracked" in t or "sky map" in t or "weather" in t:
            return URL_MRO_EARLY
        return URL_COMM_DELAY
    if any(k in t for k in ["selfie", "gale crater"]):
        return URL_CURIOSITY_SELFIE
    if any(k in t for k in ["spotted a small", "small storm", "before it grew", "early warning", "satellites in space", "reconnaissance orbiter", "sky map"]):
        return URL_MRO_EARLY
    if any(k in t for k in ["1,000", "listened daily", "comes to an end", "mission end", "silent"]):
        if "1,000" in t or "listened" in t or "end" in t:
            return URL_OPP_END
        return URL_STORM_WATCH_2018
    if any(k in t for k in ["heat", "warm", "cool", "cold", "frigid", "temperature", "100x thinner", "thinner air", "60 mph", "insulating", "twilight", "97%", "99%", "blackout"]):
        if "opportunity" in t or "97%" in t or "99%" in t or "blackout" in t:
            return URL_STORM_WATCH_2018
        return URL_DUST_TEMP
    if "sol" in t and ("24 hours" in t or "39 minutes" in t or "martian day" in t):
        return URL_MARS_SOL
    if "opportunity" in t:
        return URL_OPP_MISSION
    if "curiosity" in t:
        return URL_CURIOSITY_STORM
    if "moxie" in t or "oxygen" in t:
        return URL_MOXIE
    return default_url

for fn in ["Assets/Resources/mission_data.json", "Assets/Resources/MarsStorm2018_Story.json"]:
    with open(fn, encoding="utf-8") as f:
        d = json.load(f)

    # 1. Intro screens
    intro_urls = {
        1: URL_JUNE_2018_STORM,
        2: URL_FSP,
        3: URL_STORM_WATCH_2018,
    }
    for scr in d.get("introScreens", []):
        scr["sourceUrl"] = intro_urls.get(scr.get("screenNumber"), URL_JUNE_2018_STORM)

    # 2. Story nodes (2018 LESSON banners) + choices
    for n in d.get("storyNodes", []):
        banner = n.get("nasaFactBanner", "")
        n["sourceUrl"] = pick_nasa_url(banner, URL_JUNE_2018_STORM)
        for k in ("choiceA", "choiceB", "choiceC"):
            c = n.get(k)
            if not c:
                continue
            combined = " ".join([
                c.get("nasaRealDecision", ""),
                c.get("nasaSource", ""),
                c.get("buttonLabel", ""),
                c.get("outcomeText", ""),
            ])
            c["sourceUrl"] = pick_nasa_url(combined, n["sourceUrl"])

    # 3. Endings
    for e in d.get("endings", []):
        combined = " ".join([
            e.get("headline", ""),
            e.get("storyText", ""),
            e.get("conditionDescription", ""),
            e.get("nasaSource", ""),
        ])
        e["sourceUrl"] = pick_nasa_url(combined, URL_OPP_END)

    # 4. Kid Glossary
    for g in d.get("kidGlossary", []):
        combined = " ".join([
            g.get("term", ""),
            g.get("headline", ""),
            g.get("simpleDefinition", ""),
            g.get("nasaSource", ""),
        ])
        g["sourceUrl"] = pick_nasa_url(combined, URL_JUNE_2018_STORM)

    with open(fn, "w", encoding="utf-8") as f:
        json.dump(d, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print("Updated", fn)
    print("  DAY_1 lesson URL:", d["storyNodes"][0]["sourceUrl"])
    print("  DAY_1 choiceA URL:", d["storyNodes"][0]["choiceA"]["sourceUrl"])
    print("  DAY_1 choiceB URL:", d["storyNodes"][0]["choiceB"]["sourceUrl"])
    print("  DAY_1 choiceC URL:", d["storyNodes"][0]["choiceC"]["sourceUrl"])
