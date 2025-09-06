using Sandbox;
using System;
using System.Collections.Generic;
using System.Numerics;

public sealed class PitchGenerator : Component
{
	// --- Authoring ---

	[Property] public GameMode gameMode { get; set; }

	// Overall pitch footprint (X = left/right extent, Y = up/down extent)
	[Property]
	public float pitchX
	{
		get => _pitchX;
		set { if ( !Nearly( _pitchX, value ) ) { _pitchX = value; MarkDirty(); } }
	}
	[Property]
	public float pitchY
	{
		get => _pitchY;
		set { if ( !Nearly( _pitchY, value ) ) { _pitchY = value; MarkDirty(); } }
	}

	[Property]
	public float height
	{
		get => _height;
		set { if ( !Nearly( _height, value ) ) { _height = value; MarkDirty(); } }
	}
	[Property]
	public float thickness
	{
		get => _thickness;
		set { if ( !Nearly( _thickness, value ) ) { _thickness = value; MarkDirty(); } }
	}

	/// <summary>Segments per semicircle (min 2).</summary>
	[Property]
	public int endResolution
	{
		get => _endResolution;
		set { var v = Math.Max( 2, value ); if ( _endResolution != v ) { _endResolution = v; MarkDirty(); } }
	}

	/// <summary>Goal opening width in Y (centered at Y=0). Arc trims to this edge.</summary>
	[Property]
	public float goalWidth
	{
		get => _goalWidth;
		set { var v = MathF.Max( 0f, value ); if ( !Nearly( _goalWidth, v ) ) { _goalWidth = v; MarkDirty(); } }
	}

	/// <summary>Top of the goal mouth (Z). Above-goal colliders start here.</summary>
	[Property]
	public float goalHeight
	{
		get => _goalHeight;
		set { var v = MathF.Max( 0f, value ); if ( !Nearly( _goalHeight, v ) ) { _goalHeight = v; MarkDirty(); } }
	}

	/// <summary>Extra overlap (meters) added to the collider LENGTH to prevent physics snagging.</summary>
	[Group( "Colliders" )]
	[Property]
	public float seamBuffer
	{
		get => _seamBuffer;
		set { var v = MathF.Max( 0f, value ); if ( !Nearly( _seamBuffer, v ) ) { _seamBuffer = v; MarkDirty(); } }
	}

	[Property] public bool autoRebuild { get; set; } = true;

	// --- Preview Mesh ---
	[Group( "Preview Mesh" )]
	[Property] public bool previewEnabled { get; set; } = true;

	[Property] public Material previewMaterial { get; set; } = Material.Load( "materials/dev/gray_grid_8.vmat" );

	/// <summary>Texture tiling in meters (U along edge length, V along height).</summary>
	[Property] public float uvTileMeters { get; set; } = 2f;

	/// <summary>Independent height for the preview quads (bottom at Z=0, top at previewHeight).</summary>
	[Property]
	public float previewHeight
	{
		get => _previewHeight;
		set { var v = MathF.Max( 0f, value ); if ( !Nearly( _previewHeight, v ) ) { _previewHeight = v; MarkDirty(); } }
	}

	// --- Debug/Info (read-only) ---
	[Property] public float endRadius { get; private set; }          // R = pitchY / 2
	[Property] public float segmentChordLength { get; private set; } // base chord at uniform Δ
	[Property] public float arcDeltaDegrees { get; private set; }    // base Δφ for uniform segmentation

	// --- Internals ---
	private GameObject _root;        // walls parent
	private GameObject _previewGo;   // mesh renderer object
	private bool _dirty;

	private float _pitchX = 10f;
	private float _pitchY = 10f;
	private float _height = 5f;
	private float _thickness = 0.5f;
	private int _endResolution = 16;
	private float _goalWidth = 3f;
	private float _goalHeight = 2f;

	private float _previewHeight = 5f; // default matches initial collider height
	private float _seamBuffer = 0.03f; // small, tweakable overlap

	// cached
	private float _cosPhi;
	private float _phiGoalDeg;

