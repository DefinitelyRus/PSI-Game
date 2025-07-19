using System;
using Godot;
namespace CommonScripts;

public partial class StandardWeapon : StandardItem
{
	#region Weapon Stats

	#region Damage

	/// <summary>
	/// The base damage dealt by the weapon.
	/// </summary>
	[ExportGroup("Weapon Stats")]
	[Export] public float BaseDamage = 10f;

	/// <summary>
	/// Returns the calculated damage value after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseDamage"/>
	/// and any modifiers targeting <c>"BaseDamage"</c> or <c>"Damage"</c>, in that order. <br/>
	/// However, this value is <i>before</i> applying any critical damage modifiers.
	/// For that, add <see cref="CriticalDamage"/> to this value. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float Damage {
		get => _damage;
		set => _damage = CalculateModifiedValue(nameof(BaseDamage), nameof(Damage), BaseDamage, false);
	}

	/// <summary>
	/// The calculated damage value after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="Damage"/> instead.
	/// </summary>
	private float _damage = 0f;

	#endregion

	#region Attack Range

	/// <summary>
	/// The base range of the weapon, in pixels.
	/// </summary>
	[Export] public float BaseRange = 640f;

	/// <summary>
	/// Gets the effective range value after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseRange"/>
	/// and any modifiers targeting <c>"BaseRange"</c> or <c>"Range"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float Range {
		get => _range;
		set => _range = CalculateModifiedValue(nameof(BaseRange), nameof(Range), BaseRange, false);
	}

	/// <summary>
	/// The effective range value after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="Range"/> instead.
	/// </summary>
	private float _range = 0f;

	#endregion

	#region Attack Speed

	/// <summary>
	/// Defines how long the weapon waits before a subsequent attack.
	/// </summary>
	[ExportGroup("Weapon Speed")]
	[Export] public float BaseAttackSpeed = 1f;

	/// <summary>
	/// Returns the calculated attack speed of the weapon after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseAttackSpeed"/>
	/// and any modifiers targeting <c>"BaseAttackSpeed"</c> or <c>"AttackSpeed"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float AttackSpeed {
		get => _attackSpeed;
		set => _attackSpeed = CalculateModifiedValue(nameof(BaseAttackSpeed), nameof(AttackSpeed), BaseAttackSpeed, false);
	}

	/// <summary>
	/// The calculated attack speed of the weapon after applying all relevant modifiers. <br/><br/>
	/// </summary>
	private float _attackSpeed = 0f;

	/// <summary>
	/// Returns the auto-calculated interval between attacks in seconds.
	/// </summary>
	public float AttackInterval {
		get {
			return SpeedType switch {
				SpeedCalculation.Hertz => 1f / BaseAttackSpeed,
				SpeedCalculation.Interval => BaseAttackSpeed,
				_ => throw new Exception("Invalid Speed Calculation Type."),
			};
		}
	}

	/// <summary>
	/// Specifies the method used to calculate speed in a system. <br/><br/>
	/// <list type="bullet">
	/// <item>
	/// <term>Hertz</term>
	/// <description>Represents speed as a frequency, measured in cycles per second.</description>
	/// </item>
	/// <item>
	/// <term>Interval</term>
	/// <description>Represents speed as a time interval between events, typically in seconds.</description>
	/// </item>
	/// </list>
	/// </summary>
	public enum SpeedCalculation {
		Hertz,
		Interval
	}

	/// <summary>
	/// The type of speed calculation used for the weapon's attack speed.
	/// </summary>
	[Export] public SpeedCalculation SpeedType = SpeedCalculation.Hertz;

	#endregion

	#endregion

	#region Critical Attacks

	#region Activation

	/// <summary>
	/// The activation condition wherein a critical attack can occur.
	/// </summary>
	[ExportGroup("Critical Attacks")]
	[Export] public CriticalActivationTypes CriticalActivationType = CriticalActivationTypes.Chance;

	/// <summary>
	/// The different types of critical activation conditions. <br/><br/>
	/// <list type="bullet">
	/// <item>
	/// <term>Chance</term>
	/// <description>Critical hits are determined by a random chance based on <see cref="CriticalChance"/>.</description>
	/// </item>
	/// <item>
	/// <term>Weakpoint</term>
	/// <description>Critical hits occur when attacking a weak point on a target character.</description>
	/// </item>
	/// <item>
	/// <term>Both</term>
	/// <description>Critical hits can occur both by chance or by hitting a weak point.</description>
	/// </item>
	/// </list>
	/// </summary>
	public enum CriticalActivationTypes {
		Chance,
		Weakpoint,
		Both
	}

	/// <summary>
	/// The base chance of a critical attack occurring. <br/><br/>
	/// This is a setter/getter for <see cref="_baseCriticalChance"/>. <br/>
	/// Setting this value will clamp it between <c>0.0</c> and <c>1.0</c>.
	/// </summary>
	[Export(PropertyHint.Range, "0, 1, 0.01")] public float BaseCriticalChance {
		get => _baseCriticalChance;
		set => _baseCriticalChance = Mathf.Clamp(value, 0f, 1f);
	}

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of <c>0.0</c> to <c>1.0</c>. <br/><br/>
	/// Do not set this value directly; use <see cref="BaseCriticalChance"/> instead.
	/// </summary>
	private float _baseCriticalChance = 0.1f;

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of <c>0.0</c> to <c>1.0</c>. <br/><br/>
	/// This is calculated based on <see cref="BaseCriticalChance"/>
	/// and any modifiers targeting <c>"BaseCriticalChance"</c> or <c>"CriticalChance"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float CriticalChance {
		get => _criticalChance;
		set {
			float result = CalculateModifiedValue(nameof(BaseCriticalChance), nameof(CriticalChance), BaseCriticalChance, false);
			_criticalChance = Mathf.Clamp(result, 0, 1);
		}
	}

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of 0 to 1. <br/><br/>
	/// Do not set this value directly; use <see cref="CriticalChance"/> instead.
	/// </summary>
	private float _criticalChance = 0f;

	#endregion

	#region Damage

	/// <summary>
	/// The base critical damage dealt by the weapon.
	/// </summary>
	[Export] public float BaseCriticalDamage = 0f;

	/// <summary>
	/// The critical damage dealt by the weapon after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseCriticalDamage"/>
	/// and any modifiers targeting <c>"BaseCriticalDamage"</c> or <c>"CriticalDamage"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// <b>Important</b>: This value is to be added to the <see cref="Damage"/> value;
	/// it is not the total damage dealt. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float CriticalDamage {
		get => _criticalDamage;
		set => _criticalDamage = CalculateModifiedValue(nameof(BaseCriticalDamage), nameof(CriticalDamage), BaseCriticalDamage, false);
	}

	/// <summary>
	/// The critical damage dealt by the weapon after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="CriticalDamage"/> instead.
	/// </summary>
	private float _criticalDamage = 0f;

	#endregion

	#endregion

	#region Nodes & Components



	#endregion

	#region Overridable Methods



	#endregion

	#region Godot Callbacks



	#endregion
}