using Sandbox;
using Sandbox.Internal;
using System;

public sealed class PuntGoal : Component, Component.ITriggerListener
{
	[Property] public TeamSide GoalTeamSide { get; set; }

	protected override void OnUpdate()
	{

	}

	protected override void OnStart()
	{
		
	}


	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy )
		{
			return;

		}

		if ( other.Tags.Has ("ball" ) )
		{
			if(TestGameMode.Instance.State == GameState.Playing )
			{

				TestGameMode.Instance.GoalScored( GoalTeamSide );
			}

			

		}
	}

	public void OnTriggerExit( Collider other )
	{
	}
}
