var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
gm.ProceedFromOutcome();
var sol2Choices = gm.GetAvailableChoices(gm.CurrentNode);
var labels = new System.Collections.Generic.List<string>();
foreach (var c in sol2Choices) labels.Add(c.label);
return $"Advanced to {gm.CurrentNode.id} (Sol {gm.State.sol}, tau={gm.State.dustTau}): AvailableChoices={sol2Choices.Count} [{string.Join(" ;; ", labels)}]";
