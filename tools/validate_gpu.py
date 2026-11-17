"""Compile the actual GLSL and execute the render passes in a native ES 3 context.

Optional Linux validation; requires system libEGL and a Mesa EGL driver.
This checks GLES behavior, not browser WebGL conformance or performance.
"""
import ctypes as C
import ctypes.util
import json
from pathlib import Path
import subprocess
import sys
import struct
import zlib

CITY = "--city" in sys.argv
W, H = (960, 540) if CITY else (64, 48)

ROOT = Path(__file__).resolve().parents[1]
node_code = """
import * as shaders from './src/shaders.js';
import {Vec3,Quat,Mat4,AABB} from './src/math.js';
import {MeshData} from './src/mesh.js';
import {BVH} from './src/spatial.js';
const mesh=MeshData.box(),eye=new Vec3(4,3,6),view=Mat4.lookAt(eye,new Vec3()),vp=Mat4.mul(Mat4.perspective(1,64/48,.1,100),view),sun=new Vec3(-.5,.8,.4).normalize();
const models=[Mat4.identity(),Mat4.compose(new Vec3(0,-1.2,0),new Quat(),new Vec3(6,.2,6))];
const triangles=[];
for(const model of models)for(let i=0;i<mesh.indices.length;i+=3){const p=[0,1,2].map(j=>Mat4.point(model,Vec3.from(mesh.positions,mesh.indices[i+j]*3)));triangles.push({a:p[0],b:p[1],c:p[2],bounds:new AABB().include(p[0]).include(p[1]).include(p[2])});}
const bvh=new BVH(triangles),pack=bvh.gpuPack(),tri=new Float32Array(pack.ordered.length*12);pack.ordered.forEach((t,i)=>{t.a.toArray(tri,i*12);t.b.toArray(tri,i*12+4);t.c.toArray(tri,i*12+8);});
console.log(JSON.stringify({shaders,mesh:{positions:[...mesh.positions],normals:[...mesh.normals],uvs:[...mesh.uvs],indices:[...mesh.indices]},models:models.map(x=>[...x]),eye:eye.toArray(),view:[...view],vp:[...vp],inverseVP:[...Mat4.invert(vp)],sun:sun.toArray(),lightVP:[...Mat4.mul(Mat4.ortho(-8,8,-8,8,1,30),Mat4.lookAt(sun.clone().scale(15),new Vec3()))],nodes:[...pack.nodes],triangles:[...tri],nodeCount:bvh.nodes.length}));
"""
scene = json.loads(subprocess.check_output((['node', 'tools/export-city-scene.mjs']+(['--night'] if '--night' in sys.argv else [])) if CITY else ['node', '--input-type=module', '-e', node_code], cwd=ROOT, text=True))

egl_path = ctypes_path = C.util.find_library('EGL')
if not egl_path:
    raise SystemExit('Native shader validation unavailable: system libEGL not found.')
egl = C.CDLL(egl_path)
def api(lib, name, result, *args):
    fn = getattr(lib, name)
    fn.restype, fn.argtypes = result, args
    return fn

voidp, integer, uint, floating = C.c_void_p, C.c_int, C.c_uint, C.c_float
platform_display = api(egl, 'eglGetPlatformDisplay', voidp, uint, voidp, C.POINTER(integer))
initialize = api(egl, 'eglInitialize', uint, voidp, C.POINTER(integer), C.POINTER(integer))
bind_api = api(egl, 'eglBindAPI', uint, uint)
choose = api(egl, 'eglChooseConfig', uint, voidp, C.POINTER(integer), C.POINTER(voidp), integer, C.POINTER(integer))
create_surface = api(egl, 'eglCreatePbufferSurface', voidp, voidp, voidp, C.POINTER(integer))
create_context = api(egl, 'eglCreateContext', voidp, voidp, voidp, voidp, C.POINTER(integer))
make_current = api(egl, 'eglMakeCurrent', uint, voidp, voidp, voidp, voidp)
get_proc = api(egl, 'eglGetProcAddress', voidp, C.c_char_p)
display = platform_display(0x31DD, None, None)
major, minor = integer(), integer()
if not display or not initialize(display, C.byref(major), C.byref(minor)):
    raise SystemExit('Native shader validation unavailable: surfaceless EGL initialization failed.')
