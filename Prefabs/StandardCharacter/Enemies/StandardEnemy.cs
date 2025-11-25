using Godot;
namespace CommonScripts;

public partial class StandardEnemy : StandardCharacter {

    #region Spawn Properties

    [ExportGroup("Spawn Properties")]
    [ExportSubgroup("Static Spawning")]
    [Export] public int StaticSpawnWeight = 1;
    [Export] public float DelayAfterSpawn = 1f;


    [ExportSubgroup("Dynamic Spawning")]
    [Export] public int DynamicSpawnCost = 0;

    [ExportGroup("Camera Shake")]
    [Export] public float DeathCameraShakeIntensity = 0.7f;

	public override void Kill() {
        CameraMan.Shake(DeathCameraShakeIntensity, GlobalPosition);

		base.Kill();
	}

    #endregion
}