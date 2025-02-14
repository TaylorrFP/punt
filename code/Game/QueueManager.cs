using Sandbox.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sandbox;

[Title( "Queue Manager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class QueueManager : Component, Component.INetworkListener
{
	/// <summary>
	/// Singleton
	/// </summary>
	public static QueueManager Instance { get; private set; }

	/// <summary>
	/// The current queue type.
	/// </summary>
	[Group( "Queue Type" ), Property]
	public QueueType SelectedQueueType { get; set; }

	/// <summary>
	/// The current player count for the current queue type.
	/// </summary>
	public int MaxPlayers => SelectedQueueType.GetPlayers();

	/// <summary>
	/// How frequently should we be searching for lobbies (While queueing)
	/// </summary>
	[Group( "Searching" ), Property] 
	public int searchFrequency { get; set; } = 10;

	/// <summary>
	/// A list of lobby names (purely for debugging in the inspector)
	/// </summary>
	[Group( "Searching" ), Property]
	public List<string> lobbyListNames { get; private set; } = new();

	/// <summary>
	/// Are we currently searching for a match?
	/// </summary>
	[Group( "Searching" ), Property] 
	public bool isSearching { get; private set; } = false;

	/// <summary>
	/// Have we found a game?
	/// </summary>
	[Group( "Searching" ), Property]
	public bool gameFound { get; set; } = false;

	/// <summary>
	/// Are we joining a game?
	/// </summary>
	[Group( "Loading" ), Property] 
	public bool gameJoined { get; set; } = false;

	/// <summary>
	/// How long should it take until we fully join once we match with a server
	/// </summary>
	[Group( "Loading" ), Property] 
	public int loadDelay { get; set; } = 3000;

	/// <summary>
	/// The lobby name for a custom game
	/// </summary>
	[Group( "CustomGame" ), Property]
	public string lobbyName { get; set; }

	/// <summary>
	/// A list of lobbies we fetch from the query.
	/// </summary>
	public List<LobbyInformation> lobbyList { get; private set; } = new();

	/// <summary>
	/// My own lobby
	/// </summary>
	public LobbyInformation myLobby { get; private set; }

	/// <summary>
	/// A cancellation token so we can cancel 
	/// </summary>
	private CancellationTokenSource searchTokenSource;

	protected override void OnAwake()
	{
		GameObject.Flags |= GameObjectFlags.DontDestroyOnLoad; // keep this around so I can see the match type
		Instance = this;
	}

	/// <summary>
	/// Starts searching for a game repeatedly until canceled.
	/// </summary>
	public async Task StartSearching( QueueType queue )
	{
		if ( queue != SelectedQueueType )
		{
			Networking.Disconnect();
		}

		StopSearching( false ); // Cancel any ongoing search before starting a new one. What if we don't destroy the server here? IE someone leaving the game?
		SelectedQueueType = queue;
		searchTokenSource = new CancellationTokenSource();
		isSearching = true;
		Log.Info( $"Started searching for games in queue: {queue.ToString()}" );

		try
		{
			while ( !searchTokenSource.Token.IsCancellationRequested )
			{
				await SearchGame( queue );
				await Task.DelaySeconds( searchFrequency, searchTokenSource.Token );
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
			// isSearching = false;
		}
	}

	public async Task SearchGame( QueueType queue )
	{
		Log.Info( "Searching Games..." );
		lobbyList = await Networking.QueryLobbies();
		lobbyListNames.Clear();

		if ( Networking.IsActive ) //if we've already made a lobby, remove our lobby from the list
		{
			// make a reference to mylobby
			myLobby = lobbyList.FirstOrDefault( lobby => lobby.OwnerId == Connection.Host.SteamId );
			// Filter and remove the player's own lobby
			lobbyList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
		}

		// remove all the lobbys that don't match the queue we're searching for
		for ( int i = 0; i < lobbyList.Count; i++ )
		{
			if ( lobbyList[i].Name != queue.ToString() )
			{
				lobbyList.Remove( lobbyList[i] );
			}
		}

		if ( lobbyList.Count == 0 )
		{
			Log.Info( "No Lobbies found" );

			if ( !Networking.IsActive ) // only create a lobby if we haven't already created one
			{
				Log.Info( "Creating Lobby" );
				CreateMatchmakingLobby( queue );
			}
		}
		else // if there is at least 1 relevant lobby
		{
			// Collect lobby names so I can read them in the editor
			foreach ( var lobby in lobbyList )
			{
				lobbyListNames.Add( lobby.Name + " " + lobby.Members + "/" + lobby.MaxMembers + " " + lobby.OwnerId );
			}

			Log.Info( "Active Lobbies in queue: " + lobbyList.Count );

			// put the lobby with most members first
			lobbyList = lobbyList.OrderByDescending( lobby => lobby.Members ).ToList();

			var found = lobbyList.First();

			Log.Info( $"Game Found! Trying to connect to lobby with ID: {found.LobbyId}, owned by {new Friend(found.OwnerId).Name}" );
			
			if ( await Networking.TryConnectSteamId( found.LobbyId ) )
			{
				Log.Info( "Success!" );
			}
			else
			{
				Log.Warning( "Couldn't join.. Not sure why. Here's the lobby info:" );
				Log.Info( $"Member count: {found.Members}" );
				Log.Info( $"OwnerId: {found.OwnerId}" );
				Log.Info( $"Lobby data:" );

				foreach ( var kv in found.Data )
				{
					Log.Info( $"Lobby data: {kv.Key} - {kv.Value}" );
				}
			}


			gameFound = true;
		}
	}

	/// <summary>
	/// Create a specific lobby, grabbing maxplayers from a queue
	/// </summary>
	/// <param name="queue"></param>
	public void CreateMatchmakingLobby( QueueType queue )
	{
		Networking.Disconnect();
		
		SelectedQueueType = queue;

		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = MaxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = queue.ToString(),
			Hidden = true
		} );
	}

	public void CreateCustomLobby( QueueType queue, string name, int maxPlayers )
	{
		Log.Info( "Creating Custom Lobby" );

		lobbyName = name;

		Networking.Disconnect();

		SelectedQueueType = queue;

		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = maxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = name,
			Hidden = false,
		} );

		Log.Info( "Lobby Created" );
	}

	/// <summary>
	/// Cancels the current search process.
	/// </summary>
	public void StopSearching( bool destroyLobby )
	{
		if ( destroyLobby )
		{
			Log.Info( "Destroying Lobby" );
			Networking.Disconnect();

			lobbyListNames.Clear();
			lobbyList.Clear();
			SelectedQueueType = QueueType.None;
		}

		isSearching = false;
		searchTokenSource?.Cancel();
	}

	/// <summary>
	/// Searches for lobbies and tries to join if a match is found.
	/// </summary>
	public async void OnActive( Connection channel )
	{
		// If the joining player isn't a host, and we haven't started the game yet
		if ( Networking.IsHost & !gameJoined & Connection.All.Count > 1 )
		{
			Log.Info( "Player Joined: " + channel.DisplayName );

			// If I'm the owner I need to stop searching?
			StopSearching( false );
		
			if ( Connection.All.Count == MaxPlayers & SelectedQueueType != QueueType.Custom )
			{
				// start game
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

		if ( SelectedQueueType == QueueType.Custom )
			return;

		if ( gameJoined & Connection.All.Count <= MaxPlayers ) // if a game is in progress and someone leaves
		{
			Networking.Disconnect();
			Scene.LoadFromFile( "scenes/mainmenu.scene" );
			QueueManager.Instance.StopSearching( true );
		}

		if ( !channel.IsHost & !gameJoined & Connection.All.Count <= 2 ) // if we haven't started the game, someone disconnects and it's just us left (2 because this is called just before the player disconnects)
		{
			Log.Info( "Not enough players after disconnect, starting search" );
			await StartSearching( SelectedQueueType );
		}

		// we're automatically searching again if we click cancel - I think because of this logic.
		// we need to check if it's just us, if it is we need to create a lobby again
	}
}
