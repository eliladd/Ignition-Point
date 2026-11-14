var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
var ui = UnityEngine.Object.FindAnyObjectByType<MissionUI>();
ui.ShowTitlePanel();
return $"Shown TitlePanel. NodesLoaded={gm.NodeLookup.Count}";
