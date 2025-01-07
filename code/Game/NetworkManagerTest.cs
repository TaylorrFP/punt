using Sandbox.Network;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sandbox;

[Title( "NetworkManagerTest" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]

public sealed class NetworkManagerTest : Component
{
	[Property] public int searchFrequency { get; set; } = 5000;
	public static NetworkManagerTest Instance { get; private set; }

	public List<LobbyInformation> lobbyList { get; set; } = new List<LobbyInformation>();

	[Property] public List<String> lobbyListNames { get; set; } = new List<String>();

	[Property] public LobbyInformation myLobby { get; set; }

	[Property] public bool isSearching { get; set; } = false;
	[Property] public String queueType { get; set; }

	protected override void OnAwake()
	{
		Instance = this;
		base.OnAwake();
	}

	protected override void OnStart()
	{
		//if ( Scene.IsEditor )
		//	return;

		//if ( StartServer && !Networking.IsActive )
		//{
		//	LoadingScreen.Title = "Creating Lobby";

		//	//Networking.CreateLobby();

		//	Networking.CreateLobby( new LobbyConfig()
		//	{
		//		MaxPlayers = 2,
		//		Privacy = LobbyPrivacy.Public,
		//		Name = Network.Owner.DisplayName + "'s 1v1 Lobby"

		//		//MaxPlayers = 4,
		//		//Privacy = LobbyPrivacy.Public,
		//		//Name = Network.Owner.DisplayName + "'s Punt Lobby"
		//	} );


		//	Log.Info( "NetworkingManager: Creating Lobby" );
		//}



	}



	//for now we'll just call the lobby the type of queue
	//1v1 = "0"
	//2v2 = "1"
	//this way we can match people up by checking the name, and ignore lobbies with those names in the server browser
	//this is probably a terrible way of doing this but I think it's the only way for now?



	public async Task SearchGame( String QueueType )
	{
		
		isSearching = true;
		queueType = QueueType;
		Log.Info( "Searching Games" );

		//filters don't work right now?
		//var filters = new Dictionary<string, string>{
		//{ "gameQueue", "1v1" }};

		
		lobbyList = await Networking.QueryLobbies();
		isSearching = false;
		lobbyListNames.Clear();


		////this includes us so remove the first one.
		//if ( lobbyList.Count >= 1 )
		//{
		//	lobbyList.RemoveAt( 0 );
		//}

		//Log.Info("My ID: " + Connection.Host.SteamId );

		


		for ( int i = 0; i < lobbyList.Count; i++ )
		{
			if ( lobbyList[i].OwnerId == Connection.Host.SteamId )
			{
				lobbyList.RemoveAt(i);
	
			}

			if ( lobbyList.Count >0 )
			{
				lobbyListNames.Add( lobbyList[i].Name );
				Log.Info( "LobbyID: " + lobbyList[i].OwnerId );

			}



		}

		Log.Info( "Active Lobbies: " + lobbyList.Count );



		if ( lobbyList.Count >= 1 )
		{
			for ( int i = 0; i < lobbyListNames.Count; i++ )
			{
				if ( lobbyListNames[i] == queueType )
				{

					Log.Info( "Game Found" );
					//Networking.Connect( lobbyList[i].LobbyId);

				}

			}
		}



	}
	public void CreateLobby( String QueueType )
	{
		Log.Info( "Lobby Created. Name: " + Steam.PersonaName + "1v1" );

		Networking.CreateLobby( new LobbyConfig()
		{
			MaxPlayers = 2,
			Privacy = LobbyPrivacy.Public,
			Name = QueueType

		}


		);

	}

	public void OnActive( Connection channel )
	{

	}
	protected override void OnUpdate()
	{

	}
}
