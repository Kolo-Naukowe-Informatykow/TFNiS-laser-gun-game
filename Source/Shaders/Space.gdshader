// based on https://www.shadertoy.com/view/MtcGDf

shader_type canvas_item;
render_mode unshaded;

const float DRAW_DISTANCE = 60.0;
const float FADEOUT_DISTANCE = 10.0;
const float FIELD_OF_VIEW = 1.05;

const float STAR_SIZE = 0.6;
const float STAR_CORE_SIZE = 0.14;

const float CLUSTER_SCALE = 0.02;

const float BLACK_HOLE_CORE_RADIUS = 0.2;
const float BLACK_HOLE_THRESHOLD = 0.9995;
const float BLACK_HOLE_DISTORTION = 0.03;

uniform int ray_march_steps : hint_range(32, 200, 1) = 96;
uniform int nebula_steps : hint_range(1, 4, 1) = 2;
uniform bool enable_black_holes = false;
uniform float flight_speed : hint_range(0.0, 20.0, 0.1) = 8.0;
uniform float star_threshold : hint_range(0.7, 0.99, 0.001) = 0.90;

float nonzero_sign(float value) {
    return value < 0.0 ? -1.0 : 1.0;
}

float safe_div(float numerator, float denominator) {
    float adjusted = abs(denominator) < 0.0001 ? 0.0001 * nonzero_sign(denominator) : denominator;
    return numerator / adjusted;
}

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, vec3(0.0), vec3(1.0)), c.y);
}

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

float channel_noise(vec2 uv) {
    return rand(floor(fract(uv) * 512.0));
}

vec3 get_ray_direction(vec2 frag_coord, vec3 camera_direction, vec2 resolution) {
    vec2 uv = frag_coord / resolution;

    const float screen_width = 1.0;
    float origin_to_screen = screen_width * 0.5 / tan(FIELD_OF_VIEW * 0.5);

    vec3 screen_center = origin_to_screen * camera_direction;
    vec3 base_x = normalize(cross(screen_center, vec3(0.0, -1.0, 0.0)));
    vec3 base_y = normalize(cross(screen_center, base_x));

    return normalize(screen_center + (uv.x - 0.5) * base_x + (uv.y - 0.5) * resolution.y / resolution.x * base_y);
}

float get_distance(ivec3 chunk_path, vec3 local_start, vec3 local_position) {
    return length(vec3(chunk_path) + local_position - local_start);
}

void move(inout vec3 local_position, vec3 ray_direction, vec3 direction_bound) {
    vec3 direction_sign = sign(ray_direction);
    vec3 amount_vector = (direction_bound - direction_sign * local_position) / abs(ray_direction);
    float amount = min(amount_vector.x, min(amount_vector.y, amount_vector.z));
    local_position += amount * ray_direction;
}

void move_inside_box(inout vec3 local_position, inout ivec3 chunk, vec3 direction_sign, vec3 direction_bound) {
    const float eps = 0.0000001;
    if (local_position.x * direction_sign.x >= direction_bound.x - eps) {
        local_position.x -= direction_sign.x;
        chunk.x += int(direction_sign.x);
    } else if (local_position.y * direction_sign.y >= direction_bound.y - eps) {
        local_position.y -= direction_sign.y;
        chunk.y += int(direction_sign.y);
    } else if (local_position.z * direction_sign.z >= direction_bound.z - eps) {
        local_position.z -= direction_sign.z;
        chunk.z += int(direction_sign.z);
    }
}

bool has_star(ivec3 chunk) {
    vec2 cxy = vec2(float(chunk.x), float(chunk.y));
    vec2 czx = vec2(float(chunk.z), float(chunk.x));
    vec2 cxz = vec2(float(chunk.x), float(chunk.z));
    vec2 czy = vec2(float(chunk.z), float(chunk.y));

    return channel_noise(mod(CLUSTER_SCALE * (cxy + czx) + vec2(0.724, 0.111), 1.0)) > star_threshold
        && channel_noise(mod(CLUSTER_SCALE * (cxz + czy) + vec2(0.333, 0.777), 1.0)) > star_threshold;
}

