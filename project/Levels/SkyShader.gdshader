shader_type sky;
render_mode use_half_res_pass;

vec3 skyColor( in vec3 rd )
{
    vec3 sundir = normalize( vec3(.0, .1, 1.) );
    
    float yd = min(rd.y, 0.);
    rd.y = max(rd.y, 0.);
    
    vec3 col = vec3(0.);
    
    col += vec3(.4, .4 - exp( -rd.y*20. )*.15, .0) * exp(-rd.y*9.); // Red / Green 
    col += vec3(.3, .5, .6) * (1. - exp(-rd.y*8.) ) * exp(-rd.y*.9) ; // Blue
    
    col = mix(col*1.2, vec3(.3),  1.-exp(yd*100.)); // Fog
    
    col += vec3(1.0, .8, .55) * pow( max(dot(rd,sundir),0.), 15. ) * .6; // Sun
    col += pow(max(dot(rd, sundir),0.), 150.0) *.15;
    
    return col;
}


float checker( vec2 p )
{
    p = mod(floor(p),2.0);
    return mod(p.x + p.y, 2.0) < 1.0 ? .25 : 0.1;
}


vec4 generate_clouds(vec3 dir, vec2 v)
{
 
    //Scene
    vec3 col = vec3( checker(dir.xz/dir.y*.5+vec2(0.,-TIME*2.)) ) + skyColor(reflect(dir,vec3(0.,1.,0.)))*.3;
    col = mix(col, skyColor(dir), exp(-max(-v.y*9.-4.8,0.)) );
    
    return vec4(col, 1.0);
}

void sky() {
	 if (AT_HALF_RES_PASS) {
        // Run cloud calculation for 1/4 of the pixels
       vec4 color = generate_clouds(EYEDIR, SCREEN_UV);
     //   COLOR = color.rgb;
      COLOR =       color.rgb;
	   ALPHA = color.a;
    } else {
      COLOR = HALF_RES_COLOR.rgb;
      ALPHA = HALF_RES_COLOR.a;
   }
}