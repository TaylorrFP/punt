using Sandbox.Network;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
namespace Sandbox;


/*
Queue Manager

The queue manager is responsible for sorting out the different queues.
Static instance so exists always.

Queuetpes:
Quickplay
1v1
2v2
Custom

Quickplay:
Searches for any available RANKED game, 1v1 or 2v2

1v1:
Ranked 1v1 queue

2v2:
Ranked 2v2 queue



*/




[Title( "QueueManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class QueueManager : Component, Component.INetworkListener
{

	public static QueueManager Instance { get; private set; }


	//Queue Type
	[Group( "Queue Type" )]

	[Property]
	public QueueTypeInfo QueueTypeInfo { get; set; } = new QueueTypeInfo( QueueType.None, -1 );//might have to initialise this as custom to allow for joining via friends menu


	//Searching
	[Group( "Searching" )] [Property] public int searchFrequency { get; set; } = 5000;
	public List<LobbyInformation> LobbyInfoList { get; private set; } = new List<LobbyInformation>();
	[Group( "Searching" )][Property] public List<string> lobbyListNames { get; private set; } = new List<string>(); //This is just for the inspector, don't really need it

	public LobbyInformation myLobby { get; private set; }

	

	[Group("Searching")][Property] public bool isSearching { get; private set; } = false;

	[Group("Searching")][Property] public bool gameFound { get; set; } = false;


	[Group( "Loading" )][Property] public bool gameJoined { get; set; } = false;

	[Group( "Loading" )][Property] public int loadDelay { get; set; } = 3000;

	private CancellationTokenSource searchTokenSource;


	[Group( "CustomGame" )][Property] public string lobbyName { get; set; }





	protected override void OnAwake()
	{

		//this.Gameobject.Flags = GameObjectFlags.DontDestroyOnLoad;

		this.GameObject.Flags = GameObjectFlags.DontDestroyOnLoad; //keep this around so I can see the match type
		Instance = this;
		base.OnAwake();
	}

	/// <summary>
	/// Creates a new lobby with the specified queue type.
	/// </summary>


	/// <summary>
	/// Starts searching for a game repeatedly until canceled.
	/// </summary>
	public async Task StartSearching( QueueType queue )
	{


		QueueTypeInfo.Type = queue;
		searchTokenSource = new CancellationTokenSource();
		isSearching = true;
		Log.Info( $"Started searching for games in queue: {queue.ToString()}" );

		try
		{
			while ( !searchTokenSource.Token.IsCancellationRequested )
			{
				await SearchGame(queue);
				await Task.Delay( searchFrequency, searchTokenSource.Token );
			}
		}
		catch ( TaskCanceledException )
		{
			Log.Info( "Search was cancelled successfully." );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error during search: {ex.Message}" );
		}
		finally
		{
			//isSearching = false;
		}
	}

	public async Task SearchGame( QueueType queue )
	{
		Log.Info( "Searching Games..." );
		LobbyInfoList = await Networking.QueryLobbies();
		lobbyListNames.Clear();

	

		if ( Networking.IsActive )//if we've already made a lobby, remove our lobby from the list
		{
			//make a reference to mylobby
			myLobby = LobbyInfoList.FirstOrDefault( lobby => lobby.OwnerId == Connection.Host.SteamId );
			// Filter and remove the player's own lobby
			LobbyInfoList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
		}

		//remove all the lobbys that don't match the queue we're searching for
		for ( int i = 0; i < LobbyInfoList.Count; i++ )
		{
			if ( LobbyInfoList[i].Name != queue.ToString() )
			{
				LobbyInfoList.Remove( LobbyInfoList[i] );
			}
		}


		if (LobbyInfoList.Count == 0 )
		{
			Log.Info( "No Lobbies found" );
			if ( !Networking.IsActive )//only create a lobby if we haven't already created one
			{
				Log.Info( "Creating Lobby" );
				CreateMatchmakingLobby( queue );
			}

		}
		else //if there is at least 1 relevant lobby
		{
			// Collect lobby names so I can read them in the editor
			foreach ( var lobby in LobbyInfoList )
			{
				lobbyListNames.Add( lobby.Name + " " + lobby.Members + "/" + lobby.MaxMembers + " " + lobby.OwnerId );
			}

			Log.Info( "Active Lobbies in queue: " + LobbyInfoList.Count );

			//put the lobby with most members first
			LobbyInfoList = LobbyInfoList.OrderByDescending( lobby => lobby.Members ).ToList();
			Networking.Disconnect();//disconnect if we were hosting
			Log.Info( "Game Found! Connecting..." );
			Networking.Connect( LobbyInfoList[0].LobbyId ); //connect to the one with the most members
			gameFound = true;




		}
	}

	public void CreateMatchmakingLobby( QueueType queue )
	{
		Networking.Disconnect();
		QueueTypeInfo.Type = queue;

		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = QueueTypeInfo.MaxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = queue.ToString(),
			Hidden = true


		} );
	}

	public void CreateCustomLobby( QueueType queue, string name, int maximuimPlayers)
	{
		Log.Info( "Creating Custom Lobby" );
		lobbyName = name;
		Networking.Disconnect();
		QueueTypeInfo.Type = queue;
		QueueTypeInfo.MaxPlayers = maximuimPlayers;
		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = maximuimPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = name,
			Hidden = false,


		} );

		Log.Info( "Lobby Created" );

	}

	/// <summary>
	/// Cancels the current search process.
	/// </summary>
	/// 



	public void StopSearching(bool DestroyLobby)
	{

		

		if ( DestroyLobby )
		{

			Log.Info( "Destroying Lobby" );
			Networking.Disconnect();
			lobbyListNames.Clear();
			LobbyInfoList.Clear();
			QueueTypeInfo.Type = QueueType.None;
		}

		isSearching = false;
		searchTokenSource?.Cancel();

		
	}

	/// <summary>
	/// Searches for lobbies and tries to join if a match is found.
	/// </summary>
	

	

	public async void OnActive( Connection channel )
	{
		
		if ( Networking.IsHost & gameJoined != true & Connection.All.Count>1) //if the joining player isn't a host, and we haven't started the game yet
		{
			Log.Info( "Player Joined: " + channel.DisplayName );


			//if I'm the owner I need to stop searching?
			StopSearching( false);

		
			if ( Connection.All.Count == QueueTypeInfo.MaxPlayers & QueueTypeInfo.Type != QueueType.Custom )
			{

				//start game
				gameFound = true;
				await Task.Delay( 3000 ); // Delays for a little bit to allow the incoming player to load?
				Log.Info( "Loading Scene" );
				Scene.LoadFromFile( "scenes/networktestscene.scene" );
				gameJoined = true; //do this with enums later
				Log.Info( "game full, starting game" );

			}


		}
	}
	public async void OnDisconnected( Connection channel )
	{
		//needs the host bit because otherwise the person connecting calls this when they join?

		//this is only called on the host
		//when there are two people left in the lobby - does it pass over to the host before or after this is called?

		//this is triggering when a player leaves a game properly

		if( QueueTypeInfo.Type == QueueType.Custom )
		{
			return;
		}


		if(gameJoined == true & Connection.All.Count <= QueueTypeInfo.MaxPlayers )//if a game is in progress and someone leaves
		{

			Networking.Disconnect();
			Scene.LoadFromFile( "scenes/mainmenu.scene" );
			QueueManager.Instance.StopSearching( true );

		}


		if ( !channel.IsHost & gameJoined != true & Connection.All.Count <= 2)//if we haven't started the game, someone disconnects and it's just us left (2 because this is called just before the player disconnects)
		{
			Log.Info( "Not enough players after disconnect, starting search" );
			await StartSearching( QueueTypeInfo.Type);
		}



		//we're automatically searching again if we click cancel - I think because of this logic.



		//we need to check if it's just us, if it is we need to create a lobby again

	}
	protected override void OnUpdate() { }
}