	// Preview edges computed from UNBUFFERED geometry
	private struct EdgePreview
	{
		public Vector3 CenterW;
		public Vector3 UHatW;
		public Vector3 VHatW;
		public Vector3 NormalW;
		public float Width;
	}
	private readonly List<EdgePreview> _edgePreviews = new();

	// ---------- Lifecycle ----------
	protected override void OnStart()
	{
		base.OnStart();
		Rebuild();
	}

	protected override void OnUpdate()
	{
		if ( autoRebuild && _dirty )
			Rebuild();
	}

	[Button( "Rebuild" )]
	public void Rebuild()
	{
		_dirty = false;
		ClearGenerated();

		_root = new GameObject( true, "PitchGenerated" ) { Parent = GameObject };
		_edgePreviews.Clear();

		// ----- Geometry setup -----
		endRadius = MathF.Max( 0f, pitchY * 0.5f );
		if ( endRadius <= 0f )
		{
			BuildPreviewMesh();
			return;
		}

		float R = endRadius;
		float half = MathF.Max( 0f, goalWidth * 0.5f );
		float s = MathF.Min( 1f, (R <= 0f) ? 0f : half / R );
		_cosPhi = MathF.Sqrt( MathF.Max( 0f, 1f - s * s ) );
		_phiGoalDeg = MathX.RadianToDegree( MathF.Asin( s ) );

		// Keep overall X length constant as goal widens
		float straightLengthX = MathF.Max( 0f, pitchX - 2f * R * _cosPhi );

		// ---------- STRAIGHT WALLS ----------
		float zPreview = MathF.Max( 0f, previewHeight ) * 0.5f;

		if ( straightLengthX > 0f )
		{
			// TOP (inner face at y = +R) — PREVIEW
			_edgePreviews.Add( new EdgePreview
			{
				CenterW = new Vector3( 0f, +R, zPreview ),
				UHatW = new Vector3( 1f, 0f, 0f ),   // +X
				VHatW = new Vector3( 0f, 0f, 1f ),   // +Z
				NormalW = new Vector3( 0f, -1f, 0f ),  // towards center (-Y)
				Width = straightLengthX
			} );

			// BOTTOM (inner face at y = -R) — PREVIEW
			_edgePreviews.Add( new EdgePreview
			{
				CenterW = new Vector3( 0f, -R, zPreview ),
				UHatW = new Vector3( 1f, 0f, 0f ),   // +X
				VHatW = new Vector3( 0f, 0f, 1f ),   // +Z
				NormalW = new Vector3( 0f, +1f, 0f ),  // towards center (+Y)
				Width = straightLengthX
			} );

			// Colliders (BUFFERED) — extend X symmetrically
			CreateWall(
				position: new Vector3( 0f, R + thickness * 0.5f, height * 0.5f ),
				size: new Vector3( straightLengthX + 2f * _seamBuffer, thickness, height ),
				name: "Top Wall"
			);

			CreateWall(
				position: new Vector3( 0f, -R - thickness * 0.5f, height * 0.5f ),
				size: new Vector3( straightLengthX + 2f * _seamBuffer, thickness, height ),
				name: "Bottom Wall"
			);
		}

		// Diagnostics
		int segments = Math.Max( 2, endResolution );
		arcDeltaDegrees = 180f / segments;
		segmentChordLength = 2f * R * MathF.Sin( MathX.DegreeToRadian( arcDeltaDegrees ) * 0.5f );

		// ---------- CURVED ENDS ----------
		CreateStadiumEnd( +1, _phiGoalDeg, _cosPhi ); // right
		CreateStadiumEnd( -1, _phiGoalDeg, _cosPhi ); // left

		// ---------- ABOVE-GOAL COLLIDERS ----------
		CreateAboveGoal( +1 );
		CreateAboveGoal( -1 );

		// ---------- CEILING COLLIDER ----------
		CreateCeiling();

		// Build preview mesh (decoupled, unbuffered)
		BuildPreviewMesh();
	}

