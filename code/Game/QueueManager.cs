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
	public List<LobbyInformation> LobbyInfoList { get; private set; } = new List<LobbyInformation>(); //depricate this

	[Group( "Global" )][Property] public List<LobbyInformation> GlobalLobbyInfoList { get; private set; } = new List<LobbyInformation>();
	[Group( "Global" )][Property] public List<LobbyInformation> SoloLobbyInfoList { get; private set; } = new List<LobbyInformation>();
			
	[Group( "Global" )][Property] public List<LobbyInformation> DuoLobbyInfoList { get; private set; } = new List<LobbyInformation>();
			 
	[Group( "Global" )][Property] public List<LobbyInformation> CustomLobbyInfoList { get; private set; } = new List<LobbyInformation>();

	[Group( "Searching" )][Property] public List<string> GlobalLobbyListNames { get; private set; } = new List<string>(); //This is just for the inspector, don't really need it

	public LobbyInformation myLobby { get; private set; }//do we really need this?


	

	[Group("Searching")][Property] public bool isSearching { get; private set; } = false;

	[Group("Searching")][Property] public bool gameFound { get; set; } = false;


	[Group( "Loading" )][Property] public bool gameJoined { get; set; } = false;

	[Group( "Loading" )][Property] public int loadDelay { get; set; } = 3000;

	private CancellationTokenSource searchTokenSource;

	//Custom Game
	[Group( "CustomGame" )][Property] public string LobbyName { get; set; }

	//Global

	[Group( "Global" )][Property] public int TotalActivePlayerCount { get; set; }
	[Group( "Global" )][Property] public int SoloQueuePlayerCount { get; set; }
	[Group( "Global" )][Property] public int DuoQueuePlayerCount { get; set; }
	[Group( "Global" )][Property] public int CustomGamePlayerCount { get; set; }



	protected override void OnAwake()
	{
		this.GameObject.Flags = GameObjectFlags.DontDestroyOnLoad; //keep this around so I can see the match type
		Instance = this;
		base.OnAwake();

		//do an initial search of all players and queue types
		_ = QuereyAllGames(true,true);
	}





	public async Task QuereyAllGames(bool IncludeOwnLobby,bool LogPlayerCounts)
	{
		
		//this queries all games and sorts them into the different queue types
		//this only does one querey so it's as effecient to search all queues as it is one
		GlobalLobbyInfoList = await Networking.QueryLobbies();
		GlobalLobbyListNames.Clear();
		SoloLobbyInfoList.Clear();
		DuoLobbyInfoList.Clear();
		CustomLobbyInfoList.Clear();

		for (int i = 0;i < GlobalLobbyInfoList.Count; i++ )
		{
			GlobalLobbyListNames.Add( GlobalLobbyInfoList[i].Name );

			if ( GlobalLobbyInfoList[i].Name == "Solo" )
			{
				SoloLobbyInfoList.Add( GlobalLobbyInfoList[i] );
			}else if ( GlobalLobbyInfoList[i].Name == "Duo" )
			{
				DuoLobbyInfoList.Add( GlobalLobbyInfoList[i] );
			}
			else
			{
				CustomLobbyInfoList.Add( GlobalLobbyInfoList[i] );
			}

		}

		//set all the player counts while we're here
		TotalActivePlayerCount = GlobalLobbyInfoList.Count;
		SoloQueuePlayerCount = SoloLobbyInfoList.Count;
		DuoQueuePlayerCount = DuoLobbyInfoList.Count;
		CustomGamePlayerCount = CustomLobbyInfoList.Count;


		if ( LogPlayerCounts )
		{
			Log.Info( "Total Active Players: " + TotalActivePlayerCount );
			Log.Info( "Solo Queue Active Players: " + SoloQueuePlayerCount );
			Log.Info( "Duo Queue Active Players: " + DuoQueuePlayerCount );
			Log.Info( "Custom Game Active Players: " +  CustomGamePlayerCount );
		}


		//Discard our own lobby after we've got the player counts
		if ( Networking.IsActive & !IncludeOwnLobby )
		{
			GlobalLobbyInfoList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
			SoloLobbyInfoList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
			DuoLobbyInfoList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
			CustomLobbyInfoList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
		}



	}


	public async Task StartQueueSearch( QueueType queue, bool tryJoin )
	{

		//cancel current search if we have one
		if ( searchTokenSource != null )
		{
			searchTokenSource.Cancel();
			searchTokenSource = new CancellationTokenSource();
		}
		else
		{
			searchTokenSource = new CancellationTokenSource();
		}

		//starts a repeating search for all games
		//the actual search happens in QueueSearch()
		if ( tryJoin )
		{
			isSearching = true;
			QueueTypeInfo.Type = queue;
		
		}

		
		Log.Info( $"Searching for games in queue: {queue.ToString()}" );

		try
		{
			while ( !searchTokenSource.Token.IsCancellationRequested )
			{
				await QueueSearch( queue, tryJoin);
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
	}

	public async Task QueueSearch(QueueType queue, bool tryJoin )
	{

		Log.Info( "Quereying Lobbies" );
		//we can use this var instead of specifying each queue type every time
		var selectedQueueList = new List<LobbyInformation>();
		await QuereyAllGames(false, false );

		if ( !tryJoin || searchTokenSource.Token.IsCancellationRequested ) return;

		switch ( queue )
		{
			case QueueType.None:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			case QueueType.QuickPlay:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			case QueueType.Solo:
				selectedQueueList = SoloLobbyInfoList;
				break;
			case QueueType.Duo:
				selectedQueueList = DuoLobbyInfoList;
				break;
			case QueueType.Custom:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			default:
				break;
		}
		if ( selectedQueueList.Count == 0 )
		{
			if ( !Networking.IsActive && !searchTokenSource.Token.IsCancellationRequested )
			{
				//this is where it's fucking up?
				Log.Info( "No lobbies found, creating lobby..." );
				CreateMatchmakingLobby( queue );
			}
		}
		else //if there is at least 1 lobby in our queue
		{
			Log.Info( "Active Lobbies in" + queue.ToString() + "queue: " + selectedQueueList.Count );

			//put the lobby with most members first
			selectedQueueList = selectedQueueList.OrderByDescending( lobby => lobby.Members ).ToList();
			Networking.Disconnect();//disconnect if we were hosting

			

			Log.Info( "Game Found! Connecting..." );
			//join the queue with the most people in it for now, later we can actually do some matchmaking logic
			Networking.Connect( selectedQueueList[0].LobbyId );
			gameFound = true;
		}
	}

	public void CreateMatchmakingLobby( QueueType queue )
	{

		Networking.Disconnect();
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
		LobbyName = name;
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



	public void StopSearching(bool DestroyLobby)
	{
		if ( DestroyLobby )
		{
			Log.Info( "Destroying Lobby" );
			Networking.Disconnect();

		}

		searchTokenSource.Cancel();



		isSearching = false;

		//start a search with no queuetype, and tryjoin off
		QueueTypeInfo.Type = QueueType.None;
		_ = StartQueueSearch( QueueType.None, false );
	}
	

	

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
			await StartQueueSearch( QueueTypeInfo.Type,true);
		}



		//we're automatically searching again if we click cancel - I think because of this logic.



		//we need to check if it's just us, if it is we need to create a lobby again

	}
	protected override void OnUpdate() { }
}
