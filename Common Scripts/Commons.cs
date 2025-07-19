using System;
using Godot;
namespace CommonScripts;

/// <summary>
/// A class representing a modifier for a stat in a game.
/// </summary>
/// <param name="id">A unique identifier for the modifier.</param>
/// <param name="target">The target stat that this modifier affects.</param>
/// <param name="value">The value of the modifier.</param>
/// <param name="type">Represents the type of modification to apply in a calculation or operation.</param>
public class StatModifier(string id, string target, float value, StatModifier.ModifierType type) {
	/// <summary>
	/// A unique identifier for the modifier. <br/>
	/// This is used to differentiate between different modifiers,
	/// allowing dynamic addition and removal of specific modifiers. <br/>
	/// </summary>
	/// <remarks>PascalCase is enforced on this value for consistency and readability.</remarks>
	public string ID { get; init; } = id.ToPascalCase();

	/// <summary>
	/// The target stat that this modifier affects. <br/>
	/// This value must <b>exactly</b> match the variable name of the stat.
	/// </summary>
	/// <remarks>PascalCase is enforced on this value for consistency and readability.</remarks>
	public string Target { get; init; } = target.ToPascalCase();

	/// <summary>
	/// The value of the modifier.
	/// </summary>
	public float Value { get; init; } = value;

	/// <summary>
	/// Represents the type of modification to apply in a calculation or operation.
	/// <list type="bullet">
	/// <item><c>Add</c>: Adds the modifier to the target value.</item>
	/// <item><c>Multiply</c>: Multiplies the target value by the modifier.</item></list>
	/// </summary>
	public enum ModifierType {
		Add,
		Multiply
	}

	/// <summary>
	/// The type of modification to apply to the target stat.
	/// </summary>
	public ModifierType Type { get; init; } = type;

	public override string ToString() {
		return Type switch {
			ModifierType.Add => $"{ID}: {Target} {Target} + {Value}",
			ModifierType.Multiply => $"{ID}: {Target} {Target} * {Value}",
			_ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null),
		};
	}
}
