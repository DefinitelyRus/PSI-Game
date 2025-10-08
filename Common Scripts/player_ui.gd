extends CanvasLayer
class_name PlayerUI 

@export_group("Player Info")
@export var health_bar: HSlider
@export var power: Array[Button] = []

@export_group("Inventory")
@export var inventory_slots: Array[Button] = []

@export_group("Containers")
@export var player_ui: CanvasLayer
@export var player_info: VBoxContainer
@export var player_inventory: VBoxContainer

# TEST for setting the inventory icon
var item_icons := {
	"bullets": "res://Sprites/Projectiles/Bullet.png",
}

func _ready() -> void:
	# Disables inventory slots upon load
	for i in range(inventory_slots.size()):
		inventory_slots[i].disabled = true
	
	# Disables power upon load
	for i in range(power.size()):
		power[i].disabled = true
	
	# TEST
	update_health(60, 100)
	manage_slots(3)
	set_power(1, 2)
	set_item("bullets", 2) 

## Will set the visibility of the HUD: 0 = whole PlayerUI, 1 = player infos, 2 = inventory
func set_visibility(visible: bool, target: int) -> void:
	match target:
		0: player_ui.visible = visible
		1: player_info.visible = visible
		2: player_inventory.visible = visible
		_: push_warning("That doesnt exist. . .")

## Set health value and health max value
func update_health(current_hp: int, max_hp: int) -> void:
	health_bar.min_value = 0
	health_bar.max_value = max_hp
	health_bar.value = clamp(current_hp, 0, max_hp)

## Set power value and power max value
func set_power(current_power: int, max_power: int) -> void:
	for i in range(current_power):
		if i < power.size():
			power[i].disabled = false

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
		push_warning("Icon key '%s' not found in dictionary" % icon)
