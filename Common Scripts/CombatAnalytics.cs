using System;
using System.Collections.Generic;
using Godot;
namespace CommonScripts;

public static class CombatAnalytics {
    public class DamageEvent {
        public string AttackerInstanceID = string.Empty;
        public string VictimInstanceID = string.Empty;
        public Vector2 AttackerGlobalPosition = Vector2.Zero;
        public Vector2 VictimGlobalPosition = Vector2.Zero;
        public float Distance = 0f;
        public float DamageAmount = 0f;
        public bool DidKill = false;
        public ulong TimestampMs = Time.GetTicksMsec();
    }

    public static readonly List<DamageEvent> DamageEvents = [];

    public static void Record(StandardCharacter attacker, StandardCharacter victim, float damage, bool didKill) {
        if (attacker == null || victim == null) return;
        if (string.IsNullOrEmpty(attacker.InstanceID) || string.IsNullOrEmpty(victim.InstanceID)) return;
        if (!IsNodeValid(attacker) || !IsNodeValid(victim)) return;

        DamageEvent e = new() {
            AttackerInstanceID = attacker.InstanceID,
            VictimInstanceID = victim.InstanceID,
            AttackerGlobalPosition = attacker.GlobalPosition,
            VictimGlobalPosition = victim.GlobalPosition,
            Distance = attacker.GlobalPosition.DistanceTo(victim.GlobalPosition),
            DamageAmount = damage,
            DidKill = didKill,
            TimestampMs = Time.GetTicksMsec()
        };

        DamageEvents.Add(e);

        float ax = MathF.Round(e.AttackerGlobalPosition.X, 2);
        float ay = MathF.Round(e.AttackerGlobalPosition.Y, 2);
        float vx = MathF.Round(e.VictimGlobalPosition.X, 2);
        float vy = MathF.Round(e.VictimGlobalPosition.Y, 2);
        float dist = MathF.Round(e.Distance, 2);
        float dmg = MathF.Round(e.DamageAmount, 2);
        Godot.Collections.Dictionary payload = new() {
            {"attackerInstanceID", e.AttackerInstanceID},
            {"victimInstanceID", e.VictimInstanceID},
            {"attackerPosition", new Godot.Collections.Dictionary {{"x", ax}, {"y", ay}}},
            {"victimPosition", new Godot.Collections.Dictionary {{"x", vx}, {"y", vy}}},
            {"distance", dist},
            {"damage", dmg},
            {"didKill", e.DidKill},
            {"victimHealth", MathF.Round(MathF.Max(victim.Health, 0f), 2)}
        };
        Level? level = SceneLoader.Instance.LoadedScene as Level;
        DataManager.SaveData("combat_damage", level, payload);
    }

    private static bool IsNodeValid(Node node) {
        return node.IsInsideTree();
    }
}