using System.Collections.Generic;
using System.Linq;
using CommonScripts;
using Godot;
namespace Game;

public partial class MovementMarker : Node2D {
    [Export] public AnimationPlayer AnimationPlayer = null!;
    [Export] public Sprite2D MarkerSprite = null!;
    [Export] public float ScanRadius = 32.0f;
    public StandardCharacter OwnerCharacter = null!;
    public string? OwnerName = null;

    public override void _EnterTree() {
        if (AnimationPlayer == null) {
            Log.Err(() => "AnimationPlayer is null in MovementMarker. Please assign it in the inspector.");
        }

        if (MarkerSprite == null) {
            Log.Err(() => "MarkerSprite is null in MovementMarker. Please assign it in the inspector.");
        }

        if (OwnerCharacter == null) {
            Log.Err(() => "OwnerCharacter is null in MovementMarker. Please assign it before adding to scene.");
        }
    }

	public override void _Ready() {
        OwnerName = null;
		OwnerName = OwnerCharacter.CharacterID switch {
			"mira_kale" => "Mira",
			"orra_kale" => "Orra",
			_ => null,
		};

        if (OwnerName == null) {
            Log.Err(() => "OwnerCharacter has invalid CharacterID in MovementMarker. Cannot set OwnerName.");
            return;
        }

		AnimationPlayer.Play($"{OwnerName}_Marker");

        // Kill other markers owned by the same character
        if (SceneLoader.Instance.LoadedScene is Level level) {
            foreach (Node2D node in level.GetChildren().OfType<Node2D>()) {
                bool isMarker = node is MovementMarker;
                if (!isMarker) continue;

                MovementMarker marker = (MovementMarker) node;
                bool isSelf = marker == this;
                bool sameOwner = marker.OwnerName == OwnerName;

                if (isMarker && !isSelf && sameOwner) marker.QueueFree();
            }
        }
    }

    public override void _Process(double delta) {
        if (Master.IsPaused) return;

        if (SceneLoader.Instance.LoadedScene is not Level) return;

        bool ownerAlive = OwnerCharacter.IsAlive;
        bool ownerValid = IsInstanceValid(OwnerCharacter);
        if (!ownerAlive || !ownerValid) {
            QueueFree();
            return;
        }

        List<StandardCharacter> units = [.. Commander.GetAllUnits()];
        
        foreach (StandardCharacter unit in units) {
            bool sameOwner = unit == OwnerCharacter;
            if (!sameOwner) continue;

            float distance = Position.DistanceTo(unit.Position);

            if (distance <= ScanRadius) {
                QueueFree();
                return;
            }
        }
    }
}
