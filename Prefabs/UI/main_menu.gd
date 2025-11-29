extends MarginContainer

@onready var main_menu: MarginContainer = $"."

@export var press_play: Label

var fade_in := true

# ---------- MAIN MENU OVERLAY ---------
## Enable/Disable main menu
func enable_main_menu(show: bool) -> void:
	main_menu.visible = show

# ---------- LABEL ANIMATION ----------
func blink() -> void:
	var tween := create_tween()

	if fade_in:
		tween.tween_property(press_play, "modulate:a", 0.2, 0.7)
	else:
		tween.tween_property(press_play, "modulate:a", 1.0, 0.7)

	tween.tween_callback(Callable(self, "_on_blink_finished"))

func _on_blink_finished() -> void:
	fade_in = !fade_in
	blink()

func _ready() -> void:
	press_play.modulate.a = 1.0
	blink()
