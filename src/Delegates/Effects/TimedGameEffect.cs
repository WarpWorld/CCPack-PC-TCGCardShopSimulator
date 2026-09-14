#nullable disable
using ConnectorLib.JSON;

namespace CrowdControl.Delegates.Effects;

/// <summary>
/// Base class for this pack's timed effects. The scheduler owns the countdown, pause/resume and
/// conflict handling; this class just bridges it to the apply / revert / tick logic in
/// <see cref="Timed"/> (TimedEffects.cs), which predates the scheduler.
/// </summary>
public abstract class TimedGameEffect(CrowdControlMod mod, NetworkClient client) : Effect(mod, client)
{
    private readonly Dictionary<uint, Timed> m_active = new();

    /// <summary>Validates the game state for this request. Return anything other than Success to refuse it.</summary>
    protected virtual EffectResponse Precheck(EffectRequest request) => EffectResponse.Success(request.ID);

    /// <summary>The timed behaviour to apply for this request, or null if the precheck already did all the work.</summary>
    protected abstract TimedType? ResolveType(EffectRequest request);

    public override EffectResponse Start(EffectRequest request)
    {
        EffectResponse response = Precheck(request);
        if (response.status != EffectStatus.Success) return response;

        TimedType? type = ResolveType(request);
        if (type != null)
        {
            Timed timed = new(type.Value);
            timed.addEffect();
            m_active[request.ID] = timed;
        }
        return response;
    }

    public override EffectResponse Tick(EffectRequest request)
    {
        if (m_active.TryGetValue(request.ID, out Timed timed)) timed.tick();
        return null;
    }

    public override EffectResponse Stop(EffectRequest request)
    {
        if (m_active.TryGetValue(request.ID, out Timed timed))
        {
            m_active.Remove(request.ID);
            Timed.removeEffect(timed.type);
        }
        return EffectResponse.Finished(request.ID);
    }
}
