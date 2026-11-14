MissionControlSetup.ApplyItchIoWebGLSettings(false);
UnityEditor.LogEntries.Clear();
var target = UnityEditor.Build.NamedBuildTarget.WebGL;
return $"compressionFormat={UnityEditor.PlayerSettings.WebGL.compressionFormat}, decompressionFallback={UnityEditor.PlayerSettings.WebGL.decompressionFallback}, dataCaching={UnityEditor.PlayerSettings.WebGL.dataCaching}, stripEngineCode={UnityEditor.PlayerSettings.stripEngineCode}, managedStripping={UnityEditor.PlayerSettings.GetManagedStrippingLevel(target)}, scene0={UnityEditor.EditorBuildSettings.scenes[0].path}";
