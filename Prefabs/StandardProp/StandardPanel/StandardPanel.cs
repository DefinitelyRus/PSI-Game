using Godot;
namespace CommonScripts;

public partial class StandardPanel : StandardProp {
	[Export] public Area2D ClickArea = null!;

	public virtual void Interact(StandardCharacter unit) {
		Log.Warn(() => $"`UseItem` on {PropID} is not implemented! Override to add custom functionality.");
		return;
	}

	public override void _EnterTree() {
		base._EnterTree();
	}
}
