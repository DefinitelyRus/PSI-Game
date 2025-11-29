extends MarginContainer

@onready var controls_panel: MarginContainer = $"."

@export_group("Camera")
@export var camera_controls_button: Button
@export var camera_controls_panel: MarginContainer

@export_group("Character Movement")
@export var character_movement_button: Button
@export var characters_controls_panel: MarginContainer

@export_group("Items Movement")
@export var item_controls_button: Button
@export var item_controls_panel: MarginContainer

# ---------- HELP PANEL ----------
## Enable the help panel
func enable_help_panel() -> void:
	controls_panel.visible = true

# ---------- UI MANAGER ----------
func set_visibility(target: int, visibility: bool) -> void:
	match target:
		1: camera_controls_panel.visible = visibility 
		2: characters_controls_panel.visible = visibility
		3: item_controls_panel.visible = visibility

# ---------- BUTTONS ----------
func _on_camera_pressed() -> void:
	if camera_controls_button.is_pressed():
		set_visibility(1, true)
		set_visibility(2, false)
		set_visibility(3, false)
	else:
		set_visibility(1, false)

func _on_char_movement_pressed() -> void:
	if character_movement_button.is_pressed():
		set_visibility(2, true)
		set_visibility(1, false)
		set_visibility(3, false)
	else:
		set_visibility(2, false)

func _on_items_pressed() -> void:
	if item_controls_button.is_pressed():
		set_visibility(3, true)
		set_visibility(1, false)
		set_visibility(2, false)
	else:
		set_visibility(3, false)

func _on_exit_pressed() -> void:
	controls_panel.visible = false

# ---------- GODOT CALLBACKS ----------
func _ready() -> void:
	set_visibility(1, false)
	set_visibility(2, false)
	set_visibility(3, false)
