using Godot;
namespace CommonScripts;

public partial class HitArea : Area2D
{
	public void OnBodyEntered(Node2D body)
	{
		Log.Me(() => $"Body entered: {body.Name}");
	}
}
