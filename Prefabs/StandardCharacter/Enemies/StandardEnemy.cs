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
    [Export] public AIDirector.EnemyType EnemyType = AIDirector.EnemyType.Drone;
    

    [ExportGroup("Camera Shake")]
    [Export] public float DeathCameraShakeIntensity = 0.7f;


	public override void Kill() {
        if (!IsAlive) return;

        CameraMan.Shake(DeathCameraShakeIntensity, GlobalPosition);

        // 10% chance to drop random item on death
        int randomValue = new RandomNumberGenerator().RandiRange(1, 100);
        if (randomValue <= 10) {
            UpgradeItem? item = GameManager.GetRandomDropItem(GlobalPosition);

            if (item == null) {
                Log.Me(() => $"Enemy {InstanceID} failed to drop a random item on death.");
                return;
            }

            Node level = SceneLoader.Instance.LoadedScene;
            level.AddChild(item);
            EntityManager.AddCharacter(item!);
            item?.SpawnInWorld();
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