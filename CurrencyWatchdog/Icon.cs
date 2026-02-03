using Dalamud.Interface.Textures;

namespace CurrencyWatchdog;

public record Icon(uint Id, bool IsHq = false) {
    public ISharedImmediateTexture GetTexture() => Service.TextureProvider.GetFromGameIcon(new GameIconLookup(Id, IsHq));
}