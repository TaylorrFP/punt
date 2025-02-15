using Sandbox;
using System;

public sealed class PackOpener : Component
{
	[Property] public GameObject TargetObjectY { get; set; }  // Rotates around Y-axis (Left/Right)
	[Property] public GameObject TargetObjectX { get; set; }  // Rotates around X-axis (Up/Down)

	private float TargetAnglesY { get; set; }  // Yaw rotation
	private float TargetAnglesX { get; set; }  // Pitch rotation

	[Property] public Vector3 SelectedPosition { get; set; }
	[Property] public Vector3 TargetPosition { get; set; }

	[Property] public float ActiveLerpSpeed { get; set; }
	[Property] public float PassiveLerpSpeed { get; set; }
	[Property] public float TranslateLerpSpeed { get; set; }

	private float LerpSpeed { get; set; }

	[Property] public float RotationSpeed { get; set; } = 0.5f;  // Mouse sensitivity
	[Property] public float MaxPitchAngle { get; set; } = 45f;  // Max pitch angle
	[Property] public bool isDragging { get; set; }
	[Property] public bool isSelected { get; set; }

	private bool wasDragging = false; // Track if dragging occurred

	// Velocity tracking for Y-axis only
	private float velocityY = 0f;
	[Property] public float DampingFactor { get; set; } = 5f;
	[Property] public float VelocityThreshold { get; set; } = 1f; // Threshold before resetting

	// Smoothing variables for rotation & position
	private float rotationVelocityY = 0f;
	private float rotationVelocityX = 0f;
	private Vector3 positionVelocity = Vector3.Zero;

	protected override void OnAwake()
	{
		// Store initial rotation values
		TargetAnglesY = TargetObjectY.LocalRotation.Angles().yaw;
		TargetAnglesX = TargetObjectX.LocalRotation.Angles().pitch;
	}

	protected override void OnUpdate()
	{
		PackTrace();

		if ( isDragging )
		{
			RotatePack();
			Mouse.CursorType = "grabbing";
		}
		else if ( MathF.Abs( velocityY ) > VelocityThreshold )
		{
			// Apply momentum on Y rotation only
			TargetAnglesY += velocityY * Time.Delta;

			// Apply damping
			velocityY = MathX.Lerp( velocityY, 0, Time.Delta * DampingFactor );
		}
		else
		{
			// Once below threshold, snap to closest front/back rotation
			ResetRotation();
		}

		// Use Quaternion Slerp to prevent weird flipping
		if ( TargetObjectY != null )
		{
			Rotation targetRotationY = Rotation.FromYaw( TargetAnglesY );
			TargetObjectY.LocalRotation = Rotation.Slerp( TargetObjectY.LocalRotation, targetRotationY, Time.Delta * LerpSpeed );
		}

		// up/down
		if ( TargetObjectX != null )
		{
			Rotation targetRotationX = Rotation.FromPitch( TargetAnglesX );
			TargetObjectX.LocalRotation = Rotation.Slerp( TargetObjectX.LocalRotation, targetRotationX, Time.Delta * LerpSpeed );

			// interpolate position - do  this always, maybe it's not the best idea
			TargetObjectX.LocalPosition = Vector3.SmoothDamp(
				TargetObjectX.LocalPosition, TargetPosition, ref positionVelocity, TranslateLerpSpeed, Time.Delta
			);
		}
	}

	private void PackTrace()
	{
		var mousePosition = Mouse.Position;
		var camera = Scene.Camera;
		var ray = camera.ScreenPixelToRay( mousePosition );
		var tr = Scene.Trace.Ray( ray, 10000f ).WithAllTags( "pack" ).Run();

		if ( tr.Hit )
		{
			Mouse.CursorType = "hovering";
			if ( Input.Pressed( "attack1" ) )
			{
				Log.Info( "Click Start" );
				isDragging = true;
				wasDragging = false;
				LerpSpeed = ActiveLerpSpeed;
			}
		}
		else
		{
			Mouse.CursorType = "pointer";
		}

		if ( Input.Released( "attack1" ) )
		{
			if ( !wasDragging )
			{
				if ( isSelected )
				{
					Log.Info( "Deselecting Object!" );
					isSelected = false;
					TargetPosition = Vector3.Zero;
				}
				else
				{
					Log.Info( "Single Click Detected!" );
					isSelected = true;
					TargetPosition = SelectedPosition;
					TargetAnglesY = 0;
				}
			}

			isDragging = false;
			LerpSpeed = PassiveLerpSpeed;
			TargetAnglesX = 0; // Reset pitch immediately when mouse is released
		}
	}

	private void RotatePack()
	{
		var mouseDelta = Mouse.Delta;

		if ( mouseDelta.Length > 0.5f )
		{
			wasDragging = true;
		}

		// Track velocity only for Y-axis (yaw)
		velocityY = mouseDelta.x * RotationSpeed;

		// Update rotation angles
		TargetAnglesY += velocityY;
		TargetAnglesX += mouseDelta.y * RotationSpeed;

		// Clamp X rotation
		TargetAnglesX = TargetAnglesX.Clamp( -MaxPitchAngle, MaxPitchAngle );
	}

	private void ResetRotation()
	{
		TargetAnglesX = 0;
		TargetAnglesY = GetClosestYaw( TargetAnglesY ); // Smoothly reset Yaw without snapping
	}

	private float GetClosestYaw( float currentYaw )
	{
		// Normalize yaw
		float normalizedYaw = ((currentYaw % 360) + 360) % 360;
		return (normalizedYaw >= 270 || normalizedYaw <= 90) ? 0f : 180f;
	}
}
