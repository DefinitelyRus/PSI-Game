using Godot;
namespace CommonScripts;

public partial class AITargetingManager : Node2D {

    #region Targeting

    [ExportGroup("Targeting")]
    [Export] public float TargetDetectionRadius = 160f;
    [Export] public float TargetLostRadius = 200f;
    [Export] public float TargetSwitchCooldown = 0.5f;
    private float _timeSinceLastTargetSwitch = 0f;

    [Export] public TargetMode CurrentTargetMode = TargetMode.Nearest;

    public enum TargetMode {
        Nearest,
        Farthest,
        LowestHealth,
        HighestHealth
    }

    public StandardCharacter? CurrentTarget { get; private set; } = null;

    public StandardCharacter? ManualTarget = null;


    private void ScanForTargets(double delta) {
        // Skip if still in cooldown
        if (_timeSinceLastTargetSwitch > 0f) {
            _timeSinceLastTargetSwitch -= (float) delta;
            return;
        }

        // Check if current target is still valid, clear if not
        if (CurrentTarget != null) {
            float distanceToCurrentTarget = ParentCharacter.Position.DistanceTo(CurrentTarget.Position);
            bool targetOutOfRange = distanceToCurrentTarget > TargetLostRadius;

            if (targetOutOfRange) {
                CurrentTarget = null;
                return;
            }

            if (!CurrentTarget.IsAlive) {
                CurrentTarget = null;
                return;
            }
        }

        // Scan for new targets
        foreach (PhysicsBody2D entity in EntityManager.Entities) {
            if (entity is not StandardCharacter character) continue;
            if (character == ParentCharacter) continue;
            Log.Me(() => $"Scanning character: {character.Name}");
            if (!character.IsAlive) continue;

            // Skip if outside detection radius
            float distanceToCharacter = ParentCharacter.Position.DistanceTo(character.Position);
            if (distanceToCharacter > TargetDetectionRadius) continue;

            // If manual target is set, only consider that one
            if (ManualTarget != null && character != ManualTarget) continue;
            
            bool isValid = CheckTargetValidity(character);
            if (isValid) break;
        }
    }

    private bool CheckTargetValidity(StandardCharacter newTarget) {
        float distanceToCurrentTarget = CurrentTarget != null ? ParentCharacter.Position.DistanceTo(CurrentTarget.Position) : float.MaxValue;
        float distanceToNewTarget = ParentCharacter.Position.DistanceTo(newTarget.Position);

        switch (CurrentTargetMode) {
            case TargetMode.Nearest:
                if (CurrentTarget == null || distanceToNewTarget < distanceToCurrentTarget) {
                    CurrentTarget = newTarget;
                    _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    return true;
                }

                break;

            case TargetMode.Farthest:
                if (CurrentTarget == null || distanceToNewTarget > distanceToCurrentTarget) {
                    CurrentTarget = newTarget;
                    _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    return true;
                }

                break;

            case TargetMode.LowestHealth:
                if (CurrentTarget == null || newTarget.Health < CurrentTarget.Health) {
                    CurrentTarget = newTarget;
                    _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    return true;
                }

                break;

            case TargetMode.HighestHealth:
                if (CurrentTarget == null || newTarget.Health > CurrentTarget.Health) {
                    CurrentTarget = newTarget;
                    _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    return true;
                }

                break;
        }

        return false;
    }

    #endregion

    #region Aiming

    [ExportGroup("Aiming")]
    [Export] public float TurnRate = 360f;

    public Vector2? AimDirection = null;
    public bool ReadyToFire = false;

    public void SetAimDirection(Vector2? directionOverride = null) {
        if (CurrentTarget == null) return;

        if (directionOverride == null) AimDirection = (CurrentTarget.Position - ParentCharacter.Position).Normalized();
        else AimDirection = directionOverride.Value.Normalized();
    }

    private void UpdateAimDirection(double delta) {
        if (AimDirection == null) return;

        Vector2 currentFacing = ParentCharacter.Control.FacingDirection;
        float angleToTarget = currentFacing.AngleTo(AimDirection.Value);
        float maxTurnAngle = Mathf.DegToRad(TurnRate) * (float) delta;

        // Rotate towards target if not already facing it
        if (Mathf.Abs(angleToTarget) <= maxTurnAngle) ParentCharacter.Control.FacingDirection = AimDirection.Value;

        // Turn incrementally towards target
        else {
            float turnDirection = Mathf.Sign(angleToTarget);
            float newAngle = currentFacing.Angle() + turnDirection * maxTurnAngle;
            Vector2 newFacing = new(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
            ParentCharacter.Control.FacingDirection = newFacing;
        }

        // Check if ready to fire
        float remainingAngle = currentFacing.AngleTo(AimDirection.Value);
        ReadyToFire = Mathf.Abs(remainingAngle) < Mathf.DegToRad(5f); // 5 degrees tolerance
    }

    #endregion

    #region Attacking

    [ExportGroup("Attacking")]
    [Export] public bool AttackPermitted = true;

    public void Attack() {
        if (!AttackPermitted) return;
        bool toAttack = CurrentTarget != null && ReadyToFire;
        ParentCharacter.Control.IsAttacking = toAttack;
    }

    #endregion

    #region Nodes & Components

    //[ExportGroup("Nodes & Components")]
    public StandardCharacter ParentCharacter => GetParent<StandardCharacter>();

    #endregion

    #region Godot Callbacks

    public override void _Process(double delta) {
        ScanForTargets(delta);
        UpdateAimDirection(delta);
        Attack();
    }

    #endregion

}