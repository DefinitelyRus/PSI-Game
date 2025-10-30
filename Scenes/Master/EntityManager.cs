using System.Collections.Generic;
using Godot;
namespace CommonScripts;

public partial class EntityManager : Node2D
{
    /// <summary>
    /// Characters that have been spawned in on the game world.
    /// </summary>
    public static List<StandardCharacter> LiveCharacters { get; private set; } = [];


    public static void AddCharacter(StandardCharacter character)
    {
        LiveCharacters.Add(character);
    }


    public static void RemoveCharacter(StandardCharacter character)
    {
        bool wasRemoved = LiveCharacters.Remove(character);
        if (!wasRemoved) Log.Warn($"Character {character.InstanceID} not found in `LiveCharacters`. Cannot remove.");
    }


}
