using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sandbox.Network;
using Sandbox.Services;
using System.Threading.Tasks;
using Sandbox.Utility;

public sealed class TestGameMode : Component
{

	// K-Factor: Determines how much the rating can change per game.
	public const int K_FACTOR = 32;

	// Starting rating is zero, since that's your constraint.
	public const int STARTING_ELO = 0;

	public static TestGameMode Instance { get; private set; }

	//Player List

	[Property] public TeamSide mySide { get; set; }

	[Property] public string queueIndent = "none";


	[Property] public SoundEvent OrganCountdownSound { get; set; }
	[Property] public SoundEvent OrganStartSound { get; set; }

	[Group( "Debug" )][Property, Sync(SyncFlags.FromHost)] public Boolean DebugServer { get; set; }
	[Group( "Player List" )][Property, Sync( SyncFlags.FromHost )] public List<PuntPlayerController> PlayerList { get; set; } = new List<PuntPlayerController>();
	

	//Team Lists
	[Group( "Team Lists" )][Property, Sync(SyncFlags.FromHost)] public List<PuntPlayerController> BlueTeam { get; set; } = new List<PuntPlayerController>();
	[Group( "Team Lists" )][Property, Sync(SyncFlags.FromHost)] public List<PuntPlayerController> RedTeam { get; set; } = new List<PuntPlayerController>();

	[Group( "Team Lists" )][Property] public int BlueTeamAverageScore { get; set; }

	[Group( "Team Lists" )][Property] public int BlueTeamPendingLoss { get; set; }
	[Group( "Team Lists" )][Property] public int BlueTeamPendingWin { get; set; }

	[Group( "Team Lists" )][Property] public int RedTeamAverageScore { get; set; }
	[Group( "Team Lists" )][Property] public int RedTeamPendingLoss { get; set; }
	[Group( "Team Lists" )][Property] public int RedTeamPendingWin { get; set; }

	//GameState
	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public GameState State { get; set; }

	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public float RoundTimeLeft { get; set; }
	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public int BlueScore { get; set; }
	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public int RedScore { get; set; }

	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public TimeUntil ResetTimer { get; set; }

	[Group( "Game State" )][Property, Sync( SyncFlags.FromHost )] public TimeUntil RoundStartTimer { get; set; }//delete this later
	private int lastTimerValue = -1;

	[Group( "Game State" )][Property] public float KickoffSideDisplayDuration { get; set; }
	[Group( "Game State" )][Property] public float CountdownTimerDuration { get; set; }

	[Group( "Game State" )][Property, Sync(SyncFlags.FromHost)] public TeamSide kickingOffSide { get; set; }


	//GameSettings

	[Group( "GameSettings" )][Property] public float RoundLength { get; set; } = 180f;
	[Group( "GameSettings" )][Property] public float ResetTimerLength { get; set; } = 4f;

	//PiecePrefabs
	[Group( "Piece Prefab" )][Property] public GameObject PiecePrefab { get; set; }
	[Group( "Piece Prefab" )][Property] public GameObject BallPrefab { get; set; }

