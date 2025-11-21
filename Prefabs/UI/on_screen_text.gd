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
    var glitch_delay = 0

    # Glitch effect
    glitch_delay = rng.randf_range(0.005, 0.05)
    duration -= glitch_delay
    delay(glitch_delay)
    center_label.visible = false
    glitch_delay = rng.randf_range(0.005, 0.05)
    duration -= glitch_delay
    delay(glitch_delay)
    center_label.visible = true
    glitch_delay = rng.randf_range(0.005, 0.05)
    duration -= glitch_delay
    delay(glitch_delay)
    center_label.visible = false
    glitch_delay = rng.randf_range(0.005, 0.05)
    duration -= glitch_delay
    delay(glitch_delay)
    center_label.visible = true

    delay(duration)

    call_thread_safe("center_fade_out")
    return


func delay(duration: float) -> void:
    await get_tree().create_timer(duration).timeout
    return


func center_fade_out():
    var alpha = 1
    while alpha > 0:
        var delta = get_process_delta_time()
        alpha -= fade_duration * delta
        center_label.modulate.a = alpha
        await get_tree().create_timer(delta).timeout
        pass
    
    center_label.visible = false
    return



func show_subtitle(text: String, duration: float = 2.0) -> void:
    subtitle.text = text
    subtitle.visible = true

    delay(duration)
    call_thread_safe("subtitle_fade_out")
    return


func subtitle_fade_out():
    var alpha = 1
    while alpha > 0:
        var delta = get_process_delta_time()
        alpha -= fade_duration * delta
        subtitle.modulate.a = alpha
        await get_tree().create_timer(delta).timeout
        pass
    
    subtitle.visible = false
    return