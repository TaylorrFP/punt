using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;

[Title( "ShirtText" )]
[Icon( "palette" )]
[ClassName( "shirttext" )]
public class CircleTextureGenerator : Sandbox.Resources.TextureGenerator
{
	[KeyProperty]
	public Color Color { get; set; } = Color.Magenta;

	public Color BackgroundColor { get; set; } = Color.White;

	[Hide, JsonIgnore]
	public override bool CacheToDisk => true;

	protected override ValueTask<Texture> CreateTexture( Options options, CancellationToken ct )
	{
		var bitmap = new Bitmap( 128, 128 );

		bitmap.SetFill( Color );

		bitmap.Clear( BackgroundColor );
		bitmap.DrawCircle( 64, 64, 40 );

		return ValueTask.FromResult( bitmap.ToTexture() );
	}
}
