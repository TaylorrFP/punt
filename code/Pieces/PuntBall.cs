using Sandbox;
using Sandbox.Internal;
using System;

public sealed class PuntBall : Component, Component.ICollisionListener
{
	[Property] public WorldPanel ballGuide;

	[Property] public GameObject ballModel;
	[Property] public Rigidbody ballRB { get; set; }

	[Property] public GameMode gameMode { get; set; }

	[Property] public SoundEvent ballBounceSound { get; set; }
	[Property] public SoundEvent ballKickSound { get; set; }
	[Property] public Curve decalLerpCurve { get; set; }

	[Property] public float wallBounciness { get; set; } = 2f;

	[Property] public Vector3 dotProduct { get; set; }

	[Property] public Vector3 preCollisionVelocity { get; set; }

	[Property] public Vector3 entryVelocity { get; set; }
	[Property] public Vector3 exitVelocity { get; set; }
	[Property] public float wallBounceThreshold { get; set; } = 2f;

	[Property] public float wallMaxBounceImpulse { get; set; } = 500f;

	[Property] public float pitchBounciness { get; set; } = 2f;
	[Property] public float pitchBounceThreshold { get; set; } = 2f;
	[Property] public float maxPitchbounceImpulse { get; set; } = 500f;
	[Property] public float maxBounce { get; set; } = 500f;

	[Property] public Vector3 bounceSpeedXY { get; set; }
	[Property] public Vector3 bounceNormal { get; set; }

	[Property] public Vector3 bounceReflection { get; set; }

	[Property] public Vector3 bouncePosition { get; set; }

	protected override void OnAwake()
	{
		//if ( IsProxy )
		//{
		//	ballRB.PhysicsBody.MotionEnabled = false;

		//}
	}
	public void OnCollisionStart( Collision collision )
	{


		if ( collision.Other.GameObject.Tags.HasAny( "wall" ) )
		{
			entryVelocity = preCollisionVelocity;//store the velocity of the frame before the collision
			exitVelocity = ballRB.Velocity;//the exit velocity
			bouncePosition = collision.Contact.Point;//the collision point
			bounceSpeedXY = new Vector3( collision.Contact.Speed.x, collision.Contact.Speed.y, 0 );
			bounceNormal = -collision.Contact.Normal;


			bounceReflection = Vector3.Reflect( -bounceSpeedXY.Normal, bounceNormal );
			bounceReflection = new Vector3( bounceReflection.x, bounceReflection.y, 0f ).Normal;




			//Gizmo.Draw.Arrow( bouncePosition + bounceNormal * 1000f, bouncePosition,12f,100f );


			//if ( bounceImpulse > wallMaxBounceImpulse )
			//{
			//	bounceImpulse = wallMaxBounceImpulse;

			//}

			//ballRB.Velocity = Vector3.Lerp( ballRB.Velocity, bounceReflection * bounceImpulse, MathF.Pow( dotProduct, 10f ) );

		}

	}
	protected override void OnFixedUpdate()
	{
		preCollisionVelocity = ballRB.Velocity;
	}
	protected override void OnUpdate()
	{
		//take exit velocity, store Z component, but set to 0
		//calculate acuteness of the angle
		//if it's almost perpendicular
		//lerp in some of the original velocity
		//add the z component back in


		CalculateBallGuide();

	
		//Gizmo.Draw.Arrow( bouncePosition + bounceNormal * 100f, bouncePosition,0f,0f );//bounce normal


		Gizmo.Draw.Arrow(bouncePosition + -entryVelocity.Normal * 200f, bouncePosition, 10f, 10f ); //entry velocity

		entryVelocity = new Vector3(entryVelocity.x, entryVelocity.y, 0);


		var angle = Vector3.GetAngle(-entryVelocity, bounceNormal );

		angle = angle.Remap( 0f, 90f);
		Log.Info( angle );

		var newAngle = Vector3.Lerp( exitVelocity, entryVelocity,0.5f );

		Gizmo.Draw.Arrow( bouncePosition, bouncePosition + newAngle.Normal * 100f, 10f, 10f );//bounce normal
	}


	private void CalculateSquash()
	{
		//Log.Info( ballRB.Velocity.Length );

		//var scaledVelocity = MathX.Remap( ballRB.Velocity.Length, 0, 700f, 1, 2 );

		//ballModel.Transform.Rotation = Rotation.LookAt( Vector3.Forward, ballRB.Velocity.Normal );

		//ballModel.Transform.sca


	}

	private void CalculateBallGuide()
	{
		ballGuide.WorldPosition = new Vector3( this.WorldPosition.x, this.WorldPosition.y, 10f );
		ballGuide.WorldRotation = Rotation.From(new Angles(-90f,0,0));
	}

	


	public void OnCollisionUpdate( Collision collision )
	{
	}


	public void OnCollisionStop( CollisionStop collision )
	{
	}


}
