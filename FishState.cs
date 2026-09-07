namespace OkunGame
{
    /// <summary>
    /// Base class for all fish behavior states (Idle/Swimming, Panic, Resisting, Captured).
    /// Each state owns its own Enter/Update/Exit logic so new states (or entirely new fish
    /// behaviors) can be added without touching the Fish class itself. This is the same
    /// pattern the future Weapon base class will use for harpoon/drill/etc.
    /// </summary>
    public abstract class FishState
    {
        public abstract void Enter(Fish fish);
        public abstract void Update(Fish fish, in FishWorldContext ctx, float dt);
        public abstract void Exit(Fish fish);
    }
}