	/// <summary>Build one rounded end with trimming to goal edges and shifted centers so X span stays constant.</summary>
	private void CreateStadiumEnd( int xSign, float phiGoalDeg, float cosPhi )
	{
		float R = endRadius;
		if ( R <= 0f ) return;

		// circle center X so arc meets ±pitchX/2
		float cx = xSign * (pitchX * 0.5f - R * cosPhi);
		int segments = Math.Max( 2, endResolution );
		float d = 180f / segments;
		const float eps = 1e-3f;

		if ( phiGoalDeg >= 90f - 1e-4f )
			return;

		float zPreview = MathF.Max( 0f, previewHeight ) * 0.5f;

		for ( int k = 0; k < segments; k++ )
		{
			float A = 90f - k * d;          // upper boundary (deg)
			float B = 90f - (k + 1) * d;    // lower boundary (deg)

			// Endpoint Ys (for gap test)
			float yA = R * MathF.Sin( MathX.DegreeToRadian( A ) );
			float yB = R * MathF.Sin( MathX.DegreeToRadian( B ) );

			float half = goalWidth * 0.5f;
			bool inA = MathF.Abs( yA ) <= half;
			bool inB = MathF.Abs( yB ) <= half;

			// Skip segments fully inside the goal gap
			if ( inA && inB )
				continue;

			// Clamp to goal boundary if one end is inside (UNBUFFERED clamp for preview)
			float Acl = A, Bcl = B;
			if ( inA ^ inB )
			{
				bool useTopBoundary = (inA ? yB : yA) > 0f;
				float boundary = useTopBoundary ? +phiGoalDeg : -phiGoalDeg;
				if ( inA ) Acl = boundary; else Bcl = boundary;
			}

			// --- UNBUFFERED values for PREVIEW ---
			float span0Deg = Acl - Bcl;
			if ( span0Deg <= eps ) continue;

			float span0Rad = MathX.DegreeToRadian( span0Deg );
			float chord0 = 2f * R * MathF.Sin( span0Rad * 0.5f );

			// Preview transform (unbuffered): inner face at radius R
			float mid0 = (Acl + Bcl) * 0.5f;
			float yaw0 = (xSign > 0) ? mid0 : (180f - mid0);
			var rot0 = Rotation.From( new Angles( 0f, yaw0, 0f ) );
			var basePos = new Vector3( cx, 0f, zPreview );
			Vector3 centerPreview = basePos + rot0.Forward * R;

			_edgePreviews.Add( new EdgePreview
			{
				CenterW = centerPreview,
				UHatW = rot0.Right,               // tangent/chord
				VHatW = new Vector3( 0f, 0f, 1f ),// world up
				NormalW = -rot0.Forward,            // inward to pitch (-X face)
				Width = chord0
			} );

			// --- BUFFERED values for COLLIDER ---
			bool goalAtA = Nearly( Acl, +phiGoalDeg, 1e-3f ) || Nearly( Acl, -phiGoalDeg, 1e-3f );
			bool goalAtB = Nearly( Bcl, +phiGoalDeg, 1e-3f ) || Nearly( Bcl, -phiGoalDeg, 1e-3f );
			bool goalAdjacent = goalAtA ^ goalAtB;

			float addLen = goalAdjacent ? _seamBuffer : (2f * _seamBuffer);
			float targetChord = MathF.Min( 2f * R - 1e-4f, chord0 + addLen );
			float span1Rad = 2f * MathF.Asin( MathX.Clamp( targetChord / (2f * R), 0f, 1f ) );
			float span1Deg = MathX.RadianToDegree( span1Rad );
			float dSpanDeg = span1Deg - span0Deg;

			float A1 = Acl, B1 = Bcl;
			if ( goalAdjacent )
			{
				// Keep goal-side fixed; extend away from goal
				if ( goalAtA ) B1 -= dSpanDeg; else A1 += dSpanDeg;
			}
			else
			{
				A1 += dSpanDeg * 0.5f;
				B1 -= dSpanDeg * 0.5f;
			}

			A1 = MathF.Min( +90f, A1 );
			B1 = MathF.Max( -90f, B1 );

			float spanFinalDeg = MathF.Max( 1e-4f, A1 - B1 );
			float spanFinalRad = MathX.DegreeToRadian( spanFinalDeg );
			float chordFinal = 2f * R * MathF.Sin( spanFinalRad * 0.5f );

			// Build collider piece (BUFFERED)
			float mid1 = (A1 + B1) * 0.5f;
			float yaw1 = (xSign > 0) ? mid1 : (180f - mid1);

			var piece = new GameObject( true, xSign > 0 ? "Right End Piece" : "Left End Piece" );
			piece.WorldPosition = new Vector3( cx, 0f, height * 0.5f );
			piece.WorldRotation = Rotation.From( new Angles( 0f, yaw1, 0f ) );

			var bc = piece.Components.Create<BoxCollider>();
			bc.Scale = new Vector3( thickness, chordFinal, height ); // Y = chord (buffered)

			// Push outward so the inner face lies at radius R (mid-plane: R + thickness/2)
			piece.WorldPosition += piece.WorldRotation.Forward * (R + thickness * 0.5f);

			piece.Parent = _root;
			piece.Tags.Add( "Wall" ); // preview only looks at "Wall"; AboveGoal/Ceiling aren't tagged
		}
	}

