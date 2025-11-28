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

    #region Entity Management

    public static void RegisterEntity(PhysicsBody2D entity) {
        Entities.Add(entity);
    }

    public static void AddEntity(PhysicsBody2D entity) {
        Entities.Add(entity);
    }


    public static void AddEntity(PhysicsBody2D entity, Vector2 position) {
        entity.Position = position;
        Instance.AddChild(entity);
        Entities.Add(entity);
    }


	public static void RemoveEntity(PhysicsBody2D entity) {
        Entities.Remove(entity);
    }


    public static bool HasEntityAtPosition(Vector2 position, out PhysicsBody2D? pointedEntity) {
        List<PhysicsBody2D> removedEntities = [];
        foreach (PhysicsBody2D entity in Entities) {
            if (!IsInstanceValid(entity)) {
                removedEntities.Add(entity);
                continue;
            }

            Node2D positionBase;
            CollisionShape2D? shapeNode = null;
            Shape2D shape;

            // Use click area for characters.
            if (entity is StandardCharacter character) {
                positionBase = character.ClickArea;
                shapeNode = positionBase.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

                if (shapeNode == null) {
                    Log.Warn(() => $"Entity {character.InstanceID} has no CollisionShape2D for click detection.");
                    continue;
                }

                shape = shapeNode.Shape;
            }

            // Use collision shape for props.
            else if (entity is StandardProp prop) {
                shapeNode = prop.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
                positionBase = shapeNode;

                if (shapeNode == null) {
                    Log.Warn(() => $"Entity {prop.InstanceID} has no CollisionShape2D for click detection.");
                    continue;
                }

                shape = shapeNode.Shape;
            }

            // Unsupported entity type.
            else continue;

            // Check if the position is within the shape.
            Vector2 localPoint = positionBase.ToLocal(position);
            bool pointingAtEntity = false;

            switch (shape) {
                case RectangleShape2D rect:
                    pointingAtEntity = new Rect2(-rect.Size / 2, rect.Size).HasPoint(localPoint);
                    break;
                case CircleShape2D circle:
                    pointingAtEntity = localPoint.Length() <= circle.Radius;
                    break;
            }

            // Return the entity if found.
            if (pointingAtEntity) {
                pointedEntity = entity;
                return true;
            }
        }

        foreach (PhysicsBody2D removedEntity in removedEntities) {
            RemoveEntity(removedEntity);
        }
        
        pointedEntity = null;
        return false;
    }

    #endregion

    #endregion
}
