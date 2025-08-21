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
	pass 
	#scene_loader.load_by_id(1, true)

func _on_exit_pressed() -> void:
	get_tree().quit()