	private void CreateWall( Vector3 position, Vector3 size, string name )
	{
		var wall = new GameObject( true, name );
		wall.WorldPosition = position;

		var collider = wall.Components.Create<BoxCollider>();
		collider.Scale = size;

		wall.Parent = _root;
		wall.Tags.Add( "Wall" );
	}

	// ---------- Above-goal (one per end), not included in preview ----------
	private void CreateAboveGoal( int xSign )
	{
		float aboveGoalHeight = MathF.Max( 0f, height - goalHeight );
		if ( aboveGoalHeight <= 0f ) return;

		// Place so the INNER face lies exactly on x = ±pitchX/2
		float xPlane = xSign * (pitchX * 0.5f + thickness * 0.5f);

		var go = new GameObject( true, xSign > 0 ? "Above Goal (Right)" : "Above Goal (Left)" );
		go.WorldPosition = new Vector3( xPlane, 0f, goalHeight + aboveGoalHeight * 0.5f );

		var bc = go.Components.Create<BoxCollider>();
		bc.Scale = new Vector3( thickness, pitchY, aboveGoalHeight );

		go.Parent = _root;
		// NOTE: not tagged "Wall" so preview ignores it
	}

	// ---------- Ceiling (single slab), not included in preview ----------
	private void CreateCeiling()
	{
		// Bottom of slab starts at 'height' (so center is height + thickness/2)
		var ceiling = new GameObject( true, "Ceiling" );
		ceiling.WorldPosition = new Vector3( 0f, 0f, height + thickness * 0.5f );

		var bc = ceiling.Components.Create<BoxCollider>();
		bc.Scale = new Vector3( pitchX, pitchY, thickness );

		ceiling.Parent = _root;
		// NOTE: not tagged "Wall" so preview ignores it
	}

	// ---------- PREVIEW MESH (one face per preview edge, independent previewHeight, ignores buffer) ----------
	private void BuildPreviewMesh()
	{
		if ( _previewGo != null && !_previewGo.IsDestroyed )
		{
			_previewGo.Destroy();
			_previewGo = null;
		}
		if ( !previewEnabled || _edgePreviews.Count == 0 )
			return;

		_previewGo = new GameObject( true, "PitchEdgePreview" ) { Parent = GameObject };
		var mr = _previewGo.Components.Create<ModelRenderer>();

		var verts = new List<Vertex>();
		var indices = new List<int>();
		var bmin = new Vector3( float.PositiveInfinity );
		var bmax = new Vector3( float.NegativeInfinity );

		Transform toLocal = _previewGo.WorldTransform; // Transform for world->local
		Rotation invRot = _previewGo.WorldRotation.Inverse;

		foreach ( var e in _edgePreviews )
		{
			AddFaceQuadWorld(
				e.CenterW, e.UHatW, e.VHatW, e.Width, MathF.Max( 0.0001f, previewHeight ), e.NormalW,
				toLocal, invRot,
				verts, indices, ref bmin, ref bmax
			);
		}

		if ( indices.Count == 0 )
			return;

		var mesh = new Mesh( previewMaterial );
		mesh.CreateVertexBuffer<Vertex>( verts.Count, Vertex.Layout, verts.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );
		mesh.Bounds = new BBox( bmin, bmax );

		mr.Model = Model.Builder.AddMesh( mesh ).Create();
	}

