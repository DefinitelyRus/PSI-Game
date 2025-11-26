
extends MarginContainer
class_name OnScreenText

@export var center_label: Label
@export var subtitle: Label
@export var timer: Label
@export var fade_duration: float = 0.5

var fade_value: float

func _ready() -> void:
	center_label.visible = false
	subtitle.visible = false
	timer.visible = false
	center_label.modulate.a = 1.0
	subtitle.modulate.a = 1.0
	timer.modulate.a = 1.0


func show_center_text(text: String, duration: float = 2.0) -> void:
	await get_tree().create_timer(0.5).timeout
	
	center_label.text = text
	center_label.visible = true
	center_label.modulate.a = 1.0

	var rng: RandomNumberGenerator = RandomNumberGenerator.new()
	rng.randomize()

	var remaining: float = max(duration, 0.0)
	var flickers: int = rng.randi_range(3, 5)

	for i in range(flickers):
		var glitch_delay: float = rng.randf_range(0.03, 0.12)
		if remaining <= glitch_delay:
			break
		await get_tree().create_timer(glitch_delay).timeout
		
		# Play text tick sound
		var master_node = get_tree().root.get_child(0)
		if master_node:
			var audio_manager: Node2D = master_node.get_node("Audio Manager")
			if audio_manager:
				audio_manager.call("StreamAudio", "text_tick", 1)
				pass
			pass

		center_label.visible = !center_label.visible
		remaining -= glitch_delay

	center_label.visible = true
	if remaining > 0.0:
		await get_tree().create_timer(remaining).timeout

	await center_fade_out()
	return


func delay(duration: float) -> void:
	await get_tree().create_timer(duration).timeout
	return


func center_fade_out():
	var alpha := 1.0
	while alpha > 0.0:
		var delta := get_process_delta_time()
		if fade_duration > 0.0:
			alpha -= delta / fade_duration
		else:
			alpha = 0.0
		center_label.modulate.a = clamp(alpha, 0.0, 1.0)
		await get_tree().process_frame
	center_label.visible = false
	return



func show_subtitle(text: String, duration: float = 2.0) -> void:
	# Play text tick sound
	var master_node = get_tree().root.get_child(0)
	if master_node:
		var audio_manager: Node2D = master_node.get_node("Audio Manager")
		if audio_manager:
			audio_manager.call("StreamAudio", "text_tick", 1)
			pass
		pass
	
	subtitle.text = text
	subtitle.visible = true
	subtitle.modulate.a = 1.0

	await get_tree().create_timer(max(duration, 0.0)).timeout
	await subtitle_fade_out()
	return


func subtitle_fade_out():
	var alpha := 1.0
	while alpha > 0.0:
		var delta := get_process_delta_time()
		if fade_duration > 0.0:
			alpha -= delta / fade_duration
		else:
			alpha = 0.0
		subtitle.modulate.a = clamp(alpha, 0.0, 1.0)
		await get_tree().process_frame
	subtitle.visible = false
	return


func  set_timer_enabled(enabled: bool) -> void:
	timer.visible = enabled
	timer.modulate.a = 1.0


func set_timer_text(text: String) -> void:
	timer.text = text


func set_timer_color(color: Color) -> void:
	timer.add_theme_color_override("font_color", color)
