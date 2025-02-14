using Sandbox.Network;
using System.Threading.Tasks;

/// <summary>
/// A system to handle searching for lobbies in one place so we can use data from there. So we're not querying too much.
/// </summary>
public static class LobbySystem
{ 
	/// <summary>
	/// A list of queried lobbies from the last search.
	/// </summary>
	private static List<LobbyInformation> QueriedLobbies { get; set; }

	/// <summary>
	/// Time since we last queued
	/// </summary>
	static TimeSince TimeSinceLastQueue { get; set; } = 999;

	/// <summary>
	/// How frequently can we queue?
	/// </summary>
	static float QueueInterval => 15f;

	/// <summary>
	/// Query for lobbies
	/// </summary>
	private static async Task Query()
	{
		if ( TimeSinceLastQueue < QueueInterval ) return;

		Log.Info( "Doing a real query" );
		TimeSinceLastQueue = 0;
		QueriedLobbies = await Networking.QueryLobbies( "fptaylor.punt_dev" );

		Log.Info( $"Queried lobby count: {QueriedLobbies.Count()}" );

		foreach ( var l in QueriedLobbies )
		{
			Log.Info( $"{new Friend( l.OwnerId).Name}'s lobby, name: {l.Name}" );
		}

		TimeSinceLastQueue = 0;
	}

	/// <summary>
	/// Return the fetched lobbies.
	/// </summary>
	/// <returns></returns>
	public static async Task<List<LobbyInformation>> GetLobbies()
	{
		// If we haven't queried, query. Or if it's been a while, query!
		if ( QueriedLobbies is null || TimeSinceLastQueue > QueueInterval )
		{
			await Query();
		}

		return QueriedLobbies ?? new();
	}

	/// <summary>
	/// Return the fetched lobbies, filtered to a specific queue type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static async Task<List<LobbyInformation>> GetLobbies( QueueType type )
	{
		var lobbies = await GetLobbies();
		return lobbies
			.Where( x => type.IsLobbyOfType( x ) )
			.ToList();
	}
}
