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

			SelectionInputs();
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
		//Trace and find the position of the hit on the pitch collider
		var mousePosition = Mouse.Position;
		var camera = Scene.Camera;
		var ray = camera.ScreenPixelToRay( mousePosition );
		var tr = Scene.Trace.Ray( ray, 10000f ).WithAllTags( "pitch" ).Run();
		SceneCursorPosition = new Vector3( tr.HitPosition.x, tr.HitPosition.y, 0f );
	}
	private void PieceTrace()
	{
		// Trace and find a piece that is under the cursor.
		var mousePosition = Mouse.Position;
		var camera = Scene.Camera;
		var ray = camera.ScreenPixelToRay( mousePosition );
		var tr = Scene.Trace.Ray( ray, 10000f )
			.WithAllTags( "piece" )
			.Run();

		if ( tr.Hit && controllerState == ControllerState.Idle )// if we hit something, and don't have a hovered piece or a selected piece
		{
			var puntPiece = tr.GameObject.Components.Get<PuntPiece>();

			if ( puntPiece.teamSide == teamSide )//if the piece we're hovering is the same team as us
			{

				switch ( puntPiece.pieceState ) {

					case PieceState.Ready:
						Log.Info( "Piece is Ready" );

						puntPiece.pieceState = PieceState.Hovered;
						hoveredPiece = puntPiece;
						Mouse.CursorType = "hovering";

						break;

					case PieceState.Hovered:
						Log.Info( "Piece is already Hovered" );//if the piece is already hovered

						puntPiece.pieceState = PieceState.Hovered;
						hoveredPiece = puntPiece;

						break;

					case PieceState.Grabbed: //if the piece is already grabbed
						Log.Info( "Piece is already Grabbed" );
						Mouse.CursorType = "disabled";

						break;

					case PieceState.Cooldown:
						Log.Info( "Piece is on Cooldown" );
						Mouse.CursorType = "cooldown";

						break;

					case PieceState.Frozen:
						Log.Info( "Piece is Frozen" );
						Mouse.CursorType = "disabled";

						//don't do anything here, we can't unfreeze
						//just set the controllerstate to disabled

						break;

				}


				//hoveredPiece = tr.GameObject.Components.Get<PuntPiece>();

				//hoveredPiece?.ToggleHover();//do this on the state change in the future

				//controllerState = ControllerState.Hovering;

			}
			else
			{
				controllerState = ControllerState.HoveringEnemy;//if it isn't
				Log.Info( "Piece is Enemy" );
				Mouse.CursorType = "hoveringenemy";
			}

		}
		else if ( !tr.Hit && hoveredPiece != null )
		{
			// No piece hit and there's currently a hovered piece
			hoveredPiece.ToggleHover();
			hoveredPiece.pieceState = PieceState.Ready;
			hoveredPiece = null;

		}
		else if ( !tr.Hit && selectedPiece == null )//no piece hit and we don't have a selected piece
		{
			controllerState = ControllerState.Idle;

		}
	}
	//private void PieceTrace() //OLD
	//{
	//	// Trace and find a piece that is under the cursor.
	//	var mousePosition = Mouse.Position;
	//	var camera = Scene.Camera;
	//	var ray = camera.ScreenPixelToRay( mousePosition );
	//	var tr = Scene.Trace.Ray( ray, 10000f )
	//		.WithAllTags( "piece" )
	//		.Run();

	//	if ( tr.Hit && hoveredPiece == null && selectedPiece == null)// if we hit something, and don't have a hovered piece or a selected piece
	//	{
	//		var puntPiece = tr.GameObject.Components.Get<PuntPiece>();

	//		if( puntPiece.teamSide == teamSide )//if the piece we're hovering is the same team as us
	//		{
	//			hoveredPiece = tr.GameObject.Components.Get<PuntPiece>();
	//			hoveredPiece?.ToggleHover();

	//			controllerState = ControllerState.Hovering;

	//		}
	//		else
	//		{
	//			controllerState = ControllerState.HoveringEnemy;//if it isn't

	//		}

	//	}
	//	else if ( !tr.Hit && hoveredPiece != null )
	//	{
	//		// No piece hit and there's currently a hovered piece
	//		hoveredPiece.ToggleHover();
	//		hoveredPiece = null;

	//	}else if ( !tr.Hit && selectedPiece == null )//no piece hit and we don't have a selected piece
	//	{
	//		controllerState = ControllerState.Idle;

	//	}
	//}
	private void SelectionInputs()
	{

		if ( Input.Pressed( "attack1" ) & hoveredPiece != null )
		{
			if ( hoveredPiece.isOnCooldown != true )
			{
				hoveredPiece.ToggleHover();
				selectedPiece = hoveredPiece;
				hoveredPiece = null;
				selectedPiece.ToggleSelection();
				controllerState = ControllerState.Grabbing;
				Mouse.CursorType = "grabbing";
				selectedPiece.pieceState = PieceState.Grabbed;

			}

		}

		if ( Input.Released( "attack1" ) & selectedPiece != null )
		{


			selectedPiece.ToggleSelection();
			FlickPiece(selectedPiece, flickVector);
			selectedPiece = null;
			controllerState = ControllerState.Idle;
		}


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



		
		//scaledFlickVector = flickVector.Length / clampedFlickStrength;
		//var finalflickVector = flickVector.Normal * flickCurve.Evaluate( scaledFlickVector ) * clampedFlickStrength;

		//piece.rigidBody.PhysicsBody.Velocity = finalflickVector;



		scaledFlickVector = flickVector.Length / clampedFlickStrength;

		//Log.Info( scaledFlickVector );

		//	if ( !Network.Owner.IsHost )
		//{
		//	piece.Network.TakeOwnership();

		//}

			piece.rigidBody.PhysicsBody.Velocity = flickVector;


		



		piece.StartCooldown();


		//selectedPiece = null;
	}




}
