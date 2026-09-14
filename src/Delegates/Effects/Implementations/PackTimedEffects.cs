#nullable disable
using ConnectorLib.JSON;

namespace CrowdControl.Delegates.Effects.Implementations;

// Timed effects. The precheck (GameActions.X) validates the game state; the timed behaviour itself lives in
// TimedEffects.cs (Timed.addEffect / removeEffect / tick), driven by the scheduler through TimedGameEffect.

[Effect(["forcemath"], 30f, ["forcemath"])]
public class ForceMathEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.ForceMath(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.FORCE_MATH;
}

[Effect(["forcepayment_cash", "forcepayment_card"], 60f, ["forcepayment_cash", "forcepayment_card"])]
public class ForcePaymentTypeEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.ForcePaymentType(req);
    protected override TimedType? ResolveType(EffectRequest req) => req.code == "forcepayment_cash" ? TimedType.FORCE_CASH : TimedType.FORCE_CARD;
}

[Effect(["largebills"], 60f, ["largebills"])]
public class LargeBillsEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.LargeBills(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.FORCE_LARGE_BILLS;
}

[Effect(["exactchange"], 60f, ["exactchange", "forcepayment_cash"])]
public class ExactChangeEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.ExactChange(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.FORCE_EXACT_CHANGE;
}

[Effect(["invertx"], 30f, ["invertx"])]
public class InvertXEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.InvertX(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.INVERT_X;
}

[Effect(["inverty"], 30f, ["inverty"])]
public class InvertYEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.InvertY(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.INVERT_Y;
}

[Effect(["highfov"], 30f, ["highfov", "lowfov"])]
public class HighFOVEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.HighFOV(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.HIGH_FOV;
}

[Effect(["lowfov"], 30f, ["highfov", "lowfov"])]
public class LowFOVEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.LowFOV(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.LOW_FOV;
}

[Effect(["language_english", "language_french", "language_german", "language_italian", "language_spanish", "language_chineset", "language_chineses", "language_korean", "language_thai"], 60f, ["language_english", "language_french", "language_german", "language_italian", "language_spanish", "language_chineset", "language_chineses", "language_korean", "language_thai"])]
public class SetLanguageEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.SetLanguage(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.SET_LANGUAGE;
}

[Effect(["player_fast", "player_slow"], 30f, ["player_fast", "player_slow"])]
public class PlayerSpeedEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.PlayerSpeed(req);
    protected override TimedType? ResolveType(EffectRequest req) => req.code == "player_fast" ? TimedType.PLAYER_FAST : TimedType.PLAYER_SLOW;
}

[Effect(["lowgravity"], 30f, ["lowgravity"])]
public class LowGravityEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.LowGravity(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.LOW_GRAVITY;
}

[Effect(["slowmo", "fastforward"], 20f, ["slowmo", "fastforward"])]
public class GameSpeedEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.GameSpeed(req);
    protected override TimedType? ResolveType(EffectRequest req) => req.code == "slowmo" ? TimedType.GAME_SLOW : TimedType.GAME_FAST;
}

[Effect(["hypercustomers"], 30f, ["hypercustomers"])]
public class HyperCustomersEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.HyperCustomers(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.HYPER_CUSTOMERS;
}

[Effect(["mute"], 30f, ["mute"])]
public class MuteAudioEffect(CrowdControlMod mod, NetworkClient client) : TimedGameEffect(mod, client)
{
    protected override EffectResponse Precheck(EffectRequest req) => GameActions.MuteAudio(req);
    protected override TimedType? ResolveType(EffectRequest req) => TimedType.MUTE_AUDIO;
}
