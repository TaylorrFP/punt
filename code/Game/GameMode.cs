using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GameMode : Component
{
	[Property] public GameObject puntPiece { get; set; }

	[Property] public int TeamBlueScore { get; set; }

	[Property] public float roundTime { get; set; } = 180;
	[Property] public RealTimeUntil roundTimeLeft { get; set; }
	[Property] public int TeamRedScore { get; set; }

	[Property] public PuntGoal[] goals { get; set; }
	[Property] public GameObject puntBall { get; set; }

	[Property] public float ballSpawnHeight { get; set; } = 35;


	[Property] public List<PuntPlayerController> playerList { get; set; } = new List<PuntPlayerController>();

	[Property] public PuntPiece[] redPieceList { get; set; } = new PuntPiece[5];
	[Property] public PuntPiece[] bluePieceList { get; set; } = new PuntPiece[5];





	//do this dynamically in pitch generator later
	Vector3[] blueSideDefend = new Vector3[5]
{
			new Vector3 (-550f,0f,0f),//keeper
			new Vector3 (-350f,160f,0f),//defenders
			new Vector3 (-350f,-160f,0f),
			new Vector3 (-160f,160f,0f),//strikers
			new Vector3 (-160f,-160f,0f),
};

	Vector3[] blueSideAttack = new Vector3[5]
	{
			new Vector3 (-550f,0f,0f),//keeper
			new Vector3 (-350f,160f,0f),//defenders
			new Vector3 (-350f,-160f,0f),
			new Vector3 (-225f,0f,0f),//strikers
			new Vector3 (90f,0f,0f)

	};

	Vector3[] redSideDefend = new Vector3[5]
	{
			new Vector3 (550f,0f,0f),//keeper
			new Vector3 (350f,160f,0f),//defenders
			new Vector3 (350f,-160f,0f),
			new Vector3 (160f,160f,0f),//strikers
			new Vector3 (160f,-160f,0f),
	};

	Vector3[] redSideAttack = new Vector3[5]
	{
			new Vector3 (550f,0f,0f),//keeper
			new Vector3 (350f,160f,0f),//defenders
			new Vector3 (350f,-160f,0f),
			new Vector3 (225f,0f,0f),//Strikers
			new Vector3 (-90f,0f,0f)
	};

	protected override void OnUpdate()
	{


	}

	public void HandleGoalScored( TeamSide side )
	{
		switch ( side )
		{
			case TeamSide.Blue:
				TeamBlueScore++;
				break;

			case TeamSide.Red:
				TeamRedScore++;
				break;

		}


		Log.Info( "goal score for " + side.ToString() );
		Log.Info( "Score = Red " + TeamRedScore + "|" + TeamBlueScore + " Blue" );

	}

	protected override void OnStart()
	{


		



		//subscribe to the ongoalscore event
		for ( int i = 0; i < goals.Length; i++ )
		{
		}


		if ( !Network.IsProxy )
		{
			ResetBall();

			//this spawns pieces if there are none, otherwise resets the pieces positions.
			//probably need to set the velocity to 0 too
			ResetPieces( TeamSide.Red );

		}

		StartRoundTimer();


	}

	private void StartRoundTimer()
	{
		roundTimeLeft = roundTime;

	}

	public void ResetPieces( TeamSide sideKickingOff )
	{





		if ( redPieceList[0] == null )
		{


			SpawnPieces( TeamSide.Red, sideKickingOff == TeamSide.Red );
			SpawnPieces( TeamSide.Blue, sideKickingOff == TeamSide.Blue );

		}
		else
		{
			for ( int i = 0; i < redPieceList.Length; i++ )
			{
				redPieceList[i].GameObject.WorldPosition = redSideDefend[i];
				bluePieceList[i].GameObject.WorldPosition = blueSideDefend[i];
			}

			if ( sideKickingOff == TeamSide.Red )
			{
				redPieceList[4].WorldPosition = redSideAttack[4];
				redPieceList[3].WorldPosition = redSideAttack[3];

			}
			else
			{
				bluePieceList[4].WorldPosition = blueSideAttack[4];
				bluePieceList[3].WorldPosition = blueSideAttack[3];
			}



		}



	}

	public void AddPlayer( PuntPlayerController player )
	{

		if ( !Network.IsProxy )
		{

			playerList.Add( player );
			if ( playerList.Count == 1 )
			{
				//if this is the first player
				player.teamSide = TeamSide.Blue;

			}
			else
			{
				//if this is the second player
				player.teamSide = TeamSide.Red;

			}
		}
	}





	public void SpawnPieces( TeamSide teamSide, bool isKickingOff )
	{

		Log.Info( "Spawning " + teamSide + ". Kicking Off: " + isKickingOff );

		//this is all really stupid, do this better

		if ( teamSide == TeamSide.Red )
		{
			for ( int i = 0; i < redSideDefend.Count(); i++ )
			{
				var spawnedPiece = puntPiece.Clone( redSideDefend[i] );
				spawnedPiece.NetworkSpawn();
				redPieceList[i] = spawnedPiece.Components.Get<PuntPiece>();
				redPieceList[i].Initialize( i, TeamSide.Red, false );

			}

			if ( isKickingOff )
			{
				redPieceList[4].WorldPosition = redSideAttack[4];
				redPieceList[3].WorldPosition = redSideAttack[3];
			}

		}

		if ( teamSide == TeamSide.Blue )
		{
			for ( int i = 0; i < blueSideDefend.Count(); i++ )
			{

				var spawnedPiece = puntPiece.Clone( blueSideDefend[i] );
				spawnedPiece.NetworkSpawn();
				bluePieceList[i] = spawnedPiece.Components.Get<PuntPiece>();
				bluePieceList[i].Initialize( i + 5, TeamSide.Blue, false );
			}

			if ( isKickingOff )
			{
				bluePieceList[4].WorldPosition = blueSideAttack[4];
				bluePieceList[3].WorldPosition = blueSideAttack[3];
			}

			Log.Info( "Spawning Blue Team" );
		}



	}

	public void ResetBall()
	{

		if ( puntBall != null )

		{
			var ball = puntBall.Clone( Vector3.Up * ballSpawnHeight );
			ball.NetworkSpawn();
		}




	}


}
