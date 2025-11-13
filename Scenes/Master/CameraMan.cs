using Godot;
namespace CommonScripts;

public partial class CameraMan : Node2D {

    #region Instance Members

    #region Target Locking

    [ExportGroup("Target Locking")]
    [Export] public float TargetFollowSpeed = 500.0f;

    /// <summary>
    /// The minimum distance before the camera starts following the target.
    /// </summary>
    [Export] public float TrackingMinDistance = 15f;

    /// <summary>
    /// The distance threshold to consider a node path reached. <br/><br/>
    /// If the camera is tracking a node path and comes within this distance,
    /// it will be considered to have reached the node path.
    /// </summary>
    [Export] public float TargetReachedThreshold = 50f;

    /// <summary>
    /// The maximum distance between the camera and the target. <br/><br/>
    /// This distance is used to either snap the camera to the target,
    /// or to scale the speed of following the target.
    /// </summary>
    [Export] public float TrackingMaxDistance = 300.0f;

    [Export] public float NodePathStayTime = 1.0f;

    [Export] public bool SmoothFollow = true;

    [Signal] public delegate void TargetReachedEventHandler();

    #endregion

    #region Directional Movement

    [ExportGroup("Directional Movement")]
    [Export] public float Speed = 3.0f;

    #endregion

    #region Dragging

    [ExportGroup("Dragging")]
    [Export] public float DragThreshold = 10.0f;

    #endregion

    #region Screen Shake

    [ExportGroup("Screen Shake")]
    [Export] public float BaseShakeIntensity = 5.0f;
    [Export] public float ShakeDecayRate = 5.0f;
    [Export] public float MinShakeIntensity = 0.1f;


    #endregion

    #region Debug

