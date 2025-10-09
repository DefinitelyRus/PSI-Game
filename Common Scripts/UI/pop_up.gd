extends Control

@export var popUP_container: Control

@export_group("Texts")
@export var header_text: Label
@export var paragraph_text: Label

@export_group("Buttons")
@export var button_container: MarginContainer

@export_subgroup("Row 1")
@export var button_row1: HBoxContainer
@export var row1_buttons: Array[Button] = []

@export_subgroup("Row 2")
@export var button_row2: HBoxContainer
@export var row2_buttons: Array[Button] = []

@export_subgroup("Row 3")
@export var button_row3: HBoxContainer
@export var row3_buttons: Array[Button] = []

# Btn signal
signal button_clicked(button: Button)

# Dragging
var is_dragging := false
var drag_offset := Vector2.ZERO

func _ready():
	for button in row1_buttons:
		button.pressed.connect(_on_any_button_pressed.bind(button))
	
	for button in row2_buttons:
		button.pressed.connect(_on_any_button_pressed.bind(button))
	
	for button in row3_buttons:
		button.pressed.connect(_on_any_button_pressed.bind(button))
	
	# TEST for btn stuff and labels
	set_btn(true, 1, 1)
	set_btn(true, 2, 1)
	set_btn(true, 3, 0)
	
	set_btn_text("GAY", 1, 1)
	set_btn_text("GAY", 2, 1)
	set_btn_text("YOU", 3, 1)
	
	set_text("EXPULSION!!", "Boom! Boom! Boom!")
	
# ---------- SEWT TEXT ----------
## Set the text for the header and the main text (paragraph)
func set_text(header: String, paragraph: String) -> void:
	header_text.text = header
	paragraph_text.text = paragraph

# ---------- BUTTON ACTIVATION ----------
## Used for showing/enabling buttons.
func set_btn(visibility: bool, target_row: int, target_btn: int = -1) -> void:
	var target_array: Array[Button]
	
	match target_row:
		1: target_array = row1_buttons
		2: target_array = row2_buttons
		3: target_array = row3_buttons
		_:
			push_warning("Invalid row index: %s" % target_row)
			return
	
	if target_btn == 0:
		for button in target_array:
			button.visible = visibility
		return
	
	if target_btn >= 0 and target_btn < target_array.size():
		target_array[target_btn].visible = visibility
	else:
		push_warning("This button doesn't exist...: Row %s, Index %s" % [target_row, target_btn])
	
## Used to set the text for a button.
func set_btn_text(text: String, target_row: int, target_btn: int) -> void:
	var target_array: Array[Button]
	
	match target_row:
		1: target_array = row1_buttons
		2: target_array = row2_buttons
		3: target_array = row3_buttons
		_:
			push_warning("Invalid row index: %s" % target_row)
			return
	
	if target_btn >= 0 and target_btn < target_array.size():
		target_array[target_btn].text = text
	else:
		push_warning("This button doesn't exist...: Row %s, Index %s" % [target_row, target_btn])

# Button signals
func _on_any_button_pressed(button: Button):
	emit_signal("button_clicked", button)
	print("[Pop-up._on_any_button_pressed] %s emitted a signal!" % button.name)

# ---------- CLOSE POP-UP ----------
func _on_exit_pressed() -> void:
	queue_free()

# ---------- MAKE DRAGGABLE ----------
func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				is_dragging = true
				drag_offset = popUP_container.global_position - get_global_mouse_position()
			else:
				is_dragging = false
	elif event is InputEventMouseMotion and is_dragging:
		popUP_container.global_position = get_global_mouse_position() + drag_offset
