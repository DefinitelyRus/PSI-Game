using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
namespace CommonScripts;

/// <summary>
/// A standard item class that can be derived from to create various types of items in the game. <br/><br/>
/// </summary>
public partial class StandardItem : Node2D
{
	#region Permanent Properties

	[ExportGroup("Permanent Properties")]
	[Export] public string ItemName = "Standard Item Template";
	[Export] public string ItemID = "StandardItemTemplate";
	[Export] public string Description = "This is a standard item template.";

	/// <summary>
	/// Various tags that can be used to categorize or filter items. <br/><br/>
	/// This is typically used when there is no better way to categorize or describe the item.
	/// </summary>
	[Export] public string[] Tags = [];

	/// <summary>
	/// The rarity of the item.
	/// </summary>
	private string _rarity = "Common";

	/// <summary>
	/// The rarity of the item. <br/><br/>
	/// This is a getter/setter for <see cref="_rarity"/>.
	/// Setting this value will validate it against <see cref="ItemRarities"/>.
	/// </summary>
	[Export] public string Rarity {
		get => _rarity;
		set {
			if (ItemRarities.Contains(value.ToPascalCase())) {
				_rarity = value.ToPascalCase();
			}
			
			else {
				Log.Me(() => $"Invalid rarity '{value.ToPascalCase()}' for item '{ItemName}'. Using default 'Common'.", true, 1);
				_rarity = "Common";
			}
		}
	}

	/// <summary>
	/// The different rarities an item can have. <br/><br/>
	/// For adding new rarities, simply add them to this array.
	/// Ensure that the names are in PascalCase.
	/// </summary>
	public static readonly string[] ItemRarities = {
		"Common",
		"Uncommon",
		"Rare",
		"Epic",
		"Legendary"
	};

	/// <summary>
	/// The base price of the item in the game's currency. <br/><br/>
	/// This value is intended to be added onto or multiplied when bought or sold in shops.
	/// </summary>
	[Export] public float BasePrice = 0f;

	[ExportSubgroup("Flags")]
	[Export] public bool CanDrop = true;
	[Export] public bool CanDegrade = false;
	[Export] public bool CanBreak = false;
	[Export] public bool IsUsable = false;
	[Export] public bool IsConsumable = false;
	[Export] public bool IsStackable = false;
	[Export] public bool IsTradable = false;
	[Export] public bool IsSellable = false;
	[Export] public bool IsBuyable = false;
	[Export] public bool DeleteOnEmpty = true;
	[Export] public bool RemoveQuantityOnBreak = true;

	#endregion

	#region Live Properties

	/// <summary>
	/// How this item is represented in the game world. <br/><br/>
	/// This value is just a marker to indicate the current state of the item.
	/// Changing its value does not affect the item's functionality, behavior, or appearance.<br/>
	/// If such changes are desired, please implement them in the relevant item management systems.
	/// </summary>
	[ExportGroup("Live Properties")]
	[Export] public EntityTypes EntityType = EntityTypes.Undefined;

	/// <summary>
	/// The different ways an item can be stored or represented in the game world. <br/><br/>
	/// <list type="bullet">
	/// <item><c>Undefined</c>: The item is not defined in any specific entity type.</item>
	/// <item><c>World</c>: The item exists in the world, such as on the ground or in a container.</item>
	/// <item><c>Inventory</c>: The item is in the player's inventory.</item>
	/// <item><c>Equipped</c>: The item is equipped by the player, such as in a weapon slot or armor slot.</item>
	/// </list>
	/// </summary>
	public enum EntityTypes {
		Undefined,
		World,
		Inventory,
		Equipped
	}

	/// <summary>
	/// A list of <see cref="StatModifier"/> objects that are applied onto the item's stats. <br/><br/>
	/// </summary>
	/// <remarks>
	/// This collection is not intended to be modified via the inspector.
	/// Only add or remove modifiers dynamically through code.
	/// </remarks>
	public List<StatModifier> Modifiers = [];

	#region Stacking

