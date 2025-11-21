extends TextureRect
class_name Transition

@export var on_screen_text: OnScreenText

@export var start_pos: Vector2
@export var middle_pos: Vector2
@export var end_pos: Vector2
var target_pos: Vector2 = start_pos
var _transition_elapsed: float = 0.0
var _transition_duration: float = 0.0
var _transition_active: bool = false
var _transition_start: Vector2 = start_pos

# Time in seconds to complete the transition
@export var transition_time: float = 3

var reached_middle: bool = false
var reached_end: bool = false


func start_transition(text: String = "") -> void:
	reached_middle = false
	reached_end = false
	target_pos = middle_pos
	_transition_start = position
	_transition_elapsed = 0.0
	_transition_duration = transition_time
	_transition_active = true
	if (text != ""): on_screen_text.show_center_text(text)
	return
	

func end_transition() -> void:
	reached_middle = false
	reached_end = false
	target_pos = end_pos
	_transition_start = position
	_transition_elapsed = 0.0
	_transition_duration = transition_time
	_transition_active = true
	return
	

func reset() -> void:
	target_pos = start_pos
	position = start_pos
	_transition_start = start_pos
	_transition_active = false
	_transition_elapsed = 0.0
	_transition_duration = 0.0
	return


func _ready():
	target_pos = start_pos
	position = start_pos
	_transition_start = start_pos
	_transition_active = false
	_transition_elapsed = 0.0
	_transition_duration = 0.0
	return
	


func _process(delta):
	if not _transition_active:
		return

	_transition_elapsed += delta
	var t: float = min(_transition_elapsed / _transition_duration, 1.0)
	var new_pos: Vector2 = _transition_start.lerp(target_pos, t)
	set_position(new_pos)

	if t >= 1.0:
		position = target_pos
		_transition_active = false
	return
	
