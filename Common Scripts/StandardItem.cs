using System.Linq;
using Godot;
namespace CommonScripts;

public partial class StandardItem : Node2D
{
	#region Permanent Properties

	[ExportGroup("Permanent Properties")]
	[Export] public string ItemName = "Standard Item Template";
	[Export] public string ItemID = "StandardItemTemplate";
	[Export] public string Description = "This is a standard item template.";
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
	[Export] protected string[] ItemRarities = {
		"Common",
		"Uncommon",
		"Rare",
		"Epic",
		"Legendary"
	};

	[ExportSubgroup("Flags")]
	[Export] public bool CanDrop = true;
	[Export] public bool CanDegrade = false;
	[Export] public bool CanBreak = false;
	[Export] public bool IsUsable = false;
	[Export] public bool IsConsumable = false;
	[Export] public bool IsStackable = false;
	[Export] public bool DeleteOnEmpty = true;
	[Export] public bool RemoveQuantityOnBreak = true;

	#endregion

	#region Live Properties

	[ExportGroup("Live Properties")]
	[Export] public EntityTypes EntityType = EntityTypes.Undefined;
	public enum EntityTypes {
		Undefined,
		World,
		Inventory,
		Equipped
	}

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
	/// How much durability the item has before it breaks. <br/><br/>
	/// This is a getter/setter for <see cref="_baseDurability"/>.
	/// Setting this value will clamp it between <c>1</c> and <see cref="int.MaxValue"/>.
	/// </summary>
	[ExportSubgroup("Durability")]
	[Export] public int BaseDurability {
		get => _baseDurability;
		set { Mathf.Clamp(value, 1, int.MaxValue); }
	}

	/// <summary>
	/// How much durability the item has before it breaks. <br/><br/>
	/// Do not set this value directly; use <see cref="Durability"/> instead.
	/// </summary>
	private int _baseDurability = 100;

	/// <summary>
	/// How much durability the item currently has. <br/><br/>
	/// This is a getter/setter for <see cref="_durability"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="BaseDurability"/>.
	/// </summary>
	[Export] public int Durability {
		get => _durability;
		set { _durability = Mathf.Clamp(value, 0, BaseDurability); }
	}

	/// <summary>
	/// How much durability the item currently has. <br/><br/>
	/// Do not set this value directly; use <see cref="Durability"/> instead.
	/// </summary>
	private int _durability = 100;

	/// <summary>
	/// Whether the item is broken or not. <br/><br/>
	/// This value cannot be set manually and returns true if <see cref="Durability"/> equal to <c>0</c>.
	/// </summary>
	public bool IsBroken => Durability == 0;

	#endregion

	#region Cooldown

	/// <summary>
	/// The amount of time to wait in seconds before the item can be used again. <br/><br/>
	/// This is a setter/getter for <see cref="_cooldownTime"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="float.MaxValue"/>.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownRemaining"/>.</remarks>
	[ExportSubgroup("Cooldown")]
	[Export] public float CooldownTime {
		get => _cooldownTime;
		set { _cooldownTime = Mathf.Clamp(value, 0f, float.MaxValue); }
	}

	/// <summary>
	/// The amount of time to wait in seconds before the item can be used again. <br/><br/>
	/// Do not set this value directly; use <see cref="CooldownTime"/> instead.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownRemaining"/>.</remarks>
	private float _cooldownTime = 0f;

	/// <summary>
	/// The amount of time <b>remaining</b> in seconds before the item can be used again. <br/><br/>
	/// This is a setter/getter for <see cref="_cooldownRemaining"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="CooldownTime"/>.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownTime"/>.</remarks>
	[Export] public float CooldownRemaining {
		get => _cooldownRemaining;
		set { _cooldownRemaining = Mathf.Clamp(value, 0f, CooldownTime); }
	}

	/// <summary>
	/// The amount of time <b>remaining</b> in seconds before the item can be used again. <br/><br/>
	/// Do not set this value directly; use <see cref="CooldownRemaining"/> instead.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="CooldownTime"/>.</remarks>
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
		if (CooldownRemaining <= 0f) return;
		CooldownRemaining -= delta;
		if (CooldownRemaining < 0f) CooldownRemaining = 0f;
		Log.Me(() => $"Cooldown remaining: {CooldownRemaining}/{CooldownTime} seconds.", v, s + 1);
	}

	#endregion

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public Texture2D Icon;
	[Export] public Sprite2D Sprite;

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
	private string _instanceID;

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
	/// Generates a unique `CharacterID` if one is not already assigned manually.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	private void GenerateItemID(bool v = false, int s = 0) {
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
			Log.Me("WARN: `ItemName` is empty. Using default: \"Unnamed Item\"", v, s + 1);
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
				Log.Me("WARN: An invalid `SpaceReplacement` value was provided. Keeping spaces instead...", v, s + 1);
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

	#region Item Actions

	/// <summary>
	/// Uses the item. <br/><br/>
	/// This method is intended to be called when the item is used, such as when a player consumes it or interacts with it.
	/// </summary>
	public virtual void Use(bool v = false, int s = 0) {
		Log.Me(() => $"`UseItem` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
		return;
	}

	/// <summary>
	/// Degrades the item by the specified amount.
	/// </summary>
	/// <param name="amount">The amount of durability to remove from the item.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public virtual void Degrade(int amount = 1, bool v = false, int s = 0) {

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

	#region Godot Callbacks

	public override void _Ready() {
		Log.Me(() => $"Readying \"{ItemName} (ItemID: {ItemID})\"...");

		GenerateItemID(LogReady, 0);

		Log.Me(() => $"Item \"{ItemName}\" (ItemID: {ItemID}, InstanceID: {InstanceID}) is ready.", LogReady);
	}

	#endregion

}
