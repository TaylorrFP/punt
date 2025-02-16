using Sandbox;
using Sandbox.Resources;
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

	[Property] public TimeSince cooldownTimeSince = 1f;

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

	[Property] public string playerNameString { get; set; }

	[Property] public string playerNumberString { get; set; }

	[Property] public string playerBadgeString { get; set; }

	[Property] public string playerSponsoString { get; set; }

	[Property] public Texture PlayerName { get; set; }
	[Property] public Texture ShirtMask2 { get; set; }
	[Property] public Texture PlayerNumberBack { get; set; }
	[Property] public Texture FrontSponsor { get; set; }
	[Property] public Texture Badge { get; set; }
	[Property] public Texture ShortsNumber { get; set; }

	[Property] public Color PrimaryColour { get; set; }

	[Property] public Color SecondaryColour { get; set; }

	[Property] public Color TertiaryColour { get; set; }


	[Property, Sync( SyncFlags.FromHost )] public bool IsDormant { get; set; } = true;

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

	[Rpc.Broadcast]
	private void HandleReadyState()
	{

		outline.Enabled = false;
	}

	[Rpc.Broadcast]
	private void HandleCooldownState()
	{

		outline.Enabled = false;
	}


	[Rpc.Broadcast]
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

	[Rpc.Broadcast]
	private void HandleFrozenState()
	{

		

		outline.Enabled = true;
		outline.Color = frozenOutline.Color;
		outline.ObscuredColor = frozenOutline.ObscuredColor;
		outline.InsideColor = frozenOutline.InsideColor;
		outline.InsideObscuredColor = frozenOutline.InsideObscuredColor;
		outline.Width = frozenOutline.Width;


	}

	[Rpc.Broadcast]
	private void HandleGrabbedState()
	{

		outline.Enabled = true;
		outline.Color = grabbedOutline.Color;
		outline.ObscuredColor = grabbedOutline.ObscuredColor;
		outline.InsideColor = grabbedOutline.InsideColor;
		outline.InsideObscuredColor = grabbedOutline.InsideObscuredColor;
		outline.Width = grabbedOutline.Width;
	}

	[Rpc.Broadcast]
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

	[Rpc.Broadcast]
	public void ToggleHover()
	{
		

			pieceState = PieceState.Ready;
	
		
	}

	[Rpc.Broadcast]
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

	[Rpc.Broadcast]
	public void Initialize(int ID,TeamSide Side, bool isFrozen)//initialise in an RPC, client sets their stuff locally
	{
		pieceID = ID;
		teamSide = Side;

		if ( teamSide == TeamSide.Red )
		{

			playerBadgeString = "⚽";


			//Log.Info ( PlayerNumberBack.CreateGenerator().FindCached().GetType().Name );

			//PlayerNumberBack.CreateGenerator().
			
		

			PrimaryColour = new Color( 0.07f, 0.07f, 0.07f, 1.00f );
			SecondaryColour = new Color( 1.00f, 0.14f, 0.14f, 0.87f );
			TertiaryColour = new Color( 0.07f, 0.07f, 0.07f, 1.00f );


			characterModell.SceneObject.Attributes.Set( "shirtmask", ShirtMask2 );
			characterModell.SceneObject.Attributes.Set( "playername", PlayerName );
			characterModell.SceneObject.Attributes.Set( "playernumberback", PlayerNumberBack );
			characterModell.SceneObject.Attributes.Set( "frontsponsor", FrontSponsor );
			characterModell.SceneObject.Attributes.Set( "badge", Badge );
			characterModell.SceneObject.Attributes.Set( "shortsnumber", ShortsNumber );

			characterModell.SceneObject.Attributes.Set( "primarycolor", PrimaryColour );
			characterModell.SceneObject.Attributes.Set( "secondarycolor", SecondaryColour );
			characterModell.SceneObject.Attributes.Set( "tertiarycolor", TertiaryColour );
			baseModell.MaterialGroup = "red";
			//characterModell.MaterialGroup = "red";


		}
		else
		{

			PrimaryColour = new Color( 1.00f, 1.00f, 1.00f, 1.00f );
			SecondaryColour = new Color( 0.00f, 0.33f, 0.74f, 1.00f );
			TertiaryColour = new Color( 1.00f, 1.00f, 1.00f, 1.00f );


			characterModell.SceneObject.Attributes.Set( "playername", PlayerName );
			characterModell.SceneObject.Attributes.Set( "playernumberback", PlayerNumberBack );
			characterModell.SceneObject.Attributes.Set( "frontsponsor", FrontSponsor );
			characterModell.SceneObject.Attributes.Set( "badge", Badge );
			characterModell.SceneObject.Attributes.Set( "shortsnumber", ShortsNumber );

			characterModell.SceneObject.Attributes.Set( "primarycolor", PrimaryColour );
			characterModell.SceneObject.Attributes.Set( "secondarycolor", SecondaryColour );
			characterModell.SceneObject.Attributes.Set( "tertiarycolor", TertiaryColour );

			baseModell.MaterialGroup = "blue";
			//characterModell.MaterialGroup = "blue";
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


	[Rpc.Broadcast]
	public void StartCooldown()
	{

		cooldownTimeSince = 0f;
		isOnCooldown = true;
		playerStatus.Enabled = true;

	}


}
