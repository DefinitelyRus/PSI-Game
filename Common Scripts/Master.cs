using Godot;
using System;

namespace CommonScripts;

public partial class Master : Node {
    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
    [Export] public SceneLoader SceneLoader { get; private set; } = null!;

    #endregion

    #region Debugging

    [ExportGroup("Debugging")]
    [Export] public bool LogReady = false;

    #endregion

    #region Methods

    public void DebugPause(bool v = false, int s = 0) {
        Log.Me(() => "Freezing game", v, s + 1);

        GetTree().Paused = true;

        Log.Me(() => "Done!", v, s + 1);
        return;
    }

    #endregion

    public override void _Ready() {
        Log.Me(() => $"Readying Master. Passing to StandardItem...", LogReady);

        if (SceneLoader == null) {
            Log.Err(() => "SceneLoader is not assigned. Cannot proceed.", LogReady);
            DebugPause(LogReady);
            return;
        }

        Log.Me(() => "Done!", LogReady);
        return;
    }
}
