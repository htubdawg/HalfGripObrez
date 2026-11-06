// Exercises the actual game-managed bolt step without constructing a Unity engine object.
// Does not test rendering, collision selection, or the running Grabber.
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
var bolt = (LinearObject)RuntimeHelpers.GetUninitializedObject(typeof(LinearObject));
bolt.position = Vector2.zero;
bolt.anchorAngle = 0f; bolt.minLimit=0; bolt.maxLimit=1; bolt.useLimits=true;
bolt.objectSpringForce=0.5f; bolt.physicsObjectSpringForce=1; bolt.directSpeedKeptOnRelease=0.9f; bolt.frozen=true;
HalfGripObrez.BoltControl.Configure(bolt);
if(bolt.frozen || bolt.isPhysicsControlled) throw new Exception("Production bolt setup blocks native direct hand motion");
bolt.handSpringForce=1; bolt.CDRatio=1; bolt.positionSmoothing=75; bolt.viscosity=1;
var flags=BindingFlags.Instance|BindingFlags.NonPublic;
void Call(string name, params object[] args) => typeof(LinearObject).GetMethod(name,flags)!.Invoke(bolt,args);
void Held(bool held) => typeof(Grabbable).GetProperty("IsGrabbed")!.GetSetMethod(true)!.Invoke(bolt,new object[]{held});
Call("OnInitialized"); Held(true); bolt.constrainedPoint=Vector2.zero; Call("OnGrab");
bolt.constrainedPoint=Vector2.right;
for(int i=0;i<60;i++) Call("StepDirect",1f/60);
if(bolt.value<0.99f) throw new Exception("Native bolt did not open");
Held(false); Call("OnRelease"); float open=bolt.value;
for(int i=0;i<600;i++) Call("StepDirect",1f/60);
if(Math.Abs(bolt.value-open)>0.00001f) throw new Exception("Native bolt moved after release");
Held(true); bolt.constrainedPoint=Vector2.right; Call("OnGrab"); bolt.constrainedPoint=Vector2.zero;
for(int i=0;i<60;i++) Call("StepDirect",1f/60);
if(bolt.value>0.01f) throw new Exception("Native bolt did not push closed");
Console.WriteLine("PASS: actual game LinearObject opens with hand input, holds for 600 released frames, and pushes closed; no frozen flag.");
