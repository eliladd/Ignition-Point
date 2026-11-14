var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
// Click "Play Again" (ProceedFromOutcome on an ending resets and starts mission)
gm.ProceedFromOutcome();
// Now test Early Loss: pick Choice 2 on Sol 1 ("Overdrive Rock Drill... powerDelta: -85") which drops Power from 80 to 0!
var sol1Choices = gm.GetAvailableChoices(gm.CurrentNode);
gm.SelectChoice(sol1Choices[2]);
return $"Early Loss tested: Power={gm.State.power}, Integrity={gm.State.integrity}, IsEarlyLoss={gm.State.IsEarlyLoss()}";