assert bind_api(0x30A0), 'Cannot bind OpenGL ES API'
attributes = (integer * 15)(0x3033, 1, 0x3040, 0x40, 0x3024, 8, 0x3023, 8, 0x3022, 8, 0x3021, 8, 0x3025, 24, 0x3038)
config, count = voidp(), integer()
assert choose(display, attributes, C.byref(config), 1, C.byref(count)) and count.value, 'No ES 3 pbuffer configuration'
surface = create_surface(display, config, (integer * 5)(0x3057, W, 0x3056, H, 0x3038))
context = create_context(display, config, None, (integer * 3)(0x3098, 3, 0x3038))
assert surface and context and make_current(display, surface, surface, context), 'Cannot create native ES context'
def gl(name, result, *args):
    address = get_proc(name.encode())
    if not address:
        raise RuntimeError(f'GL entry point missing: {name}')
    return C.CFUNCTYPE(result, *args)(address)

get_string = gl('glGetString', C.c_char_p, uint)
get_error = gl('glGetError', uint)
create_shader = gl('glCreateShader', uint, uint)
shader_source = gl('glShaderSource', None, uint, integer, C.POINTER(C.c_char_p), C.POINTER(integer))
compile_shader = gl('glCompileShader', None, uint)
get_shader = gl('glGetShaderiv', None, uint, uint, C.POINTER(integer))
shader_log = gl('glGetShaderInfoLog', None, uint, integer, C.POINTER(integer), C.c_char_p)
create_program = gl('glCreateProgram', uint)
attach_shader = gl('glAttachShader', None, uint, uint)
link_program = gl('glLinkProgram', None, uint)
get_program = gl('glGetProgramiv', None, uint, uint, C.POINTER(integer))
program_log = gl('glGetProgramInfoLog', None, uint, integer, C.POINTER(integer), C.c_char_p)
use = gl('glUseProgram', None, uint)
uniform_location = gl('glGetUniformLocation', integer, uint, C.c_char_p)
uniform_i = gl('glUniform1i', None, integer, integer)
uniform_f = gl('glUniform1f', None, integer, floating)
uniform_2 = gl('glUniform2f', None, integer, floating, floating)
uniform_3 = gl('glUniform3f', None, integer, floating, floating, floating)
uniform_4m = gl('glUniformMatrix4fv', None, integer, integer, C.c_ubyte, C.POINTER(floating))
gen_textures = gl('glGenTextures', None, integer, C.POINTER(uint))
bind_texture = gl('glBindTexture', None, uint, uint)
active_texture = gl('glActiveTexture', None, uint)
tex_image = gl('glTexImage2D', None, uint, integer, integer, integer, integer, integer, uint, uint, voidp)
tex_param = gl('glTexParameteri', None, uint, uint, integer)
gen_fbo = gl('glGenFramebuffers', None, integer, C.POINTER(uint))
bind_fbo = gl('glBindFramebuffer', None, uint, uint)
attach_texture = gl('glFramebufferTexture2D', None, uint, uint, uint, uint, integer)
draw_buffers = gl('glDrawBuffers', None, integer, C.POINTER(uint))
read_buffer = gl('glReadBuffer', None, uint)
fbo_status = gl('glCheckFramebufferStatus', uint, uint)
gen_vao = gl('glGenVertexArrays', None, integer, C.POINTER(uint))
bind_vao = gl('glBindVertexArray', None, uint)
gen_buffers = gl('glGenBuffers', None, integer, C.POINTER(uint))
bind_buffer = gl('glBindBuffer', None, uint, uint)
buffer_data = gl('glBufferData', None, uint, C.c_ssize_t, voidp, uint)
enable_attribute = gl('glEnableVertexAttribArray', None, uint)
attribute = gl('glVertexAttribPointer', None, uint, integer, uint, C.c_ubyte, integer, voidp)
viewport = gl('glViewport', None, integer, integer, integer, integer)
clear_color = gl('glClearColor', None, floating, floating, floating, floating)
clear = gl('glClear', None, uint)
enable = gl('glEnable', None, uint)
disable = gl('glDisable', None, uint)
polygon_offset = gl('glPolygonOffset', None, floating, floating)
draw_elements = gl('glDrawElements', None, uint, integer, uint, voidp)
attribute_divisor = gl('glVertexAttribDivisor',None,uint,uint)
draw_instanced = gl('glDrawElementsInstanced',None,uint,integer,uint,voidp,integer)
uniform_4v = gl('glUniform4fv',None,integer,integer,C.POINTER(floating))
draw_arrays = gl('glDrawArrays', None, uint, integer, integer)
read_pixels = gl('glReadPixels', None, integer, integer, integer, integer, uint, uint, voidp)

