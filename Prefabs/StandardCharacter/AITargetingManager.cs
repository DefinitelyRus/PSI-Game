using System.Collections.Generic;
using System.Linq;
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

    public StandardCharacter? CurrentTarget { get; set; } = null;

    public StandardCharacter? ManualTarget = null;

    [Export] public bool EnableScanning = true;

    private void ScanForTargets(double delta) {
        if (!EnableScanning) return;
        if (!Character.IsAlive) return;
        if (!IsInstanceValid(Character)) return;

        // When explicit targeting is active, do NOT scan for new targets.
        bool targetingFlag = Character.AIAgent.Targeting;
        // If we've exited explicit targeting mode, release any manual target so scanning works normally.
        if (!targetingFlag && ManualTarget != null) {
            // Clear manual target reference if it's no longer valid or we are back to search mode.
            if (!IsInstanceValid(ManualTarget) || !ManualTarget.IsAlive) ManualTarget = null;
            else ManualTarget = null; // Always clear after leaving targeting mode to allow free selection.
        }
        if (targetingFlag) {
            // Maintain current target only: validate and keep aiming, else clear.
            if (CurrentTarget != null) {
                if (CheckTargetValidity(CurrentTarget, false)) {
                    SetAimDirection();
                } else {
                    ClearTarget();
                }
            }
            return; // Skip any scanning logic.
        }

        // Skip if still in cooldown (only applies while scanning/searching)
        if (_timeSinceLastTargetSwitch > 0f) {
            _timeSinceLastTargetSwitch -= (float) delta;
            return;
        }

        // Determine whether scanning is allowed.
        bool isUnit = Character.Tags.Contains("Unit");
        bool searchingFlag = Character.AIAgent.Searching;
        bool isStandingStill = !Character.AIAgent.HasDestination;

        bool allowScan = (isUnit && searchingFlag) || isStandingStill || Character.Tags.Contains("Enemy");
        if (!allowScan) return;

        // First validate existing target (non-targeting mode)
        if (CurrentTarget != null) {
            bool stillValid = CheckTargetValidity(CurrentTarget, false);
            if (stillValid) {
                SetAimDirection();
                return;
            } else {
                // For enemies, optionally continue moving toward last target position.
                if (Character.Tags.Contains("Enemy")) Character.AIAgent.GoTo(CurrentTarget.GlobalPosition);
                ClearTarget();
            }
        }

        // Perform scan for new targets.
        Node loadedScene = SceneLoader.Instance.LoadedScene;
        List<StandardCharacter> potentialTargets = [.. loadedScene.GetChildren(false).OfType<StandardCharacter>()];

        foreach (PhysicsBody2D entity in potentialTargets) {
            if (entity is not StandardCharacter candidate) continue;
            if (candidate == Character) continue;
            if (candidate == CurrentTarget) continue;
            if (!candidate.IsAlive) continue;

            float distance = Character.Position.DistanceTo(candidate.Position);
            if (distance > TargetDetectionRadius) continue;
            // Only restrict to manual target while in explicit targeting mode.
            if (targetingFlag && ManualTarget != null && candidate != ManualTarget) continue;

            if (CheckTargetValidity(candidate)) {
                CurrentTarget = candidate;
                SetAimDirection();
                break;
            }
        }
    }

    private bool CheckTargetValidity(StandardCharacter target, bool setAsCurrent = true) {
        if (!IsInstanceValid(target)) return false;

        float distanceToCurrentTarget = CurrentTarget != null ? Character.Position.DistanceTo(CurrentTarget.Position) : float.MaxValue;
        float distanceToNewTarget = Character.Position.DistanceTo(target.Position);
        bool closerThanCurrent = distanceToNewTarget < distanceToCurrentTarget;
        bool fartherThanCurrent = distanceToNewTarget > distanceToCurrentTarget;
        bool isCurrentTarget = target == CurrentTarget;

        bool inRange = distanceToNewTarget <= TargetDetectionRadius;
        bool isAlive = target.IsAlive;

        if (!inRange || !isAlive) return false;

        if (Character.Tags.Contains("Unit") && target.Tags.Contains("Unit")) return false;
        if (Character.Tags.Contains("Enemy") && target.Tags.Contains("Enemy")) return false;

        switch (CurrentTargetMode) {
            case TargetMode.Nearest:
                if (closerThanCurrent || isCurrentTarget) {
                    if (setAsCurrent) {
                        CurrentTarget = target;
                        _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    }

                    return true;
                }

                break;

            case TargetMode.Farthest:
                if (fartherThanCurrent || isCurrentTarget) {
                    if (setAsCurrent) {
                        CurrentTarget = target;
                        _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    }

                    return true;
                }

                break;

            case TargetMode.HighestHealth:

                float currentHealth = CurrentTarget != null ? CurrentTarget.Health : float.MinValue;
                float targetHealth = target.Health;
                bool higherHealthThanCurrent = targetHealth > currentHealth;

                if (higherHealthThanCurrent || isCurrentTarget) {
                    if (setAsCurrent) {
                        CurrentTarget = target;
                        _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    }
                    
                    return true;
                }

                break;

            case TargetMode.LowestHealth:

                float currentLowHealth = CurrentTarget != null ? CurrentTarget.Health : float.MaxValue;
                float targetLowHealth = target.Health;
                bool lowerHealthThanCurrent = targetLowHealth < currentLowHealth;
                if (lowerHealthThanCurrent || isCurrentTarget) {
                    if (setAsCurrent) {
                        CurrentTarget = target;
                        _timeSinceLastTargetSwitch = TargetSwitchCooldown;
                    }

                    return true;
                }

                break;
        }
        return false;
    }


    public void ClearTarget() {
        CurrentTarget = null;
        AimDirection = null;
    }

    #endregion

    #region Aiming

    [ExportGroup("Aiming")]
    [Export] public float TurnRate = 360f;

    public Vector2? AimDirection = null;
    public bool ReadyToFire = false;

    public void SetAimDirection(Vector2? directionOverride = null) {
        if (CurrentTarget == null) return;

        // Set aim direction towards current target
        Vector2 targetPosition = CurrentTarget.HitArea.GlobalPosition;
        if (directionOverride == null) AimDirection = (targetPosition - Character.GlobalPosition).Normalized();

        // Use provided direction override
        else AimDirection = directionOverride.Value.Normalized();
    }

    private void UpdateAimDirection(double delta) {
        if (AimDirection == null) return;
        if (!Character.IsAlive) return;

        // Skip if character is moving
        bool isCharacterMoving = Character.Control.MovementDirection.Length() > 0f;
        if (isCharacterMoving) {
            ReadyToFire = false;
            return;
        }

        Vector2 currentFacing = Character.Control.FacingDirection;
        float angleToTarget = currentFacing.AngleTo(AimDirection.Value);
        float maxTurnAngle = Mathf.DegToRad(TurnRate) * (float) delta;

        // Rotate towards target if not already facing it
        if (Mathf.Abs(angleToTarget) <= maxTurnAngle) Character.Control.FacingDirection = AimDirection.Value;

        // Turn incrementally towards target
        else {
            float turnDirection = Mathf.Sign(angleToTarget);
            float newAngle = currentFacing.Angle() + turnDirection * maxTurnAngle;
            Vector2 newFacing = new(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
            Character.Control.FacingDirection = newFacing;
        }

        // Check if ready to fire
        Vector2 updatedFacing = Character.Control.FacingDirection;
        float remainingAngle = updatedFacing.AngleTo(AimDirection.Value);
        ReadyToFire = Mathf.Abs(remainingAngle) < Mathf.DegToRad(5f);
    }

    #endregion

    #region Attacking

    [ExportGroup("Attacking")]
    [Export] public bool AttackPermitted = true;

    public void Attack() {
        if (!AttackPermitted) return;
        if (!Character.IsAlive) {
            Character.Control.IsAttacking = false;
            return;
        }

        // Skip if character is moving
        bool isCharacterMoving = Character.Control.MovementDirection.Length() > 0f;
        if (isCharacterMoving) {
            Character.Control.IsAttacking = false;
            return;
        }

        bool toAttack = CurrentTarget != null && ReadyToFire;
        Character.Control.IsAttacking = toAttack;
    }

    #endregion

    #region Nodes & Components

    public StandardCharacter Character => GetParent<StandardCharacter>();

    #endregion

    #region Godot Callbacks

    public override void _Process(double delta) {
        ScanForTargets(delta);
        UpdateAimDirection(delta);
        Attack();
    }

    #endregion

}