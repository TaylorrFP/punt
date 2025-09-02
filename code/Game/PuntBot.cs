using Sandbox;
using System;
using System.Collections.Generic;

public sealed class PuntBot : Component
{
	// -------------------- Tuning (exposed) --------------------
	[Property] public int PredictionSteps { get; set; } = 30;
	[Property] public float PredictionTime { get; set; } = 6f;
	[Property] public float LinearDamping { get; set; } = 0.10f;
	[Property] public bool UseGravity { get; set; } = true;
	[Property] public Vector3 GravityOverride { get; set; } = Vector3.Zero;
	[Property] public float BallRadius { get; set; } = 30f;
	[Property] public float Bounciness { get; set; } = 0.65f;
	[Property] public float BounceFriction { get; set; } = 0.20f;
	[Property] public int MaxBouncesPerStep { get; set; } = 3;
	[Property] public float SkinWidth { get; set; } = 0.2f;
	[Property] public float SleepSpeed { get; set; } = 10f;

	[Property] public float PieceLinearDamping { get; set; } = 0.10f;
	[Property] public float MaxFlickSpeed { get; set; } = 650f;
	[Property] public float FlickFrequency { get; set; } = 2f;

	// Optional: rotate which piece we control
	[Property] public float PieceSelectPeriod { get; set; } = 2f; // seconds
	[Property] public bool FlickOnSelect { get; set; } = true;

	[Property] public PuntPiece BotPiece { get; set; }

	// Debug
	[Property] public bool DebugDrawPrediction { get; set; } = true;
	[Property] public bool DebugDrawIntercept { get; set; } = true;

	// Read-only diagnostics
	[Property] public float DistToBall { get; private set; }
	[Property] public Vector3 LastShotTarget { get; private set; }
	[Property] public float LastShotDist { get; private set; }

	public void RecordShot( Vector3 target, float dist )
	{
		LastShotTarget = target;
		LastShotDist = dist;
	}

	// -------------------- Internals --------------------
	private TimeUntil _flickCountdown;
	private TimeUntil _pieceSelectCountdown;
	private readonly Random _rng = new();

	private readonly List<TrajectoryNode> _cache = new();
	private readonly InterceptSolver _intercept = new();
	private readonly TrajectoryPredictor _predictor = new();

	// Simple FSM scaffold — start in Intercept mode
	private IBotState _state;

	protected override void OnAwake()
	{
		base.OnAwake();
		_flickCountdown = FlickFrequency;
		_pieceSelectCountdown = 0f; // pick immediately

		SetState( new InterceptState() );
	}

	protected override void OnUpdate()
	{
		// Rotate controlled piece every PieceSelectPeriod seconds
		if ( _pieceSelectCountdown <= 0f )
		{
			SelectRandomPieceFromBlueTeam();
			if ( FlickOnSelect ) _flickCountdown = 0f; // allow immediate flick after selecting
			_pieceSelectCountdown = PieceSelectPeriod;
		}

		var ball = TestGameMode.Instance?.Ball;
		if ( ball is null || ball.ballRB is null || BotPiece is null ) return;

		// 1) Predict once & cache
		var cfg = new TrajectoryConfig
		{
			Scene = Scene,
			BallGo = ball.GameObject,
			StartPos = ball.WorldPosition,
			StartVel = ball.ballRB.Velocity,
			TotalTime = PredictionTime,
			Steps = PredictionSteps,
			LinearDamping = LinearDamping,
			UseGravity = UseGravity,
			Gravity = GravityOverride,
			BallRadius = BallRadius,
			Bounciness = Bounciness,
			BounceFriction = BounceFriction,
			MaxBouncesPerStep = MaxBouncesPerStep,
			SkinWidth = SkinWidth,
			SleepSpeed = SleepSpeed
		};
		_predictor.Predict( ref cfg, _cache );

		// 2) Debug draw
		if ( DebugDrawPrediction )
			DrawTrajectory( _cache, BallRadius );

		// 3) Perception data (2D ground distance)
		DistToBall = (_cache.Count > 0 ? _cache[0].Pos : ball.WorldPosition)
					 .WithZ( 0 )
					 .Distance( BotPiece.WorldPosition.WithZ( 0 ) );

		// 4) Run state
		_state?.Tick( this );
	}

	private void SetState( IBotState next )
	{
		_state?.Exit( this );
		_state = next;
		_state?.Enter( this );
	}