def stage(kind, source):
    handle = create_shader(kind)
    text = C.c_char_p(source.encode())
    shader_source(handle, 1, C.byref(text), None)
    compile_shader(handle)
    success = integer()
    get_shader(handle, 0x8B81, C.byref(success))
    if not success.value:
        log = C.create_string_buffer(32768)
        shader_log(handle, len(log), None, log)
        raise RuntimeError(log.value.decode())
    return handle

def program(vs, fs):
    result = create_program()
    attach_shader(result, stage(0x8B31, scene['shaders'][vs]))
    attach_shader(result, stage(0x8B30, scene['shaders'][fs]))
    link_program(result)
    success = integer()
    get_program(result, 0x8B82, C.byref(success))
    if not success.value:
        log = C.create_string_buffer(32768)
        program_log(result, len(log), None, log)
        raise RuntimeError(log.value.decode())
    return result

programs = {name: program(vs, fs) for name, vs, fs in [
    ('geometry', 'meshVS', 'geometryFS'), ('shadow', 'shadowVS', 'shadowFS'),
    ('query', 'shadowVS', 'queryFS'), ('gi', 'fullscreenVS', 'giFS'),
    ('lighting', 'fullscreenVS', 'lightingFS'), ('rain', 'rainVS', 'rainFS'), ('aa', 'fullscreenVS', 'aaFS')]}

def uniform(p, name, value, kind='f'):
    loc = uniform_location(p, name.encode())
    if kind == 'i': uniform_i(loc, int(value))
    elif kind == 'm': uniform_4m(loc, 1, 0, (floating * 16)(*value))
    elif isinstance(value, list) and len(value) == 3: uniform_3(loc, *value)
    elif isinstance(value, list) and len(value) == 2: uniform_2(loc, *value)
    else: uniform_f(loc, value)

def texture(width, height, internal=0x8058, fmt=0x1908, typ=0x1401, data=None):
    handle = uint()
    gen_textures(1, C.byref(handle)); bind_texture(0x0DE1, handle)
    tex_image(0x0DE1, 0, internal, width, height, 0, fmt, typ, data)
    for k, v in [(0x2801, 0x2600), (0x2800, 0x2600), (0x2802, 0x812F), (0x2803, 0x812F)]: tex_param(0x0DE1, k, v)
    return handle.value

def fbo(colors, depth=None):
    handle = uint(); gen_fbo(1, C.byref(handle)); bind_fbo(0x8D40, handle)
    for i, t in enumerate(colors): attach_texture(0x8D40, 0x8CE0+i, 0x0DE1, t, 0)
    if depth: attach_texture(0x8D40, 0x8D00, 0x0DE1, depth, 0)
    draw_buffers(max(1, len(colors)), (uint * max(1, len(colors)))(*([0x8CE0+i for i in range(len(colors))] or [0])))
    if not colors: read_buffer(0)
    assert fbo_status(0x8D40) == 0x8CD5, 'Incomplete native framebuffer'
    return handle.value

