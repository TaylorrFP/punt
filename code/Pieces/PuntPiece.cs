using Sandbox;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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


	[Property] public ModelRenderer playerCooldownRenderer;

	TimeSince cooldownTimeSince = 1f;

	[Property] public float cooldownDuration;

	[Property] public bool isHovered { get; set; }
	[Property] public bool isSelected { get; set; }

	[Property] public bool isOnCooldown { get; set; }

	[Property] public HighlightOutline outline { get; set; }

	[Property] public float highlightWidth { get; set; } = 0.25f;

	[Property] public Rigidbody rigidBody { get; set; }

	[Property] public GameObject playerModelHolder { get; set; }

	[Property] public ModelRenderer baseModell { get; set; }
	[Property] public ModelRenderer characterModell { get; set; }
	[Property] public TeamSide teamSide { get; set; }
	[Property] public int pieceID { get; set; }

	[Property] public ShakeEffect shakeEffect { get; set; }

	[Property] public ParticleBoxEmitter emitter { get; set; }
	[Property] public float emitterRate { get; set; }



	private PieceState _pieceState;

	//if we just sync the variables, only the server can change these values

	//so we have to do RPCs, but then is there a chance they get out of sync?




	[Property] //issue here is, only the server can set these so we'll get some lag?
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
			case PieceState.Ready:
				HandleReadyState();
				break;

			case PieceState.Frozen:
				HandleFrozenState();
				break;
			case PieceState.Grabbed:
				HandleGrabbedState();
				break;
			case PieceState.Hovered:
				HandleHoveredState();
				break;

			case PieceState.Cooldown:
				HandleCooldownState();
				break;
			// Add additional cases as needed for other states
			default:
				outline.Enabled = false;
				Log.Info( "Unhandled PieceState: " + newState );
				break;
		}
	}

	[Broadcast]
	private void HandleReadyState()
	{

		outline.Enabled = false;
	}

	[Broadcast]
	private void HandleCooldownState()
	{

		outline.Enabled = false;
	}


	[Broadcast]
	public void PieceFlicked()
	{

		// Start the ToggleEmitter task without awaiting it
		_ = ToggleEmitter();
	}
	private async Task ToggleEmitter()
	{
		emitter.Enabled = true;

		await Task.Delay( 1000 ); // Delay for 1 second (1000 ms)

		emitter.Enabled = false;
	}

	[Broadcast]
	private void HandleFrozenState()
	{

		

		outline.Enabled = true;
		outline.Color = frozenOutline.Color;
		outline.ObscuredColor = frozenOutline.ObscuredColor;
		outline.InsideColor = frozenOutline.InsideColor;
		outline.InsideObscuredColor = frozenOutline.InsideObscuredColor;
		outline.Width = frozenOutline.Width;


	}

	[Broadcast]
	private void HandleGrabbedState()
	{

		outline.Enabled = true;
		outline.Color = grabbedOutline.Color;
		outline.ObscuredColor = grabbedOutline.ObscuredColor;
		outline.InsideColor = grabbedOutline.InsideColor;
		outline.InsideObscuredColor = grabbedOutline.InsideObscuredColor;
		outline.Width = grabbedOutline.Width;
	}

	[Broadcast]
	private void HandleHoveredState()
	{
		pieceState = PieceState.Hovered;
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

	[Broadcast]
	public void ToggleHover()
	{
		

			pieceState = PieceState.Ready;
	
		
	}

	[Broadcast]
	public void ToggleSelection()
	{
		isSelected = !isSelected;

		if ( isSelected )
		{
			pieceState = PieceState.Grabbed;
			playerSquashStretch.StartSquash( 0.4f );
			Sound.Play( "sounds/pieceselect.sound" );

			shakeEffect.Strength = 1f;


		}
		else
		{

			shakeEffect.Strength = 0f;
			pieceState = PieceState.Ready;
		}
	}

	[Broadcast]
	public void Initialize(int ID,TeamSide Side, bool isFrozen)//initialise in an RPC, client sets their stuff locally
	{
		pieceID = ID;
		teamSide = Side;

		if ( teamSide == TeamSide.Red )
		{
			baseModell.MaterialGroup = "red";
			characterModell.MaterialGroup = "red";
		}
		else
		{
			baseModell.MaterialGroup = "blue";
			characterModell.MaterialGroup = "blue";
		}


		playerCooldownRenderer.SceneObject.Attributes.Set( "Progress", 0f );
		cooldownTimeSince = cooldownDuration;


		if ( isFrozen )
		{
			pieceState = PieceState.Frozen; //don't do this on debug server maybe

		}

		
	}

	protected override void OnUpdate()
	{
		CalculatePlayerStatusPanel();
		CalculateCooldown();

	}

	private void CalculateCooldown()
	{

		if ( cooldownTimeSince < cooldownDuration && playerCooldownRenderer!=null)
		{
			playerCooldownRenderer.SceneObject.Attributes.Set( "Progress", cooldownTimeSince / cooldownDuration );
		}
		else if (pieceState == PieceState.Cooldown )
	
		{
			isOnCooldown = false;
			playerStatus.Enabled = false;

			pieceState = PieceState.Ready;

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