	/// <summary>
	/// How many of this item can be stacked together. <br/><br/>
	/// This is a getter/setter for <see cref="_maxQuantity"/>.
	/// Setting this value will clamp it between <c>1</c> and <see cref="int.MaxValue"/>.
	/// </summary>
	[ExportSubgroup("Stacking")]
	[Export] public int MaxQuantity {
		get => _maxQuantity; 
		set => _maxQuantity = Mathf.Clamp(value, 1, int.MaxValue);
	}

	/// <summary>
	/// How many of this item can be stacked together. <br/><br/>
	/// Do not set this value directly; use <see cref="MaxQuantity"/> instead.
	/// </summary>
	private int _maxQuantity = 99;

	/// <summary>
	/// The current quantity of this item in the stack. <br/><br/>
	/// This is a getter/setter for <see cref="_quantity"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="MaxQuantity"/>.
	/// </summary>
	/// <remarks>This value cannot be modified by a <see cref="StatModifier"/>.</remarks>
	[Export] public int Quantity {
		get => _quantity;
		set { _quantity = Mathf.Clamp(value, 0, MaxQuantity); }
	}

	/// <summary>
	/// The current quantity of this item in the stack.
	/// </summary>
	private int _quantity = 1;

	/// <summary>
	/// Adds the specified amount to the stack. <br/><br/>
	/// This method requires <see cref="IsStackable"/> to be enabled.
	/// </summary>
	/// <param name="amount">The amount of items to add to the stack.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	/// <returns>The excess amount if the total exceeds <see cref="MaxQuantity"/>.</returns>
	public int AddQuantity(int amount, bool v = false, int s = 0) {
		Log.Me(() => $"Adding {amount}x \"{ItemName}\" to stack...", v, s + 1);

		// Not stackable
		if (!IsStackable) {
			Log.Me(() => "Cannot add to stack: Item is not stackable.", v, s + 1);
			return amount;
		}

		// Invalid amount
		if (amount <= 0) {
			Log.Me(() => $"Cannot add {amount} to stack: Amount must be greater than 0. Returning 0.", v, s + 1);
			return 0;
		}

		// Excess
		int excess = Quantity + amount - MaxQuantity;
		if (excess > 0 && v) Log.Me(() => $"Exceeds max stack size by {excess}. Returning excess amount...", true, s + 1);

		// Add to stack
		Quantity += amount - excess;
		Log.Me(() => $"Added {amount} to stack. ({Quantity}/{MaxQuantity})", true, s + 1);
		return excess;
	}

	/// <summary>
	/// Merges another <see cref="StandardItem"/> of the same ID into this stack. <br/><br/>
	/// This method requires the item IDs to match and for <see cref="IsStackable"/> to be enabled.
	/// </summary>
	/// <param name="item">The <see cref="StandardItem"/> to merge with this one.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	/// <returns>The excess amount if the total exceeds <see cref="MaxQuantity"/>.</returns>
	public int MergeQuantity(StandardItem item, bool v = false, int s = 0) {
		Log.Me(() => "Adding to stack...", v, s + 1);

		// Null check
		if (item == null) {
			Log.Me(() => "Cannot add null item to stack.", v, s + 1);
			return 0;
		}

		// Invalid amount
		if (item.Quantity <= 0) {
			Log.Me(() => $"Cannot add item with quantity {item.Quantity} to stack: Quantity must be greater than 0. Returning 0.", v, s + 1);
			return 0;
		}

		// Check if this item is stackable
		if (!IsStackable) {
			Log.Me(() => "Cannot add item to stack: One or both items are not stackable.", v, s + 1);
			return item.Quantity;
		}

		// Check if item IDs match
		if (item.ItemID != ItemID) {
			Log.Me(() => $"Cannot add item with ID {item.ItemID} to stack with {ItemID}: Item IDs do not match.", v, s + 1);
			return item.Quantity;
		}

		// Add to stack
		int excess = AddQuantity(item.Quantity, v, s + 1);
		Log.Me(() => $"Added {item.Quantity} to stack. ({Quantity}/{MaxQuantity})", v, s + 1);
		return excess;
	}

	/// <summary>
	/// Removes the specified amount from the stack. <br/><br/>
	/// This method requires <see cref="IsStackable"/> to be enabled.
	/// </summary>
	/// <param name="amount">The amount of items to remove from the stack.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void RemoveQuantity(int amount, bool v = false, int s = 0) {
		Log.Me(() => "Removing from stack...", v, s + 1);

