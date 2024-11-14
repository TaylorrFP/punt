using Sandbox;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

public sealed class PuntPlayerController : Component
{
	[Property, Sync] public bool isReady { get; set; }
	[Property, Sync] public Vector3 SceneCursorPosition { get; set; }


	[Property] public Curve arrowCurve { get; set; }

	[Property] public Curve flickCurve { get; set; }


	[Property] public WorldPanel arrow { get; set; }


	[Property, Sync] public PuntPiece hoveredPiece { get; set; }

	[Property, Sync] public PuntPiece selectedPiece { get; set; }

	[Property, Sync] public Vector3 flickVector { get; set; }

	[Property] public float scaledFlickVector { get; set; }

	[Property] public float flickStrength { get; set; } = 0.6f;

	[Property] public float clampedFlickStrength { get; set; } = 650f;


	[Property] public Vector2 targetmouseOffset { get; set; }
	[Property] public Vector2 mouseOffset { get; set; }


	[Property, Sync] public TeamSide teamSide { get; set; }

	[Property, Sync] public ControllerState controllerState { get; set; }

	[Property] public Vector3 targetArrowPos { get; set; }

	[Property] public float arrowUIScale { get; set; }


	protected override void OnStart()
	{
		base.OnStart();


	}

	[Broadcast]
	public void AssignTeam(TeamSide teamSide )
	{
		if ( !IsProxy )
		{
			this.teamSide = teamSide;
		}
		
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )//just do this shit locally
		{

			ReadyInputs(); //lobby stuff - do this better

			PitchTrace();
			PieceTrace();

			//SelectionInputs();
			CalculateFlick();

			CalculateArrow();
			RotatePiece();
			//SetCursor();

		}










	}



	private void ReadyInputs()
	{
		if ( Input.Pressed( "use" ) & this.GameObject.Network.IsOwner)
		{

			if(TestGameMode.Instance.State == GameState.Waiting || TestGameMode.Instance.State == GameState.Countdown )//Only mess with ready if we're in waiting or countdown
			{
				isReady = !isReady;
				TestGameMode.Instance.EvaluateReadyState( this, isReady );
			}


		}
	}
	private void PitchTrace()
	{

		if ( !IsProxy )
		{

			//Trace and find the position of the hit on the pitch collider
			var mousePosition = Mouse.Position;
			var camera = Scene.Camera;
			var ray = camera.ScreenPixelToRay( mousePosition );
			var tr = Scene.Trace.Ray( ray, 10000f ).WithAllTags( "pitch" ).Run();
			SceneCursorPosition = new Vector3( tr.HitPosition.x, tr.HitPosition.y, 0f );
			this.WorldPosition = SceneCursorPosition;

		}




	}
	private void PieceTrace()
	{

		if(controllerState != ControllerState.Grabbing )//only do this stuff if we're not grabbing anything
		{
			// Trace and find a piece that is under the cursor.
			var mousePosition = Mouse.Position;
			var camera = Scene.Camera;
			var ray = camera.ScreenPixelToRay( mousePosition );
			var tr = Scene.Trace.Ray( ray, 10000f )
				.WithAllTags( "piece" )
				.Run();

			if (tr.Hit)// if we hit a piece
			{
				
				

				var puntPiece = tr.GameObject.Components.Get<PuntPiece>();


				if ( puntPiece.teamSide != teamSide )//if it's an enemy piece
				{
					controllerState = ControllerState.HoveringEnemy;
					Mouse.CursorType = "hoveringenemy";

				}
				else//if it is on our team
				{
					switch ( puntPiece.pieceState )
					{

						case PieceState.Ready:

							controllerState = ControllerState.Hovering;
							puntPiece.pieceState = PieceState.Hovered;
							hoveredPiece = puntPiece;
							Mouse.CursorType = "hovering";
							Sound.Play( "sounds/piecehover.sound" );

							break;

						case PieceState.Hovered: //if the piece is already hovered


							controllerState = ControllerState.Hovering;
							puntPiece.pieceState = PieceState.Hovered;
							hoveredPiece = puntPiece;
							Mouse.CursorType = "hovering";

							break;

						case PieceState.Grabbed: //if the piece is already grabbed

							controllerState = ControllerState.Disabled;
							hoveredPiece = null;
							Mouse.CursorType = "disabled";

							break;

						case PieceState.Cooldown:

							controllerState = ControllerState.Busy;
							hoveredPiece = null;
							Mouse.CursorType = "cooldown";

							break;

						case PieceState.Frozen:

							controllerState = ControllerState.Disabled;
							hoveredPiece = null;
							Mouse.CursorType = "disabled";

							//don't do anything here, we can't unfreeze
							//just set the controllerstate to disabled

							break;

					}

				}
			}else
			{
				//if we don't hit anything, do a bit of a reset
				controllerState = ControllerState.Idle;
				if(hoveredPiece != null) {hoveredPiece.pieceState = PieceState.Ready;}
				hoveredPiece = null;
				Mouse.CursorType = "pointer";
			}

		}//we've done all the initial traces, we can check our inputs to see if we want to select, or deselect anything

		if ( Input.Pressed( "attack1" ) )
		{
			

			switch ( controllerState )
			{
				case ControllerState.Hovering:
					hoveredPiece.ToggleHover();
					selectedPiece = hoveredPiece;
					selectedPiece.ToggleSelection();
					hoveredPiece = null;

					selectedPiece.playerModelHolder.Network.TakeOwnership();//so the rotate is networked

					selectedPiece.pieceState = PieceState.Grabbed;

					Mouse.CursorType = "grabbing";
					controllerState = ControllerState.Grabbing;
					break;

				case ControllerState.HoveringEnemy:
					Sound.Play( "sounds/piecedud.sound" );
					break;

				case ControllerState.Busy:
					Sound.Play( "sounds/piecedud.sound" );
					break;

				case ControllerState.Disabled:
					Sound.Play( "sounds/piecedud.sound" );
					Log.Info( "here!" );
					break;

			}
			


		}

		if ( Input.Released( "attack1" ) )
		{
			switch ( controllerState )
			{

				case ControllerState.Grabbing:


					selectedPiece.playerModelHolder.Network.DropOwnership();//drop ownership of the model rotation
					selectedPiece.ToggleSelection();
					FlickPiece( selectedPiece, flickVector );
					controllerState = ControllerState.Idle;


					Mouse.CursorType = "pointer";
					break;
			}

		}




	}
	private void SelectionInputs()
	{

		//if ( Input.Pressed( "attack1" ) && controllerState == ControllerState.Hovering && hoveredPiece.pieceState==PieceState.Hovered)//if we press input, and the piece isn't frozen or anything
		//{
		//	hoveredPiece.ToggleHover();
		//	selectedPiece = hoveredPiece;
		//	hoveredPiece = null;

		//	selectedPiece.pieceState = PieceState.Grabbed;
		//	Mouse.CursorType = "grabbing";

		//	controllerState = ControllerState.Grabbing;
		//	//if ( hoveredPiece.isOnCooldown != true )
		//	//{
		//	//	hoveredPiece.ToggleHover();
		//	//	selectedPiece = hoveredPiece;
		//	//	hoveredPiece = null;
		//	//	selectedPiece.ToggleSelection();
		//	//	controllerState = ControllerState.Grabbing;
		//	//	Mouse.CursorType = "grabbing";
		//	//	selectedPiece.pieceState = PieceState.Grabbed;

		//	//}

		//}

		//if ( Input.Released( "attack1" ) & selectedPiece != null )
		//{


		//	selectedPiece.ToggleSelection();
		//	FlickPiece(selectedPiece, flickVector);
		//	controllerState = ControllerState.Idle;
		//	Mouse.CursorType = "pointer";
			
		//}


	}
	
	public void CalculateFlick()
	{
		if (selectedPiece != null)
		{
			var screenFudge = 1995f/ Screen.Width; //use the screen width for now, so it feels roughly the same on different resolutions

			targetmouseOffset += Mouse.Delta*1f*screenFudge;
			//mouseOffset = Vector3.Lerp( mouseOffset, targetmouseOffset, Time.Delta * 170f ); //don't bother lerping for now, just do it for the UI?
			mouseOffset = targetmouseOffset;


			flickVector = new Vector3(-mouseOffset.x, mouseOffset.y, 0 );

			flickVector = flickVector * flickStrength;



			flickVector = flickVector.ClampLength( clampedFlickStrength );


			//if ( (flickVector.Length > clampedFlickStrength) )//clamp it
			//{
			//	flickVector = flickVector.Normal * clampedFlickStrength;
			//}


		

		}
		else
		{
			mouseOffset = Vector3.Zero;
			targetmouseOffset = 0;
		}
	}

	private void CalculateArrow()
	{
		

		if ( selectedPiece != null )
		{
			arrow.GameObject.Enabled = true;


			arrow.WorldPosition = selectedPiece.WorldPosition + Vector3.Up * 20f;

			arrow.WorldRotation = Rotation.LookAt( Vector3.Up, flickVector ) * Rotation.FromAxis( Vector3.Forward, 90 );

			arrowUIScale = (flickVector.Length / clampedFlickStrength) * 8000f;

			//arrow.PanelSize = new Vector2(arrowScale *17f, 1000f );

			
			arrow.WorldPosition += flickVector.Normal * arrow.PanelSize.x * 0.025f;

		}
		else
		{

			arrow.GameObject.Enabled = false;
		}

	}
	private void RotatePiece()
	{
		if ( selectedPiece !=null )
		{
			var axisVector = selectedPiece.WorldRotation.Up;
			var target = flickVector;

			Vector3 projectedTargetDir = Vector3.VectorPlaneProject(target,axisVector);

			Rotation targetRotation = Rotation.LookAt( projectedTargetDir, axisVector );

			selectedPiece.playerModelHolder.WorldRotation = targetRotation;

		}
	}

	private void SetCursor()
	{




		if ( !selectedPiece.IsValid() && !hoveredPiece.IsValid() )
		{
			Mouse.CursorType = "pointer";
		}

		if ( hoveredPiece != null )
		{

			if ( hoveredPiece.isOnCooldown )
			{
				Mouse.CursorType = "not-allowed";
			}
			else
			{
				Mouse.CursorType = "grab";
			}


		}

		if ( selectedPiece != null )
		{
			Mouse.CursorType = "grabbing";
		}

	}






	[Broadcast]
	public void FlickPiece(PuntPiece piece, Vector3 flickVector)
	{

		scaledFlickVector = flickVector.Length / clampedFlickStrength;

		piece.rigidBody.PhysicsBody.Velocity = flickVector;

		piece.StartCooldown();
		piece.pieceState = PieceState.Cooldown;
		selectedPiece = null;


	}




}
