using Sandbox;
using Sandbox.Internal;
using System;

public sealed class PuntBall : Component, Component.ICollisionListener
{
	[Property] public WorldPanel ballGuide;

	[Property] public GameObject ballModel;
	[Property] public Rigidbody ballRB { get; set; }

	[Property] public GameMode gameMode { get; set; }

	[Property] public Curve ballReflectionCurve { get; set; }

	[Property] public float cornerRollMult { get; set; } = 0.05f;

	[Property] public Vector3 preCollisionVelocity { get; set; }

	private Vector3 entryVelocity;
	private Vector3 exitVelocity;
	private Vector3 newExitVelocity;

	[Property] public Vector3 collisionNormal { get; set; }

	[Property] public Vector3 bounceReflection { get; set; }

	[Property] public float angleAcuteness { get; set; }

	[Property] public float exitZVelocity { get; set; }

	[Property] public Vector3 collisionPosition { get; set; }

	

	protected override void OnAwake()
	{
	}
	public void OnCollisionStart( Collision collision )
	{


		if ( collision.Other.GameObject.Tags.HasAny( "wall" ) )
		{
			entryVelocity = preCollisionVelocity.Normal;//store the velocity of the frame before the collision
			exitVelocity = ballRB.Velocity;//the exit velocity
			collisionPosition = collision.Contact.Point;//the collision point
			collisionNormal = -collision.Contact.Normal;



			entryVelocity = new Vector3( entryVelocity.x, entryVelocity.y, 0 );

			exitZVelocity = exitVelocity.z;

			exitVelocity = new Vector3( exitVelocity.x, exitVelocity.y, 0 );

			angleAcuteness = Vector3.GetAngle( -entryVelocity, collisionNormal );
			angleAcuteness = angleAcuteness.Remap( 0f, 90f );
			angleAcuteness = ballReflectionCurve.Evaluate( angleAcuteness );
			Log.Info( angleAcuteness );
			newExitVelocity = Vector3.Lerp( exitVelocity.Normal, entryVelocity.Normal, angleAcuteness );

			newExitVelocity = newExitVelocity * exitVelocity.Length*(1 + (angleAcuteness * cornerRollMult));
			newExitVelocity = new Vector3(newExitVelocity.x, newExitVelocity.y, exitZVelocity );




			ballRB.Velocity = newExitVelocity;


		}

	}
	protected override void OnFixedUpdate()
	{
		preCollisionVelocity = ballRB.Velocity;//keep this every fixed update so we know before the collision
	}
	protected override void OnUpdate()
	{
		//take exit velocity, store Z component, but set to 0
		//calculate acuteness of the angle
		//if it's almost perpendicular
		//lerp in some of the original velocity
		//add the z component back in



		
		//newExitVelocity = newExitVelocity * exitVelocity.Length;
		//newExitVelocity = new Vector3( newExitVelocity.x, newExitVelocity.y, exitZVelocity );


		Gizmo.Draw.Arrow( collisionPosition + collisionNormal * 100f, collisionPosition, 0f,0f );//bounce normal
		Gizmo.Draw.Arrow(collisionPosition + -entryVelocity.Normal * 200f, collisionPosition, 10f, 10f ); //entry velocity
		Gizmo.Draw.Arrow( collisionPosition, collisionPosition + newExitVelocity.Normal * 100f, 10f, 10f );//exit normal



		CalculateBallGuide();
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
