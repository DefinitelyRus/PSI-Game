using System;
using Godot;
namespace CommonScripts;

public partial class CameraMan : Node2D {

    #region Instance Members

    #region Target Locking

    [ExportGroup("Target Locking")]
    [Export] public float TargetFollowSpeed = 200.0f;

    /// <summary>
    /// The minimum distance before the camera starts following the target.
    /// </summary>
    [Export] public float MinDistance = 25.0f;

    /// <summary>
    /// The maximum distance between the camera and the target. <br/><br/>
    /// This distance is used to either snap the camera to the target,
    /// or to scale the speed of following the target.
    /// </summary>
    [Export] public float MaxDistance = 500.0f;

    [Export] public bool SmoothFollow = true;

    #endregion

    #region Directional Movement

    [ExportGroup("Directional Movement")]
    [Export] public float Speed = 500.0f;

    #endregion

    #region Dragging

    [ExportGroup("Dragging")]
    [Export] public float DragThreshold = 10.0f;

    #endregion

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of CameraMan detected. There should only be one CameraMan in the scene.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _Process(double delta) {
        FollowTarget(delta);
    }

    #endregion

    #endregion

    #region Static Members

    public static CameraMan Instance { get; private set; } = null!;

    #region Target Locking

    private static Node2D? Target = null!;


    public static void SetTarget(Node2D target) {
        Target = target;

        if (Instance.SmoothFollow) {
            Instance.GlobalPosition = Instance.GlobalPosition.Lerp(Target.GlobalPosition, 0.1f);
        }

        else {
            Instance.GlobalPosition = Target.GlobalPosition;
        }
    }


    public static void ClearTarget() {
        Target = null;
    }


    private static void FollowTarget(double delta) {
        if (Target == null) return;

        Log.Me($"Following target at {Target.GlobalPosition}");

        if (!Instance.SmoothFollow) {
            Instance.GlobalPosition = Target.GlobalPosition;
            return;
        }

        // Calculate distance to target
        Vector2 distanceVector = Instance.GlobalPosition - Target.GlobalPosition;
        double distance = distanceVector.Length();
        if (distance <= Instance.MinDistance) return;

        // Smoothly move towards target
        Vector2 direction = -distanceVector.Normalized();
        float speed = Mathf.Lerp(0, Instance.TargetFollowSpeed, (float) ((distance - Instance.MinDistance) / Instance.MaxDistance));
        Instance.GlobalPosition += direction * speed * (float) delta;
    }

    #endregion

    #region Directional Movement

    public static void Move(Vector2 direction) {
        Instance.GlobalPosition += direction.Normalized() * Instance.Speed;

        if (Target != null) Target = null;
    }

    #endregion

    #region Dragging
    private static Vector2? LastMousePos { get; set; } = null;

    public static void Drag(InputEvent input) {
        if (input is not InputEventMouseMotion mouseMotion) return;

        Vector2 mousePos = mouseMotion.Position;

        if (LastMousePos == null) {
            Log.Me(() => $"Setting LastMousePos to {mousePos}");
            LastMousePos = mousePos;
            return;
        }

        if (Target != null) Target = null;

        // Calculate drag distance and update camera position
        Vector2 dragDelta = mousePos - LastMousePos.Value;
        Instance.GlobalPosition -= dragDelta;
        LastMousePos = mousePos;
        //Log.Me(() => $"Dragging camera by {dragDelta}, new position: {Instance.GlobalPosition}");
    }

    public static void StopDragging() {
        LastMousePos = null;
    }
    
    #endregion

    #endregion

}