	// -------------------- Actions used by states --------------------
	public bool TryGetIntercept( out float tHit, out Vector3 hitPos, out Vector3 aimVel )
	{
		tHit = 0f; hitPos = default; aimVel = default;
		if ( BotPiece is null ) return false;

		var botPos = BotPiece.WorldPosition;
		return _intercept.TryComputeEarliest(
			_cache, botPos, MaxFlickSpeed, PieceLinearDamping,
			out tHit, out hitPos, out aimVel
		);
	}

	public void TryFlick( Vector3 aimVelocity2D )
	{
		// Only flick during Playing
		if ( _flickCountdown > 0f || TestGameMode.Instance?.State != GameState.Playing ) return;

		_flickCountdown = FlickFrequency;

		// XY only
		var v = new Vector3( aimVelocity2D.x, aimVelocity2D.y, 0f );
		if ( BotPiece?.rigidBody is not null )
			BotPiece.rigidBody.Velocity = v;

		if ( BotPiece?.rigidBody?.PhysicsBody is not null )
			BotPiece.rigidBody.PhysicsBody.Velocity = v; // keep if your setup needs it
	}

	// -------------------- Utility: select a random controllable piece --------------------
	private void SelectRandomPieceFromBlueTeam()
	{
		var list = TestGameMode.Instance?.BluePieceList;
		if ( list == null || list.Count == 0 )
		{
			BotPiece = null;
			return;
		}

		for ( int attempts = 0; attempts < list.Count; attempts++ )
		{
			int idx = _rng.Next( list.Count );
			var candidate = list[idx];
			if ( candidate != null && candidate.rigidBody != null && candidate.rigidBody.PhysicsBody != null )
			{
				BotPiece = candidate;

				// Optional: highlight the chosen piece
				if ( DebugDrawPrediction )
				{
					Gizmo.Draw.Color = Color.Yellow;
					Gizmo.Draw.LineSphere( BotPiece.WorldPosition, 35f );
				}
				return;
			}
		}

		BotPiece = null;
	}

	// -------------------- Debug draw helpers --------------------
	private static void DrawTrajectory( List<TrajectoryNode> nodes, float ballRadius )
	{
		if ( nodes == null || nodes.Count < 2 ) return;

		Gizmo.Draw.Color = Color.Cyan;
		for ( int i = 0; i < nodes.Count - 1; i++ )
			Gizmo.Draw.Line( nodes[i].Pos, nodes[i + 1].Pos );

		Gizmo.Draw.Color = Color.Red;
		for ( int i = 0; i < nodes.Count; i++ )
		{
			if ( !nodes[i].Hit ) continue;
			Gizmo.Draw.LineSphere( nodes[i].Pos, ballRadius * 1.05f );
			Gizmo.Draw.Line( nodes[i].Pos, nodes[i].Pos + nodes[i].Normal * (ballRadius * 1.5f) );
		}
	}

	public void DebugDrawInterceptMark( Vector3 pos, float t, float radius )
	{
		if ( !DebugDrawIntercept ) return;
		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.LineSphere( pos, radius * 1.05f );
		Gizmo.Draw.Text( $"{t:0.00}s", new Transform( pos + Vector3.Up * 70f ), "Poppins", 24 );
	}
}

#region ---------- Prediction ----------

public struct TrajectoryConfig
{
	public Scene Scene;
	public GameObject BallGo;
	public Vector3 StartPos;
	public Vector3 StartVel;
	public float TotalTime;
	public int Steps;

	public float LinearDamping;
	public bool UseGravity;
	public Vector3 Gravity; // (0,0,0) = use Scene gravity

	public float BallRadius;
	public float Bounciness;
	public float BounceFriction;
	public int MaxBouncesPerStep;
	public float SkinWidth;
	public float SleepSpeed;
}

public struct TrajectoryNode
{
	public Vector3 Pos;
	public bool Hit;
	public Vector3 Normal;
	public Vector3 HitPos;
	public float Time;
}

