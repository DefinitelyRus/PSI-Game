using Godot;
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
        OldLog.Me(() => "Freezing game", v, s + 1);

        GetTree().Paused = true;

        OldLog.Me(() => "Done!", v, s + 1);
        return;
    }

    #endregion

    public override void _Ready() {
        OldLog.Me(() => $"Readying Master. Passing to StandardItem...", LogReady);

        if (SceneLoader == null) {
            OldLog.Err(() => "SceneLoader is not assigned. Cannot proceed.", LogReady);
            DebugPause(LogReady);
            return;
        }

        OldLog.Me(() => "Done!", LogReady);
        return;
    }
}
