using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public sealed class TestGameMode : Component
{
	public static TestGameMode Instance { get; private set; }

	//Player List

	[Group( "Debug" )][Property, HostSync] public Boolean DebugServer { get; set; }
	[Group( "Player List" )][Property, HostSync] public List<PuntPlayerController> PlayerList { get; set; } = new List<PuntPlayerController>();

	//Team Lists
	[Group( "Team Lists" )][Property, HostSync] public List<PuntPlayerController> BlueTeam { get; set; } = new List<PuntPlayerController>();
	[Group( "Team Lists" )][Property, HostSync] public List<PuntPlayerController> RedTeam { get; set; } = new List<PuntPlayerController>();

	//GameState
	[Group( "Game State" )][Property, HostSync] public GameState State { get; set; }

	[Group( "Game State" )][Property, HostSync] public float RoundTimeLeft { get; set; }
	[Group( "Game State" )][Property, HostSync] public int BlueScore { get; set; }
	[Group( "Game State" )][Property, HostSync] public int RedScore { get; set; }

	[Group( "Game State" )][Property, HostSync] public TimeUntil ResetTimer { get; set; }


	//GameSettings

	[Group( "GameSettings" )][Property] public float RoundLength { get; set; } = 180f;
	[Group( "GameSettings" )][Property] public float ResetTimerLength { get; set; } = 4f;

	//PiecePrefabs
	[Group( "Piece Prefab" )][Property] public GameObject PiecePrefab { get; set; }
	[Group( "Piece Prefab" )][Property] public GameObject BallPrefab { get; set; }

	//PieceLists
	[Group( "Piece GameObjects" )][Property, HostSync] public List<PuntPiece> BluePieceList { get; set; } = new List<PuntPiece>();
	[Group( "Piece GameObjects" )][Property, HostSync] public List<PuntPiece> RedPieceList { get; set; } = new List<PuntPiece>();
	[Group( "Piece GameObjects" )][Property, HostSync] public PuntBall Ball { get; set; }
	//Spawn Points
	[Group( "Spawn Points" )][Property] public List<GameObject> RedSpawns { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> RedSpawnsKickoff { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> BlueSpawns { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> BlueSpawnsKickoff { get; set; } = new List<GameObject>();



	//Spawn Points
	[Group( "Music" )][Property] public SoundPointComponent musicSoundPoint { get; set; }

	[HostSync] public TimeSince TimeSinceCountdown { get; set; }



	protected override void OnAwake()
	{
		Instance = this;
		State = GameState.Waiting;
		base.OnAwake();


		if ( DebugServer )
		{



			State = GameState.KickingOff;
			StartGame(TeamSide.Red);
		}
	}
	protected override void OnUpdate()
	{
		UpdateTimeLeft();

		//if we're doing a countdown and the timer is over 3 then start playing.
		if ( State == GameState.Countdown & TimeSinceCountdown >3.0f)
		{
			State = GameState.Playing;
			StartGame( TeamSide.Blue );
		}

		if( State == GameState.Resetting & ResetTimer < 0f)
		{
			ResetBall();
			ResetTeamPieces(TeamSide.Blue); //not handling team who scored for now
			State = GameState.Playing;

		}

	}



	private void UpdateTimeLeft()
	{
		//subtract time if we're playing
		//probably need to do this by timescale in the future
		if ( State == GameState.Playing )
		{
			RoundTimeLeft = MathX.Clamp( RoundTimeLeft - Time.Delta,0,RoundLength);
			if ( RoundTimeLeft == 0 )
			{
				TryFinishRound();
			}
		}
	}

	private void TryFinishRound()
	{
		if ( RedScore == BlueScore )
		{
			//if it's a draw
			State = GameState.Overtime;
		}
		else
		{
			FinishGame();
		}
	}

	private void FinishGame()
	{
		State = GameState.Results;
		if ( BlueScore > RedScore )
		{
			Log.Info( "Game Finished. Winner: Blue Team!" );

		}
		else
		{
			Log.Info( "Game Finished. Winner: Red Team!" );
		}

	}

	private void StartGame( TeamSide kickoffSide )
	{


		musicSoundPoint.StartSound();
		musicSoundPoint.Repeat = true;
		ResetTeamPieces( kickoffSide );
		ResetBall();

		RoundTimeLeft = RoundLength;

		

		// Start the game timer

		// Set the score to 0-0

		// Additional setup if needed
	}

	[Broadcast]
	public void GoalScored(TeamSide goalTeam )
	{
		Sound.Play( "sounds/ball/whistle.sound" );
		if ( IsProxy )
		{
			return;
		}


		if ( State != GameState.Playing && State != GameState.Overtime )
		{
			return;
		}

		switch ( goalTeam )
		{
			case TeamSide.Blue:
				RedScore++;
				break;

			case TeamSide.Red:
				BlueScore++;
				break;

		}

		switch( State )
		{
			case GameState.Playing:
				{
					State = GameState.Resetting;
					ResetTimer = ResetTimerLength;
					return;
				}
			case GameState.Overtime:
				{
					Log.Info( "Finish game" );
					FinishGame();
					return;
				}

		}





	}

	private void ResetBall()
	{
		if ( !IsProxy )
		{

			if ( Ball != null )

			{
				Ball.Network.DisableInterpolation();
				Ball.WorldPosition = Vector3.Up * 30f;
				Ball.GetComponent<Rigidbody>().Velocity = Vector3.Zero;
				Ball.GetComponent<Rigidbody>().AngularVelocity = Vector3.Zero;
				Ball.Network.EnableInterpolation();

			}
			else
			{
	
				var ball = BallPrefab.Clone( Vector3.Up * 30f );
				ball.NetworkSpawn();
				Ball = ball.GetComponent<PuntBall>();


			}


		}
	}

	private void ResetTeamPieces( TeamSide kickoffSide )
	{
		if ( IsProxy )
		{
			return;
		}

		// Select spawn points based on kickoffSide
		List<GameObject> currentRedSpawns = (kickoffSide == TeamSide.Red) ? RedSpawnsKickoff : RedSpawns;
		List<GameObject> currentBlueSpawns = (kickoffSide == TeamSide.Blue) ? BlueSpawnsKickoff : BlueSpawns;


		if ( RedPieceList.Count>0 )//if the pieces are already spawned
		{
			for ( int i = 0; i < currentRedSpawns.Count; i++ )//Reset positions/rotations/velocities
			{
				ResetPiece( RedPieceList[i], currentRedSpawns[i] );
				ResetPiece( BluePieceList[i], currentBlueSpawns[i] );


			}



		}
		else
		{
			// Spawn Red Team players
			for ( int i = 0; i < currentRedSpawns.Count; i++ ) // do red guys
			{
				var spawnedPiece = PiecePrefab.Clone( currentRedSpawns[i].WorldPosition, currentRedSpawns[i].WorldRotation );
				spawnedPiece.NetworkSpawn();
				RedPieceList.Add( spawnedPiece.Components.Get<PuntPiece>() );
				RedPieceList[i].Initialize( i, TeamSide.Red );

				Log.Info( "Init red piece" );

			}

			// Spawn Blue Team players
			for ( int i = 0; i < currentBlueSpawns.Count; i++ ) // do blue guys
			{
				var spawnedPiece = PiecePrefab.Clone( currentBlueSpawns[i].WorldPosition, currentBlueSpawns[i].WorldRotation );
				spawnedPiece.NetworkSpawn();
				BluePieceList.Add( spawnedPiece.Components.Get<PuntPiece>() );
				BluePieceList[i].Initialize( i, TeamSide.Blue );
				Log.Info( "Init blue piece" );

			}


		}

		
	}

	private void ResetPiece(PuntPiece piece, GameObject spawn )
	{
		piece.Network.DisableInterpolation();
		piece.WorldPosition = spawn.WorldPosition;
		piece.WorldRotation = spawn.WorldRotation;
		piece.GetComponent<Rigidbody>().Velocity = Vector3.Zero;
		piece.GetComponent<Rigidbody>().AngularVelocity = Vector3.Zero;
		piece.puntPlayerModel.LocalRotation = Rotation.From( Angles.Zero );
		piece.Network.EnableInterpolation();
	}

	[Broadcast]
	public void EvaluateReadyState( PuntPlayerController player, bool ready )
	{
		if ( State == GameState.Waiting || State == GameState.Countdown )//only do this is we're waiting or in cooldown
		{
			State = GameState.Waiting;//assume we're waiting, if we're not we set it later
			if ( IsProxy )
			{
				return;
			}

			if ( PlayerList.Count <= 1 )
			{
				Log.Info( "Not enough players to evaluate readiness." );
				return;
			}

			// If the incoming request is not ready, no need to evaluate others.
			if ( !ready )
			{
				Log.Info( $"Player {player.Network.Owner.DisplayName} is not ready." );
				return;
			}

			// Iterate through the player list to see if anyone isn't ready, don't bother with the incoming one as we've processed that already.
			foreach ( var p in PlayerList )
			{
				if ( p != player && !p.isReady )
				{
					// If a player is not ready, log and exit early.
					Log.Info( "Not all players are ready." );
					return;
				}
			}

			// All players are ready
			Log.Info( "All players are ready!" );

			State = GameState.Countdown;
			TimeSinceCountdown = 0f;

		}
	}


	public void AddPlayer( PuntPlayerController player)
	{
		if ( !IsProxy )
		{
			PlayerList.Add( player );
			FindTeam( player );
		}
	}

	public void RemovePlayer( Connection channel )
	{
		//Remove the disconnected player
		PlayerList.RemoveAll( p => p.GameObject.Network.Owner == channel );

		//Clear the teams
		RedTeam.Clear();
		BlueTeam.Clear();

		//Reshufle them
		for ( int i = 0; i < PlayerList.Count; i++ )
		{
			FindTeam( PlayerList[i]);
		}

	}

	private void FindTeam( PuntPlayerController controller )
	{
		//Trys to even out the teams
		if ( BlueTeam.Count < 2 && BlueTeam.Count <= RedTeam.Count )
		{
			BlueTeam.Add( controller );
			controller.teamSide = TeamSide.Blue;
		}
		else if ( RedTeam.Count < 2 )
		{
			RedTeam.Add( controller );
			controller.teamSide = TeamSide.Red;
		}
		else
		{
			Log.Error( "Teams Full" );
		}
	}
}
