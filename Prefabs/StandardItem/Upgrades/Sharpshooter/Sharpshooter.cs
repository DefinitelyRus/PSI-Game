using CommonScripts;
namespace Game;

public partial class Sharpshooter : UpgradeItem {
    
    public override void PowerOn() {
        AITargetingManager? targeter = OwnerCharacter?.TargetingManager;

        if (targeter == null) {
            Log.Warn(() => $"PowerOn called on {ItemName}, but OwnerCharacter has no TargetingManager.");
            return;
        }

        OriginalValue = targeter.TargetDetectionRadius;

        targeter.TargetDetectionRadius *= Value;
        base.PowerOn();
        return;
    }

    public override void PowerOff() {
        AITargetingManager? targeter = OwnerCharacter?.TargetingManager;

        if (targeter == null) {
            Log.Warn(() => $"PowerOff called on {ItemName}, but OwnerCharacter has no TargetingManager.");
            return;
        }

        targeter.TargetDetectionRadius = OriginalValue;
        base.PowerOff();
        return;
    }
}