shader_type canvas_item;

// Fundo do menu principal: gradiente radial azul-petróleo, uma grade de hexágonos discreta
// (o mesmo motivo do grid do Center) com algumas células acendendo devagar, e uma vinheta.

uniform vec4 color_edge : source_color = vec4(0.020, 0.027, 0.059, 1.0);
uniform vec4 color_mid : source_color = vec4(0.043, 0.082, 0.161, 1.0);
uniform vec4 color_glow : source_color = vec4(0.078, 0.263, 0.396, 1.0);
uniform vec4 line_color : source_color = vec4(0.36, 0.70, 0.90, 1.0);

// Tamanho da janela em pixels (o script atualiza ao redimensionar) e do hexágono.
uniform vec2 screen_size = vec2(1280.0, 720.0);
uniform float hex_size = 64.0;

// O brilho do gradiente fica um pouco acima do centro, onde está o título.
uniform vec2 glow_center = vec2(0.5, 0.40);

float hex_dist(vec2 p) {
	p = abs(p);
	return max(dot(p, normalize(vec2(1.0, 1.73))), p.x);
}

// xy: posição dentro do hexágono; zw: id da célula.
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

void fragment() {
	float aspect = screen_size.x / screen_size.y;
	vec2 centered = (UV - glow_center) * vec2(aspect, 1.0);
	float d = length(centered);

	// Gradiente radial: brilho no centro, azul-marinho no meio, quase preto nas bordas.
	vec3 col = mix(color_glow.rgb, color_mid.rgb, smoothstep(0.0, 0.55, d));
	col = mix(col, color_edge.rgb, smoothstep(0.45, 1.25, d));

	// Grade de hexágonos.
	vec2 grid_uv = UV * screen_size / hex_size;
	vec4 hc = hex_coords(grid_uv);
	float edge = 0.5 - hex_dist(hc.xy);

	// Linhas finas, mais visíveis perto do centro e sumindo pras bordas.
	float fade = 1.0 - smoothstep(0.2, 1.1, d);
	float line = smoothstep(0.045, 0.0, edge) * 0.16 * fade;
	col += line_color.rgb * line;

	// Algumas células acendem e apagam devagar, cada uma na sua fase.
	float h = hash21(hc.zw);
	float lit = step(0.90, h);
	float pulse = 0.5 + 0.5 * sin(TIME * 0.7 + h * 40.0);
	col += line_color.rgb * lit * pulse * 0.10 * smoothstep(0.0, 0.18, edge) * fade;

	// Vinheta.
	col *= 1.0 - 0.35 * smoothstep(0.55, 1.3, d);

	COLOR = vec4(col, 1.0);
}
