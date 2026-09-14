#if NETSTANDARD1_3_OR_GREATER
using System;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ConnectorLib.JSON;

/// <summary>Builds the RPC calls that add and remove a game mod's custom effects.</summary>
/// <remarks>
/// Custom effects are registered over the existing RPC channel rather than a message type of their
/// own. The client relays these calls to the Crowd Control API, which is where the streamer's
/// credentials and the active game pack ID live - a game mod knows neither.
/// <para>
/// The game pack must have <c>allowCustomEffects</c> set, or the call is rejected.
/// </para>
/// </remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class CustomEffectsRpc
{
    /// <summary>The RPC method that registers custom effects.</summary>
    public const string METHOD_ADD = "addEffects";

    /// <summary>The RPC method that deletes custom effects.</summary>
    public const string METHOD_REMOVE = "removeEffect";

    /// <summary>Creates a call registering the supplied custom effects.</summary>
    /// <param name="effects">The effects to register.</param>
    /// <param name="preserveExisting">
    /// True to treat these definitions as defaults and leave any effect the streamer already has
    /// untouched. A mod re-registers the same effects on every launch, so without this the streamer's
    /// prices would be reset each session.
    /// </param>
    /// <returns>An RPC request to send to the client.</returns>
    /// <remarks>
    /// Registering an effect does not make it visible: it has to be reported with
    /// <see cref="EffectStatus.Visible"/> for the session as well.
    /// </remarks>
    public static RpcRequest AddEffects(IEnumerable<CustomEffectDefinition> effects, bool preserveExisting = true) => new()
    {
        method = METHOD_ADD,
        target = RpcTarget.Server,
        args = [new AddEffectsArgs { effects = [.. effects], preserveExisting = preserveExisting }]
    };

    /// <summary>Creates a call deleting the supplied custom effects.</summary>
    /// <param name="effectIDs">The IDs of the effects to delete.</param>
    /// <returns>An RPC request to send to the client.</returns>
    /// <remarks>
    /// This throws away the streamer's price and other settings for those effects along with the
    /// effects themselves, so it is only ever appropriate when the streamer has asked for it. An
    /// effect that is merely unavailable this session should be hidden instead.
    /// </remarks>
    public static RpcRequest RemoveEffects(IEnumerable<string> effectIDs) => new()
    {
        method = METHOD_REMOVE,
        target = RpcTarget.Server,
        args = [new RemoveEffectsArgs { effects = [.. effectIDs] }]
    };

    /// <summary>The argument object for an <see cref="METHOD_ADD"/> call.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class AddEffectsArgs
    {
        /// <summary>The effects to register.</summary>
        public List<CustomEffectDefinition> effects = new();

        /// <summary>True to leave effects that already exist untouched.</summary>
        public bool preserveExisting = true;
    }

    /// <summary>The argument object for a <see cref="METHOD_REMOVE"/> call.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class RemoveEffectsArgs
    {
        /// <summary>The IDs of the effects to delete.</summary>
        public List<string> effects = new();
    }
}
