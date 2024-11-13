using Sandbox;
using System;
using System.Text.RegularExpressions;

public sealed class PuntPiece : Component
{
	[Property] public GameObject playerStatus;
	[Property] public Material playerStatusMaterial;
	[Property] public SquashAndStretch playerSquashStretch;

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
 


	protected override void OnStart()
	{
		base.OnStart();
		playerStatusMaterial = playerStatus.Components.Get<ModelRenderer>().GetMaterial( 0 );
	



	}

	public void ToggleHover()
	{
		outline.Color = Color.White;
		isHovered = !isHovered;

		if ( isHovered )
		{
			outline.Enabled = true;
			Sound.Play( "sounds/piecehover.sound" ); //weird delay on this for some reason
		}
		else
		{
			
			outline.Enabled = false;
		}
	}

	public void ToggleSelection()
	{
		isSelected = !isSelected;

		if ( isSelected )
		{
			outline.Enabled = true;
			outline.Color = new Color( 0, 1, 0, 1 );
			playerSquashStretch.StartSquash( 0.4f );
			Sound.Play( "sounds/pieceselect.sound" );

			shakeEffect.Strength = 1f;
			//Scene.TimeScale = 0.1f;

		}
		else
		{
			outline.Enabled = false;
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
