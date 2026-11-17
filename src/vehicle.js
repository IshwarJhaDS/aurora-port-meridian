import { Vec3, clamp, lerp } from './math.js';
import { RigidBody } from './physics.js';

export class TorqueCurve {
  constructor(points=[[800,160],[2000,270],[4000,335],[5600,300],[6800,0]]){
    if(points.length<2||points.some((p,i)=>p.length!==2||p[0]<0||p[1]<0||(i>0&&p[0]<=points[i-1][0])))throw new RangeError('Torque points must be increasing');
    this.points=points;
  }
  at(rpm){
    if(rpm<=this.points[0][0])return this.points[0][1];
    for(let i=1;i<this.points.length;i++){
      const [r,t]=this.points[i],[r0,t0]=this.points[i-1];
      if(rpm<=r)return lerp(t0,t,(rpm-r0)/(r-r0));
    }
    return 0;
  }
}

export class RaycastVehicle {
  constructor(world,{
    position=new Vec3(0,1,0),mass=1250
  } = {}){
    this.world=world;
    this.body=world.add(new RigidBody({
      mass,half:new Vec3(.85,.26,1.8),position,friction:.4,restitution:.05
    }));
    this.body.angularDamping=.32;
    this.curve=new TorqueCurve();
    this.gears=[-3.2,0,3.5,2.2,1.5,1.1,.85];
    this.gear=2;
    this.finalDrive=3.4;
    this.rpm=900;
    this.shiftTimer=0;
    this.throttle=0;
    this.brake=0;
    this.steer=0;
    this.handbrake=0;
    this.wetness=0;
    this.wheels=[];
    for(const z of [-1.24,1.24])for(const x of [-.82,.82])this.wheels.push({
      anchor:new Vec3(x,-.05,z),radius:.34,rest:.42,travel:.2,spring:33000,damper:4200,inertia:1.4,omega:0,compression:0,load:0,steering:z<0,driven:z>0,hit:null,angle:0
    });
    this.stepCallback=dt=>this.step(dt);
    world.beforeStep.add(this.stepCallback);
  }
  setControls({
    throttle=0,brake=0,steer=0,handbrake=0
  }){
    this.throttle=clamp(throttle,-1,1);
    this.brake=clamp(brake,0,1);
    this.steer=clamp(steer,-1,1);
    this.handbrake=clamp(handbrake,0,1);
  }
  step(dt){
    const b=this.body,up=b.rotation.rotate(new Vec3(0,1,0)),down=up.clone().scale(-1),forward=b.rotation.rotate(new Vec3(0,0,-1)),speed=b.velocity.dot(forward),wheelRPM=this.wheels.filter(w=>w.driven).reduce((s,w)=>s+Math.abs(w.omega),0)/2*60/(2*Math.PI);
    this.shiftTimer=Math.max(0,this.shiftTimer-dt);
    if(this.throttle<-.05&&Math.abs(speed)<1)this.gear=0;
    else if(this.throttle>.05&&this.gear===0&&Math.abs(speed)<1)this.gear=2;
    this.rpm=clamp(lerp(this.rpm,Math.max(850,wheelRPM*Math.abs(this.gears[this.gear])*this.finalDrive),1-Math.exp(-dt*12)),850,6800);
    if(this.gear>=2&&!this.shiftTimer){
      if(this.rpm>5600&&this.gear<this.gears.length-1){
        this.gear++;
        this.shiftTimer=.22;
      }else if(this.rpm<1700&&this.gear>2){
        this.gear--;
        this.shiftTimer=.22;
      }
    }
    const drive=this.shiftTimer?0:this.curve.at(this.rpm)*Math.abs(this.throttle)*this.gears[this.gear]*this.finalDrive*.88/2;
    for(const w of this.wheels){
      const origin=b.localToWorld(w.anchor),hit=this.world.raycast(origin,down,w.rest+w.travel+w.radius,b);
      w.hit=hit;
      let fx=0,fy=0;
      const steer=w.steering?this.steer*.5/(1+Math.abs(speed)*.035):0,c=Math.cos(steer),s=Math.sin(steer),f=b.rotation.rotate(new Vec3(-s,0,-c)),right=Vec3.cross(f,up).normalize();
      if(hit){
        const length=clamp(hit.distance-w.radius,w.rest-w.travel,w.rest+w.travel),compression=w.rest-length,groundV=hit.body?hit.body.pointVelocity(hit.point):new Vec3(),rel=Vec3.sub(b.pointVelocity(hit.point),groundV),compressionSpeed=-rel.dot(up);
        w.compression=compression;
        w.load=clamp(w.spring*compression+w.damper*compressionSpeed,0,b.mass*9.81*1.6);
        const vx=rel.dot(f),vy=rel.dot(right),slip=(w.omega*w.radius-vx)/Math.max(2,Math.abs(vx)),angle=Math.atan2(vy,Math.max(2,Math.abs(vx))),mu=lerp(1.08,.53,this.wetness)*(w.driven?lerp(1,.48,this.handbrake):1),limit=mu*w.load;
        fx=limit*Math.tanh(7*slip);
        fy=-limit*Math.tanh(5*angle);
        const total=Math.hypot(fx,fy);
        if(total>limit&&total>0){
          fx*=limit/total;
          fy*=limit/total;
        }
        const force=up.clone().scale(w.load).addScaled(f,fx).addScaled(right,fy);
        b.applyForce(force,hit.point);
        if(hit.body?.invMass)hit.body.applyForce(force.clone().scale(-1),hit.point);
      }else{
        w.compression=-w.travel;
        w.load=0;
      }
      const engineTorque=w.driven?drive:0,brakeTorque=2400*this.brake+(w.driven?3400*this.handbrake:0),net=engineTorque-fx*w.radius-w.omega*.12;
      w.omega+=net/w.inertia*dt;
      const stop=brakeTorque/w.inertia*dt;
      w.omega=Math.sign(w.omega)*Math.max(0,Math.abs(w.omega)-stop);
      w.omega=clamp(w.omega,-240,240);
      w.angle+=w.omega*dt;
    }
    const v=b.velocity.length();
    if(v>1e-5)b.applyForce(b.velocity.clone().scale(-.5*1.225*.32*2.2*v));
    b.applyForce(up.clone().scale(-.5*1.225*.8*2.2*speed*speed));
  }
  wheelPosition(w){
    return this.body.localToWorld(w.anchor).addScaled(this.body.rotation.rotate(new Vec3(0,-1,0)),w.rest-w.compression);
  }
  dispose(){
    this.world.beforeStep.delete(this.stepCallback);
    this.world.remove(this.body);
  }
}
