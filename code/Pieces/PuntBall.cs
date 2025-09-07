using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Internal;
using System;

public sealed class PuntBall : Component, Component.ICollisionListener
{
	[Property] public WorldPanel ballGuide;

	// Visual + physics
	[Property] public GameObject ballModel;               // your visible mesh (ideally no collider here)
	[Property] public Rigidbody ballRB { get; set; }      // physics on the parent/this
	[Property] public GameMode gameMode { get; set; }

	// Back-of-net damping (kept as your original name)
	[Property] public float goalWallDampning { get; set; } = 0.9f;

	// --- Squash & Stretch (keep visible rotation unchanged) -------------------
	// Hierarchy: Ball(this) -> DeformSpace(aims to velocity & gets scaled)
	//                           -> DeformMesh(=your model, counter-rotated so it looks unrotated)
	[Group( "Squash & Stretch" )][Property] public GameObject DeformSpace { get; set; }
	[Group( "Squash & Stretch" )][Property] public GameObject DeformMesh { get; set; }

	// Speed → stretch
	[Group( "Squash & Stretch" )][Property] public float MaxSpeedForStretch { get; set; } = 1600f;
	[Group( "Squash & Stretch" )][Property] public float MaxStretchScale { get; set; } = 1.25f; // along velocity at max speed
	[Group( "Squash & Stretch" )][Property] public float MinSquashScale { get; set; } = 0.72f; // along velocity on impact
	[Group( "Squash & Stretch" )][Property] public Curve StretchBySpeed { get; set; } = Curve.EaseOut;

	// Smoothing
	[Group( "Squash & Stretch" )][Property] public float AlignLerpSpeed { get; set; } = 12f; // DeformSpace world-aim speed
	[Group( "Squash & Stretch" )][Property] public float ScaleLerpSpeed { get; set; } = 12f; // scale ease speed
	[Group( "Squash & Stretch" )][Property] public float UpdateWhenSpeedAbove { get; set; } = 10f; // ignore noise at tiny speeds

	// Bounce pulse from sharp Δv & direction change
	[Group( "Squash & Stretch" )][Property] public float ImpactSensitivity { get; set; } = 0.9f;
	[Group( "Squash & Stretch" )][Property] public float ImpactDecayPerSecond { get; set; } = 6f;

	private Rotation _meshBaseLocalRot = Rotation.Identity; // used to counter-rotate mesh
	private Vector3 _prevVel;
	private float _impactPulse; // 0..1
	private bool _rigReady;

	// --------------------------------------------------------------------------

	protected override void OnAwake()
	{
		SetupDeformRig();
	}

	private void SetupDeformRig()
	{
		// Choose the visual mesh to deform
		if ( DeformMesh == null ) DeformMesh = ballModel;
		if ( DeformMesh == null )
		{
			Log.Warning( "[PuntBall] No DeformMesh/ballModel assigned; squash&stretch disabled." );
			_rigReady = false;
			return;
		}

		// Ensure a DeformSpace exists (child of the ball)
		if ( DeformSpace == null )
		{
			DeformSpace = new GameObject( "DeformSpace" )
			{
				Parent = GameObject
			};
			DeformSpace.LocalPosition = Vector3.Zero;
			DeformSpace.LocalRotation = Rotation.Identity;
			DeformSpace.LocalScale = Vector3.One;
		}

		// Reparent the mesh under DeformSpace, preserving world transform
		var meshWT = DeformMesh.WorldTransform;
		DeformMesh.Parent = DeformSpace;
		DeformMesh.WorldTransform = meshWT;

		// Record mesh's local rotation so we can counter-rotate later
		_meshBaseLocalRot = DeformMesh.LocalRotation;

		_rigReady = true;
	}

	// ------------------- Collisions ------------------------------------------

