using Sandbox.Network;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sandbox;

[Title( "QueueManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class QueueManager : Component, Component.INetworkListener
{
	[Property] public int searchFrequency { get; set; } = 5000;
	[Property] public int loadDelay { get; set; } = 3000;
	public static QueueManager Instance { get; private set; }

	private CancellationTokenSource searchTokenSource;

	public List<LobbyInformation> lobbyList { get; private set; } = new List<LobbyInformation>();
	[Property] public List<string> lobbyListNames { get; private set; } = new List<string>();

	[Property] public LobbyInformation myLobby { get; private set; }
	[Property] public bool isSearching { get; private set; } = false;
	[Property] public string queueType { get; private set; }




	protected override void OnAwake()
	{




		Instance = this;
		base.OnAwake();
	}

	/// <summary>
	/// Creates a new lobby with the specified queue type.
	/// </summary>
	public void CreateLobby( string QueueType )
	{
		Log.Info( "Lobby Created. Name: " + Steam.PersonaName + QueueType );
		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = 2,
			Privacy = LobbyPrivacy.Public,
			Name = QueueType
		} );
	}

	/// <summary>
	/// Starts searching for a game repeatedly until canceled.
	/// </summary>
	public async Task StartSearching( string QueueType )
	{
		StopSearching(); // Cancel any ongoing search before starting a new one
		searchTokenSource = new CancellationTokenSource();
		queueType = QueueType;
		isSearching = true;

		Log.Info( $"Started searching for games in queue: {queueType}" );

		try
		{
			while ( !searchTokenSource.Token.IsCancellationRequested )
			{
				await SearchGame( queueType );
				await Task.Delay( searchFrequency, searchTokenSource.Token );
			}
		}
		catch ( TaskCanceledException )
		{
			Log.Info( "Search was canceled successfully." );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error during search: {ex.Message}" );
		}
		finally
		{
			isSearching = false;
		}
	}

	/// <summary>
	/// Cancels the current search process.
	/// </summary>
	public void StopSearching()
	{
		if ( !isSearching )
		{
			Log.Info( "No active search to cancel." );
			return;
		}

		searchTokenSource?.Cancel();
		isSearching = false;
		Log.Info( "Search stopped." );
	}

	/// <summary>
	/// Searches for lobbies and tries to join if a match is found.
	/// </summary>
	public async Task SearchGame( string QueueType )
	{
		Log.Info( "Searching Games..." );
		lobbyList = await Networking.QueryLobbies();
		lobbyListNames.Clear();

		// Filter and remove the player's own lobby
		lobbyList.RemoveAll( lobby => lobby.OwnerId == Connection.Host.SteamId );

		// Collect lobby names for logging and filtering
		foreach ( var lobby in lobbyList )
		{
			lobbyListNames.Add( lobby.Name );
			Log.Info( "LobbyID: " + lobby.OwnerId );
		}

		Log.Info( "Active Lobbies: " + lobbyList.Count );

		// Attempt to join the first matching lobby
		foreach ( var lobby in lobbyList )
		{
			if ( lobby.Name == QueueType )
			{
				Log.Info( "Game Found! Connecting..." );
				Networking.Connect( lobby.LobbyId );
				return;
			}
		}

		Log.Info( "No matching lobbies found." );
	}

	public async void OnActive( Connection channel )
	{
		if ( !channel.IsHost )
		{
			StopSearching();
			Log.Info( "Player Joined: " + channel.DisplayName );
			Log.Info( "Starting game..." );

			await Task.Delay( 3000 ); // Delays for 3 seconds once
			Scene.LoadFromFile( "scenes/networktestscene.scene" );
		}
	}
	public void OnDisconnected( Connection connection )
	{
	
	}
	protected override void OnUpdate() { }
}
