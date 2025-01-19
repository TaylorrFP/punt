using Sandbox;
using Sandbox.ModelEditor.Nodes;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

public sealed class PuntPlayerController : Component
{

	[Property, Sync] public Vector3 SceneCursorPosition { get; set; }


	[Property] public Curve arrowCurve { get; set; }

	[Property] public Curve flickCurve { get; set; }

	[Property] public int queueScore { get; set; }



	[Property, Sync] public WorldPanel arrow { get; set; }


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

	[Property, Sync] public float arrowUIScale { get; set; }

	[Property] public SpriteRenderer cursorRenderer { get; set; }




	protected override void OnStart()
	{
		base.OnStart();

		if ( !IsProxy ) //if we control this - set tell the game mode our local side.
		{
			TestGameMode.Instance.mySide = teamSide;
		}


		


	}

	[Rpc.Broadcast]
	public void AssignTeam(TeamSide teamSide )
	{
		if ( !IsProxy )
		{
			this.teamSide = teamSide;
		}
		
	}
	protected override void OnUpdate()
	{
		if ( !IsProxy )// if you own this controller
		{

			PitchTrace();
			PieceTrace();
			CalculateFlick();
			RotatePiece();


		}


		CalculateArrow();





		

		if ( !IsProxy || (IsProxy && TestGameMode.Instance.mySide != teamSide) )
		{
			cursorRenderer.Enabled = false;
		}
		else
		{
			cursorRenderer.Enabled = true;
		}





	}

	[Rpc.Broadcast]
	public void InitArrow(GameObject arrowGO)
	{

		//As soon as we spawn in the arrow, initialise the values here
		//they can't be set in the prefab for some reason
		arrow = arrowGO.GetComponent<WorldPanel>();
		var arrowComponent = arrowGO.GetComponent<AimArrow>();
		arrowComponent.playerController = this;

	}

	private void CalculateArrow()
	{


		if ( arrow!=null)
		{
			if ( selectedPiece != null ) //if this controller does have a selected piece
			{
				if ( IsProxy && TestGameMode.Instance.mySide != teamSide )
				{

					
					arrow.GameObject.Enabled = false;

				}
				else
				{
					

					arrow.GameObject.Enabled = true;

					arrow.WorldPosition = selectedPiece.WorldPosition + Vector3.Up * 20f;

					arrow.WorldRotation = Rotation.LookAt( Vector3.Up, flickVector ) * Rotation.FromAxis( Vector3.Forward, 90 );

					arrowUIScale = (flickVector.Length / clampedFlickStrength) * 8000f;

					arrow.WorldPosition += flickVector.Normal * arrow.PanelSize.x * 0.025f;



				}


			}
			else
			{
				arrow.GameObject.Enabled = false;

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

		if(TestGameMode.Instance.State == GameState.Resetting )
		{
			//reset everything for now
			//Log.Info( "Resetting" );
			controllerState = ControllerState.Idle;
			Mouse.CursorType = "cooldown";

			if(selectedPiece != null )
			{
				selectedPiece.ToggleSelection();
				selectedPiece.pieceState = PieceState.Ready;
				

			}

			if ( hoveredPiece != null )
			{
				hoveredPiece.ToggleHover();
				hoveredPiece.pieceState = PieceState.Ready;

			}
		}
		else
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
				if ( hoveredPiece != null )
				{
					controllerState = ControllerState.Idle;
					Log.Info( "hovered pice is valid" );
					hoveredPiece.ToggleHover();
					hoveredPiece = null;
					Mouse.CursorType = "pointer";

				}

				controllerState = ControllerState.Idle;
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



	}

	
	public void CalculateFlick()
	{
		if (selectedPiece != null)
		{
			var screenFudge = 1995f/ Screen.Width; //use the screen width for now, so it feels roughly the same on different resolutions

			targetmouseOffset += Mouse.Delta*1f*screenFudge;
			mouseOffset = targetmouseOffset;


			flickVector = new Vector3(-mouseOffset.x, mouseOffset.y, 0 );

			flickVector = flickVector * flickStrength;



			flickVector = flickVector.ClampLength( clampedFlickStrength );


		

		}
		else
		{
			mouseOffset = Vector3.Zero;
			targetmouseOffset = 0;
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






	[Rpc.Broadcast]
	public void FlickPiece(PuntPiece piece, Vector3 flickVector)
	{

		piece.PieceFlicked();
		scaledFlickVector = flickVector.Length / clampedFlickStrength;

		piece.rigidBody.PhysicsBody.Velocity = flickVector;

		piece.StartCooldown();
		piece.pieceState = PieceState.Cooldown;
		selectedPiece = null;


	}




}
