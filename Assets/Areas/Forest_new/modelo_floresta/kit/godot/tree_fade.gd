# Godot 4 - anexe a um Area2D filho de cada arvore (o Sprite2D da arvore e o pai).
# O Area2D deve cobrir a copa (um retangulo/elipse sobre a parte alta da arvore).
# Digimons/NPCs precisam estar no grupo "digimon". Quando um deles esta ATRAS da arvore
# (mais acima na tela = y menor que o pe da arvore) e dentro da area, a arvore fica translucida.
extends Area2D

@export var faded_alpha := 0.45
@export var fade_time := 0.15
var _behind := 0
var _tween: Tween

func _ready() -> void:
	body_entered.connect(_on_enter)
	body_exited.connect(_on_exit)

func _on_enter(body: Node2D) -> void:
	if body.is_in_group("digimon") and body.global_position.y < get_parent().global_position.y:
		_behind += 1
		_fade(faded_alpha)

func _on_exit(body: Node2D) -> void:
	if body.is_in_group("digimon"):
		_behind = max(0, _behind - 1)
		if _behind == 0:
			_fade(1.0)

func _fade(a: float) -> void:
	if _tween: _tween.kill()
	_tween = create_tween()
	_tween.tween_property(get_parent(), "modulate:a", a, fade_time)
