extends MarginContainer
class_name OnScreenText

@export var center_label: Label
@export var subtitle: Label
@export var fade_duration: float = 0.5

var fade_value: float

func _ready() -> void:
    center_label.visible = false
    subtitle.visible = false


func show_center_text(text: String, duration: float = 2.0) -> void:
    center_label.text = text
    center_label.visible = true

    var rng: RandomNumberGenerator = RandomNumberGenerator.new()
    var delay = 0

    # Glitch effect
    delay = rng.randf_range(0.005, 0.05)
    duration -= delay
    delay(delay)
    center_label.visible = false
    delay = rng.randf_range(0.005, 0.05)
    duration -= delay
    delay(delay)
    center_label.visible = true
    delay = rng.randf_range(0.005, 0.05)
    duration -= delay
    delay(delay)
    center_label.visible = false
    delay = rng.randf_range(0.005, 0.05)
    duration -= delay
    delay(delay)
    center_label.visible = true

    delay(duration)

    call_thread_safe("center_fade_out")
    return


func delay(duration: float) -> void:
    await get_tree().create_timer(duration).timeout
    return


func center_fade_out():
    # Update fadeout until alpha reaches zero
    var alpha = 1
    
    return