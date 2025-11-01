extends MarginContainer
class_name PlayerUI 

@export_group("Player Info")
@export var health_bar: HSlider
@export var power: Array[Button] = []
@export var current_character: Button

@export_group("Inventory")
@export var inventory_slots: Array[Button] = []

@export_group("Containers")
@export var hud_container: MarginContainer
@export var char_info: VBoxContainer
@export var char_inventory: VBoxContainer
@export var char_preview: VBoxContainer

@export_group("Opacity Manager")
@export var default_opacity: float = 0.8     
@export var active_opacity: float = 1.0       
@export var near_cursor_opacity: float = 0.15
@export var fade_speed: float = 3.0          
@export var active_duration: float = 2.0

var _current_opacity: float
var _active_timer: float = 0.0

# TEST: Setting the inventory and char preview icon
var item_icons := {
	"bullets": "res://Sprites/Projectiles/Bullet.png",
}

var char_icon := {
	"char1": "res://Sprites/Units/Mira Kale//Idle.png",
}

# ---------- UI MANAGER ----------
## Will set the visibility of the HUD: 0 = whole PlayerUI, 1 = player infos, 2 = inventory
func set_visibility(visibility: bool, target: int) -> void:
	match target:
		0: hud_container.visible = visibility
		1: char_info.visible = visibility
		2: char_inventory.visible = visibility
		_: push_warning("[Player_HUD.set_visibility] That doesnt exist. . .")

# ---------- HEALTH AND POWER ------------
## Set health value and health max value
func update_health(current_hp: int, max_hp: int) -> void:
	health_bar.min_value = 0
	health_bar.max_value = max_hp
	health_bar.value = clamp(current_hp, 0, max_hp)

## Set power value and power max value
func set_power(current_power: int, _max_power: int) -> void:
	for i in range(current_power):
		if i < power.size():
			power[i].disabled = false

# ----------- INVENTORY MANAGER ----------
## Set number of slots to make visible (enabled)
func manage_slots(target: int) -> void:
	for i in range(target):
		if i < inventory_slots.size():
			inventory_slots[i].disabled = false

## Set item icon to a specific hotbar slot
func set_item(icon: String, target: int) -> void:
	if target < 0 or target >= inventory_slots.size():
		push_warning("Invalid slot index: %d" % target)
		return
	
	var button := inventory_slots[target]
	
	if item_icons.has(icon):
		var tex := load(item_icons[icon]) as Texture2D
		var style := StyleBoxTexture.new()
		style.texture = tex
	
		button.add_theme_stylebox_override("normal", style)
		button.add_theme_stylebox_override("hover", style)
		button.add_theme_stylebox_override("pressed", style)
	else:
		push_warning("[Player_UI.set_item] This thing is NOT found in dictionary: " % icon)

func _on_inventory_slot_pressed() -> void:
	activate_hud()

# ---------- CHARACTER PREVIEW ----------
func set_char_preview(icon: String) -> void:
	if char_icon.has(icon):
		var tex := load(char_icon[icon]) as Texture2D
		var style := StyleBoxTexture.new()
		style.texture = tex
	
		current_character.add_theme_stylebox_override("disabled", style)
	else:
		push_warning("[Player_UI.set_char_preview] This thing is NOT found in dictionary: " % icon)

# ---------- OPACITY MANAGER ----------
func _process(delta: float) -> void:
	if _active_timer > 0:
		_active_timer -= delta
		_current_opacity = lerp(_current_opacity, active_opacity, delta * fade_speed)
	else:
		var target_opacity = default_opacity
		if is_cursor_near_hud():
			target_opacity = near_cursor_opacity
		_current_opacity = lerp(_current_opacity, target_opacity, delta * fade_speed)
	
	hud_container.modulate.a = _current_opacity

func activate_hud() -> void:
	_active_timer = active_duration

func is_cursor_near_hud() -> bool:
	var mouse_pos = get_viewport().get_mouse_position()
	var rect = hud_container.get_global_rect()
	return rect.has_point(mouse_pos)

# ---------- GODOT CALLBACKS -----------
func _ready() -> void:
	# TEST: Disables inventory slots upon load
	for i in range(inventory_slots.size()):
		var slot = inventory_slots[i]
		slot.disabled = true
		slot.pressed.connect(_on_inventory_slot_pressed)
	
	# TEST: Disables power upon load
	for i in range(power.size()):
		power[i].disabled = true
	
	# TEST for the health and power info
	update_health(60, 100)
	manage_slots(3)
	set_power(1, 2)
	set_item("bullets", 2) 
	
	# TEST: Sets the char_preview icon upon load
	set_char_preview("char1")
	
	# Opacity Manager Variables
	_current_opacity = default_opacity
	hud_container.modulate.a = _current_opacity
