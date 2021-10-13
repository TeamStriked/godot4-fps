shader_type sky;
render_mode use_half_res_pass;

void sky() {
   if (AT_CUBEMAP_PASS) {
        COLOR = vec3(1.0); // Reflections will be all white.
    } else if (AT_HALF_RES_PASS) {
      //  vec4 col = generate_fancy_clouds(EYEDIR, TIME); // Clouds will be rendered to half res texture.
     ///    ALPHA = col.a; // Subpasses can use alpha.
	COLOR = vec3(255,255,255);
	ALPHA = 1.0;
    } else {
        COLOR = HALF_RES_COLOR.rgb; // Background will read from HALF_RES_PASS.
		ALPHA = HALF_RES_COLOR.a;
    }
}
