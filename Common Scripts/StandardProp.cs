using System;
using Godot;
namespace CommonScripts;

public partial class StandardProp : RigidBody2D {

	#region PROPerties

	[ExportGroup("PROPerties")]
	[Export] public string PropName = null!;
	[Export] public string PropDescription = null!;
	[Export] public string PropID = null;
	[Export] public string[] Tags = [];

	#region Flags

	[ExportSubgroup("Flags")]
	[Export] public bool IsInteractable = true;
	[Export] public bool IsDestructible = false;

	#endregion

	#region Item Drops

	[ExportSubgroup("Item Drops")]
	[Export] public StandardItem[] ItemDropList = [];

	#endregion

	#endregion

	#region Live PROPerties

	[ExportGroup("Live PROPerties")]
	[Export] public float Health
	{
		get => _health;
		set => _health = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _health = 100f;

	#endregion
	
	#region Debugging

	[ExportGroup("Debugging")]
	[Export] public bool LogReady = false;
	[Export] public bool LogProcess = false;
	[Export] public bool LogPhysics = false;
	[Export] public bool LogCollision = false;

	#region Instance ID

	/// <summary>
	/// This is the unique identifier for the item instance. <br/><br/>
	/// This ID is generated automatically if not set manually. <br/>
	/// This is a setter/getter for <see cref="_instanceID"/>.
	/// </summary>
	[ExportSubgroup("Instance ID")]
	[Export]
	public string InstanceID
	{
		get => _instanceID;
		private set => _instanceID = value ?? throw new ArgumentNullException(nameof(value), "InstanceID cannot be null.");
	}

	/// <summary>
	/// This is the unique identifier for the item instance. <br/><br/>
	/// Do not use this value directly; use <see cref="InstanceID"/> instead.
	/// </summary>
	private string _instanceID = "";

	/// <summary>
	/// A custom prefix for the instance ID. <br/><br/>
	/// If not provided, it will default to the <see cref="ItemName"/>. <br/>
	/// </summary>
	[Export] public string CustomPrefix = "";

	/// <summary>
	/// The action taken when a space is encountered in the character ID prefix. <br/><br/>
	/// <list type="bullet">
	/// <item><c>Keep</c>: Keeps the space. <b>Not recommended.</b></item>
	/// <item><c>Remove</c>: Removes the space.</item>
	/// <item><c>Underscore</c>: Replaces with underscores.</item>
	/// <item><c>Hyphen</c>: Replaces with hyphens.</item>
	/// </list>
	/// </summary>
	public enum SpaceReplacement
	{
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
	private void GenerateInstanceID(bool v = false, int s = 0)
	{
		Log.Me($"Generating an `InstanceID`...", v, s + 1);

		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID))
		{
			Log.Me($"`InstanceID` is already assigned a value (\"{InstanceID}\"). Skipping...", v, s + 1);
			return;
		}

		string prefix;
		string randomID = string.Empty;

		// Use default name if PropName is blank.
		if (string.IsNullOrEmpty(PropName))
		{
			Log.Warn("`PropName` is empty. Using default: \"Unnamed Prop\"", v, s + 1);
			PropName = "Unnamed Prop";
		}

		// Use PropName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix))
		{
			Log.Me($"`CustomPrefix` is empty. Using `PropName` \"{PropName}\"...", v, s + 1);
			prefix = PropName;
		}

		// Use CustomPrefix if provided.
		else
		{
			Log.Me($"Using `CustomPrefix` \"{CustomPrefix}\"...", v, s + 1);
			prefix = CustomPrefix;
		}

		// Replace space with the specified type.
		switch (ReplaceSpaceWith)
		{

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
		for (int i = 0; i < SuffixLength; i++)
		{
			randomID += SuffixChars[(int)(GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null)
		{
			Log.Me($"Instance ID \"{InstanceID}\" is already taken. Generating a new one...", v, s + 1);
			randomID = string.Empty;
			goto GenerateInstanceID;
		}

		Log.Me($"Generated ID \"{InstanceID}\"!", v, s + 1);
	}

	#endregion

	#region Ignore Unassigned Values

	[ExportSubgroup("Ignore Unassigned Values")]
	[Export] public bool SilentlyAutoAssignDefaultName = false;
	[Export] public bool SilentlyAutoAssignInstanceID = true;

	#endregion

	#endregion

	#region Godot Callbacks

	public override void _EnterTree()
	{
		Log.Me(() => "A StandardProp has entered the tree. Checking properties...", LogReady);

		if (string.IsNullOrEmpty(PropName))
		{
			if (!SilentlyAutoAssignDefaultName) Log.Warn(() => "PropName should not be empty. Using default \"Unnamed Prop\"...");
			PropName = "Unnamed Prop";
		}

		if (string.IsNullOrEmpty(InstanceID))
		{
			if (!SilentlyAutoAssignInstanceID) Log.Warn(() => "InstanceID is not assigned. Generating a new one...", LogReady);
			GenerateInstanceID(LogReady);
		}

		Log.Me(() => "Done!", LogReady);
	}

	public override void _Ready()
	{
		Log.Me(() => $"Readying {InstanceID}...", LogReady);

		Log.Me(() => $"Changing node name to \"{InstanceID}\"...", LogReady);
		Name = InstanceID;

		Log.Me(() => "Done!", LogReady);
	}

	#endregion

}
