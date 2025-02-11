public enum TeamSide
{
	None,
	Red,
	Blue
}

public enum QueueType
{
	None,
	QuickPlay,
	Solo,
	Duo,
	Custom
}

public class QueueTypeInfo
{
	[KeyProperty]
	public QueueType Type
	{
		get => _type;
		set
		{
			_type = value;

			//automatically set some values here - can override the maxplayers seperately for custom and quickplay
			switch ( _type )
			{
				case QueueType.None:
					MaxPlayers = -1;
					break;
				case QueueType.QuickPlay:
					MaxPlayers = 2;
					break;
				case QueueType.Solo:
					MaxPlayers = 2;
					break;
				case QueueType.Duo:
					MaxPlayers = 4;
					break;
				case QueueType.Custom:
					MaxPlayers = 2;
					break;
				default:
					break;
			}
		}
	}

	[KeyProperty]
	public int MaxPlayers { get; set; }

	private QueueType _type;

	public QueueTypeInfo()
	{
		Type = QueueType.None;  // Default value
		MaxPlayers = -1;         // Default value
	}

	public QueueTypeInfo( QueueType type, int maxPlayers )
	{
		Type = type;
		MaxPlayers = maxPlayers;
	}

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
	Idle,//default state
	Hovering,//hovering a team piece
	Grabbing,//currently grabbing a piece
	Busy,//piece is on cooldown
	Disabled,//piece is disabled (for kick off)
	HoveringEnemy,//hovering enemy



}