bool has_black_hole(ivec3 chunk) {
    return rand(0.0001 * vec2(float(chunk.x), float(chunk.y)) + 0.002 * vec2(float(chunk.y), float(chunk.z))) > BLACK_HOLE_THRESHOLD;
}

vec3 get_star_to_ray_vector(vec3 ray_base, vec3 ray_direction, vec3 star_position) {
    float r = dot(ray_direction, star_position - ray_base);
    vec3 point_on_ray = ray_base + r * ray_direction;
    return point_on_ray - star_position;
}

vec3 get_star_position(ivec3 chunk, float star_size) {
    float x = float(chunk.x);
    float y = float(chunk.y);
    float z = float(chunk.z);

    vec3 position = abs(vec3(
        rand(vec2(safe_div(x, y) + 0.24, safe_div(y, z) + 0.66)),
        rand(vec2(safe_div(x, z) + 0.73, safe_div(z, y) + 0.45)),
        rand(vec2(safe_div(y, x) + 0.12, safe_div(y, z) + 0.76))
    ));

    return star_size * vec3(1.0) + (1.0 - 2.0 * star_size) * position;
}

vec4 get_nebula_color(vec3 global_position, vec3 ray_direction) {
    vec3 color = vec3(0.0);
    float space_left = 1.0;

    const float layer_distance = 10.0;
    float ray_z = abs(ray_direction.z) < 0.0001 ? 0.0001 * nonzero_sign(ray_direction.z) : ray_direction.z;

    const int max_steps = 4;
    int steps = clamp(nebula_steps, 1, max_steps);
    for (int i = 0; i <= max_steps; i++) {
        if (i > steps) {
            break;
        }
        vec3 noise_eval = global_position + ray_direction * ((1.0 - fract(global_position.z / layer_distance) + float(i)) * layer_distance / ray_z);
        noise_eval.xy += noise_eval.z;

        float value = 0.06 * channel_noise(noise_eval.xy / 60.0);

        if (i == 0) {
            value *= 1.0 - fract(global_position.z / layer_distance);
        } else if (i == steps) {
            value *= fract(global_position.z / layer_distance);
        }

        float hue = mod(noise_eval.z / layer_distance / 34.444, 1.0);

        color += space_left * hsv2rgb(vec3(hue, 1.0, value));
        space_left = max(0.0, space_left - value * 2.0);
    }
    return vec4(color, 1.0);
}

vec4 get_star_glow_color(float star_distance, float angle_value, float hue) {
    float progress = 1.0 - star_distance;
    float star_shape = abs(sin(angle_value * 2.5));
    star_shape *= star_shape;
    star_shape *= star_shape;
    star_shape *= star_shape;
    return vec4(hsv2rgb(vec3(hue, 0.3, 1.0)), 0.4 * progress * progress * mix(star_shape, 1.0, progress));
}

vec3 get_star_color(vec3 star_surface_location, float seed, float view_distance) {
    const float distance_far = 20.0;
    const float distance_near = 15.0;

    if (view_distance > distance_far) {
        return vec3(1.0);
    }

    float fade_to_white = max(0.0, (view_distance - distance_near) / (distance_far - distance_near));

    vec3 coordinate = vec3(acos(star_surface_location.y), atan(star_surface_location.x, star_surface_location.z), seed);
    float progress = channel_noise(0.3 * coordinate.xy + seed * vec2(1.1));
    progress *= progress;
    progress *= progress;

    return mix(mix(vec3(1.0, 0.98, 0.9), vec3(1.0, 0.627, 0.01), progress), vec3(1.0), fade_to_white);
}

vec4 blend_colors(vec4 front, vec4 back) {
    float alpha_sum = max(front.a + back.a, 0.0001);
    return vec4(mix(back.rgb, front.rgb, front.a / alpha_sum), front.a + back.a - front.a * back.a);
}