public sealed class TrajectoryPredictor
{
	public void Predict( ref TrajectoryConfig c, List<TrajectoryNode> outNodes )
	{
		outNodes.Clear();

		var gravity = GetGravity( ref c );
		var dt = (c.Steps <= 0) ? c.TotalTime : c.TotalTime / c.Steps;

		var pos = c.StartPos;
		var vel = c.StartVel;

		float t = 0f;
		outNodes.Add( new TrajectoryNode { Pos = pos, Time = 0f } );

		bool endedEarly = false;

		for ( int i = 0; i < c.Steps; i++ )
		{
			float remaining = dt;
			int sub = 0;

			while ( remaining > 0f && sub++ < c.MaxBouncesPerStep )
			{
				var vAfter = ApplyDamping( vel + gravity * remaining, c.LinearDamping, remaining );
				var endPos = pos + vAfter * remaining;

				var tr = c.Scene.Trace
					.Sphere( c.BallRadius, pos, endPos )
					.IgnoreGameObjectHierarchy( c.BallGo )
					.UseHitPosition( true )
					.WithoutTags( "ball", "trigger" )
					.Run();

				if ( !tr.Hit )
				{
					pos = endPos;
					vel = vAfter;
					t += remaining;
					outNodes.Add( new TrajectoryNode { Pos = pos, Time = t } );
					remaining = 0f;
					break;
				}

				float tHit = remaining * tr.Fraction;

				// Horizon clamp (rare)
				if ( t + tHit >= c.TotalTime )
				{
					float leftover = c.TotalTime - t;
					var vPartial = ApplyDamping( vel + gravity * leftover, c.LinearDamping, leftover );
					var endAtHorizon = pos + vPartial * leftover;
					t = c.TotalTime;
					outNodes.Add( new TrajectoryNode { Pos = endAtHorizon, Time = t } );
					endedEarly = true;
					break;
				}

				var vAtHit = ApplyDamping( vel + gravity * tHit, c.LinearDamping, tHit );
				pos = tr.EndPosition + tr.Normal * c.SkinWidth;
				vel = Reflect( vAtHit, tr.Normal, c.Bounciness, c.BounceFriction );
				t += tHit;

				outNodes.Add( new TrajectoryNode
				{
					Pos = pos,
					Hit = true,
					Normal = tr.Normal,
					HitPos = tr.HitPosition,
					Time = t
				} );

				remaining -= tHit;

				if ( vel.Length <= c.SleepSpeed )
				{
					endedEarly = true;
					break; // do NOT return; we'll add a horizon node below
				}
			}

			if ( endedEarly || vel.Length <= c.SleepSpeed )
				break;
		}

		// Always cover the requested horizon time, even if the ball is "sleeping"
		if ( outNodes.Count > 0 && outNodes[^1].Time < c.TotalTime )
		{
			outNodes.Add( new TrajectoryNode { Pos = outNodes[^1].Pos, Time = c.TotalTime } );
		}
	}

	public static Vector3 SampleAt( List<TrajectoryNode> nodes, float targetTime )
	{
		if ( nodes == null || nodes.Count == 0 ) return default;

		if ( targetTime <= 0f ) return nodes[0].Pos;
		if ( targetTime >= nodes[^1].Time ) return nodes[^1].Pos;

		// Binary search for bracket
		int lo = 0, hi = nodes.Count - 1;
		while ( hi - lo > 1 )
		{
			int mid = (lo + hi) >> 1;
			if ( nodes[mid].Time >= targetTime ) hi = mid; else lo = mid;
		}

		var a = nodes[lo]; var b = nodes[hi];
		float span = MathF.Max( 1e-6f, b.Time - a.Time );
		float alpha = (targetTime - a.Time) / span;
		return Vector3.Lerp( a.Pos, b.Pos, alpha );
	}

	private static Vector3 GetGravity( ref TrajectoryConfig c )
	{
		if ( !c.UseGravity ) return Vector3.Zero;
		if ( c.Gravity != Vector3.Zero ) return c.Gravity;
		return c.Scene?.PhysicsWorld.Gravity ?? new Vector3( 0f, 0f, -800f );
	}

	private static Vector3 ApplyDamping( Vector3 v, float k, float dt )
	{
		if ( k <= 1e-6f ) return v;
		return v * (1f / (1f + k * dt));
	}

	private static Vector3 Reflect( Vector3 v, Vector3 n, float bounciness, float tangentFriction )
	{
		n = n.Normal;
		var vN = Vector3.Dot( v, n ) * n;
		var vT = v - vN;
		return (-vN * bounciness) + (vT * (1f - tangentFriction));
	}
}
#endregion

#region ---------- Interception ----------