	/// <summary>
	/// Build one rectangular face, defined in WORLD space, then convert vertices + directions to the preview's LOCAL space.
	/// </summary>
	private void AddFaceQuadWorld(
		Vector3 centerW, Vector3 uHatW, Vector3 vHatW,
		float width, float height, Vector3 normalW,
		Transform previewWorld, Rotation previewInvWorldRot,
		List<Vertex> verts, List<int> indices,
		ref Vector3 bmin, ref Vector3 bmax )
	{
		uHatW = uHatW.Normal;
		vHatW = vHatW.Normal;

		Vector3 hxW = uHatW * (width * 0.5f);
		Vector3 hzW = vHatW * (height * 0.5f);

		// World-space corners
		Vector3 V0w = centerW - hxW - hzW; // bottom-left
		Vector3 V1w = centerW + hxW - hzW; // bottom-right
		Vector3 V2w = centerW + hxW + hzW; // top-right
		Vector3 V3w = centerW - hxW + hzW; // top-left

		// Convert to preview LOCAL space (avoid double-transform)
		Vector3 v0 = previewWorld.PointToLocal( V0w );
		Vector3 v1 = previewWorld.PointToLocal( V1w );
		Vector3 v2 = previewWorld.PointToLocal( V2w );
		Vector3 v3 = previewWorld.PointToLocal( V3w );

		// Convert directions to preview local (rotation only)
		Vector3 normalLocal = (previewInvWorldRot * normalW).Normal;
		Vector3 tangentLocal = (previewInvWorldRot * uHatW).Normal;

		// Ensure winding matches desired normal
		var nGeom = Vector3.Cross( v1 - v0, v2 - v0 ).Normal;
		bool flip = Vector3.Dot( nGeom, normalLocal ) < 0f;

		// UVs: tile by meters
		float uMax = (uvTileMeters > 0f) ? (width / uvTileMeters) : 1f;
		float vMax = (uvTileMeters > 0f) ? (height / uvTileMeters) : 1f;

		var uv0 = new Vector4( 0f, 0f, 0f, 0f );
		var uv1 = new Vector4( uMax, 0f, 0f, 0f );
		var uv2 = new Vector4( uMax, vMax, 0f, 0f );
		var uv3 = new Vector4( 0f, vMax, 0f, 0f );

		int baseIndex = verts.Count;

		verts.Add( new Vertex( v0, normalLocal, tangentLocal, uv0 ) );
		verts.Add( new Vertex( v1, normalLocal, tangentLocal, uv1 ) );
		verts.Add( new Vertex( v2, normalLocal, tangentLocal, uv2 ) );
		verts.Add( new Vertex( v3, normalLocal, tangentLocal, uv3 ) );

		if ( !flip )
		{
			indices.Add( baseIndex + 0 ); indices.Add( baseIndex + 1 ); indices.Add( baseIndex + 2 );
			indices.Add( baseIndex + 2 ); indices.Add( baseIndex + 3 ); indices.Add( baseIndex + 0 );
		}
		else
		{
			indices.Add( baseIndex + 0 ); indices.Add( baseIndex + 2 ); indices.Add( baseIndex + 1 );
			indices.Add( baseIndex + 0 ); indices.Add( baseIndex + 3 ); indices.Add( baseIndex + 2 );
		}

		// Local bounds
		bmin = Vector3.Min( bmin, v0 ); bmin = Vector3.Min( bmin, v1 ); bmin = Vector3.Min( bmin, v2 ); bmin = Vector3.Min( bmin, v3 );
		bmax = Vector3.Max( bmax, v0 ); bmax = Vector3.Max( bmax, v1 ); bmax = Vector3.Max( bmax, v2 ); bmax = Vector3.Max( bmax, v3 );
	}

	private void ClearGenerated()
	{
		if ( _root != null && !_root.IsDestroyed ) { _root.Destroy(); _root = null; }
		if ( _previewGo != null && !_previewGo.IsDestroyed ) { _previewGo.Destroy(); _previewGo = null; }
	}

	private void MarkDirty() => _dirty = true;
	private static bool Nearly( float a, float b, float eps = 1e-4f ) => MathF.Abs( a - b ) <= eps;
}
