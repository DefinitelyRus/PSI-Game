using System.Collections.Generic;
using Godot;
namespace CommonScripts;

public partial class EntityManager : Node2D {
    
    #region Instance Members

    #region Godot Callbacks

    public override void _Ready() {
        if (Instance != null)
        {
            Log.Err("Multiple instances of EntityManager detected. There should only be one EntityManager in the scene.");
            QueueFree();
            return;
        }

        Instance = this;
	}

    #endregion

    #endregion

    #region Static Members
    
    public static EntityManager Instance { get; private set; } = null!;

    /// <summary>
    /// Characters that have been spawned in on the game world.
    /// </summary>
    public static List<PhysicsBody2D> Entities { get; private set; } = [];

    #region Character Management

    public static void AddCharacter(PhysicsBody2D entity)
    {
        Entities.Add(entity);
    }


    public static void AddCharacter(PhysicsBody2D entity, Vector2 position) {
        entity.Position = position;
        Instance.AddChild(entity);
        Entities.Add(entity);
    }


	public static void RemoveCharacter(PhysicsBody2D entity)
    {
        Entities.Remove(entity);
    }


    public static bool HasEntityAtPosition(Vector2 position, out PhysicsBody2D? pointedEntity) {
        foreach (PhysicsBody2D entity in Entities) {

            bool isCharacter = entity is StandardCharacter;
            bool isProp = entity is StandardPanel;
            Area2D clickArea;

            if (isCharacter) {
                StandardCharacter character = (StandardCharacter) entity;
                clickArea = character.ClickArea;
            }

            else if (isProp) {
                StandardPanel panel = (StandardPanel) entity;
                clickArea = panel.ClickArea;
            }

            else continue;

            CollisionShape2D shapeNode = clickArea.GetNode<CollisionShape2D>("CollisionShape2D");
            Shape2D shape = shapeNode.Shape;
            Vector2 localPoint = clickArea.ToLocal(position);
            bool pointingAtEntity = false;

            switch (shape) {
                case RectangleShape2D rect:
                    pointingAtEntity = new Rect2(-rect.Size / 2, rect.Size).HasPoint(localPoint);
                    break;
                case CircleShape2D circle:
                    pointingAtEntity = localPoint.Length() <= circle.Radius;
                    break;
            }

            if (pointingAtEntity) {
                pointedEntity = entity;
                return true;
            }
        }

        pointedEntity = null;
        return false;
    }

    #endregion

    #endregion
}