	public void OnCollisionStart( Collision collision )
	{
		if ( collision.Other.GameObject.Tags.Has( "piece" ) )
		{
			// hook for future piece-specific logic
		}

		if ( collision.Other.GameObject.Tags.HasAny( "Net" ) )
		{
			// dampen ball impact when it's the back of the net
			ballRB.Velocity = ballRB.Velocity * MathX.Clamp( 1f - goalWallDampning, 0f, 1f );
		}

		if ( collision.Other.GameObject.Tags.HasAny( "GoalPost" ) )
		{
			if ( !IsProxy )
				HitPost();
		}
	}

	[Rpc.Broadcast]
	private void HitPost()
	{
		Sound.Play( "sounds/ball/post.sound" );

		Random random = new Random();
		if ( random.Next( 1, 5 ) == 1 ) // 1-in-4
			Sound.Play( "sounds/ball/crowdgasp.sound" );
	}

	public void OnCollisionUpdate( Collision collision ) { }
	public void OnCollisionStop( CollisionStop collision ) { }

	// ------------------- Tick -------------------------------------------------

	protected override void OnUpdate()
	{
		CalculateBallGuide();
		UpdateSquashStretch();
	}

	protected override void OnFixedUpdate()
	{
		// physics step not needed for this feature
	}

	private void CalculateBallGuide()
	{
		if ( ballGuide == null ) return;
		ballGuide.WorldPosition = new Vector3( WorldPosition.x, WorldPosition.y, 10f );
		ballGuide.WorldRotation = Rotation.From( new Angles( -90f, 0, 0 ) );
	}

	// ------------------- Squash & Stretch Core -------------------------------

	private void UpdateSquashStretch()
	{
		if ( !_rigReady || ballRB == null ) return;

		Vector3 vel = ballRB.Velocity;
		float speed = vel.Length;

		// Aim DeformSpace in *world* space so its +X axis aligns to velocity globally
		if ( speed > UpdateWhenSpeedAbove )
		{
			var dir = vel.Normal;
			var aimWorld = Rotation.LookAt( dir, Vector3.Up );
			float t = 1f - MathF.Exp( -AlignLerpSpeed * Time.Delta ); // framerate-independent
			DeformSpace.WorldRotation = Rotation.Slerp( DeformSpace.WorldRotation, aimWorld, t );
		}

		// Base stretch from speed via curve
		float speed01 = MathX.Clamp( speed / MathF.Max( 1f, MaxSpeedForStretch ), 0f, 1f );
		float curve01 = StretchBySpeed.Evaluate( speed01 ); // 0..1
		float baseAlong = 1f + (MaxStretchScale - 1f) * curve01; // along velocity

		// Impact pulse from sharp Δv & direction changes (bounces, hard touches)
		if ( _prevVel.Length > 1f && speed > 1f )
		{
			float angleDeg = Vector3.GetAngle( _prevVel.Normal, vel.Normal ); // 0..180
			float dvNorm = (vel - _prevVel).Length / MathF.Max( 1f, MaxSpeedForStretch );
			float impulse = dvNorm * (angleDeg / 180f) * ImpactSensitivity;
			if ( impulse > _impactPulse ) _impactPulse = impulse;
		}
		_prevVel = vel;

		// Decay pulse
		_impactPulse = MathX.Clamp( _impactPulse - ImpactDecayPerSecond * Time.Delta, 0f, 1f );

		// Combine base stretch with impact squash along the velocity axis
		float along = MathX.Lerp( baseAlong, MinSquashScale, _impactPulse ).Clamp( 0.001f, 5f );

		// Preserve volume-ish: along * perp^2 ≈ 1  => perp = sqrt(1/along)
		float perp = 1f / MathF.Sqrt( along );
		Vector3 targetScale = new Vector3( along, perp, perp );

		// Smooth scale on DeformSpace (mesh keeps its own base scale)
		float s = 1f - MathF.Exp( -ScaleLerpSpeed * Time.Delta );
		DeformSpace.LocalScale = Vector3.Lerp( DeformSpace.LocalScale, targetScale, s );

		// Counter-rotate the visible mesh so its *world* orientation stays unchanged
		DeformMesh.LocalRotation = DeformSpace.LocalRotation.Inverse * _meshBaseLocalRot;
	}
}