	//PieceLists
	[Group( "Piece GameObjects" )][Property, Sync(SyncFlags.FromHost)] public List<PuntPiece> BluePieceList { get; set; } = new List<PuntPiece>();
	[Group( "Piece GameObjects" )][Property, Sync(SyncFlags.FromHost)] public List<PuntPiece> RedPieceList { get; set; } = new List<PuntPiece>();
	[Group( "Piece GameObjects" )][Property, Sync(SyncFlags.FromHost)] public PuntBall Ball { get; set; }
	//Spawn Points
	[Group( "Spawn Points" )][Property] public List<GameObject> RedSpawns { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> RedSpawnsKickoff { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> BlueSpawns { get; set; } = new List<GameObject>();
	[Group( "Spawn Points" )][Property] public List<GameObject> BlueSpawnsKickoff { get; set; } = new List<GameObject>();


	//Music
	[Group( "Music" )][Property] public SoundPointComponent musicSoundPoint { get; set; }


	//GameState
	[Group( "Timescale" )][Property, Sync(SyncFlags.FromHost)] public float timescaleMult { get; set; } = 0.1f;

	




	protected override void OnAwake()
	{
		//Sandbox.Services.Stats.SetValue( "solo_q_points", (0) ); //reset stats

		QueueManager.Instance.gameJoined = true; //this is just a hack so the clients don't start searching after someone left at the end of the game


		Instance = this;
		State = GameState.Waiting;
		base.OnAwake();


		if ( DebugServer )
		{
			InitialiseGame();
			//State = GameState.KickingOff;
			//SetupGame(kickingOffSide);
		}

	}


	protected override void OnUpdate()
	{
		UpdateTimeLeft();

		int currentTimerValue = MathX.CeilToInt( RoundStartTimer );

		if ( currentTimerValue != lastTimerValue )
		{
			lastTimerValue = currentTimerValue;

			if ( currentTimerValue == 3 || currentTimerValue == 2 || currentTimerValue == 1 )
			{
				Sound.Play( OrganCountdownSound );
			}
			else if ( currentTimerValue == 0 )
			{
				Sound.Play( OrganStartSound );
				State = GameState.Playing;
				StartMusic();
			}
		}


		if ( State == GameState.Resetting & ResetTimer < 0f)
		{
			ResetBall();
			ResetTeamPieces(kickingOffSide); //not handling team who scored for now
			State = GameState.KickingOff;
			
		}

		CalculateTimescale();
		// Decrease the timer


	}

	private void StartMusic()
	{
		Log.Info( "Start Music" );
		musicSoundPoint.StartSound();
		musicSoundPoint.Repeat = true;
		musicSoundPoint.SoundOverride = true;
	}

	private void PlayCountdownBeep()
	{
		Sound.Play( "countdown_beep" ); // Replace with your actual sound event
	}

	private void PlayStartSound()
	{
		Sound.Play( "start_game_sound" ); // Replace with your actual sound event
	}



	[Rpc.Broadcast]
	private void InitialiseGame()//everyone's connected so we can start the game
	{
		//get player scores
		if (!DebugServer ) //don't do this on the debug server for now
		{
			_ = GetPlayerScores( QueueManager.Instance.SelectedQueueType );
		}
		

		SetupGame( kickingOffSide );

		if ( !IsProxy )
		{
			State = GameState.Countdown;
			RoundStartTimer = KickoffSideDisplayDuration + CountdownTimerDuration;
		}

	}

	private void CalculateTimescale()
	{

		for ( int i = 0; i < PlayerList.Count; i++ )
		{

			if ( PlayerList[i].selectedPiece != null )
			{
				Scene.TimeScale = timescaleMult;
				break;
			}

				Scene.TimeScale = 1.0f;


		}



	}

	private void UpdateTimeLeft()
	{
		//subtract time if we're playing
		//probably need to do this by timescale in the future
		if ( State == GameState.Playing )
		{
			RoundTimeLeft = MathX.Clamp( RoundTimeLeft - Time.Delta,0,RoundLength);


			//check scores again if there's not much time left 
			if( RoundTimeLeft < 5 )
			{
				_ = GetPlayerScores( QueueManager.Instance.SelectedQueueType );

			}


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
			PlayOvertimeSound();
			ResetTeamPieces( kickingOffSide );
			ResetBall();
		}
		else
		{
			FinishGame();
		}
	}

	[Rpc.Broadcast]
	private void PlayOvertimeSound()
	{
		Sound.Play( "sounds/kenney/ui/ui.navigate.deny.sound" );
		musicSoundPoint.Pitch = 1.125f;
	}

	[Property] public int winningSideStat { get; set; }
	[Property] public int losingSideStat { get; set; }
	

	public TeamSide winningTeam = TeamSide.Red;

	[Rpc.Broadcast]
	private void FinishGame()
	{


		State = GameState.Results;
		if ( BlueScore > RedScore )
		{
			Log.Info( "Game Finished. Winner: Blue Team!" );
			winningTeam = TeamSide.Blue;
		}
		else
		{
			Log.Info( "Game Finished. Winner: Red Team!" );
			winningTeam = TeamSide.Red;
		}

		//you can only do your local score so...

		_ = RefreshScoresAtFinish();

		

		Sandbox.Services.Stats.SetValue( queueIndent + "_pendingloss", (0) );//reset my pending loss here now we've finished the game. We can do the punishment later.

	}


	public async Task RefreshScoresAtFinish()
	{


		await GetPlayerScores(QueueManager.Instance.SelectedQueueType);
		SetSteamScores();

	}

	private void SetSteamScores()
	{
		if ( mySide == winningTeam )
		{
			switch ( winningTeam )
			{
				case TeamSide.None:
					break;

				case TeamSide.Red:

					Sandbox.Services.Stats.SetValue( queueIndent, (RedTeamPendingWin) );
					//set my score to be red winning team

					break;

				case TeamSide.Blue:

					Sandbox.Services.Stats.SetValue( queueIndent, (BlueTeamPendingWin) );
					//set my score to be red losing team

					break;

				default:
					break;
			}

		}
		else
		{
			switch ( winningTeam )
			{
				case TeamSide.None:
					break;

				case TeamSide.Red:

					Sandbox.Services.Stats.SetValue( queueIndent, (BlueTeamPendingLoss) );
					//set my score to be blue losing team

					break;

				case TeamSide.Blue:

					Sandbox.Services.Stats.SetValue( queueIndent, (RedTeamPendingLoss) );
					//set my score to be red losing team

					break;

				default:
					break;
			}


		}
	}

	// Default baseline adjustment
	private const int BaselineAdjustment = 1200;
	private const int KFactor = 32; // Adjust for faster or slower rating adjustments

	// Function to calculate new ELO ratings
	public static (int, int) CalculateElo( int winnerRating, int loserRating )
	{
		// Adjust for baseline
		winnerRating += BaselineAdjustment;
		loserRating += BaselineAdjustment;

		// Convert ratings to probabilities
		double winnerExpected = 1 / (1 + Math.Pow( 10, (loserRating - winnerRating) / 400.0 ));
		double loserExpected = 1 / (1 + Math.Pow( 10, (winnerRating - loserRating) / 400.0 ));

		// Calculate the new ratings
		int newWinnerRating = (int)Math.Round( winnerRating + KFactor * (1 - winnerExpected) );
		int newLoserRating = (int)Math.Round( loserRating + KFactor * (0 - loserExpected) );

		// Adjust back from baseline
		newWinnerRating -= BaselineAdjustment;
		newLoserRating -= BaselineAdjustment;

		// Return the updated ratings
		return (newWinnerRating, newLoserRating);
	}




	[Rpc.Broadcast]
	private void SetupGame( TeamSide kickoffSide )
	{

		


		ResetTeamPieces( kickoffSide );
		ResetBall();
		RoundTimeLeft = RoundLength;



	}
	[Rpc.Broadcast]
	public void StartGame()
	{



		if ( !IsProxy )//fixes the bug where it starts playing by itself, I guess the client was Rpc.Broadcasting it a second later and then the server was setting it?
		{
			State = GameState.Playing;

		}
		

		for ( int i = 0; i < BluePieceList.Count; i++ )
		{
			if( BluePieceList[i].pieceState == PieceState.Frozen )
			{
				BluePieceList[i].pieceState = PieceState.Ready;

			}

			if ( RedPieceList[i].pieceState == PieceState.Frozen )
			{
				RedPieceList[i].pieceState = PieceState.Ready;

			}

		}

	}

	[Rpc.Broadcast]
	public void StartOvertimeGame()
	{

		for ( int i = 0; i < BluePieceList.Count; i++ )
		{
			BluePieceList[i].pieceState = PieceState.Ready;
			RedPieceList[i].pieceState = PieceState.Ready;

		}

	}

	[Rpc.Broadcast]
	public void GoalScored(TeamSide goalTeam )// goal scored in this team's goal
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
				kickingOffSide = TeamSide.Blue;
				break;

			case TeamSide.Red:
				BlueScore++;
				kickingOffSide = TeamSide.Red;
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
				//account for the strikers who need to be unfrozen on kickoff - clean this up later it's dumb
				if ( kickoffSide == TeamSide.Red && i == 2 )

				{
					ResetPiece( RedPieceList[i], currentRedSpawns[i], false );
					ResetPiece( BluePieceList[i], currentBlueSpawns[i], true );
				}
				else if ( kickoffSide == TeamSide.Blue && i == 2 )
				{
					ResetPiece( RedPieceList[i], currentRedSpawns[i], true );
					ResetPiece( BluePieceList[i], currentBlueSpawns[i],false );
				}
				else
				{
					ResetPiece( RedPieceList[i], currentRedSpawns[i], true );
					ResetPiece( BluePieceList[i], currentBlueSpawns[i], true );
				}
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

				if(i == 2 && kickoffSide == TeamSide.Red ) { RedPieceList[i].Initialize( i, TeamSide.Red, false ); } else
				{
					RedPieceList[i].Initialize( i, TeamSide.Red, true );
				}

			}

			// Spawn Blue Team players
			for ( int i = 0; i < currentBlueSpawns.Count; i++ ) // do blue guys
			{
				var spawnedPiece = PiecePrefab.Clone( currentBlueSpawns[i].WorldPosition, currentBlueSpawns[i].WorldRotation );
				spawnedPiece.NetworkSpawn();
				BluePieceList.Add( spawnedPiece.Components.Get<PuntPiece>() );

				if ( i == 2 && kickoffSide == TeamSide.Blue ) { BluePieceList[i].Initialize( i, TeamSide.Blue, false ); }
				else
				{
					BluePieceList[i].Initialize( i, TeamSide.Blue, true );
				}

			}


		}

		
	}

	[Rpc.Broadcast]
	private void ResetPiece(PuntPiece piece, GameObject spawn, bool isFrozen )
	{
		piece.Network.DisableInterpolation();
		piece.WorldPosition = spawn.WorldPosition;
		piece.WorldRotation = spawn.WorldRotation;
		piece.GetComponent<Rigidbody>().Velocity = Vector3.Zero;
		piece.GetComponent<Rigidbody>().AngularVelocity = Vector3.Zero;
		piece.baseModell.LocalRotation = Rotation.From( Angles.Zero );
		piece.Network.EnableInterpolation();

		//piece.playerCooldownRenderer.SceneObject.Attributes.Set( "Progress", 0f );
		piece.cooldownTimeSince = piece.cooldownDuration;



		if ( isFrozen )
		{
			piece.pieceState = PieceState.Frozen;
		}
		else
		{
			piece.pieceState = PieceState.Ready;
		}
	}

	


	public void AddPlayer( PuntPlayerController player)
	{
		if ( !IsProxy )
		{
			PlayerList.Add( player );
			FindTeam( player );

			if(PlayerList.Count == QueueManager.Instance.maxPlayers )
			{
				InitialiseGame();

			}
		}

		//_ = GetPlayerScores( QueueManager.Instance.SelectedQueueType );
	}



	public async Task GetPlayerScores(QueueType queueType)
	{

		switch ( queueType )
		{
			case QueueType.None:
				queueIndent = "none";
				break;

			case QueueType.Solo:
				queueIndent = "solo_q_points";
				break;

			case QueueType.Duo:
				queueIndent = "duo_q_points";
				break;

			case QueueType.Custom:
				queueIndent = "custom";
				break;

			default:
				queueIndent = "none";
				break;
		}

		for ( int i = 0; i < PlayerList.Count; i++ )
		{
			var playerName = PlayerList[i].Network.Owner.Name;
			var stats = Stats.GetPlayerStats( "fptaylor.punt", PlayerList[i].Network.Owner.SteamId);
			await stats.Refresh();
			var queueStats = stats.Get( queueIndent);
			PlayerList[i].queueScore = (int)queueStats.LastValue;
		}

		//get average Blue team score
		var totalBlueScore = 0;
		for ( int i = 0; i < BlueTeam.Count; i++ )
		{
			totalBlueScore += BlueTeam[i].queueScore;
		}
		BlueTeamAverageScore = totalBlueScore / BlueTeam.Count;


		//get average Red team score
		var totalRedScore = 0;
		for ( int i = 0; i < RedTeam.Count; i++ )
		{
			totalRedScore += RedTeam[i].queueScore;
		}
		RedTeamAverageScore = totalRedScore / RedTeam.Count;

		//if red team loses
		(BlueTeamPendingWin, RedTeamPendingLoss) = CalculatePendingLoss(BlueTeamAverageScore, RedTeamAverageScore);
		//if blue team loses
		(RedTeamPendingWin, BlueTeamPendingLoss) = CalculatePendingLoss( RedTeamAverageScore, BlueTeamAverageScore);

		switch ( mySide )
		{
			case TeamSide.None:
				break;

			case TeamSide.Red:
				Sandbox.Services.Stats.SetValue( queueIndent + "_pendingloss", (RedTeamPendingLoss) );
				break;

			case TeamSide.Blue:
				Sandbox.Services.Stats.SetValue( queueIndent + "_pendingloss", (BlueTeamPendingLoss) );
				break;

			default:
				break;
		}
	}

	public (int, int) CalculatePendingLoss(int WinningScore, int LosingScore)
	{

		var pendingWinningScore = 0;
		var pendingLosingScore = 0;

		(pendingWinningScore, pendingLosingScore) = CalculateElo(WinningScore, LosingScore);

		return (pendingWinningScore, pendingLosingScore);

		//get whatever team I'm on
		//set my pending loss stat here
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
		if ( BlueTeam.Count < 2 && BlueTeam.Count <= RedTeam.Count ) //if the blue team has 1 person, and blue team is smaller than, or equal to red team
		{
			BlueTeam.Add( controller );
			controller.AssignTeam (TeamSide.Blue);


		}
		else if ( RedTeam.Count < 2 )
		{
			RedTeam.Add( controller );
			controller.AssignTeam(TeamSide.Red);
		}
		else
		{
			Log.Error( "Teams Full" );
		}
	}
}
