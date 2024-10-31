shader_type spatial;

render_mode diffuse_lambert_wrap;

uniform samplerCube cubemap_texture;
uniform vec3 tint_color : source_color = vec3(1, 1, 1);

vec3 rotate_x(vec3 v, float a){
    return mat3(vec3(1, 0, 0), vec3(0, cos(a), sin(a)), vec3(0, -sin(a), cos(a))) * v;
}

vec3 rotate_y(vec3 v, float a){
    return mat3(vec3(cos(a), 0, -sin(a)), vec3(0, 1, 0),  vec3(sin(a),0, cos(a))) * v;
}

vec3 rotate_z(vec3 v, float a){
    return mat3(vec3(cos(a), sin(a), 0), vec3(-sin(a), cos(a), 0), vec3(0, 0, 1)) * v;
}

void fragment() {
	vec3 i = CAMERA_POSITION_WORLD - (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 n = (INV_VIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	vec3 cubemap_sample = reflect(i, n);
	cubemap_sample = rotate_x(cubemap_sample, sin(TIME * 0.5) * 0.5);
	cubemap_sample = rotate_y(cubemap_sample, TIME);
	cubemap_sample = rotate_z(cubemap_sample, sin(TIME * 0.5) * 0.5);
	ALBEDO = texture(cubemap_texture, cubemap_sample).rgb * tint_color;
	SPECULAR = 0.0;
}
