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
	Idle,//default state
	Hovering,//hovering a team piece
	Grabbing,//currently grabbing a piece
	Busy,//piece is on cooldown
	Disabled,//piece is disabled (for kick off)
	HoveringEnemy,//hovering enemy



}
