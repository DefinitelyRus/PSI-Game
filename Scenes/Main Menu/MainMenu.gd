extends CanvasLayer

#var master: Master

#func _ready() -> void:
	# Verify master instance
	#if master == null:
		#print("[WARN: Map_0._ready] Master is still null. Calling members from this object may cause issues.")
		#return

func _on_play_pressed() -> void:
	pass 
	#scene_loader.load_by_id(1, true)

func _on_exit_pressed() -> void:
	get_tree().quit()
