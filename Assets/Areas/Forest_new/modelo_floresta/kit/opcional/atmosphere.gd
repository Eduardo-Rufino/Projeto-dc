# Godot 4 - sombras de folhas, nevoa e raios de luz no MUNDO (rolam devagar), vinheta na TELA.
# Use: crie um Node2D "Atmosphere" no mapa (z_index alto, acima do chao e das arvores) e anexe este script.
# Coloque os PNGs de atmosphere/ na pasta res://atmosphere/. Texturas: filtro Nearest, escala 2 (igual ao cenario).
extends Node2D

@export var map_rect := Rect2(-1200, -1200, 4400, 4600)   # area do mapa em pixels (ajuste ao seu mapa)
@export var shadow_speed := Vector2(4, 2)                  # px/s: sombras de folhas derivam devagar
@export var fog_speed := Vector2(-3, 1)
var _layers: Array = []

func _ready() -> void:
	_add_scroller("res://atmosphere/leaf_shadows_0.png", shadow_speed, 1.0)
	_add_scroller("res://atmosphere/fog_0.png", fog_speed, 1.0)
	_add_scroller("res://atmosphere/fog_1.png", fog_speed * 1.7, 0.8)
	_add_rays()
	var cl := CanvasLayer.new()
	cl.layer = 50
	var v := TextureRect.new()
	v.texture = load("res://atmosphere/vignette.png")
	v.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	v.stretch_mode = TextureRect.STRETCH_SCALE
	v.set_anchors_preset(Control.PRESET_FULL_RECT)
	v.mouse_filter = Control.MOUSE_FILTER_IGNORE
	cl.add_child(v)
	add_child(cl)

func _add_scroller(path: String, speed: Vector2, alpha: float) -> void:
	var s := Sprite2D.new()
	s.texture = load(path)
	s.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	s.texture_repeat = CanvasItem.TEXTURE_REPEAT_ENABLED
	s.region_enabled = true
	s.region_rect = Rect2(Vector2.ZERO, map_rect.size / 2.0)
	s.scale = Vector2(2, 2)
	s.centered = false
	s.position = map_rect.position
	s.modulate.a = alpha
	add_child(s)
	_layers.append([s, speed])

func _add_rays() -> void:
	var rng := RandomNumberGenerator.new(); rng.seed = 7
	for i in 9:
		var s := AnimatedSprite2D.new()
		var sf := SpriteFrames.new()
		sf.add_animation("shimmer"); sf.set_animation_speed("shimmer", 2.0)
		for f in 4:
			sf.add_frame("shimmer", load("res://atmosphere/light_ray_%d_f%d.png" % [i % 3, f]))
		s.sprite_frames = sf; s.play("shimmer"); s.frame = i % 4
		s.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
		s.scale = Vector2(2, 2)
		s.position = map_rect.position + Vector2(rng.randf() * map_rect.size.x, rng.randf() * map_rect.size.y)
		var m := CanvasItemMaterial.new(); m.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
		s.material = m
		add_child(s)

func _process(delta: float) -> void:
	for l in _layers:
		var s: Sprite2D = l[0]
		s.region_rect.position += l[1] * delta
