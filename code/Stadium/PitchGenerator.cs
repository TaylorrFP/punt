using Sandbox;
using Sandbox.Network;
using System;
using System.Drawing;
using System.Numerics;
using static Sandbox.VertexLayout;

public sealed class PitchGenerator : Component
{

	//Have a list of gameobjects, so you can delete them and remake them if any variables change
	[Property] public GameMode gameMode { get; set; }
	[Property] public float pitchX { get; set; } = 10f;
	[Property] public float pitchY { get; set; } = 10f;
	[Property] public float height { get; set; } = 5f;
	[Property] public float thickness { get; set; } = 0.5f;
	[Property] public float cornerRadius { get; set; } = 2f;
	[Property]  public int cornerResolution { get; set; } = 8;


	[Property] public float goalWidth { get; set; } = 150;
	[Property] public float goalHeight { get; set; } = 270;

	[Property] public Surface wallSurface { get; set; }

	[Property] public Surface goalBackSurface { get; set; }

	[Property] public float cornerAngle { get; set; }
	[Property] public float cornerSegmentWidth { get; set; }
	protected override void OnUpdate()
	{

		if ( true )
		{

		}

	}

	protected override void OnStart()
	{
		base.OnStart();

		GeneratePitch();

	}


	private void GeneratePitch()
	{
		// Adjust dimensions to account for corner radius
		float adjustedLength = pitchY;
		float adjustedWidth = pitchX;

		// calculate corner angle and segment width
		cornerAngle = 90f / (cornerResolution - 1);
		cornerSegmentWidth = MathF.Sqrt( 2 * cornerRadius * cornerRadius - 2 * cornerRadius * cornerRadius * MathF.Cos( MathX.DegreeToRadian( cornerAngle ) ) );

		// Create the walls
		CreateWall( new Vector3( 0, adjustedLength/2 + (thickness / 2), height*0.5f ), new Vector3( pitchX - cornerRadius * 2 - cornerSegmentWidth, thickness, height ) ); // Top Wall
		CreateWall( new Vector3( 0, -adjustedLength / 2 - (thickness / 2), height * 0.5f ), new Vector3( pitchX - cornerRadius * 2 - cornerSegmentWidth, thickness, height ) ); // Bottom Wall
		CreateWall( Vector3.Up * height, new Vector3( pitchX, pitchY, thickness ) );

		// Create the goal ends

		CreateGoalEnds( new Vector3(adjustedWidth / 2 + (thickness / 2), 0, height * 0.5f ), new Vector3( thickness, goalWidth, goalHeight), new Vector3 ( thickness, pitchY - cornerRadius * 2 - cornerSegmentWidth, height),1f);
		CreateGoalEnds( new Vector3(-adjustedWidth / 2 + (-thickness / 2), 0, height * 0.5f ), new Vector3( thickness, goalWidth, goalHeight ), new Vector3( thickness, pitchY - cornerRadius * 2 - cornerSegmentWidth, height),-1f );




		//// Create the rounded corners

		CreateRoundedCorner( new Vector3( (pitchX / 2) - cornerRadius, (pitchY / 2) - cornerRadius, height * 0.5f ),0f ); // Top Right
		CreateRoundedCorner( new Vector3( (-pitchX / 2) + cornerRadius, (pitchY / 2) - cornerRadius, height * 0.5f ),90f ); //Top Left
		CreateRoundedCorner( new Vector3( (pitchX / 2) - cornerRadius, (-pitchY / 2) + cornerRadius, height * 0.5f ), -90f ); //Bottom Right
		CreateRoundedCorner( new Vector3( (-pitchX / 2) + cornerRadius, (-pitchY / 2) + cornerRadius, height * 0.5f ), 180f ); //Bottom Left







	}

	void CreateWall( Vector3 position, Vector3 size )
	{
		GameObject wall = new GameObject( true, "Wall" );
		wall.WorldPosition = position;

		BoxCollider collider = wall.Components.Create<BoxCollider>();
		collider.Scale = size; // Set the size of the BoxCollider directly
		collider.Surface = wallSurface;

		wall.Parent = this.GameObject;
		wall.Tags.Add( "Wall" );
	}

