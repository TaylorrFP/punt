using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Internal;
using System;

public sealed class PuntBall : Component, Component.ICollisionListener
{
	[Property] public WorldPanel ballGuide;

	[Property] public GameObject ballModel;
	[Property] public Rigidbody ballRB { get; set; }

	[Property] public GameMode gameMode { get; set; }

	[Property] public bool cornerRollBias { get; set; } = true;

	[Property] public float goalWallDampning { get; set; } = 0.9f;
	[Property] public bool cornerRollBiasDebug { get; set; } = false;
	[Property] public Curve ballCornerRollBias { get; set; }



	[Property] public Vector3 preCollisionVelocity { get; set; }

	private Vector3 entryNormal;
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

		if(collision.Other.GameObject.Tags.Has("piece") )
		{

			if( TestGameMode.Instance.State == GameState.KickingOff )
			{
				if ( !IsProxy )
				{
					TestGameMode.Instance.StartGame();
					Log.Info( "ball restarting game" );


				}


			}

			if ( TestGameMode.Instance.State == GameState.Overtime )
			{
				if ( !IsProxy )
				{
					TestGameMode.Instance.StartOvertimeGame();
					Log.Info( "ball restarting game for overtime" );


				}


			}

		}


		//this is all really shit, replace it or get rid maybe - it is quite nice though

		if ( collision.Other.GameObject.Tags.HasAny( "wall" ) && cornerRollBias )
		{
			entryNormal = preCollisionVelocity.Normal;//store the velocity of the frame before the collision

			exitVelocity = ballRB.Velocity;//the exit velocity
			collisionPosition = collision.Contact.Point;//the collision point
			collisionNormal = -collision.Contact.Normal;//collision normal



			//remove the z components of exit and entryvelocity, we don't want to change behaviour in the z axis
			entryNormal = new Vector3( entryNormal.x, entryNormal.y, 0 );
			exitZVelocity = exitVelocity.z;//save this for later
			exitVelocity = new Vector3( exitVelocity.x, exitVelocity.y, 0 );

			//work out how acute the angle is.
			angleAcuteness = Vector3.GetAngle( -entryNormal, collisionNormal );
			angleAcuteness = angleAcuteness.Remap( 0f, 90f );
			angleAcuteness = ballCornerRollBias.Evaluate( angleAcuteness );//once we've worked it out, use the curve to customise it.

			float dotProduct = Vector3.Dot( entryNormal, collisionNormal );
			Vector3 perpendicularComponent = dotProduct * collisionNormal;
			Vector3 slidingNormal = entryNormal - perpendicularComponent;

			newExitVelocity = Vector3.Lerp( exitVelocity.Normal, slidingNormal, angleAcuteness );
			newExitVelocity = newExitVelocity.Normal * exitVelocity.Length;//get the 2D speed of the original exit velocity


			newExitVelocity = new Vector3( newExitVelocity.x, newExitVelocity.y, exitZVelocity );//add back in the z component 
			ballRB.Velocity = newExitVelocity;


			exitVelocity = new Vector3( exitVelocity.x, exitVelocity.y, exitZVelocity );//re-add the z to this


		}

		if ( collision.Other.GameObject.Tags.HasAny( "Net" ))
		{
			//dampen ball impact when it's the back of the net
			ballRB.Velocity = ballRB.Velocity * MathX.Clamp(1f-goalWallDampning,0,1f);
		}

		if ( collision.Other.GameObject.Tags.HasAny( "GoalPost" ) )
		{
			//dampen ball impact when it's the back of the net
			Sound.Play( "sounds/ball/post.sound" );
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


		//Gizmo.Draw.Arrow( collisionPosition + collisionNormal * 100f, collisionPosition, 0f,0f );//bounce normal
		//Gizmo.Draw.Arrow(collisionPosition + -entryNormal.Normal * 200f, collisionPosition, 10f, 10f ); //entry velocity

		//Gizmo.Draw.Arrow( collisionPosition, collisionPosition + newExitVelocity.Normal * 110f, 10f, 10f );//newexit normal

		//Gizmo.Draw.Arrow( collisionPosition, collisionPosition + exitVelocity.Normal * 100f, 10f, 10f );//exit normal





		//Gizmo.Draw.Arrow( collisionPosition, collisionPosition + slidingVelocity.Normal * 100f, 10f, 10f );//exit normal

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
