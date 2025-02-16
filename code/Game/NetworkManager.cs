using Sandbox.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandbox;

[Title( "NetworkManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class NetworkManager : Component, Component.INetworkListener
{
	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	[Property] public GameObject ArrowPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>

	protected override void OnStart()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !Networking.IsActive ) // this is just for debug reasons
		{
			LoadingScreen.Title = "Creating Lobby";

			Networking.CreateLobby( new LobbyConfig()
			{
				MaxPlayers = 4,
				Privacy = LobbyPrivacy.Public,
				Name = "Editor Test Lobby",
				Hidden = true

			} );

			Log.Info( "NetworkingManager: Creating Lobby" );
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		if ( TestGameMode.Instance.DebugServer )
		{
			// if debugserver is on just spawn the player
			var player = PlayerPrefab.Clone();
			player.Name = $"Player - {channel.DisplayName}";
			player.NetworkSpawn( channel );
			PuntPlayerController controller = player.Components.Get<PuntPlayerController>();
			// Add player in gamemode

			var playerArrow = ArrowPrefab.Clone();
			playerArrow.Name = $"Arrow - {channel.DisplayName}";
			playerArrow.NetworkSpawn( channel );
			controller.InitArrow( playerArrow );

			Log.Info( "arrow spawned" );


			//spawn the arrow here too????
			TestGameMode.Instance.AddPlayer( controller );
		}

		if ( !channel.IsHost )
		{
			Log.Info( $"Player '{channel.DisplayName}' has joined the game as a client" );

		}
		else
		{
			Log.Info( $"Player '{channel.DisplayName}' has joined the game as host" );
		}

		if ( PlayerPrefab is null )
			return;


		if ( TestGameMode.Instance.State == GameState.Waiting)//only add a new player if we're in the waiting state
		{
			var player = PlayerPrefab.Clone();
			player.Name = $"Player - {channel.DisplayName}";
			player.NetworkSpawn( channel );
			var controller = player.Components.Get<PuntPlayerController>();

			var playerArrow = ArrowPrefab.Clone();
			playerArrow.Name = $"Arrow - {channel.DisplayName}";
			playerArrow.NetworkSpawn( channel );
			controller.InitArrow(playerArrow);




			TestGameMode.Instance.AddPlayer( controller );
			

		}



	}

	/// <summary>
	/// Called when someone leaves the server.
	/// </summary>
	/// //this shit don't work
	void INetworkListener.OnDisconnected( Connection connection )
	{

	}

		/// <summary>
		/// Called when the host of the game has left - and you are now the new host.
		/// </summary>
		void OnBecameHost( Connection previousHost )
	{

	}



}