	void CreateRoundedCorner( Vector3 cornerPosition, float angleOffset)
	{



		for ( int i = 0; i < cornerResolution; i++ )
		{



			GameObject cornerPiece = new GameObject( true, "CornerPiece" );
			cornerPiece.WorldPosition = cornerPosition;
			BoxCollider collider = cornerPiece.Components.Create<BoxCollider>();
			collider.Scale = new Vector3( thickness, cornerSegmentWidth, height ); //y is the thickness of the thing
			collider.Surface = wallSurface;





			cornerPiece.WorldRotation = Rotation.From( cornerPiece.WorldRotation.Angles() + new Angles( 0, cornerAngle * (i), 0 ) + new Angles(0,angleOffset,0) );
			cornerPiece.WorldPosition += cornerPiece.WorldRotation.Forward * (cornerRadius + thickness/2);

			cornerPiece.Parent = this.GameObject;
			cornerPiece.Tags.Add( "Wall" );
		}

	}

	void CreateGoalEnds( Vector3 position, Vector3 goalSize, Vector3 wallSize,float side)
	{

		//spawn goals from a prefab

		//this is all dogshit, do this better

		//GameObject goalvolume = new GameObject( true, "Goal Volume" );
		//goalvolume.WorldPosition = new Vector3( position.x + 30*side, position.y, goalHeight * 0.5f );
		//BoxCollider goalcollider = goalvolume.Components.Create<BoxCollider>();
		//goalcollider.IsTrigger = true;
		//goalcollider.Scale = goalSize;
		//goalvolume.Parent = this.GameObject;
		//goalcollider.Surface = wallSurface;

		//GameObject goalBack = new GameObject( true, "Goal Wall a" );
		//goalBack.WorldPosition = new Vector3( position.x + thickness * side, position.y, goalHeight * 0.5f );
		//BoxCollider goalBackcollider = goalBack.Components.Create<BoxCollider>();
		//goalBackcollider.Scale = goalSize;
		//goalBack.Parent = this.GameObject;
		//goalBackcollider.Surface = goalBackSurface;
		//goalBack.Tags.Add( "Net" );


		GameObject goalWall = new GameObject( true, "Goal Wall b" );
		goalWall.WorldPosition = new Vector3( position.x, position.y, position.z + (goalHeight * 0.5f) );
		BoxCollider goalwallcollider = goalWall.Components.Create<BoxCollider>();
		goalwallcollider.Scale = new Vector3( wallSize.x, wallSize.y, wallSize.z - goalHeight );
		goalWall.Parent = this.GameObject;
		goalwallcollider.Surface = wallSurface;
		goalWall.Tags.Add( "Wall" );


		GameObject goalWallR = new GameObject( true, "Goal Wall c" );
		goalWallR.WorldPosition = new Vector3( position.x, (wallSize.y * 0.5f + goalSize.y * 0.5f) * 0.5f, goalHeight * 0.5f );
		BoxCollider goalwallcolliderR = goalWallR.Components.Create<BoxCollider>();
		goalwallcolliderR.Scale = new Vector3( thickness, wallSize.y * 0.5f - goalSize.y * 0.5f, goalHeight );
		goalWallR.Parent = this.GameObject;
		goalwallcolliderR.Surface = goalBackSurface;
		goalWallR.Tags.Add( "Wall" );


		GameObject goalWallL = new GameObject( true, "Goal Wall d" );
		goalWallL.WorldPosition = new Vector3( position.x, (wallSize.y * 0.5f + goalSize.y * 0.5f) * -0.5f, goalHeight * 0.5f );
		BoxCollider goalwallcolliderL = goalWallL.Components.Create<BoxCollider>();
		goalwallcolliderL.Scale = new Vector3( thickness, wallSize.y * 0.5f - goalSize.y * 0.5f, goalHeight );
		goalWallL.Parent = this.GameObject;
		goalwallcolliderL.Surface = goalBackSurface;
		goalwallcolliderL.Tags.Add( "Wall" );
	}
}


//protected override void OnStart()
//{
//	base.OnStart();
//	GameNetworkSystem.CreateLobby();

//	Log.Info( "NetworkManager.OnStart IsProxy = " + Network.IsProxy );
//}

//void OnActive( Connection connection )
//{

//	var player = PlayerPrefab.Clone( Vector3.Zero );

//	// Spawn it on the network, assign connection as the owner
//	player.NetworkSpawn( connection );
//	gameMode.AddPlayer( player.Components.Get<PlayerController>() );

//}
