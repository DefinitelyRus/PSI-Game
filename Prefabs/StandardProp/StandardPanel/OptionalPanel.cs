using Godot;
namespace CommonScripts;

public partial class OptionalPanel : StandardPanel {

    [Export] public Node2D? ItemSpawnPoint = null;
    [Export] public PackedScene? ItemPacked = null;

    public override void Interact(StandardCharacter character) {
        Activated = true;
        IsEnabled = false;

        bool shouldNotSpawn = ItemPacked == null && ItemSpawnPoint == null;
        if (shouldNotSpawn) return;

        if (ActivationSound != null) AudioManager.StreamAudio2D(ActivationSound, GlobalPosition, AudioManager.AudioChannels.SFX);

        StandardItem itemInstance = ItemPacked!.Instantiate<StandardItem>();
        SceneLoader.Instance.LoadedScene.AddChild(itemInstance);
        itemInstance.GlobalPosition = ItemSpawnPoint!.GlobalPosition;
        itemInstance.EntityType = StandardItem.EntityTypes.World;

        ItemSpawnPoint.QueueFree();
        ItemSpawnPoint = null;
    }
}