void fragment() {
    vec2 resolution = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 frag_coord = FRAGCOORD.xy;

    vec3 movement_direction = normalize(vec3(0.01, 0.0, 1.0));

    vec3 ray_direction = get_ray_direction(frag_coord, movement_direction, resolution);
    vec3 direction_sign = sign(ray_direction);
    vec3 direction_bound = vec3(0.5) + 0.5 * direction_sign;

    vec3 global_position = vec3(PI, PI, 0.0) + (TIME + 1000.0) * flight_speed * movement_direction;
    ivec3 chunk = ivec3(floor(global_position));
    vec3 local_position = fract(global_position);
    move_inside_box(local_position, chunk, direction_sign, direction_bound);

    ivec3 start_chunk = chunk;
    vec3 local_start = local_position;
    float draw_distance_sq = DRAW_DISTANCE * DRAW_DISTANCE;

    vec4 frag_color = vec4(0.0);

    for (int i = 0; i < 200; i++) {
        if (i >= ray_march_steps) {
            break;
        }

        move(local_position, ray_direction, direction_bound);
        move_inside_box(local_position, chunk, direction_sign, direction_bound);

        if (has_star(chunk)) {
            vec3 star_position = get_star_position(chunk, 0.5 * STAR_SIZE);
            float current_distance = get_distance(chunk - start_chunk, local_start, star_position);

            vec3 star_to_ray_vector = get_star_to_ray_vector(local_position, ray_direction, star_position);
            float distance_to_star_sq = dot(star_to_ray_vector, star_to_ray_vector) * 4.0;
            float star_size_sq = STAR_SIZE * STAR_SIZE;

            if (distance_to_star_sq < star_size_sq) {
                float distance_to_star = sqrt(distance_to_star_sq);
                float star_max_brightness = clamp((DRAW_DISTANCE - current_distance) / FADEOUT_DISTANCE, 0.001, 1.0);

                float star_color_seed = (float(chunk.x) + 13.0 * float(chunk.y) + 7.0 * float(chunk.z)) * 0.00453;
                if (distance_to_star < STAR_SIZE * STAR_CORE_SIZE) {
                    float core_radius = STAR_CORE_SIZE * STAR_SIZE;
                    float root_term = max(core_radius * core_radius - distance_to_star * distance_to_star, 0.0);
                    vec3 star_surface_vector = normalize(star_to_ray_vector + ray_direction * sqrt(root_term));

                    frag_color = blend_colors(frag_color, vec4(get_star_color(star_surface_vector, star_color_seed, current_distance), star_max_brightness));
                    break;
                }

                float local_star_distance = ((distance_to_star / STAR_SIZE) - STAR_CORE_SIZE) / (1.0 - STAR_CORE_SIZE);
                vec4 glow_color = get_star_glow_color(local_star_distance, atan(star_to_ray_vector.x, star_to_ray_vector.y), star_color_seed);
                glow_color.a *= star_max_brightness;
                frag_color = blend_colors(frag_color, glow_color);
            }
        } else if (enable_black_holes && has_black_hole(chunk)) {
            const vec3 black_hole_position = vec3(0.5);
            float current_distance = get_distance(chunk - start_chunk, local_start, black_hole_position);
            float fadeout = min(1.0, (DRAW_DISTANCE - current_distance) / FADEOUT_DISTANCE);

            vec3 core_to_ray_vector = get_star_to_ray_vector(local_position, ray_direction, black_hole_position);
            float distance_to_core = length(core_to_ray_vector);
            if (distance_to_core < BLACK_HOLE_CORE_RADIUS * 0.5) {
                frag_color = blend_colors(frag_color, vec4(vec3(0.0), fadeout));
                break;
            }
            if (distance_to_core < 0.5) {
                ray_direction = normalize(ray_direction - fadeout * (BLACK_HOLE_DISTORTION / distance_to_core - BLACK_HOLE_DISTORTION / 0.5) * core_to_ray_vector / distance_to_core);
            }
        }

        vec3 chunk_delta = vec3(chunk - start_chunk);
        if (dot(chunk_delta, chunk_delta) > draw_distance_sq) {
            break;
        }
    }

    if (frag_color.a < 1.0) {
        frag_color = blend_colors(frag_color, get_nebula_color(global_position, ray_direction));
    }

    COLOR = frag_color;
}
