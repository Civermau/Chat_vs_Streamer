public partial class Action
{
    public ActionType actionType;
    public Others others;
    public string name;

    public Action(ActionType actionType, Others? others = null)
    {
        this.actionType = actionType;
        this.others = others ?? default;
        this.name = null;
    }

    public void runAction()
    {
        switch (actionType)
        {
            case ActionType.GiveItem:
                // GiveRandomItem();
                break;
            case ActionType.RemoveItem:
                // RemoveRandomItemFromInventory();
                break;
            case ActionType.AlterStats:
                break;
            case ActionType.SpawnSomething:
                break;
            case ActionType.Teleport:
                break;
            case ActionType.Others:
                switch (others)
                {
                    case Others.TurnPlayerIntoAFish:
                        TurnPlayerIntoAFish();
                        break;
                    case Others.GiveRandomItem:
                        GiveRandomItem();
                        break;
                    case Others.RemoveRandomItemFromInventory:
                        RemoveRandomItemFromInventory();
                        break;
                    case Others.DehydratePlayer:
                        DehydratePlayer();
                        break;
                    case Others.HydratePlayer:
                        HydratePlayer();
                        break;
                    case Others.StarvePlayer:
                        StarvePlayer();
                        break;
                    case Others.FeedPlayer:
                        FeedPlayer();
                        break;
                    case Others.WipeInventory:
                        WipeInventory();
                        break;
                }
                break;
        }
    }
}
