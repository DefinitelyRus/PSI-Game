extends MarginContainer
class_name PlayerUI 

@export_group("Player Info")
@export var health_bar: HSlider
@export var power: Array[Button] = []
@export var current_character: Label

@export_group("Inventory")
@export var inventory_slots: Array[Button] = []

@export_group("Containers")
@export var hud_container: MarginContainer
@export var char_info: VBoxContainer
@export var char_inventory: VBoxContainer
@export var char_preview: VBoxContainer

@export var item_icons: Dictionary[String, Texture2D] = {}

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
			pass
		pass

## Set health bar color
func set_health_color(character: String) -> void:
	var grabber_style: StyleBoxFlat = health_bar.get("theme_override_styles/grabber_area") as StyleBoxFlat
	var mira_color = Color(145/255.0, 60/255.0, 81/255.0)
	var orra_color =  Color(60/255.0, 41/255.0, 94/255.0)
	
	match character:
		"mira_kale":
			grabber_style.bg_color = mira_color
			print("Updated grabber color to:", mira_color)
		"orra_kale": 
			grabber_style.bg_color = orra_color
			print("Updated grabber color to:", orra_color)


# ----------- INVENTORY MANAGER ----------
## Set number of slots to make visible (enabled)
func manage_slots(target: int) -> void:
	for i in range(target):
		if i < inventory_slots.size():
			inventory_slots[i].disabled = false

## Set item icon to a specific hotbar slot
func set_item(icon: Texture2D, target: int) -> void:
	if target < 0 or target >= inventory_slots.size():
		push_warning("Invalid slot index: %d" % target)
		return
	
	var button := inventory_slots[target]
	var style := StyleBoxTexture.new()
	style.texture = icon

	button.add_theme_stylebox_override("normal", style)
	button.add_theme_stylebox_override("hover", style)
	button.add_theme_stylebox_override("pressed", style)

## Set item alpha to a specific hotbar slot
func set_item_alpha(target: int, alpha: float) -> void:
	if target < 0 or target >= inventory_slots.size():
		push_warning("Invalid slot index: %d" % target)
		return
	
	var button := inventory_slots[target]
	button.modulate.a = alpha

# ---------- CHARACTER PREVIEW ----------
## Set the name of the currently active character
func set_character_name(character: String) -> void:
	match character:
		"mira_kale": current_character.text = "Mira"
		"orra_kale": current_character.text = "Orra"
		_: current_character.text = ""

# ---------- GODOT CALLBACKS -----------
func _ready() -> void:
	set_health_color("mira_kale")
	pass
	## TEST: Disables inventory slots upon load
	#for i in range(inventory_slots.size()):
		#var slot = inventory_slots[i]
		#slot.disabled = true
		#slot.pressed.connect(_on_inventory_slot_pressed)
	#
	## TEST: Disables power upon load
	#for i in range(power.size()):
		#power[i].disabled = true
	#
	## TEST for the health and power info
	#update_health(60, 100)
	#manage_slots(3)
	#set_power(1, 2)
	#
	## TEST: Sets the char name upon load
	#set_character_name("mira")
