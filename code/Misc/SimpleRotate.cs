using Sandbox;

public sealed class SimpleRotate : Component
{


	[Property] public float speed { get; set; } = 0.1f;

	

	protected override void OnStart()
	{


	}


	protected override void OnUpdate()
	{
		GameObject.WorldRotation = Rotation.From( new Angles( 0, WorldRotation.Angles().yaw + speed, 0 ) );

	}
}
