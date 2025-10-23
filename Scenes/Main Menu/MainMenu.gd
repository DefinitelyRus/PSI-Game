extends CanvasLayer

var master: Variant

@export var log_ready: bool = false
@export var log_interact: bool = false

func _ready() -> void:
	
	#Assign and verify master instance
	master = get_node("/root/Master")
	var masterType: String = type_string(typeof(master))
	if masterType != "Master":
		if log_ready: print("[MainMenu._ready] Master node loaded in as \"%s\"." % masterType)
		pass
	return

func _on_play_pressed() -> void:
	if (log_interact): print("[MainMenu._on_play_pressed] Starting game...")

	master.SceneLoader.LoadLevel(0, Context.new())
	return

func _on_exit_pressed() -> void:
	if (log_interact): print("[MainMenu._on_exit_pressed] Exiting game.")
	get_tree().quit()
	return