		// Not stackable
		if (!IsStackable) {
			Log.Me(() => "Cannot remove from stack: Item is not stackable.", v, s + 1);
			return;
		}

		// Invalid amount
		if (amount <= 0) {
			Log.Me(() => $"Cannot remove {amount} from stack: Amount must be greater than zero.", v, s + 1);
			return;
		}

		// Excess
		if (Quantity - amount < 0) {
			Log.Me(() => $"Cannot remove {amount} from stack; removing the entire quantity instead.", v, s + 1);
			amount = Quantity;
		}

		// Remove from stack
		Quantity -= amount;
		Log.Me(() => $"Removed {amount} from stack. New quantity: {Quantity}/{MaxQuantity}.", v, s + 1);

		// Check if stack is empty
		if (Quantity == 0 && DeleteOnEmpty) {
			Log.Me(() => "Stack is empty. Deleting item...", v, s + 1);
			QueueFree();
			Log.Me(() => "Item deleted.", v, s + 1);
		}
		
		// Log remaining quantity
		else if (Quantity == 0) Log.Me(() => "Item stack is now empty.", v, s + 1);
		Log.Me(() => $"Done!", true, s + 1);
	}

	#endregion

	#region Durability

	/// <summary>
	/// Determines how many times this item can be used. <br/><br/>
	/// This is a getter/setter for <see cref="_baseDurability"/>. <br/>
	/// Setting this value will clamp it between <c>1</c> and <see cref="int.MaxValue"/>.
	/// </summary>
	[ExportSubgroup("Durability")]
	[Export] public int BaseDurability {
		get => _baseDurability;
		set { Mathf.Clamp(value, 1, int.MaxValue); }
	}

	/// <summary>
	/// Determines how many times this item can be used. <br/><br/>
	/// Do not set this value directly; use <see cref="Durability"/> instead.
	/// </summary>
	private int _baseDurability = 100;

	/// <summary>
	/// How much durability the item currently has. <br/><br/>
	/// This is a getter/setter for <see cref="_durability"/>. <br/>
	/// Setting this value will clamp it between <c>0</c> and <see cref="int.MaxValue"/>.
	/// </summary>
	/// <remarks>This value cannot be modified by a <see cref="StatModifier"/>.</remarks>
	[Export] public int Durability {
		get => _durability;
		set { _durability = Mathf.Clamp(value, 0, int.MaxValue); }
	}

	/// <summary>
	/// How much durability the item currently has. <br/><br/>
	/// Do not set this value directly; use <see cref="Durability"/> instead.
	/// </summary>
	private int _durability = 100;

	/// <summary>
	/// Whether the item is broken or not. <br/><br/>
	/// This value cannot be set manually and returns true if <see cref="Durability"/> is equal to <c>0</c>.
	/// </summary>
	public bool IsBroken => Durability == 0;

	/// <summary>
	/// Degrades the item by the specified amount.
	/// </summary>
	/// <param name="amount">The amount of durability to remove from the item.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void Degrade(int amount = 1, bool v = false, int s = 0) {

		// Cannot be degraded
		if (!CanDegrade) {
			Log.Me(() => $"\"{ItemName}\" ({ItemID}) cannot be degraded. Returning...", v, s + 1);
			return;
		}

		// Invalid amount
		if (amount <= 0) {
			Log.Me(() => $"Cannot degrade \"{ItemName}\" (ItemID: {ItemID}) by {amount}. Amount must be greater than 0.", v, s + 1);
			return;
		}

		// Already broken
		if (IsBroken) {
			Log.Me(() => $"\"{ItemName}\" (ItemID: {ItemID}) is already broken. Cannot degrade further.", v, s + 1);
			return;
		}

		// Degrade the item
		Log.Me(() => $"Degrading \"{ItemName}\" (ItemID: {ItemID})...", v, s + 1);
		Durability -= amount;

		// Replace if broken
		if (RemoveQuantityOnBreak) {
			if (IsBroken) {
				Log.Me(() => $"\"{ItemName}\" (ItemID: {ItemID}) is broken! Removing quantity and replenishing durability...", v, s + 1);
				RemoveQuantity(1, v, s + 1);
				Durability = BaseDurability;
			}
		}

		Log.Me(() => $"Degraded \"{ItemName}\" (ItemID: {ItemID}) by {amount} ({Durability}/{BaseDurability})...", v, s + 1);
	}

	#endregion

	#region Cooldown

	/// <summary>
	/// The base amount of time to wait in seconds before the item can be used again. <br/><br/>
	/// This is a setter/getter for <see cref="_cooldownTime"/>. <br/>
	/// Setting this value will clamp it between <c>0</c> and <see cref="float.MaxValue"/>.
	/// </summary>
	[ExportSubgroup("Cooldown")]
	[Export] public float BaseCooldownTime {
		get => _baseCooldownTime;
		set => Mathf.Clamp(value, 0f, float.MaxValue);
	}

	/// <summary>
	/// The base amount of time to wait in seconds <b>total</b> before the item can be used again. <br/><br/>
	/// Do not set this value directly; use <see cref="CooldownTime"/> instead.
	/// </summary>
	private float _baseCooldownTime = 0f;

	/// <summary>
	/// The amount of time to wait in seconds <b>total</b> before the item can be used again. <br/><br/>
	/// This is calculated based on <see cref="BaseCooldownTime"/>
	/// and any modifiers targeting <c>"BaseCooldownTime"</c> or <c>"CooldownTime"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownRemaining"/>.</remarks>
	[Export] public float CooldownTime {
		get => _cooldownTime;
		set {
			float result = CalculateModifiedValue(nameof(BaseCooldownTime), nameof(CooldownTime), BaseCooldownTime, false);
			_cooldownTime = Mathf.Clamp(result, 0f, float.MaxValue);
		}
	}

	/// <summary>
	/// The amount of time to wait in seconds before the item can be used again. <br/><br/>
	/// Do not set this value directly; use <see cref="CooldownTime"/> instead.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="_cooldownRemaining"/>.</remarks>
	private float _cooldownTime = 0f;

	/// <summary>
	/// The amount of time in seconds <b>remaining</b> before the item can be used again. <br/><br/>
	/// This is a setter/getter for <see cref="_cooldownRemaining"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="CooldownTime"/>.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownTime"/>.</remarks>
	[Export] public float CooldownRemaining {
		get => _cooldownRemaining;
		set { _cooldownRemaining = Mathf.Clamp(value, 0f, CooldownTime); }
	}

	/// <summary>
	/// The amount of time in seconds <b>remaining</b> before the item can be used again. <br/><br/>
	/// Do not set this value directly; use <see cref="CooldownRemaining"/> instead.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="_cooldownTime"/>.</remarks>
	private float _cooldownRemaining = 0f;

	/// <summary>
	/// Whether the item is currently on cooldown. <br/><br/>
	/// This value cannot be set manually and returns true if <see cref="CooldownRemaining"/> greater than <c>0</c>.
	/// </summary>
	public bool IsOnCooldown => CooldownRemaining > 0f;

	/// <summary>
	/// Depletes the cooldown time by the specified delta time. <br/><br/>
	/// This method is intended to be called in the <see cref="Node2D.Process(float)"/> or <see cref="Node2D.PhysicsProcess(float)"/> methods.
	/// </summary>
	/// <param name="delta">The time between this frame and the last.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	protected void CoolDown(float delta, bool v = false, int s = 0) {
		if (CooldownRemaining == 0f) return;

		CooldownRemaining -= delta;
		if (CooldownRemaining < 0f) CooldownRemaining = 0f;

		Log.Me(() => $"Cooldown remaining: {CooldownRemaining}/{CooldownTime} seconds.", v, s + 1);
	}

	#endregion

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public Texture2D? Icon = null;
	[Export] public Sprite2D? Sprite = null;

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;

	/// <summary>
	/// This is the unique identifier for the item instance. <br/><br/>
	/// This ID is generated automatically if not set manually. <br/>
	/// This is a setter/getter for <see cref="_instanceID"/>.
	/// </summary>
	[ExportSubgroup("Instance ID")]
	[Export] public string InstanceID = string.Empty;

	/// <summary>
	/// This is the unique identifier for the item instance. <br/><br/>
	/// Do not use this value directly; use <see cref="InstanceID"/> instead.
	/// </summary>
	private string _instanceID = null!;

	/// <summary>
	/// A custom prefix for the instance ID. <br/><br/>
	/// If not provided, it will default to the <see cref="ItemName"/>. <br/>
	/// </summary>
	[Export] public string CustomPrefix = string.Empty;

	/// <summary>
	/// The action taken when a space is encountered in the character ID prefix. <br/><br/>
	/// <list type="bullet">
	/// <item><c>Keep</c>: Keeps the space. <b>Not recommended.</b></item>
	/// <item><c>Remove</c>: Removes the space.</item>
	/// <item><c>Underscore</c>: Replaces with underscores.</item>
	/// <item><c>Hyphen</c>: Replaces with hyphens.</item>
	/// </list>
	/// </summary>
	public enum SpaceReplacement {
		Keep,
		Remove,
		Underscore,
		Hyphen
	}

	/// <summary>
	/// The action taken when a space is encountered in the character ID prefix.
	/// </summary>
	[Export] public SpaceReplacement ReplaceSpaceWith = SpaceReplacement.Remove;

	/// <summary>
	/// What symbol, if any, to insert between the prefix and the random ID suffix.
	/// </summary>
	[Export] public string Separator = "#";

	/// <summary>
	/// The character selection for the random ID suffix.
	/// </summary>
	[Export] public string SuffixChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	/// <summary>
	/// How long the random ID suffix should be.
	/// </summary>
	[Export] public int SuffixLength = 5;

	/// <summary>
	/// Generates a unique `InstanceID` if one is not already assigned manually.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	private void GenerateInstanceID(bool v = false, int s = 0) {
		Log.Me($"Generating an `InstanceID`...", v, s + 1);

		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID)) {
			Log.Me($"`InstanceID` is already assigned a value (\"{InstanceID}\"). Skipping...", v, s + 1);
			return;
		}

		string prefix;
		string randomID = string.Empty;

		// Use default name if ItemName blank.
		if (string.IsNullOrEmpty(ItemName)) {
			Log.Warn("`ItemName` is empty. Using default: \"Unnamed Item\"", v, s + 1);
			ItemName = "Unnamed Item";
		}

		// Use ItemName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix)) {
			Log.Me($"`CustomPrefix` is empty. Using `ItemName` \"{ItemName}\"...", v, s + 1);
			prefix = ItemName;
		}

		// Use CustomPrefix if provided.
		else {
			Log.Me($"Using `CustomPrefix` \"{CustomPrefix}\"...", v, s + 1);
			prefix = CustomPrefix;
		}

		// Replace space with the specified type.
		switch (ReplaceSpaceWith) {

			case SpaceReplacement.Keep:
				Log.Me("Keeping spaces...", v, s + 1);
				break;

			case SpaceReplacement.Remove:
				Log.Me("Removing spaces...", v, s + 1);
				prefix = prefix.Replace(" ", "");
				break;

			case SpaceReplacement.Underscore:
				Log.Me("Replacing spaces with underscores...", v, s + 1);
				prefix = prefix.Replace(" ", "_");
				break;

			case SpaceReplacement.Hyphen:
				Log.Me("Replacing spaces with hyphens...", v, s + 1);
				prefix = prefix.Replace(" ", "-");
				break;

			default:
				Log.Warn("An invalid `SpaceReplacement` value was provided. Keeping spaces instead...", v, s + 1);
				break;

		}

		GenerateInstanceID:
		Log.Me("Generating a unique ID...", v, s + 1);
		for (int i = 0; i < SuffixLength; i++) {
			randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null) {
			Log.Me($"Instance ID \"{InstanceID}\" is already taken. Generating a new one...", v, s + 1);
			randomID = string.Empty;
			goto GenerateInstanceID;
		}

		Log.Me($"Generated ID \"{InstanceID}\"!", v, s + 1);
	}

	#endregion

	#region Common Methods

	/// <summary>
	/// Applies all relevant <see cref="StatModifier"/> values to <c>baseTarget</c> and <c>target</c>, in that order. <br/><br/>
	/// To add/remove modifiers, update the <see cref="Modifiers"/> list. <br/>
	/// These modifiers are apply chronologically based on which ones were added first.
	/// </summary>
	/// <param name="baseTarget">
	/// The name of the property acting as the base value source. <br/><br/>
	/// e.g. <c>BaseDamage</c>, <c>BasePrice</c>, <c>BaseDurability</c>
	/// </param>
	/// <param name="target">
	/// The name of the property where the value should be set to. <br/>
	/// This value is unused if <c>setToTarget</c> is <c>false</c>. <br/><br/>
	/// e.g. <c>Damage</c>, <c>Price</c>, <c>Durability</c>
	/// </param>
	/// <param name="fallback"> The value that will be returned if something goes wrong. </param>
	/// <param name="setToTarget"> Whether the return value will automatically be set to the <c>target</c> property.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	/// <returns>The value to apply to the <c>target</c> property.</returns>
	/// <exception cref="Exception"/>
	public float CalculateModifiedValue(string baseTarget, string target, float fallback = 0, bool setToTarget = true, bool v = false, int s = 0) {
		Log.Me(() => $"Calculating damage for \"{ItemName}\" ({ItemID})...", v, s + 1);

		float baseValue = 0;
		PropertyInfo targetProperty = null!;

		#region Search for target & baseTarget

		try {
			if (setToTarget) {
				//`target` null check
				if (target == null) {
					Log.Me(() => $"The `target` parameter cannot be null if `setToTarget` is true. Returning fallback value ({fallback}).", v, s + 1);
					return fallback;
				}

				// Grab `target` by name
				Log.Me(() => $"Attempting to find property \"{target}\" from class {GetType().Name}...", v, s + 1);
				targetProperty = GetType().GetProperty(target)!;

				// Missing/inaccessible `target` property
				if (targetProperty == null) {
					Log.Me(() => $"No property with name {target} found or accessible in class {GetType().Name}. Is it private/protected? Returning fallback value ({fallback}).", v, s + 1);
					return fallback;
				}

				// `target` cannot be written onto
				if (!targetProperty.CanWrite) {
					Log.Me(() => $"Property \"{target}\" cannot be written onto. Returning fallback value ({fallback}).", v, s + 1);
					return fallback;
				}

				// `target` is not a float
				if (targetProperty.PropertyType != typeof(float)) {
					Log.Me(() => $"Property \"{target}\" is not a float. Returning fallback value ({fallback}).", v, s + 1);
					return fallback;
				}

				Log.Me($"Property \"{target}\" is writable and is a float type. Proceeding...", v, s + 1);
			}

			//`baseTarget` null check
			if (baseTarget == null) {
				Log.Me(() => $"The `baseTarget` parameter cannot be null. Returning fallback value ({fallback}).", v, s + 1);
				return fallback;
			}

			// Grab `baseTarget` by name
			Log.Me(() => $"Attempting to find property \"{baseTarget}\" from class {GetType().Name}...", v, s + 1);
			PropertyInfo baseTargetProperty = GetType().GetProperty(baseTarget)!;

			// Missing/inaccessible `baseTarget` property
			if (baseTargetProperty == null) {
				Log.Me(() => $"No property with name {baseTarget} found or accessible in class {GetType().Name}. Is it private/protected? Returning fallback value ({fallback}).", v, s + 1);
				return fallback;
			}

			object baseValueObj = baseTargetProperty.GetValue(this, null)!;

			// Value is a float.
			if (baseValueObj is float f) {
				baseValue = f;
				Log.Me(() => $"Assigned value {baseValue} as baseValue.", v, s + 1);
			}

			// Value is not a float.
			else {
				Log.Me(() => $"Property \"{baseTarget}\" is not a float. Returning fallback value ({fallback}).", v, s + 1);
				return fallback;
			}
		}

		catch (Exception e) {
			Log.Err(() => $"Returning fallback value ({fallback}). {e.GetType().Name} was thrown. {e.Message}", v, s + 1);
			return fallback;
		}

		#endregion

		float result = baseValue;
		List<StatModifier> finalModifiers = [];

		#region Apply modifiers to base value

		foreach (StatModifier modifier in Modifiers) {

			// Apply modifiers to baseValue
			if (modifier.Target.Equals(baseTarget)) {
				Log.Me(() => $"Modifier \"{modifier.ID}\" found targeting \"{modifier.Target}\". Applying modifier...", v, s + 1);

				result += modifier.Type switch {
					StatModifier.ModifierType.Add => modifier.Value,
					StatModifier.ModifierType.Multiply => baseValue * (modifier.Value - 1),
					_ => throw new Exception($"Invalid modifier type {modifier.Type} for {modifier.ID}.")
				};

				Log.Me(() => $"New base value: {result}", v, s + 1);
			}

			// Queue modifiers for finalValue
			else if (modifier.Target == target) {
				Log.Me(() => $"Modifier \"{modifier.ID}\" found targeting \"{modifier.Target}\". Queueing...", v, s + 1);
				finalModifiers.Add(modifier);
			}
		}

		#endregion

		#region Apply modifiers to final value

		// Apply queued modifiers
		foreach (StatModifier modifier in finalModifiers) {
			Log.Me(() => $"Applying modifier \"{modifier.ID}\" to \"{modifier.Target}\"...", v, s + 1);

			result = modifier.Type switch {
				StatModifier.ModifierType.Add => result + modifier.Value,
				StatModifier.ModifierType.Multiply => result * modifier.Value,
				_ => throw new Exception($"Invalid modifier type {modifier.Type} for {modifier.ID}.")
			};

			Log.Me(() => $"New value: {result}", v, s + 1);
		}

		#endregion

		Log.Me(() => $"Final {target} value: {result}", v, s + 1);

		if (setToTarget) {
			Log.Me(() => $"Applying value {result} to \"{target}\"...", v, s + 1);
			targetProperty!.SetValue(this, result);
		}

		return result;
	}

	#endregion

	#region Overridable Methods

	/// <summary>
	/// Uses the item. <br/><br/>
	/// This method is intended to be called when the item is used, such as when a player consumes it or interacts with it.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void Use(bool v = false, int s = 0) {
		Log.Me(() => $"`UseItem` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is picked up by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is picked up, such as when a player collects it from the world or from a container.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnPickup(bool v = false, int s = 0) {
		Log.Me(() => $"`OnPickup` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is equipped by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is equipped, such as when put on an equipment slot.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnEquip(bool v = false, int s = 0) {
		Log.Me(() => $"`OnEquip` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is unequipped by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is unequipped, such as when removed from an equipment slot or dropped to the game world.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnUnequip(bool v = false, int s = 0) {
		Log.Me(() => $"`OnUnequip` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is dropped by a player or entity into the game world. <br/><br/>
	/// This method is intended to be called when the item is dropped, such as when a player drops it from their inventory or when an entity drops it in the world.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnDrop(bool v = false, int s = 0) {
		Log.Me(() => $"`OnDrop` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is broken by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is broken, such as when it reaches zero durability or is destroyed through other means.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnBreak(bool v = false, int s = 0) {
		Log.Me(() => $"`OnBreak` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is bought by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is bought, such as when a player purchases it from a shop or vendor.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnBuy(bool v = false, int s = 0) {
		Log.Me(() => $"`OnBuy` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Called when the item is sold by a player or entity. <br/><br/>
	/// This method is intended to be called when the item is sold, such as when a player sells it to a shop or vendor.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void OnSell(bool v = false, int s = 0) {
		Log.Me(() => $"`OnSell` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	#endregion

	#region Godot Callbacks

	public override void _Ready() {
		Log.Me(() => $"Readying \"{ItemName} (ItemID: {ItemID})\"...");

		if (Icon == null) Log.Warn(() => "No icon set for this item. Please set an icon in the inspector.", LogReady);
		if (Sprite == null) Log.Warn(() => "No sprite set for this item. Please set a sprite in the inspector.", LogReady);

		GenerateInstanceID(LogReady, 0);

		Log.Me(() => $"Item \"{ItemName}\" (ItemID: {ItemID}, InstanceID: {InstanceID}) is ready.", LogReady);
	}

	#endregion
}
