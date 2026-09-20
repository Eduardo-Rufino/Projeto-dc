shader_type canvas_item;

// Fundo das janelas do jogo: o mesmo motivo do menu principal (Scenes/Main/menu_background.gdshader) -
// gradiente azul-petróleo com grade de hexágonos discreta e algumas células acendendo devagar -, só que
// recortado com cantos arredondados (o recorte fica 'inset' px pra dentro pra não cobrir a borda de 1px
// que o StyleBoxFlat da janela desenha por baixo) e mais escuro no geral, pra não brigar com o texto.

uniform vec4 color_edge : source_color = vec4(0.024, 0.034, 0.071, 1.0);
uniform vec4 color_mid : source_color = vec4(0.047, 0.086, 0.165, 1.0);
uniform vec4 color_glow : source_color = vec4(0.070, 0.190, 0.300, 1.0);
uniform vec4 line_color : source_color = vec4(0.36, 0.70, 0.90, 1.0);

// Tamanho do retângulo em pixels (o script atualiza ao redimensionar) e do hexágono.
uniform vec2 rect_size = vec2(520.0, 520.0);
uniform float hex_size = 44.0;
uniform float corner_radius = 10.0;
uniform float inset = 1.0;

uniform vec2 glow_center = vec2(0.5, 0.25);

float hex_dist(vec2 p) {
	p = abs(p);
	return max(dot(p, normalize(vec2(1.0, 1.73))), p.x);
}

vec4 hex_coords(vec2 uv) {
	vec2 r = vec2(1.0, 1.73);
	vec2 h = r * 0.5;
	vec2 a = mod(uv, r) - h;
	vec2 b = mod(uv - h, r) - h;
	vec2 gv = dot(a, a) < dot(b, b) ? a : b;
	return vec4(gv, uv - gv);
}

float hash21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Distância assinada até um retângulo com cantos arredondados (negativa por dentro).
float round_box(vec2 p, vec2 half_size, float r) {
	vec2 q = abs(p) - half_size + vec2(r);
	return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - r;
}

void fragment() {
	vec2 px = UV * rect_size;

	float r = max(corner_radius - inset, 0.0);
	float sd = round_box(px - rect_size * 0.5, rect_size * 0.5 - vec2(inset), r);
	float mask = 1.0 - smoothstep(-0.5, 0.5, sd);

	// O limite evita que barras muito largas (HUD) fiquem quase pretas longe do centro.
	float aspect = min(rect_size.x / rect_size.y, 2.2);
	vec2 centered = (UV - glow_center) * vec2(aspect, 1.0);
	float d = length(centered);

	vec3 col = mix(color_glow.rgb, color_mid.rgb, smoothstep(0.0, 0.7, d));
	col = mix(col, color_edge.rgb, smoothstep(0.55, 1.5, d));

	vec4 hc = hex_coords(px / hex_size);
	float edge = 0.5 - hex_dist(hc.xy);

	float fade = 1.0 - smoothstep(0.3, 1.4, d);
	float line = smoothstep(0.045, 0.0, edge) * 0.14 * (0.35 + 0.65 * fade);
	col += line_color.rgb * line;

	float h = hash21(hc.zw);
	float lit = step(0.90, h);
	float pulse = 0.5 + 0.5 * sin(TIME * 0.7 + h * 40.0);
	col += line_color.rgb * lit * pulse * 0.08 * smoothstep(0.0, 0.18, edge) * fade;

	col *= 1.0 - 0.30 * smoothstep(0.6, 1.5, d);

	COLOR = vec4(col, mask);
}
