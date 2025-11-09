using Godot;
namespace CommonScripts;

public partial class AITargetingManager : Node2D {
    #region Instance Members

    #region Targeting

    [ExportGroup("Targeting")]
    [Export] public float TargetDetectionRadius = 500.0f;
    [Export] public float TargetLostRadius = 600.0f;
    [Export] public float TargetSwitchCooldown = 1.0f;

    #endregion

    

    #endregion

    #region Static Members



    #endregion
}