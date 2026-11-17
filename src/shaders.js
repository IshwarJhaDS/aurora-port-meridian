export const fullscreenVS=`#version 300 es
precision highp float;
out vec2 vUV;
void main(){vec2 p=vec2(float((gl_VertexID<<1)&2),float(gl_VertexID&2));vUV=p;gl_Position=vec4(p*2.0-1.0,0.0,1.0);}`;
export const meshVS=`#version 300 es
precision highp float;
layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec2 aUV;
layout(location=3) in vec3 aColor;
layout(location=4) in vec4 aMaterial;
layout(location=5) in mat4 aInstanceModel;
layout(location=9) in vec4 aInstanceColor;
layout(location=10) in vec4 aInstanceMaterial;
uniform mat4 uModel,uVP;
uniform int uInstanced,uVertexMaterials,uWater;
uniform float uTime;
out vec3 vWorld,vNormal,vColor;
out vec4 vMaterial;
out vec2 vUV;
void main(){
  mat4 model=uInstanced==1?aInstanceModel:uModel;
  vec4 p=model*vec4(aPosition,1.0);vec3 normal=transpose(inverse(mat3(model)))*aNormal;
  if(uWater==1){float a=p.x*.12+uTime*.7,b=p.z*.19-uTime*.5;p.y+=sin(a)*.045+sin(b)*.035;normal=normalize(vec3(-cos(a)*.0054,1.0,-cos(b)*.00665));}
  vWorld=p.xyz;vNormal=normal;vUV=aUV;vColor=uVertexMaterials==1?aColor:vec3(1.0);vMaterial=aMaterial;
  if(uInstanced==1){vColor=aInstanceColor.rgb;vMaterial=aInstanceMaterial;}
  gl_Position=uVP*p;
}`;
export const geometryFS=`#version 300 es
precision highp float;precision highp int;
in vec3 vWorld,vNormal,vColor;in vec2 vUV;in vec4 vMaterial;
uniform vec3 uColor,uEmissive;
uniform float uRoughness,uMetal,uWettable,uUVScale,uWetExtent,uNightFactor;
uniform vec2 uWetOrigin;
uniform int uVertexMaterials;
uniform sampler2D uAlbedoTex,uWet;
layout(location=0) out vec4 gAlbedo;
layout(location=1) out vec4 gNormal;
layout(location=2) out vec4 gEmission;
void main(){
  vec3 normal=normalize(vNormal);if(!gl_FrontFacing)normal=-normal;
  float rough=uVertexMaterials==1?vMaterial.x:uRoughness,metal=uVertexMaterials==1?vMaterial.y:uMetal,wettable=uVertexMaterials==1?vMaterial.w:uWettable;
  float wet=texture(uWet,(vWorld.xz-uWetOrigin)/(uWetExtent*2.0)+.5).r*wettable*max(normal.y,0.0);
  vec3 color=uColor*vColor*texture(uAlbedoTex,vUV*uUVScale).rgb;
  float grain=fract(sin(dot(floor(vWorld*5.0),vec3(127.1,311.7,74.7)))*43758.5453);
  color*=mix(1.0,.62,wet)*(.965+.035*grain);
  vec3 emission=uEmissive;
  if(uVertexMaterials==1)emission+=(vMaterial.z<0.0?mix(vColor,vec3(1.0,.55,.19),.85):vColor)*abs(vMaterial.z)*(vMaterial.z<0.0?uNightFactor:1.0);
  gAlbedo=vec4(color,metal);gNormal=vec4(normal*.5+.5,mix(max(.045,rough),max(.055,rough*.24),wet));gEmission=vec4(emission,wet);
}`;
export const shadowVS=`#version 300 es
precision highp float;
layout(location=0)in vec3 aPosition;layout(location=5)in mat4 aInstanceModel;
uniform mat4 uModel,uVP;uniform int uInstanced;
void main(){gl_Position=uVP*(uInstanced==1?aInstanceModel:uModel)*vec4(aPosition,1.0);}`;
export const shadowFS=`#version 300 es
precision highp float;void main(){}`;
export const queryFS=`#version 300 es
precision highp float;out vec4 color;void main(){color=vec4(0.0);}`;
const sceneGLSL=`
const float PI=3.141592653589793;
uniform sampler2D uDepth,uAlbedo,uNormal,uEmission,uShadow;
uniform mat4 uInvVP,uVP,uView,uLightVP;
uniform vec3 uEye,uSunDirection,uSunColor;
uniform float uSunIntensity;
vec3 worldAt(vec2 uv){float d=texture(uDepth,uv).r;vec4 p=uInvVP*vec4(uv*2.0-1.0,d*2.0-1.0,1.0);return p.xyz/p.w;}
float mapShadow(vec3 p,vec3 n){
  vec4 l=uLightVP*vec4(p+n*.018,1.0);vec3 uv=l.xyz/l.w*.5+.5;
  if(any(lessThan(uv,vec3(0.0)))||any(greaterThan(uv,vec3(1.0))))return 1.0;
  vec2 px=1.0/vec2(textureSize(uShadow,0));float lit=0.0,bias=max(.00007,.0004*(1.0-dot(n,uSunDirection)));
  for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)lit+=uv.z-bias<=texture(uShadow,uv.xy+vec2(x,y)*px).r?1.0:0.0;
  return lit/9.0;
}
vec3 simpleLight(vec2 uv){vec3 n=normalize(texture(uNormal,uv).xyz*2.0-1.0);vec3 albedo=texture(uAlbedo,uv).rgb;return albedo*(vec3(.025,.035,.055)+uSunColor*uSunIntensity*max(0.0,dot(n,uSunDirection))/PI)+texture(uEmission,uv).rgb;}
bool screenRay(vec3 origin,vec3 dir,float stride,int steps,out vec2 hitUV){
  for(int i=1;i<=32;i++){if(i>steps)break;vec3 p=origin+dir*(float(i)*stride+.06*float(i*i));vec4 clip=uVP*vec4(p,1.0);if(clip.w<=0.0)break;vec2 uv=clip.xy/clip.w*.5+.5;if(any(lessThanEqual(uv,vec2(.003)))||any(greaterThanEqual(uv,vec2(.997))))break;
    if(texture(uDepth,uv).r>.99999)continue;vec3 scene=worldAt(uv);float delta=-(uView*vec4(p,1.0)).z+(uView*vec4(scene,1.0)).z;
    if(delta>0.0&&delta<.5+stride*.4&&distance(origin,scene)>.18){hitUV=uv;return true;}
  }return false;
}
`;
export const giFS=`#version 300 es
precision highp float;precision highp int;
in vec2 vUV;out vec4 outGI;
${sceneGLSL}
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
void main(){
  if(texture(uDepth,vUV).r>.99999){outGI=vec4(0.0);return;}
  vec3 p=worldAt(vUV),n=normalize(texture(uNormal,vUV).xyz*2.0-1.0),t=normalize(cross(abs(n.y)<.95?vec3(0,1,0):vec3(1,0,0),n)),b=cross(n,t),sum=vec3(0.0);float angle=hash(floor(gl_FragCoord.xy))*2.0*PI;
  for(int i=0;i<4;i++){float u=(float(i)+.5)/4.0,phi=angle+float(i)*2.39996323;vec3 d=t*cos(phi)*sqrt(u)+b*sin(phi)*sqrt(u)+n*sqrt(1.0-u);vec2 hit;
    if(screenRay(p+n*.12,d,.3,12,hit)){vec3 hn=normalize(texture(uNormal,hit).xyz*2.0-1.0);float valid=max(0.0,dot(hn,-d));sum+=simpleLight(hit)*valid;}
  }outGI=vec4(sum/4.0,1.0);
}`;
const traceGLSL=`
uniform sampler2D uBVH,uTriangles;
uniform int uNodeCount,uDataWidth,uShadowMode;
vec4 dataAt(sampler2D tex,int index){return texelFetch(tex,ivec2(index%uDataWidth,index/uDataWidth),0);}
bool rayBox(vec3 origin,vec3 dir,vec3 lo,vec3 hi){
  float nearT=0.0,farT=300.0;
  for(int j=0;j<3;j++){if(abs(dir[j])<1e-8){if(origin[j]<lo[j]||origin[j]>hi[j])return false;}else{float a=(lo[j]-origin[j])/dir[j],b=(hi[j]-origin[j])/dir[j];nearT=max(nearT,min(a,b));farT=min(farT,max(a,b));if(nearT>farT)return false;}}
  return true;
}
bool rayTriangle(vec3 o,vec3 d,int index){
  vec3 a=dataAt(uTriangles,index*3).xyz,b=dataAt(uTriangles,index*3+1).xyz,c=dataAt(uTriangles,index*3+2).xyz,e1=b-a,e2=c-a,p=cross(d,e2);float det=dot(e1,p);if(abs(det)<1e-8)return false;
  vec3 s=o-a;float u=dot(s,p)/det;if(u<0.0||u>1.0)return false;vec3 q=cross(s,e1);float v=dot(d,q)/det;if(v<0.0||u+v>1.0)return false;float t=dot(e2,q)/det;return t>.008&&t<300.0;
}
float tracedShadow(vec3 p,vec3 n){
  vec3 o=p+n*.035;int node=0;
  for(int it=0;it<16384;it++){if(node>=uNodeCount)break;vec4 lo=dataAt(uBVH,node*3),hi=dataAt(uBVH,node*3+1),meta=dataAt(uBVH,node*3+2);
    if(!rayBox(o,uSunDirection,lo.xyz,hi.xyz)){node=int(lo.w);continue;}
    int count=int(hi.w),first=int(meta.x);for(int j=0;j<4;j++){if(j>=count)break;if(rayTriangle(o,uSunDirection,first+j))return 0.0;}node++;
  }return 1.0;
}
`;
const atmosphereGLSL=`
uniform float uClouds,uGameTime;
float cloudHash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float cloudNoise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(cloudHash(i),cloudHash(i+vec2(1,0)),f.x),mix(cloudHash(i+vec2(0,1)),cloudHash(i+vec2(1,1)),f.x),f.y);}
vec2 sphereInterval(vec3 o,vec3 d,float r){float b=dot(o,d),c=dot(o,o)-r*r,h=b*b-c;if(h<0.0)return vec2(-1.0);return vec2(-b-sqrt(h),-b+sqrt(h));}
vec3 atmosphere(vec3 direction){
  const float earth=6360000.0,outer=6460000.0;const vec3 betaR=vec3(5.8e-6,13.5e-6,33.1e-6);const float betaM=21e-6;
  vec3 o=vec3(0.0,earth+max(uEye.y,2.0),0.0);vec2 air=sphereInterval(o,direction,outer),ground=sphereInterval(o,direction,earth);float end=air.y;
  if(ground.x>0.0)end=min(end,ground.x);end=max(0.0,end);float ds=end/8.0,odR=0.0,odM=0.0;vec3 sumR=vec3(0.0),sumM=vec3(0.0);
  float mu=dot(direction,uSunDirection),phaseR=3.0/(16.0*PI)*(1.0+mu*mu),g=.76,phaseM=(1.0-g*g)/(4.0*PI*pow(max(.01,1.0+g*g-2.0*g*mu),1.5));
  for(int i=0;i<8;i++){vec3 p=o+direction*((float(i)+.5)*ds);float h=max(0.0,length(p)-earth),r=exp(-h/8000.0)*ds,m=exp(-h/1200.0)*ds;odR+=r*.5;odM+=m*.5;
    vec2 light=sphereInterval(p,uSunDirection,outer),planet=sphereInterval(p,uSunDirection,earth);float stepL=max(0.0,light.y)/4.0,lr=0.0,lm=0.0;
    if(planet.x<0.0){for(int j=0;j<4;j++){float lh=max(0.0,length(p+uSunDirection*((float(j)+.5)*stepL))-earth);lr+=exp(-lh/8000.0)*stepL;lm+=exp(-lh/1200.0)*stepL;}
      vec3 trans=exp(-(betaR*(odR+lr)+vec3(betaM)*(odM+lm)));sumR+=trans*r;sumM+=trans*m;}
    odR+=r*.5;odM+=m*.5;
  }
  vec3 sky=22.0*(sumR*betaR*phaseR+sumM*betaM*phaseM)+vec3(.001,.002,.006);
  if(ground.x<0.0&&dot(direction,uSunDirection)>.9998&&uSunDirection.y>-.03)sky+=uSunColor*8.0;
  float stars=pow(fract(sin(dot(floor(direction.xz/max(.08,abs(direction.y))*700.0),vec2(127.1,311.7)))*43758.5453),150.0)*(1.0-smoothstep(-.25,.0,uSunDirection.y))*max(0.0,direction.y);
  vec3 result=sky+vec3(stars*.3);
  if(direction.y>.02&&uClouds>0.0){vec2 q=direction.xz/direction.y*2.8+vec2(uGameTime*.006,uGameTime*.003);float noise=cloudNoise(q)*.58+cloudNoise(q*2.0)*.28+cloudNoise(q*4.0)*.14;float cover=smoothstep(.74-uClouds*.42,.88-uClouds*.28,noise);vec3 cloudColor=mix(vec3(.07,.09,.13),vec3(.55,.58,.58),max(0.0,uSunDirection.y));result=mix(result,cloudColor,cover*min(1.0,direction.y*12.0));}
  return result;
}
`;
export const lightingFS=`#version 300 es
precision highp float;precision highp int;
in vec2 vUV;out vec4 outColor;
${sceneGLSL}
${traceGLSL}
${atmosphereGLSL}
uniform sampler2D uGI;
uniform float uRain,uExposure,uFogDensity;
uniform int uEnableGI,uEnableFog,uEnableSSR,uDebug;
uniform int uPointCount;
uniform vec4 uLightPositions[12];
uniform vec4 uLightColors[12];
vec3 fresnel(float cosTheta,vec3 f0){return f0+(1.0-f0)*pow(1.0-cosTheta,5.0);}
vec3 brdf(vec3 n,vec3 v,vec3 l,vec3 albedo,float metallic,float roughness){
  if(dot(n,l)<=0.0)return vec3(0.0);
  vec3 h=normalize(v+l),f0=mix(vec3(.04),albedo,metallic),F=fresnel(max(0.0,dot(v,h)),f0);float nl=max(0.0,dot(n,l)),nv=max(.0001,dot(n,v)),nh=max(0.0,dot(n,h)),a=roughness*roughness,a2=a*a,denom=nh*nh*(a2-1.0)+1.0,D=a2/(PI*denom*denom),Gv=2.0*nv/(nv+sqrt(a2+(1.0-a2)*nv*nv)),Gl=2.0*nl/max(.0001,nl+sqrt(a2+(1.0-a2)*nl*nl));
  return ((1.0-F)*(1.0-metallic)*albedo/PI+D*Gv*Gl*F/max(.0001,4.0*nv*nl))*nl;
}
vec3 bilateralGI(vec2 uv,vec3 p,vec3 n){vec2 px=1.0/vec2(textureSize(uGI,0));vec3 sum=vec3(0.0);float total=0.0;
  for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){vec2 q=uv+vec2(x,y)*px;vec3 qn=normalize(texture(uNormal,q).xyz*2.0-1.0);float w=exp(-distance(p,worldAt(q))*3.0)*pow(max(0.0,dot(n,qn)),8.0);sum+=texture(uGI,q).rgb*w;total+=w;}
  return sum/max(.0001,total);
}
vec3 tone(vec3 x){
  x=max(vec3(0.0),x*uExposure);
  x=clamp((x*(2.8*x+.06))/(x*(2.7*x+.72)+.14),0.0,1.0);
  x=pow(max(x,vec3(0.0)),vec3(1.0/1.14));
  return mix(x*1.08,1.06*pow(x,vec3(1.0/2.2))-.06,step(vec3(.0031308),x));
}
void main(){
  float depth=texture(uDepth,vUV).r;vec3 p=worldAt(vUV),ray=normalize(p-uEye),color;
  if(depth>.99999){color=atmosphere(ray);}
  else{
    vec4 am=texture(uAlbedo,vUV),nr=texture(uNormal,vUV),em=texture(uEmission,vUV);vec3 n=normalize(nr.xyz*2.0-1.0),v=normalize(uEye-p);float roughness=nr.w,metallic=am.w;
    float visibility=uSunDirection.y>0.0?(uShadowMode==1?tracedShadow(p,n):mapShadow(p,n)):0.0;
    vec3 f0=mix(vec3(.04),am.rgb,metallic),F=fresnel(max(0.0,dot(n,v)),f0),skyAmbient=vec3(.025,.04,.065)+vec3(.045,.055,.065)*max(0.0,uSunDirection.y);
    color=brdf(n,v,uSunDirection,am.rgb,metallic,roughness)*uSunColor*uSunIntensity*visibility+skyAmbient*am.rgb*(1.0-metallic)+em.rgb;
    for(int li=0;li<12;li++){if(li>=uPointCount)break;vec3 delta=uLightPositions[li].xyz-p;float dist=length(delta),range=uLightPositions[li].w;float falloff=pow(clamp(1.0-pow(dist/range,4.0),0.0,1.0),2.0)/max(1.0,dist*dist);color+=brdf(n,v,delta/max(.001,dist),am.rgb,metallic,roughness)*uLightColors[li].rgb*uLightColors[li].a*falloff;}
    if(uEnableGI==1)color+=bilateralGI(vUV,p,n)*am.rgb*(1.0-metallic)*.8;
    vec3 reflectionDir=reflect(-v,n),reflection=skyAmbient*1.8+pow(max(0.0,dot(reflectionDir,uSunDirection)),mix(800.0,5.0,roughness))*uSunColor*uSunIntensity;
    if(uEnableSSR==1&&(em.a>.02||metallic>.2)){vec2 hit;if(screenRay(p+n*.1,reflectionDir,.3,24,hit)){float edge=min(min(hit.x,hit.y),min(1.0-hit.x,1.0-hit.y));reflection=mix(reflection,simpleLight(hit),smoothstep(0.0,.08,edge));}}
    color+=reflection*F*(1.0-roughness*.8);
    if(uDebug==1)color=am.rgb;if(uDebug==2)color=n*.5+.5;if(uDebug==3)color=vec3(visibility);if(uDebug==4)color=vec3(em.a);if(uDebug==5)color=texture(uGI,vUV).rgb;
  }
  if(uEnableFog==1&&uDebug==0){float distanceToSurface=depth>.99999?170.0:min(170.0,distance(p,uEye)),ds=distanceToSurface/12.0,trans=1.0;vec3 fog=vec3(0.0);float g=.52,mu=dot(ray,uSunDirection),phase=(1.0-g*g)/(4.0*PI*pow(1.0+g*g-2.0*g*mu,1.5));
    for(int i=0;i<12;i++){vec3 q=uEye+ray*((float(i)+.5)*ds);float density=uFogDensity*(1.0+uRain*1.5)*exp(-max(0.0,q.y)*.055),segment=exp(-density*ds),lit=mapShadow(q,vec3(0,1,0));vec3 source=vec3(.12,.17,.23)+uSunColor*uSunIntensity*phase*lit;fog+=trans*(1.0-segment)*source;trans*=segment;}
    color=color*trans+fog;
  }
  vec2 centered=vUV*2.0-1.0;
  float vignette=1.0-dot(centered,centered)*0.22;
  vec3 bloom=max(vec3(0.0),color-vec3(1.25))*0.38;
  color=color+bloom;
  color*=vignette;
  outColor=vec4(tone(color),1.0);
}`;
export const rainVS=`#version 300 es
precision highp float;uniform mat4 uVP;uniform vec3 uEye;uniform float uTime,uRain;out float vAlpha;
float hash(float n){return fract(sin(n*127.1)*43758.5453);}
void main(){float id=float(gl_VertexID/2),tip=float(gl_VertexID%2);vec3 p=vec3((hash(id+1.0)-.5)*55.0,mod(hash(id+2.0)*28.0-uTime*15.0,28.0),(hash(id+3.0)-.5)*55.0);p.xz+=uEye.xz;p+=vec3(.07,-.55,.03)*tip;gl_Position=uVP*vec4(p,1.0);vAlpha=.18*uRain;}`;
export const rainFS=`#version 300 es
precision highp float;uniform sampler2D uDepth;uniform vec2 uResolution;in float vAlpha;out vec4 outColor;void main(){float scene=texture(uDepth,gl_FragCoord.xy/uResolution).r;if(gl_FragCoord.z>scene+.0001)discard;outColor=vec4(.62,.78,.94,vAlpha);}`;
export const aaFS=`#version 300 es
precision highp float;
in vec2 vUV;out vec4 outColor;uniform sampler2D uSource;uniform vec2 uResolution;
float luma(vec3 c){return dot(c,vec3(.2126,.7152,.0722));}
void main(){
  vec2 px=1.0/uResolution;vec3 m=texture(uSource,vUV).rgb,n=texture(uSource,vUV+vec2(0,px.y)).rgb,s=texture(uSource,vUV-vec2(0,px.y)).rgb,e=texture(uSource,vUV+vec2(px.x,0)).rgb,w=texture(uSource,vUV-vec2(px.x,0)).rgb;
  float lm=luma(m),ln=luma(n),ls=luma(s),le=luma(e),lw=luma(w),hi=max(lm,max(max(ln,ls),max(le,lw))),lo=min(lm,min(min(ln,ls),min(le,lw))),range=hi-lo;
  if(range<max(.025,hi*.12)){outColor=vec4(m,1.0);return;}
  vec2 direction=abs(le-lw)>abs(ln-ls)?vec2(px.x,0):vec2(0,px.y);
  vec3 filtered=(texture(uSource,vUV+direction*.5).rgb+texture(uSource,vUV-direction*.5).rgb)*.5;
  outColor=vec4(mix(m,filtered,min(.8,range/max(.04,hi))),1.0);
}`;
