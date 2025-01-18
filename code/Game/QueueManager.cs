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

[Title( "QueueManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class QueueManager : Component, Component.INetworkListener
{

	public static QueueManager Instance { get; private set; }


	//Queue Type
	[Group( "Queue Type" )]

	
	private QueueType selectedQueueType;

	[Property]
	public QueueType SelectedQueueType
	{
		get => selectedQueueType;
		set
		{
			selectedQueueType = value;
			UpdateMaxPlayers( value ); // Call the method to handle max players
		}
	}
	private void UpdateMaxPlayers( QueueType queue )
	{
		switch ( queue ) // Set the max players based on the queue type
		{
			case QueueType.None:
				maxPlayers = 0;
				break;

			case QueueType.Solo:
				maxPlayers = 2;
				break;

			case QueueType.Duo:
				maxPlayers = 4;
				break;

			case QueueType.Custom:
				maxPlayers = 4;
				break;
		}

	}


	//Searching
	[Group( "Searching" )] [Property] public int searchFrequency { get; set; } = 5000;
	public List<LobbyInformation> lobbyList { get; private set; } = new List<LobbyInformation>();

	public LobbyInformation myLobby { get; private set; }

	[Group( "Searching" )] [Property] public List<string> lobbyListNames { get; private set; } = new List<string>();

	[Group("Searching")][Property] public bool isSearching { get; private set; } = false;

	[Group("Searching")][Property] public bool gameFound { get; private set; } = false;

	[Group( "Searching" )][Property] public int maxPlayers { get; private set; }

	[Group( "Loading" )][Property] public bool gameJoined { get; private set; } = false;

	[Group( "Loading" )][Property] public int loadDelay { get; set; } = 3000;

	private CancellationTokenSource searchTokenSource;




	



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

		StopSearching(false); // Cancel any ongoing search before starting a new one. What if we don't destroy the server here? IE someone leaving the game?
		SelectedQueueType = queue;
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
		lobbyList = await Networking.QueryLobbies();
		lobbyListNames.Clear();

	

		if ( Networking.IsActive )//if we've already made a lobby, remove our lobby from the list
		{
			//make a reference to mylobby
			myLobby = lobbyList.FirstOrDefault( lobby => lobby.OwnerId == Connection.Host.SteamId );
			// Filter and remove the player's own lobby
			lobbyList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );
		}

		//remove all the lobbys that don't match the queue we're searching for
		for ( int i = 0; i < lobbyList.Count; i++ )
		{
			if ( lobbyList[i].Name != queue.ToString() )
			{
				lobbyList.Remove( lobbyList[i] );
			}
		}


		if (lobbyList.Count == 0 )
		{
			Log.Info( "No Lobbies found" );
			if ( !Networking.IsActive )//only create a lobby if we haven't already created one
			{
				Log.Info( "Creating Lobby" );
				CreateLobby( queue );
			}

		}
		else //if there is at least 1 relevant lobby
		{
			// Collect lobby names so I can read them in the editor
			foreach ( var lobby in lobbyList )
			{
				lobbyListNames.Add( lobby.Name + " " + lobby.Members + "/" + lobby.MaxMembers + " " + lobby.OwnerId );
			}

			Log.Info( "Active Lobbies in queue: " + lobbyList.Count );

			//put the lobby with most members first
			lobbyList = lobbyList.OrderByDescending( lobby => lobby.Members ).ToList();
			Networking.Disconnect();//disconnect if we were hosting
			Log.Info( "Game Found! Connecting..." );
			Networking.Connect( lobbyList[0].LobbyId ); //connect to the one with the most members
			gameFound = true;

		}
	}

	public void CreateLobby( QueueType queue )
	{
		SelectedQueueType = queue;

		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = maxPlayers,
			Privacy = LobbyPrivacy.Public,
			Name = queue.ToString(),
			Hidden = true


		} );
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
		
		if ( Networking.IsHost & gameJoined != true & Connection.All.Count>1) //if the joining player isn't a host, and we haven't started the game yet
		{
			Log.Info( "Player Joined: " + channel.DisplayName );


			//if I'm the owner I need to stop searching?
			StopSearching( false);

		
			if ( Connection.All.Count == maxPlayers )
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



		if ( !channel.IsHost & gameJoined != true & Connection.All.Count <= 2)//if we haven't started the game, someone disconnects and it's just us left (2 because this is called just before the player disconnects)
		{
			Log.Info( "Not enough players after disconnect, starting search" );
			await StartSearching(selectedQueueType);
		}

		//we're automatically searching again if we click cancel - I think because of this logic.



		//we need to check if it's just us, if it is we need to create a lobby again

	}
	protected override void OnUpdate() { }
}