public sealed class InterceptSolver
{
	/// <summary>
	/// Find earliest time t where bot can reach ball path, given max initial speed and linear damping.
	/// Returns aim velocity (XY) to apply as the *initial* flick velocity.
	/// </summary>
	public bool TryComputeEarliest(
	List<TrajectoryNode> path,
	Vector3 botPosWorld,
	float maxInitialSpeed,
	float pieceLinearDamping,
	out float interceptTime,
	out Vector3 interceptPos,
	out Vector3 aimVelocityXY
)
	{
		interceptTime = 0f; interceptPos = default; aimVelocityXY = default;
		if ( path == null || path.Count == 0 ) return false;

		Vector3 Flat( Vector3 v ) => new Vector3( v.x, v.y, 0f );
		var bot2D = Flat( botPosWorld );
		float vmax = MathF.Max( 0.01f, maxInitialSpeed );
		float k = MathF.Max( 0f, pieceLinearDamping );

		float ReachableDist( float t )
		{
			if ( t <= 0f ) return 0f;
			if ( k <= 1e-6f ) return vmax * t;
			return (vmax / k) * (1f - MathF.Exp( -k * t ));
		}

		// Reused variables to avoid shadowing
		Vector3 aimDir2D = Vector3.Zero;
		float requiredV0 = 0f;
		float speed = 0f;

		// --- Fallback for stationary kickoff: single node path
		if ( path.Count < 2 )
		{
			var p = Flat( path[0].Pos );
			float dist = (p - bot2D).Length;

			// Time needed with damping
			float t = (k <= 1e-6f) ? dist / vmax
					 : (-MathF.Log( MathF.Max( 1e-5f, 1f - (dist * k / vmax) ) ) / k);
			if ( float.IsNaN( t ) || t < 0f ) return false;

			interceptTime = t;
			interceptPos = path[0].Pos;

			aimDir2D = (dist > 1e-4f) ? ((p - bot2D) / dist) : Vector3.Zero;
			requiredV0 = (t <= 1e-5f) ? 0f
					   : (k <= 1e-6f) ? (dist / t)
					   : (dist * k / (1f - MathF.Exp( -k * t )));
			speed = MathF.Min( vmax, requiredV0 );
			aimVelocityXY = new Vector3( aimDir2D.x * speed, aimDir2D.y * speed, 0f );
			return true;
		}

		// Find first node we can reach
		int idx = -1;
		for ( int i = 1; i < path.Count; i++ )
		{
			float t = path[i].Time;
			float dist = (Flat( path[i].Pos ) - bot2D).Length;
			if ( ReachableDist( t ) >= dist ) { idx = i; break; }
		}
		if ( idx < 0 ) return false;

		// Refine within segment [idx-1, idx]
		float tLo = path[idx - 1].Time;
		float tHi = path[idx].Time;
		for ( int it = 0; it < 12; it++ )
		{
			float mid = 0.5f * (tLo + tHi);
			var pMid = Flat( TrajectoryPredictor.SampleAt( path, mid ) );
			float distMid = (pMid - bot2D).Length;
			if ( ReachableDist( mid ) >= distMid ) tHi = mid; else tLo = mid;
		}

		interceptTime = tHi;
		interceptPos = TrajectoryPredictor.SampleAt( path, interceptTime );

		// Required initial speed to arrive at t
		var toHit2D = Flat( interceptPos ) - bot2D;
		float distReq = toHit2D.Length;
		requiredV0 = (interceptTime <= 1e-5f) ? 0f
				   : (k <= 1e-6f) ? (distReq / interceptTime)
				   : (distReq * k / (1f - MathF.Exp( -k * interceptTime )));
		speed = MathF.Min( vmax, requiredV0 );
		aimDir2D = distReq > 1e-4f ? (toHit2D / distReq) : Vector3.Zero;
		aimVelocityXY = new Vector3( aimDir2D.x * speed, aimDir2D.y * speed, 0f );
		return true;
	}
}
#endregion

#region ---------- Simple FSM scaffold ----------

public interface IBotState
{
	void Enter( PuntBot bot ) { }
	void Tick( PuntBot bot );
	void Exit( PuntBot bot ) { }
}

public sealed class InterceptState : IBotState
{
	public void Tick( PuntBot bot )
	{
		if ( bot.TryGetIntercept( out var tHit, out var hitPos, out var aimVel ) )
		{
			bot.DebugDrawInterceptMark( hitPos, tHit, bot.BallRadius );
			bot.TryFlick( aimVel ); // only fires during GameState.Playing
			bot.RecordShot( hitPos, (hitPos - bot.BotPiece.WorldPosition).WithZ( 0 ).Length );
		}
		else
		{
			// e.g., unreachable: switch states later (Defend/Chase etc.)
		}
	}
}
#endregion
