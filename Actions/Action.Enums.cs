public partial class Action
{
    public enum ActionType
    {
        GiveItem,
        RemoveItem,
        AlterStats,
        SpawnSomething,
        Teleport,
        Others
    }

    public enum Others
    {
        TurnPlayerIntoAFish,
        GiveRandomItem,
        RemoveRandomItemFromInventory, 
        WipeInventory,
        DehydratePlayer,
        HydratePlayer,
        StarvePlayer,
        FeedPlayer
    }
}