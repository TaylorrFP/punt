using Sandbox;
using Sandbox.Resources;


public sealed class TestTextureGenerator : Component
{

	[Property] public ModelRenderer Model { get; set; }
	[Property] public Texture PlayerName { get; set; }
	[Property] public Texture PlayerNumberBack { get; set; }
	[Property] public Texture FrontSponsor { get; set; }
	[Property] public Texture Badge { get; set; }
	[Property] public Texture ShortsNumber { get; set; }

	[Property] public Color PrimaryColour { get; set; }

	[Property] public Color SecondaryColour { get; set; }

	[Property] public Color TertiaryColour { get; set; }

	protected override void OnStart()
	{




		

		Model.SceneObject.Attributes.Set( "playername", PlayerName );
		Model.SceneObject.Attributes.Set("playernumberback", PlayerNumberBack );
		Model.SceneObject.Attributes.Set( "frontsponsor", FrontSponsor );
		Model.SceneObject.Attributes.Set( "badge", Badge );
		Model.SceneObject.Attributes.Set( "shortsnumber", ShortsNumber );


		Model.SceneObject.Attributes.Set( "primarycolor", PrimaryColour );
		Model.SceneObject.Attributes.Set( "secondarycolor", SecondaryColour );
		Model.SceneObject.Attributes.Set( "tertiarycolor", TertiaryColour );

		base.OnStart();
	}


	protected override void OnUpdate()
	{


		Model.SceneObject.Attributes.Set( "playername", PlayerName );
		Model.SceneObject.Attributes.Set( "playernumberback", PlayerNumberBack );
		Model.SceneObject.Attributes.Set( "frontsponsor", FrontSponsor );
		Model.SceneObject.Attributes.Set( "badge", Badge );
		Model.SceneObject.Attributes.Set( "shortsnumber", ShortsNumber );

		Model.SceneObject.Attributes.Set( "primarycolor", PrimaryColour );
		Model.SceneObject.Attributes.Set( "secondarycolor", SecondaryColour );
		Model.SceneObject.Attributes.Set( "tertiarycolor", TertiaryColour );

	}
}
