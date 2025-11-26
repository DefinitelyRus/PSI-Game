using System;
using System.Linq;
using Game;
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

        // 1% chance to drop random item on death
        int randomValue = new RandomNumberGenerator().RandiRange(1, 100);
        if (randomValue <= 100) {
            UpgradeItem? item = GameManager.GetRandomDropItem(GlobalPosition);
            item?.SpawnInWorld();

            if (item == null) Log.Me(() => $"Enemy {Name} failed to drop a random item on death.");
            else Log.Me(() => $"Enemy {Name} dropped item {item.Name} on death at position {GlobalPosition}.");
        }

		base.Kill();
	}

	public override void _Process(double delta) {
        // Flip sprite based on movement direction
        if (Velocity.X != 0 && !Tags.Contains("Unit")) {
            Sprite.FlipH = Velocity.X < 0;
        }

		base._Process(delta);
	}

    #endregion
}