var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
gm.StartMission();
// Choose Choice 0 on Sol 1: "Deploy Atmospheric Spectrometer & Calibrate High-Gain Antenna"
// Sets flag "calibrated_antenna", Power 80->55, Integrity 90->85, Science 0->30
var sol1Choices = gm.GetAvailableChoices(gm.CurrentNode);
gm.SelectChoice(sol1Choices[0]);
return $"Sol 1 Choice 0 selected: Power={gm.State.power}, Integrity={gm.State.integrity}, Science={gm.State.science}, HasFlag(calibrated_antenna)={gm.State.HasFlag("calibrated_antenna")}";
