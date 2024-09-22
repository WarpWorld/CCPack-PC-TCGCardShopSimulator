using CrowdControl.Common;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs.TCGCardShopSimulator
{

    public class TCGCardShopSimulator : SimpleTCPPack
    {
        public override string Host => "127.0.0.1";

        public override ushort Port => 51337;

        public override ISimpleTCPPack.MessageFormat MessageFormat => ISimpleTCPPack.MessageFormat.CrowdControlLegacy;

        public TCGCardShopSimulator(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

        public override Game Game { get; } = new("TCG Card Shop Simulator", "TCGCardShopSimulator", "PC", ConnectorType.SimpleTCPServerConnector);

        public override EffectList Effects => new List<Effect>
        {
                new Effect("Toggle Lights", "lights") { Description = "Toggle the Shop Lights", Category = "Misc"},
                new Effect("Spawn Customer", "spawn") { Description = "Spawns a New Customer", Category = "Spawn"},
                new Effect("Give $100", "give_100") { Description = "Give the Player $100", Category = "Money"},
                new Effect("Give $1000", "give_1000") { Description = "Give the Player $1000", Category = "Money"},
                new Effect("Give $1000", "give_1000") { Description = "Give The Player $10000", Category = "Money"},
                new Effect("Take $100", "take_100") { Description = "Take away $100 from the Player", Category = "Money"},
                new Effect("Take $1000", "take_1000") { Description = "Take Away $1000 from the Player", Category = "Money"},
                new Effect("Take $1000", "take_1000") { Description = "Take Away $10000 from the Player", Category = "Money"},
        };
    }
}
