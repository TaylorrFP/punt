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
	[Property] public float wallBounceThreshold { get; set; } = 2f;

	[Property] public float wallMaxBounceImpulse { get; set; } = 500f;

	[Property] public float pitchBounciness { get; set; } = 2f;
	[Property] public float pitchBounceThreshold { get; set; } = 2f;
	[Property] public float maxPitchbounceImpulse { get; set; } = 500f;
	[Property] public float maxBounce { get; set; } = 500f;

	[Property] public Vector3 bounceSpeed { get; set; }
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

	protected override void OnUpdate()
	{

			CalculateBallGuide();
		CalculateSquash();

		//debug
		//Gizmo.Draw.Arrow( bouncePosition + (bounceSpeed * 1f), bouncePosition  );
		//Gizmo.Draw.Arrow( bouncePosition, bouncePosition + (bounceNormal* 100f ) );
		//Gizmo.Draw.Arrow( bouncePosition, bouncePosition + ( bounceReflection * 100f) );


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

	public void OnCollisionStart( Collision collision )
	{

		////this is all messy as fuck but it works - clean this up asap


		//if ( collision.Other.GameObject.Tags.HasAny( "piece" ) )
		//{
		//	ballKickSound.Volume = MathX.Lerp( 0, 0.5f, collision.Contact.Speed.Length / 1000f );
		//	Sound.Play( ballKickSound );

		//}


		//if ( collision.Other.GameObject.Tags.HasAny( "wall" ) )
		//{
		//	bouncePosition = collision.Contact.Point;
		//	bounceSpeed = new Vector3( collision.Contact.Speed.x, collision.Contact.Speed.y, 0 );
		//	bounceNormal = -collision.Contact.Normal;
		//	bounceReflection = Vector3.Reflect( -bounceSpeed.Normal, bounceNormal );
		//	bounceReflection = new Vector3( bounceReflection.x, bounceReflection.y, 0f ).Normal;

		//	var dotProduct = MathF.Abs( Vector3.Dot( -bounceSpeed.Normal, bounceNormal ) );

		//	var bounceImpulse = bounceSpeed.Length * wallBounciness;

		//	if ( bounceImpulse > wallMaxBounceImpulse )
		//	{
		//		bounceImpulse = wallMaxBounceImpulse;

		//	}

		//	ballRB.Velocity = Vector3.Lerp( ballRB.Velocity, bounceReflection * bounceImpulse, MathF.Pow( dotProduct, 2f ) );

		//	ballBounceSound.Volume = MathX.Lerp( 0, 1, collision.Contact.Speed.Length / 1000f * MathF.Pow( dotProduct, 2f ) );
		//	ballBounceSound.Pitch = MathX.Lerp( 0.2f, 1.5f, collision.Contact.Speed.Length / 1000f * MathF.Pow( dotProduct, 2f ) );
		//	Sound.Play( ballBounceSound );

		//}
		//else if ( collision.Other.GameObject.Tags.HasAny( "pitch" ) )
		//{
		//	bouncePosition = collision.Contact.Point;
		//	bounceSpeed = collision.Contact.Speed;
		//	bounceNormal = -collision.Contact.Normal;
		//	bounceReflection = Vector3.Reflect( -bounceSpeed.Normal, bounceNormal );

		//	var dotProduct = MathF.Abs( Vector3.Dot( -bounceSpeed.Normal, bounceNormal ) );

		//	var bounceImpulse = bounceSpeed.Length * pitchBounciness;


		//	if ( bounceImpulse > maxPitchbounceImpulse )
		//	{
		//		bounceImpulse = maxPitchbounceImpulse;

		//	}


		//	ballRB.Velocity = new Vector3( ballRB.Velocity.x, ballRB.Velocity.y, bounceReflection.z * bounceImpulse );

		//	ballBounceSound.Volume = MathX.Lerp( 0, 1, collision.Contact.Speed.Length / 1000f );
		//	ballBounceSound.Pitch = MathX.Lerp( 0.2f, 1.5f, collision.Contact.Speed.Length / 1000f );
		//	Sound.Play( ballBounceSound );

		//}

	}


	public void OnCollisionUpdate( Collision collision )
	{
	}


	public void OnCollisionStop( CollisionStop collision )
	{
	}


}
