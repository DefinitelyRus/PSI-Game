extends CharacterBody2D

@export var speed := 150
@onready var animation_tree: AnimationTree = $AnimationTree
@onready var anim_state: AnimationNodeStateMachinePlayback = animation_tree.get("parameters/playback")

var last_direction := Vector2.DOWN  

func _physics_process(delta):
	var input_vector = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	
	velocity = input_vector * speed
	move_and_slide()
	
	if input_vector != Vector2.ZERO:
		var anim_dir = Vector2(input_vector.x, -input_vector.y)
		animation_tree.set("parameters/Walk/blend_position", anim_dir)
		anim_state.travel("Walk")
		last_direction = anim_dir
	else:
		anim_state.travel("Idle")
