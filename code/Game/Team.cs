using System.Collections.Generic;

public class Team
{
	public string TeamName { get; private set; }
	public List<PuntPlayerController> Players { get; private set; }
	public int Score { get; private set; }

}
