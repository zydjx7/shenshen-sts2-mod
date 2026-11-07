shader_type canvas_item;

uniform vec4 shade : source_color;
uniform vec4 peach : source_color;
uniform vec4 cream : source_color;
uniform vec4 paper : source_color;

varying vec4 modulate_color;

void vertex() {
    modulate_color = COLOR;
}

void fragment() {
    vec4 source = texture(TEXTURE, UV);
    float rim_mask = smoothstep(0.085, 0.16, source.r - max(source.g, source.b));
    float brightness = clamp((source.r - 0.24) / 0.62, 0.0, 1.0);
    vec3 rim = mix(shade.rgb, peach.rgb, smoothstep(0.0, 0.62, brightness));
    rim = mix(rim, cream.rgb, smoothstep(0.56, 1.0, brightness) * 0.9);
    float luminance = dot(source.rgb, vec3(0.299, 0.587, 0.114));
    vec3 text_background = paper.rgb * (0.77 + 0.9 * luminance);
    COLOR = vec4(mix(text_background, rim, rim_mask), source.a) * modulate_color;
}
