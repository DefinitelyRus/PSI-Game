extends TextureRect
class_name Transition

@export var start_pos: Vector2
@export var middle_pos: Vector2
@export var end_pos: Vector2
var target_pos: Vector2 = start_pos

@export var transition_speed: float = 3

var reached_middle: bool = false
var reached_end: bool = false


func start_transition(text: String = "") -> void:
	reached_middle = false
	reached_end = false
	position = start_pos
	target_pos = middle_pos
	if (text != ""): on_screen_text.show_center_text(text)
	return
	
func end_transition() -> void:
	reached_middle = false
	reached_end = false
	position = middle_pos
	target_pos = end_pos
	return
	
func reset() -> void:
	target_pos = start_pos
	position = start_pos
	return

func _ready():
	target_pos = start_pos
	position = start_pos
	return
	

func _process(delta):
	if (position == end_pos):
		reset()
		return 
	
	var distance: float = position.distance_to(target_pos)
	if (distance < 1):
		position = target_pos
		return
	
	var time: float = delta * transition_speed
	var new_pos: Vector2 = position.lerp(target_pos, time)
	
	set_position(new_pos)
	return
	
