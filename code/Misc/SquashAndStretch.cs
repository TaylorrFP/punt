using Sandbox;
using System;

public sealed class SquashAndStretch : Component
{
	[Property] public Curve squashCurve { get; set; }

	[Property] public RealTimeSince squashTimeSince { get; set; } = 0.5f;


	[Property] public float squashDuration { get; set; } = 0.5f;

	[Property] public Vector3 currentScale { get; set; }

	//squash and stretch hard-coded to between 0 and 2

	protected override void OnUpdate()
	{
		if ( squashTimeSince < squashDuration )
		{


			float scale = squashCurve.Evaluate( (squashTimeSince / squashDuration) );
			scale = MathX.Remap( scale, 0, 1, 0, 2 );

			this.GameObject.LocalScale = new Vector3(2 - scale, 2 - scale,scale);
			currentScale = GameObject.LocalScale;
		}

	}

	public void StartSquash( float Duration )
	{
		squashTimeSince = 0;
		squashDuration = Duration;


	}
}
