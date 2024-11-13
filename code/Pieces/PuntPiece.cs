using Sandbox;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

public struct OutlinePreset
{
	[Property] public Color Color { get; set; }
	[Property] public Color ObscuredColor { get; set; }
	[Property] public Color InsideColor { get; set; }
	[Property] public Color InsideObscuredColor { get; set; }
	[Property] public float Width { get; set; }
}
public sealed class PuntPiece : Component
{
	[Property] public GameObject playerStatus;
	[Property] public Material playerStatusMaterial;
	[Property] public SquashAndStretch playerSquashStretch;

	[Property] public OutlinePreset hoveredOutline { get; set; }
	[Property] public OutlinePreset grabbedOutline { get; set; }
	[Property] public OutlinePreset frozenOutline { get; set; }

	//[Property] public PieceState pieceState { get; set; }

	[Property] public ModelRenderer playerStatusRenderer;

	TimeSince cooldownTimeSince = 1f;

	[Property] public float cooldownDuration;

	[Property] public bool isHovered { get; set; }
	[Property] public bool isSelected { get; set; }

	[Property] public bool isOnCooldown { get; set; }

	[Property] public HighlightOutline outline { get; set; }

	[Property] public float highlightWidth { get; set; } = 0.25f;

	[Property] public Rigidbody rigidBody { get; set; }

	[Property] public GameObject playerModelHolder { get; set; }

	[Property] public ModelRenderer puntPlayerModel { get; set; }

	[Property] public TeamSide teamSide { get; set; }
	[Property] public int pieceID { get; set; }

	[Property] public ShakeEffect shakeEffect { get; set; }



	private PieceState _pieceState;

	[Property, Sync]
	public PieceState pieceState
	{
		get => _pieceState;
		set
		{
			if ( _pieceState != value )
			{
				_pieceState = value;
				OnPieceStateChanged( _pieceState ); // Call the method to handle state changes
			}
		}
	}

	private void OnPieceStateChanged( PieceState newState )
	{
		switch ( newState )
		{
			case PieceState.Frozen:
				HandleFrozenState();
				break;
			case PieceState.Grabbed:
				HandleGrabbedState();
				break;
			case PieceState.Hovered:
				HandleHoveredState();
				break;
			// Add additional cases as needed for other states
			default:
				outline.Enabled = false;
				Log.Info( "Unhandled PieceState: " + newState );
				break;
		}
	}


	private void HandleFrozenState()
	{
		Log.Info( "Piece is now Frozen" );

		outline.Enabled = true;
		outline.Color = frozenOutline.Color;
		outline.ObscuredColor = frozenOutline.ObscuredColor;
		outline.InsideColor = frozenOutline.InsideColor;
		outline.InsideObscuredColor = frozenOutline.InsideObscuredColor;
		outline.Width = frozenOutline.Width;

	}

	private void HandleGrabbedState()
	{
		Log.Info( "Piece is now Frozen" );

		outline.Enabled = true;
		outline.Color = grabbedOutline.Color;
		outline.ObscuredColor = grabbedOutline.ObscuredColor;
		outline.InsideColor = grabbedOutline.InsideColor;
		outline.InsideObscuredColor = grabbedOutline.InsideObscuredColor;
		outline.Width = grabbedOutline.Width;
	}

	private void HandleHoveredState()
	{
		outline.Enabled = true;
		outline.Color = hoveredOutline.Color;
		outline.ObscuredColor = hoveredOutline.ObscuredColor;
		outline.InsideColor = hoveredOutline.InsideColor;
		outline.InsideObscuredColor = hoveredOutline.InsideObscuredColor;
		outline.Width = hoveredOutline.Width;
	}



	protected override void OnStart()
	{
		base.OnStart();
		playerStatusMaterial = playerStatus.Components.Get<ModelRenderer>().GetMaterial( 0 );
	



	}

	public void ToggleHover()
	{
		//outline.Color = Color.White;
		isHovered = !isHovered;

		if ( isHovered )
		{
			//outline.Enabled = true;
			Sound.Play( "sounds/piecehover.sound" ); //weird delay on this for some reason
		}
		else
		{
			
			//outline.Enabled = false;
		}
	}

	public void ToggleSelection()
	{
		isSelected = !isSelected;

		if ( isSelected )
		{
			//outline.Enabled = true;
			//outline.Color = new Color( 0, 1, 0, 1 );
			playerSquashStretch.StartSquash( 0.4f );
			Sound.Play( "sounds/pieceselect.sound" );

			shakeEffect.Strength = 1f;
			//Scene.TimeScale = 0.1f;

		}
		else
		{
			//outline.Enabled = false;
			shakeEffect.Strength = 0f;
			//Scene.TimeScale = 1f;
		}
	}

	[Broadcast]
	public void Initialize(int ID,TeamSide Side )//initialise in an RPC, client sets their stuff locally
	{
		pieceID = ID;
		teamSide = Side;

		if ( teamSide == TeamSide.Red )
		{
			puntPlayerModel.MaterialGroup = "red";
		}
		else
		{
			puntPlayerModel.MaterialGroup = "blue";
		}


		playerStatusRenderer.SceneObject.Attributes.Set( "Progress", 0f );
		cooldownTimeSince = cooldownDuration;

		//pieceState = PieceState.Frozen; //don't do this on debug server maybe

		//if ( IsProxy )
		//{
		//	rigidBody.PhysicsBody.MotionEnabled = false;

		//}
	}

	protected override void OnUpdate()
	{
		CalculatePlayerStatusPanel();
		CalculateCooldown();

	}

	private void CalculateCooldown()
	{

		if ( cooldownTimeSince < cooldownDuration & playerStatusRenderer!=null)
		{
			playerStatusRenderer.SceneObject.Attributes.Set( "Progress", cooldownTimeSince / cooldownDuration );
		}
		else
		{
			isOnCooldown = false;
			playerStatus.Enabled = false;

		}
	}

	private void CalculatePlayerStatusPanel()
	{
		playerStatus.WorldPosition = new Vector3( this.WorldPosition.x, this.WorldPosition.y, 10f );
		playerStatus.WorldRotation = Rotation.From( new Angles( 0, -90, 0 ) );
	}


	[Broadcast]
	public void StartCooldown()
	{

		cooldownTimeSince = 0f;
		isOnCooldown = true;
		playerStatus.Enabled = true;

	}


}