def data_texture(data):
    height = max(1, (len(data)+1023)//1024)
    packed = (floating * (256*height*4))()
    for i, v in enumerate(data): packed[i] = v
    return texture(256, height, 0x8814, 0x1908, 0x1406, packed)

white = texture(1, 1, data=(C.c_ubyte * 4)(255, 255, 255, 255))
wet = texture(1, 1, internal=0x8229, fmt=0x1903, data=(C.c_ubyte * 1)(0 if CITY else 180))
albedo = texture(W, H)
normal = texture(W, H, 0x881A, 0x1908, 0x140B)
emission = texture(W, H, 0x881A, 0x1908, 0x140B)
depth = texture(W, H, 0x81A6, 0x1902, 0x1405)
gbuffer = fbo([albedo, normal, emission], depth)
shadow = texture(1536 if CITY else 128, 1536 if CITY else 128, 0x81A6, 0x1902, 0x1405)
shadow_fbo = fbo([], shadow)
gi = texture(W//2, H//2, 0x881A, 0x1908, 0x140B)
gi_fbo = fbo([gi])
display_texture=texture(W,H)
tex_param(0x0DE1,0x2801,0x2601);tex_param(0x0DE1,0x2800,0x2601)
display_fbo=fbo([display_texture])
bvh, triangles = data_texture(scene['nodes']), data_texture(scene['triangles'])

def bind(p, name, tex, unit):
    active_texture(0x84C0+unit); bind_texture(0x0DE1, tex); uniform(p, name, unit, 'i')

vao = uint(); gen_vao(1, C.byref(vao)); bind_vao(vao)
mesh = scene['mesh']; buffers = []
for i, key, size in [(0, 'positions', 3), (1, 'normals', 3), (2, 'uvs', 2), *([(3, 'colors', 3), (4, 'materials', 4)] if CITY else [])]:
    handle = uint(); gen_buffers(1, C.byref(handle)); buffers.append(handle)
    bind_buffer(0x8892, handle); values = (floating * len(mesh[key]))(*mesh[key])
    buffer_data(0x8892, C.sizeof(values), values, 0x88E4); enable_attribute(i); attribute(i, size, 0x1406, 0, 0, None)
indices = (uint * len(mesh['indices']))(*mesh['indices'])
index_buffer = uint(); gen_buffers(1, C.byref(index_buffer)); bind_buffer(0x8893, index_buffer); buffer_data(0x8893, C.sizeof(indices), indices, 0x88E4)
full_vao = uint(); gen_vao(1, C.byref(full_vao))

def draw_scene(p):
    bind_vao(vao)
    for model in scene['models']:
        uniform(p, 'uModel', model, 'm'); 
        if CITY and p==programs['shadow']:
            for start,count in scene['shadowRanges']:draw_elements(4,count,0x1405,C.c_void_p(start*4))
        else: draw_elements(4, len(mesh['indices']), 0x1405, None)

use(programs['shadow']); uniform(programs['shadow'], 'uVP', scene['lightVP'], 'm')
bind_fbo(0x8D40, shadow_fbo); viewport(0, 0, 1536 if CITY else 128, 1536 if CITY else 128); enable(0x0B71); clear(0x100); enable(0x8037);polygon_offset(1.5,2.5);draw_scene(programs['shadow']);disable(0x8037)
use(programs['geometry']); p = programs['geometry']; uniform(p, 'uVP', scene['vp'], 'm')
bind(p, 'uAlbedoTex', white, 0); bind(p, 'uWet', wet, 1)
uniform(p,'uVertexMaterials',1 if CITY else 0,'i');uniform(p,'uNightFactor',scene.get('nightFactor',0));uniform(p,'uWetOrigin',[0,0])
for k, v in {'uColor':([1,1,1] if CITY else [.4,.15,.08]), 'uEmissive':[0,0,0], 'uRoughness':.45, 'uMetal':.1, 'uWettable':1, 'uUVScale':1, 'uWetExtent':45}.items(): uniform(p, k, v)
bind_fbo(0x8D40, gbuffer); viewport(0, 0, W, H); clear_color(0,0,0,0); clear(0x4100); draw_scene(p); disable(0x0B71)

def common(p):
    use(p)
    for i, (name, tex) in enumerate([('uAlbedo',albedo), ('uNormal',normal), ('uEmission',emission), ('uDepth',depth), ('uShadow',shadow)]): bind(p,name,tex,i)
    for name, value in [('uInvVP',scene['inverseVP']), ('uVP',scene['vp']), ('uView',scene['view']), ('uLightVP',scene['lightVP'])]: uniform(p,name,value,'m')
    for name,value in [('uEye',scene['eye']), ('uSunDirection',scene['sun']), ('uSunColor',scene.get('sunColor',[1,.9,.7])), ('uSunIntensity',scene.get('intensity',5.5))]: uniform(p,name,value)

geometry_pixels = (C.c_ubyte * (W*H*4))()
bind_fbo(0x8D40, gbuffer)
read_buffer(0x8CE0)
read_pixels(0,0,W,H,0x1908,0x1401,geometry_pixels)
geometry_mask = [geometry_pixels[i+3]>0 for i in range(0,len(geometry_pixels),4)]
assert sum(geometry_mask)>100, 'Geometry pass did not write the expected material pixels'
bind_vao(full_vao); bind_fbo(0x8D40,gi_fbo); viewport(0,0,W//2,H//2); common(programs['gi']); draw_arrays(4,0,3)
def present():
    bind_fbo(0x8D40,0);p=programs['aa'];use(p);bind(p,'uSource',display_texture,0);uniform(p,'uResolution',[W,H]);draw_arrays(4,0,3)

results = []
for mode in ((0,) if CITY else (0, 1)):
    bind_fbo(0x8D40,display_fbo); viewport(0,0,W,H); p = programs['lighting']; common(p)
    bind(p,'uGI',gi,5); bind(p,'uBVH',bvh,6); bind(p,'uTriangles',triangles,7)
    for name,value in [('uDataWidth',256),('uNodeCount',scene['nodeCount']),('uShadowMode',mode),('uEnableGI',0 if CITY else 1),('uEnableFog',1),('uEnableSSR',1),('uDebug',0)]: uniform(p,name,value,'i')
    for name,value in [('uRain',0 if CITY else .5),('uExposure',1.05 if CITY else 1.1),('uFogDensity',.003 if CITY else .005)]: uniform(p,name,value)
    lights=scene.get('lights',[])[:12]
    uniform(p,'uPointCount',len(lights),'i')
    if lights:
        lp=(floating*48)();lc=(floating*48)()
        for li,light in enumerate(lights):
            for j,v in enumerate([light['x'],light['y'],light['z'],light['radius']]):lp[li*4+j]=v
            for j,v in enumerate(light['color']+[light['power']]):lc[li*4+j]=v
        uniform_4v(uniform_location(p,b'uLightPositions[0]'),12,lp);uniform_4v(uniform_location(p,b'uLightColors[0]'),12,lc)
    draw_arrays(4,0,3)
    present()
    pixels = (C.c_ubyte * (W*H*4))(); read_pixels(0,0,W,H,0x1908,0x1401,pixels)
    values = [pixels[i]+pixels[i+1]+pixels[i+2] for i in range(0,len(pixels),4)]
    error = get_error(); assert error == 0, f'Native GL error: {error:#x}'
    assert max(values)-min(values)>30 and sum(values)>10000, 'Render did not produce a varied, lit image'
    if CITY:
        rgba=bytes(pixels);raw=bytearray()
        for row in range(H-1,-1,-1):
            raw.append(0)
            for col in range(W):raw.extend(rgba[(row*W+col)*4:(row*W+col)*4+3])
        def png_chunk(kind,data):return struct.pack('!I',len(data))+kind+data+struct.pack('!I',zlib.crc32(kind+data)&0xffffffff)
        png=b'\x89PNG\r\n\x1a\n'+png_chunk(b'IHDR',struct.pack('!2I5B',W,H,8,2,0,0,0))+png_chunk(b'IDAT',zlib.compress(bytes(raw),8))+png_chunk(b'IEND',b'')
        output=Path(sys.argv[sys.argv.index('--output')+1]) if '--output' in sys.argv else ROOT/'docs'/'Native_City_Preview.png'
        output.parent.mkdir(parents=True,exist_ok=True);output.write_bytes(png)
    mean_channel = sum(values)/(W*H*3)
    bind_fbo(0x8D40,display_fbo);use(p);bind(p,'uAlbedo',albedo,0);uniform(p,'uDebug',3,'i')
    draw_arrays(4,0,3)
    present()
    read_pixels(0,0,W,H,0x1908,0x1401,pixels)
    dark = sum(geometry_mask[i] and pixels[i*4]<5 for i in range(W*H))
    lit = sum(geometry_mask[i] and pixels[i*4]>100 for i in range(W*H))
    if scene.get('intensity',5.5)>0:assert dark>0 and lit>0, 'Sun-visibility buffer must contain occluded and visible geometry'
    assert get_error()==0
    results.append({'shadow_mode':mode,'rgb_sum_range':[min(values),max(values)],'mean_channel':mean_channel,'shadowed_geometry_pixels':dark,'lit_geometry_pixels':lit,'gl_error':error})

instance_result = None
if not CITY:
    p=programs['geometry'];use(p);bind_fbo(0x8D40,gbuffer);bind_vao(vao);viewport(0,0,W,H);enable(0x0B71);clear_color(0,0,0,0);clear(0x4100)
    bind(p,'uAlbedoTex',white,0);bind(p,'uWet',wet,1);uniform(p,'uColor',[1,1,1]);uniform(p,'uVertexMaterials',1,'i');uniform(p,'uInstanced',1,'i');uniform(p,'uVP',scene['vp'],'m')
    values=[]
    for x,color in [(-1.25,[1,.1,.05,1]),(1.25,[.05,1,.1,1])]:
        values += [.65,0,0,0,0,.65,0,0,0,0,.65,0,x,0,0,1]+color+[.7,0,0,0]
    instance_buffer=uint();gen_buffers(1,C.byref(instance_buffer));bind_buffer(0x8892,instance_buffer);values=(floating*len(values))(*values);buffer_data(0x8892,C.sizeof(values),values,0x88E8)
    for location,offset in [(5,0),(6,16),(7,32),(8,48),(9,64),(10,80)]:
        enable_attribute(location);attribute(location,4,0x1406,0,96,C.c_void_p(offset));attribute_divisor(location,1)
    draw_instanced(4,len(mesh['indices']),0x1405,None,2)
    colors=(C.c_ubyte*(W*H*4))();read_buffer(0x8CE0);read_pixels(0,0,W,H,0x1908,0x1401,colors)
    red=sum(colors[i]>colors[i+1]*2 and colors[i]>30 for i in range(0,len(colors),4));green=sum(colors[i+1]>colors[i]*2 and colors[i+1]>30 for i in range(0,len(colors),4))
    assert red>10 and green>10,'Independent instance matrices/materials did not reach the G-buffer'
    assert get_error()==0
    instance_result={'instances':2,'red_pixels':red,'green_pixels':green,'gl_error':0}
print(json.dumps({'driver':get_string(0x1F02).decode(),'renderer':get_string(0x1F01).decode(),'programs_linked':len(programs),'framebuffers_complete':4,'renders':results,'instanced_render':instance_result},indent=2))
make_current(display,None,None,None)
api(egl,'eglDestroyContext',uint,voidp,voidp)(display,context)
api(egl,'eglDestroySurface',uint,voidp,voidp)(display,surface)
api(egl,'eglTerminate',uint,voidp)(display)
