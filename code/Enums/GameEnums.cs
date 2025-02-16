using Sandbox.Network;

public enum TeamSide
{
	None,
	Red,
	Blue
}

public enum QueueType
{
	None,
	Solo,
	Duo,
	Custom
}

public enum PieceState
{

	Ready,
	Hovered,
	Grabbed,
	Cooldown,
	Frozen
}

public enum ControllerState
{
	Idle, // default state
	Hovering, // hovering a team piece
	Grabbing, // currently grabbing a piece
	Busy, // piece is on cooldown
	Disabled, // piece is disabled (for kick off)
	HoveringEnemy, // hovering enemy
}

/// <summary>
/// Some extenstion methods so we don't have to duplicate code.
/// </summary>
public static class EnumExtensions
{
	/// <summary>
	/// Gets the max players in a lobby for a specific queue type.
	/// </summary>
	/// <param name="queue"></param>
	/// <returns></returns>
	public static int GetPlayers( this QueueType queue )
	{
		return queue switch
		{
			QueueType.Solo => 2,
			QueueType.Duo => 4,
			_ => 0,
		};
	}

	/// <summary>
	/// Gets the max players in a lobby for a specific queue type.
	/// </summary>
	/// <param name="lobby"></param>
	/// <returns></returns>
	public static QueueType GetQueueType( this LobbyInformation lobby )
	{
		if ( lobby.Name.Equals( "solo", System.StringComparison.InvariantCultureIgnoreCase ) ) return QueueType.Solo;
		if ( lobby.Name.Equals( "duo", System.StringComparison.InvariantCultureIgnoreCase ) ) return QueueType.Duo;
		return QueueType.Custom;
	}
	
	/// Is this lobby of a specific queue type?
	/// </summary>
	/// <param name="queue"></param>
	/// <param name="lobby"></param>
	/// <returns></returns>
	public static bool IsLobbyOfType( this QueueType queue, LobbyInformation lobby )
	{
		return queue switch
		{
			QueueType.Solo => lobby.Name.Equals( "solo", System.StringComparison.InvariantCultureIgnoreCase ),
			QueueType.Duo => lobby.Name.Equals( "duo", System.StringComparison.InvariantCultureIgnoreCase ),
			_ => !lobby.Name.Equals( "solo", System.StringComparison.InvariantCultureIgnoreCase ) 
				&& !lobby.Name.Equals( "duo", System.StringComparison.InvariantCultureIgnoreCase )
		};
	}
}