    [ExportGroup("Debug")]
    [Export] public bool LogReady = true;
    [Export] public bool LogInput = false;

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
        ScanPathReached();
        ShakeDecay(delta);
    }

    #endregion

    #endregion

    #region Static Members

    public static CameraMan Instance { get; private set; } = null!;

    #region Node Paths

    public const string StartTargetName = "TARGET_NODE";

    private static Node2D[] NodePaths { get; set; } = [];
    private static int CurrentPathIndex = 0;
    private static float WaitTimer = 0f;


    public static void SetCameraPath(Node2D[] nodePaths) {
        if (nodePaths.Length == 0) {
            Log.Err(() => "No camera path nodes provided. Please provide at least one node path.");
            return;
        }

        int index = 0;
        foreach (Node2D path in nodePaths) {
            if (path == null) {
                Log.Err(() => "A camera path node is null. Please apply a valid node in the inspector.");
                return;
            }

            path.Name = $"{StartTargetName}_{index}";
            index++;
        }

        NodePaths = nodePaths;
        CurrentPathIndex = 0;
        WaitTimer = Instance.NodePathStayTime;
        SetTarget(NodePaths[CurrentPathIndex]);
    }


    private static void ScanPathReached() {
        if (Target == null) return;

        bool isNodePath = Target.Name.ToString().StartsWith(StartTargetName);
        if (!isNodePath) return;

        bool reached = HasArrivedAtTarget();
        bool waitExpired = WaitTimer <= 0f;

        if (!waitExpired && reached) {
            WaitTimer -= (float) Instance.GetProcessDeltaTime();
            return;
        }

        if (reached && isNodePath) NextNodePath();
    }


    public static void NextNodePath() {
        if (NodePaths.Length == 0) return;

        CurrentPathIndex++;
        if (CurrentPathIndex >= NodePaths.Length) return;

        SetTarget(NodePaths[CurrentPathIndex]);
        WaitTimer = Instance.NodePathStayTime;
    }


    public static void ClearCameraPath() {
        NodePaths = [];
        CurrentPathIndex = 0;
        WaitTimer = 0f;
    }

    #endregion

    #region Target Locking

    private static Node2D? Target = null!;


    public static void SetTarget(Node2D target, bool instant = false) {
        Target = target;

        bool snapToTarget = !Instance.SmoothFollow || instant;

        if (snapToTarget) Instance.GlobalPosition = Target.GlobalPosition;
    }


    public static void ClearTarget() {
        Target = null;
    }


    private static void FollowTarget(double delta) {
        if (Target == null) return;

        if (!Instance.SmoothFollow) {
            Instance.GlobalPosition = Target.GlobalPosition;
            return;
        }

        // Calculate distance to target
        Vector2 distanceVector = Instance.GlobalPosition - Target.GlobalPosition;
        double distance = distanceVector.Length();
        if (distance <= Instance.TrackingMinDistance) {
            Instance.EmitSignal(SignalName.TargetReached);
            return;
        }

        // Smoothly move towards target
        Vector2 direction = -distanceVector.Normalized();
        float speed = Mathf.Lerp(0, Instance.TargetFollowSpeed, (float) ((distance - Instance.TrackingMinDistance) / Instance.TrackingMaxDistance));

        Vector2 newPosition = Instance.GlobalPosition + (direction * speed * (float) delta);// + ShakeOffset;
        Instance.GlobalPosition = newPosition;
    }


    public static bool HasArrivedAtTarget(bool precise = false) {
        if (Target == null) return true;

        Vector2 distanceVector = Instance.GlobalPosition - Target.GlobalPosition;
        double distance = distanceVector.Length();
        float distanceThreshold = precise ? 1f : Instance.TargetReachedThreshold;
        bool isAtTarget = distance <= distanceThreshold;

        return isAtTarget;
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

        Vector2 mousePos = GetCleanMousePosition(mouseMotion.Position);

        if (LastMousePos == null) {
            Log.Me(() => $"Setting LastMousePos to {mousePos}", Instance.LogInput);
            LastMousePos = mousePos;
            return;
        }

        if (Target != null) Target = null;

        // Calculate drag distance and update camera position
        Vector2 dragDelta = mousePos - LastMousePos.Value;
        Instance.GlobalPosition -= dragDelta;
        LastMousePos = mousePos;
    }

    public static void StopDragging() {
        LastMousePos = null;
    }

    #endregion

    #region Screen Shake

    private static Vector2 OriginalPosition { get; set; }
    private static Vector2 ShakeOffset { get; set; }
    private static float CurrentShakeIntensity { get; set; }


    public static void Shake(float intensityMultiplier = 1f) {
        if (intensityMultiplier <= 0f) return;

        CurrentShakeIntensity = Instance.BaseShakeIntensity * intensityMultiplier;

        // Initial shake offset
        float offsetX = (GD.Randf() * 2 - 1) * CurrentShakeIntensity;
        float offsetY = (GD.Randf() * 2 - 1) * CurrentShakeIntensity;
        ShakeOffset = new(offsetX, offsetY);

        // Apply initial shake offset
        OriginalPosition = Instance.GlobalPosition;
        Instance.GlobalPosition += ShakeOffset;
    }


    private static void ShakeDecay(double delta) {
        if (CurrentShakeIntensity <= 0f) return;

        // Reduce shake intensity over time
        CurrentShakeIntensity -= Instance.ShakeDecayRate * (float) delta;

        // Check if shake has ended
        if (CurrentShakeIntensity < Instance.MinShakeIntensity) {
            CurrentShakeIntensity = 0f;
            Instance.GlobalPosition -= ShakeOffset;
            ShakeOffset = Vector2.Zero;
            Log.Me(() => "Camera shake ended.");
            return;
        }

        Instance.GlobalPosition -= ShakeOffset;

        // Calculate new shake offset
        float offsetX, offsetY;
        offsetX = (GD.Randf() * 2 - 1) * CurrentShakeIntensity;
        offsetY = (GD.Randf() * 2 - 1) * CurrentShakeIntensity;

        ShakeOffset = new(offsetX, offsetY);
        Instance.GlobalPosition += ShakeOffset;
    }


    public static Vector2 GetCleanMousePosition(Vector2? mousePos = null) {
        if (mousePos == null) {
            Vector2 globalMousePos = Instance.GetGlobalMousePosition();
            mousePos = globalMousePos - ShakeOffset;
        }

        else mousePos -= ShakeOffset;

        return mousePos.Value;
    }

    #endregion

    #region Other

    public static bool IsPointVisible(Vector2 point, float margin = 0f) {
        Vector2 viewportSize = Instance.GetViewportRect().Size;
        Vector2 cameraTopLeft = Instance.GlobalPosition - (viewportSize / 2);
        Vector2 cameraBottomRight = Instance.GlobalPosition + (viewportSize / 2);

        Rect2 cameraRect = new(
            cameraTopLeft - new Vector2(margin, margin),
            cameraBottomRight - cameraTopLeft + new Vector2(margin * 2, margin * 2)
        );

        return cameraRect.HasPoint(point);
    }

    #endregion

    #endregion

}