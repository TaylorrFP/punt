using Sandbox;
using System;
using System.Net.Http.Headers;

public sealed class PackOpener : Component
{
	[Property] public SkinnedModelRenderer SkinnedModelRenderer { get; set; }
	[Property] public float OpenAmount { get; set; }
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
	[Property] public bool isOpening { get; set; }
	[Property] public bool isOpened { get; set; } = false;

	[Property] public float mouseX { get; set; }
	[Property] public float mouseY { get; set; }

	private bool wasDragging = false; // Track if dragging occurred

	// Velocity tracking for Y-axis only
	private float velocityY = 0f;
	[Property] public float DampingFactor { get; set; } = 5f;
	[Property] public float VelocityThreshold { get; set; } = 1f; // Threshold before resetting

	// Smoothing variables for rotation & position

	private Vector3 positionVelocity = Vector3.Zero;

	[Property] public LineRenderer LineRenderer { get; set; }

	[Property] public Vector3 LineRendererStartPoint { get; set; }

	protected override void OnAwake()
	{
		// Store initial rotation values
		TargetAnglesY = TargetObjectY.LocalRotation.Angles().yaw;
		TargetAnglesX = TargetObjectX.LocalRotation.Angles().pitch;

		SkinnedModelRenderer.Sequence.Name = "puntpack_opening";

	}

	protected override void OnUpdate()
	{
		

		PackTrace();


		if ( isSelected )
		{
			PackOpenerTrace(); // Run new trace logic only when pack is selected
		}
		if ( Input.Released( "attack1") & isOpening)
		{
			isOpening = false;

		}

		if ( isOpening )
		{
			LineRenderer.Enabled = true;
			SkinnedModelRenderer.Sequence.PlaybackRate = Mouse.Delta.x*0.3f;
			mouseX += Mouse.Delta.x * 0.3f;
			mouseY += Mouse.Delta.y * 0.3f;

			LineRenderer.VectorPoints[0] = LineRendererStartPoint;


			LineRenderer.VectorPoints[1] = LineRendererStartPoint + new Vector3( 0, mouseX * 0.6f, -mouseY*0.5f );


			if(mouseX > 80f )
			{
				isOpening = false;
				isOpened = true;

			}
		}
		else
		{
			SkinnedModelRenderer.Sequence.PlaybackRate = 0;
		}

		if ( isOpened )
		{

			
			SkinnedModelRenderer.Sequence.PlaybackRate = 1;

			LineRenderer.VectorPoints[1] = LineRendererStartPoint + new Vector3( 0, 600f, 0);
			LineRenderer.Width = 0.5f;

			if ( SkinnedModelRenderer.Sequence.TimeNormalized > 0.25f )
			{
				LineRenderer.Enabled = false;

			}

			if ( SkinnedModelRenderer.Sequence.TimeNormalized > 0.99 )
			{
				SkinnedModelRenderer.Sequence.PlaybackRate = 0;

			}

			//if( SkinnedModelRenderer.Sequence.IsFinished){

			//	SkinnedModelRenderer.Sequence.PlaybackRate = 0;

			//}

		}


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

	private void PackOpenerTrace()
	{
		var mousePosition = Mouse.Position;
		var camera = Scene.Camera;
		var ray = camera.ScreenPixelToRay( mousePosition );
		var tr = Scene.Trace.Ray( ray, 10000f ).WithAllTags( "packopener" ).Run();


		if ( tr.Hit & Input.Pressed( "attack1" ) )
		{
			isOpening = true;
			LineRendererStartPoint = tr.HitPosition;
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
				//Log.Info( "Click Start" );
				isDragging = true;
				wasDragging = false; // Reset dragging flag
				LerpSpeed = ActiveLerpSpeed;
			}
		}
		else
		{
			Mouse.CursorType = "pointer";
		}

		// Track dragging while the mouse is held down
		if ( isDragging && Mouse.Delta.Length > 0.5f )
		{
			wasDragging = true;
		}

		if ( Input.Released( "attack1" ) )
		{
			if ( !wasDragging ) // Ensure it's a true single click
			{
				if ( isSelected )
				{
					if ( tr.Hit ) // Only deselect if clicking directly on the pack
					{
						//Log.Info( "Deselecting Object!" );
						isSelected = false;
						TargetPosition = Vector3.Zero;
					}
				}
				else
				{
					//Log.Info( "Single Click Detected!" );
					isSelected = true;
					TargetPosition = SelectedPosition;
					TargetAnglesY = 0;
				}
			}

			isDragging = false;
			wasDragging = false; // Reset dragging flag
			LerpSpeed = PassiveLerpSpeed;
			TargetAnglesX = 0; // Reset pitch immediately when mouse is released
		}
	}



	private void RotatePack()
	{
		if ( isSelected )
			return;  // Disable rotation when the card is selected

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
