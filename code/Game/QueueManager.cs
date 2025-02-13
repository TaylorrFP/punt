using Sandbox.Network;
using System;
using System.Text.Json.Serialization;
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

	// Queue Type
	[Group( "Queue Type" )]

	[Property]
	public QueueTypeInfo QueueTypeInfo { get; set; } = new QueueTypeInfo( QueueType.None, -1 ); // might have to initialise this as custom to allow for joining via friends menu

	// Searching
	[Group( "Searching" ), Property] public int searchFrequency { get; set; } = 5000;

	[Group( "Global" ), Property] public List<LobbyInformation> FoundLobbies { get; private set; } = new List<LobbyInformation>();
	
	/// <summary>
	/// Filters the lobby list to find solo games.
	/// </summary>
	public IEnumerable<LobbyInformation> SoloLobbyInfoList => FoundLobbies.Where( x => x.Name.Equals( "Solo" ) );

	/// <summary>
	/// Filters the lobby list to find duo games.
	/// </summary>
	public IEnumerable<LobbyInformation> DuoLobbyInfoList => FoundLobbies.Where( x => x.Name.Equals( "Duo" ) );

	/// <summary>
	/// Filters the lobby lis to find custom lobbies.
	/// </summary>
	public IEnumerable<LobbyInformation> CustomLobbyInfoList => FoundLobbies.Where( x => !x.Name.Equals( "Solo" ) && !x.Name.Equals( "Duo" ) );

	[Group( "Searching" ), Property] public List<string> GlobalLobbyListNames { get; private set; } = new List<string>(); // This is just for the inspector, don't really need it	
	[Group( "Searching" ), Property] public bool isSearching { get; private set; } = false;
	[Group( "Searching" ), Property] public bool gameFound { get; set; } = false;

	[Group( "Loading" ), Property] public bool gameJoined { get; set; } = false;

	[Group( "Loading" ), Property] public int loadDelay { get; set; } = 3000;

	// Custom Game
	[Group( "CustomGame" ), Property] public string LobbyName { get; set; }

	// Global
	[Group( "Global" ), Property, JsonIgnore] public int TotalActivePlayerCount => FoundLobbies.Count;
	[Group( "Global" ), Property, JsonIgnore] public int SoloQueuePlayerCount => SoloLobbyInfoList.Count();
	[Group( "Global" ), Property, JsonIgnore] public int DuoQueuePlayerCount => DuoLobbyInfoList.Count();
	[Group( "Global" ), Property, JsonIgnore] public int CustomGamePlayerCount => CustomLobbyInfoList.Count();

	[ConVar( "punt_debug_mm" )]
	private static bool IsDebug { get; set; } = false;

	protected override void OnAwake()
	{
		this.GameObject.Flags = GameObjectFlags.DontDestroyOnLoad; //keep this around so I can see the match type
		Instance = this;
		base.OnAwake();
	}

	public async Task QueryAllGames(bool IncludeOwnLobby )
	{
		Log.Info( "Trying to query some lobbies" );

		// this queries all games and sorts them into the different queue types
		// this only does one query so it's as effecient to search all queues as it is one
		FoundLobbies = await Networking.QueryLobbies();

		GlobalLobbyListNames.Clear();

		for ( int i = 0;i < FoundLobbies.Count; i++ )
		{
			GlobalLobbyListNames.Add( FoundLobbies[i].Name );
		}

		if ( IsDebug )
		{
			Log.Info( "Total Active Players: " + TotalActivePlayerCount );
			Log.Info( "Solo Queue Active Players: " + SoloQueuePlayerCount );
			Log.Info( "Duo Queue Active Players: " + DuoQueuePlayerCount );
			Log.Info( "Custom Game Active Players: " +  CustomGamePlayerCount );

			Log.Info( "Lobby info below:" );

			foreach ( var lobby in FoundLobbies )
			{
				Log.Info( $"	Found lobby: {lobby.Name} owned by {lobby.OwnerId}" );
			}
		}

		// Discard our own lobby after we've got the player counts
		if ( Networking.IsActive & !IncludeOwnLobby )
		{
			FoundLobbies.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
		}
	}

	/// <summary>
	/// Starts queueing for a certain queue. Sets the is searching flag so the UI knows.
	/// </summary>
	/// <param name="queue"></param>
	/// <returns></returns>
	public async Task StartQueueSearch( QueueType queue )
	{
		if ( isSearching )
		{
			Log.Info( "Already searching, fuck off" );
			return;
		}

		// starts a repeating search for all games
		// the actual search happens in QueueSearch()

		isSearching = true;
		QueueTypeInfo.Type = queue;

		// we can use this var instead of specifying each queue type every time
		var selectedQueueList = new List<LobbyInformation>();
		await QueryAllGames( false );

		switch ( queue )
		{
			case QueueType.None:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			case QueueType.QuickPlay:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			case QueueType.Solo:
				selectedQueueList = SoloLobbyInfoList.ToList();
				break;
			case QueueType.Duo:
				selectedQueueList = DuoLobbyInfoList.ToList();
				break;
			case QueueType.Custom:
				Log.Info( "Error: cannot search for queuetype: " + queue.ToString() );
				break;
			default:
				break;
		}
		if ( selectedQueueList.Count == 0 )
		{
			if ( !Networking.IsActive )
			{
				//this is where it's fucking up?
				Log.Info( "No lobbies found, creating lobby..." );
				CreateMatchmakingLobby( queue );
			}
		}
		else // if there is at least 1 lobby in our queue
		{
			Log.Info( "Active Lobbies in" + queue.ToString() + "queue: " + selectedQueueList.Count );

			//put the lobby with most members first
			selectedQueueList = selectedQueueList.OrderByDescending( lobby => lobby.Members ).ToList();
			Networking.Disconnect(); //disconnect if we were hosting

			Log.Info( "Game Found! Connecting..." );

			// join the queue with the most people in it for now, later we can actually do some matchmaking logic
			if ( await Networking.TryConnectSteamId( selectedQueueList[0].OwnerId ) )
			{
				gameFound = true;
			}
			else
			{
				Log.Error( "Failed to join server wtf" );
			}
		}
	}

	/// <summary>
	/// Creates a matchmaking lobby for a specific <see cref="QueueType"/>
	/// </summary>
	/// <param name="queue"></param>
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

	/// <summary>
	/// Create a custom lobby with however many players you decide
	/// </summary>
	/// <param name="queue"></param>
	/// <param name="name"></param>
	/// <param name="maximuimPlayers"></param>
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

	/// <summary>
	/// Stop searching for a lobby - optionally, disconnect from your current lobby.
	/// </summary>
	/// <param name="DestroyLobby"></param>
	public void StopSearching( bool DestroyLobby )
	{
		if ( DestroyLobby )
		{
			Log.Info( "Destroying Lobby" );
			Networking.Disconnect();
		}

		isSearching = false;

		// start a search with no queuetype, and try join off
		QueueTypeInfo.Type = QueueType.None;
	}

	/// <summary>
	/// Called when we join a server.
	/// </summary>
	/// <param name="channel"></param>
	public async void OnActive( Connection channel )
	{
		if ( Networking.IsHost & gameJoined != true & Connection.All.Count >1 ) //if the joining player isn't a host, and we haven't started the game yet
		{
			Log.Info( "Player Joined: " + channel.DisplayName );

			//if I'm the owner I need to stop searching?
			StopSearching( false );
		
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
		// needs the host bit because otherwise the person connecting calls this when they join?
		// this is only called on the host
		// when there are two people left in the lobby - does it pass over to the host before or after this is called?
		// this is triggering when a player leaves a game properly

		if ( QueueTypeInfo.Type == QueueType.Custom )
			return;

		if ( gameJoined == true & Connection.All.Count <= QueueTypeInfo.MaxPlayers ) // if a game is in progress and someone leaves
		{
			Networking.Disconnect();
			Scene.LoadFromFile( "scenes/mainmenu.scene" );
			QueueManager.Instance.StopSearching( true );
		}

		if ( !channel.IsHost & gameJoined != true & Connection.All.Count <= 2) //if we haven't started the game, someone disconnects and it's just us left (2 because this is called just before the player disconnects)
		{
			Log.Info( "Not enough players after disconnect, starting search" );
			await StartQueueSearch( QueueTypeInfo.Type );
		}

		// we're automatically searching again if we click cancel - I think because of this logic.
		// we need to check if it's just us, if it is we need to create a lobby again
	}
}
