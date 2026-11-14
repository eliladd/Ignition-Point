var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
var sol2Choices = gm.GetAvailableChoices(gm.CurrentNode);
// Pick Choice 0 on Sol 2 ("[Antenna Calibrated] Transmit High-Priority Storm Telemetry to Earth") -> leads to ending_breakthrough
gm.SelectChoice(sol2Choices[0]);
return $"Sol 2 Choice 0 selected -> Ending: Power={gm.State.power}, Integrity={gm.State.integrity}, Science={gm.State.science}, HasFlag(telemetry_sent)={gm.State.HasFlag("telemetry_sent")}";
