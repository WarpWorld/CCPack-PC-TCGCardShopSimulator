#if NETSTANDARD1_3_OR_GREATER
using System;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace ConnectorLib.JSON;

/// <summary>The menu presentation of a single custom effect.</summary>
/// <remarks>
/// These fields mirror the per-effect record the Crowd Control API accepts for a custom effect. A
/// game mod sends them with <see cref="CustomEffectsRpc.AddEffects"/>; the client fills in the parts
/// a mod has no business knowing (the game pack ID and the streamer's credentials).
/// </remarks>
#if NETSTANDARD1_3_OR_GREATER
[Serializable]
#endif
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class CustomEffectDefinition
{
    /// <summary>The ID of the effect. Required.</summary>
    /// <remarks>May only contain letters, numbers, and underscores, and is limited to 150 characters.</remarks>
    public string effectID = string.Empty;

    /// <summary>The name shown in the menu. Required.</summary>
    public string name = string.Empty;

    /// <summary>The default price in coins. Required.</summary>
    /// <remarks>The API clamps this to a minimum of one coin.</remarks>
    public long price;

    /// <summary>The menu description.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? description;

    /// <summary>The category path the effect appears under.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string[]? category;

    /// <summary>The groups the effect belongs to.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string[]? group;

    /// <summary>Freeform tags. Tags prefixed with a double underscore are conventionally machine-readable.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string[]? tags;

    /// <summary>The name of the image to display for the effect.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? image;

    /// <summary>The duration of the effect. Omitted entirely for non-timed effects.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Duration? duration;

    /// <summary>The allowed quantity range, if the effect is a quantity effect.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Range? quantity;

    /// <summary>The effect's moral and chaotic leanings, used by the client to sort and theme it.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Alignment? alignment;

    /// <summary>The cooldown applied to the whole session after the effect fires, in seconds.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float? sessionCooldown;

    /// <summary>The cooldown applied per viewer after the effect fires, in seconds.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float? userCooldown;

    /// <summary>The parameters offered to the viewer when purchasing the effect.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, Parameter>? parameters;

    /// <summary>True if the effect should not appear in the menu at all.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? hidden;

    /// <summary>True if the effect should appear but not be purchasable.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? inactive;

    /// <summary>A note shown to the streamer.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? note;

    /// <summary>The ID of a pack effect to inherit unset properties from.</summary>
    /// <remarks>
    /// Resolved when the effect is saved and not stored afterwards, so this is a convenience for
    /// filling in price, duration, and image from an existing effect - not a routing mechanism.
    /// Anything the mod needs at runtime has to travel in <see cref="args"/>.
    /// </remarks>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? clone;

    /// <summary>Additional data delivered to the connector as <see cref="EffectRequest.arguments"/>.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, object?>? args;

    /// <summary>A duration, with an optional lock preventing the streamer from changing it.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class Duration
    {
        /// <summary>The duration in seconds. Must be greater than zero.</summary>
        public long value;

        /// <summary>True if the streamer may not change the duration.</summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? immutable;

        /// <summary>Creates a new instance of the <see cref="Duration"/> class.</summary>
        public Duration() { }

        /// <inheritdoc cref="Duration()"/>
        /// <param name="value">The duration in seconds.</param>
        /// <param name="immutable">True if the streamer may not change the duration.</param>
        public Duration(long value, bool? immutable = null)
        {
            this.value = value;
            this.immutable = immutable;
        }
    }

    /// <summary>An inclusive integer range.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class Range
    {
        /// <summary>The lowest permitted value.</summary>
        public long min;

        /// <summary>The highest permitted value.</summary>
        public long max;

        /// <summary>Creates a new instance of the <see cref="Range"/> class.</summary>
        public Range() { }

        /// <inheritdoc cref="Range()"/>
        /// <param name="min">The lowest permitted value.</param>
        /// <param name="max">The highest permitted value.</param>
        public Range(long min, long max)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>Where an effect sits on the orderly/chaotic and good/evil axes.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class Alignment
    {
        /// <summary>Chaotic (-1) to orderly (+1).</summary>
        public float orderliness;

        /// <summary>Evil (-1) to good (+1).</summary>
        public float morality;

        /// <summary>Creates a new instance of the <see cref="Alignment"/> class.</summary>
        public Alignment() { }

        /// <inheritdoc cref="Alignment()"/>
        /// <param name="orderliness">Chaotic (-1) to orderly (+1).</param>
        /// <param name="morality">Evil (-1) to good (+1).</param>
        public Alignment(float orderliness, float morality)
        {
            this.orderliness = orderliness;
            this.morality = morality;
        }
    }

    /// <summary>A viewer-supplied parameter on an effect.</summary>
#if NETSTANDARD1_3_OR_GREATER
    [Serializable]
#endif
    public class Parameter
    {
        /// <summary>The label shown to the viewer.</summary>
        public string name = string.Empty;

        /// <summary>The parameter kind. Either <c>options</c> or <c>hex-color</c>.</summary>
        public string type = TYPE_OPTIONS;

        /// <summary>The selectable values, for an <c>options</c> parameter.</summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Option>? options;

        /// <summary>A parameter offering the viewer a list of choices.</summary>
        public const string TYPE_OPTIONS = "options";

        /// <summary>A parameter asking the viewer for a colour.</summary>
        public const string TYPE_HEX_COLOR = "hex-color";

        /// <summary>A single selectable value on an <c>options</c> parameter.</summary>
#if NETSTANDARD1_3_OR_GREATER
        [Serializable]
#endif
        public class Option
        {
            /// <summary>The label shown to the viewer.</summary>
            public string name = string.Empty;

            /// <summary>The sort priority within the option list.</summary>
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float? priority;

            /// <summary>Creates a new instance of the <see cref="Option"/> class.</summary>
            public Option() { }

            /// <inheritdoc cref="Option()"/>
            /// <param name="name">The label shown to the viewer.</param>
            /// <param name="priority">The sort priority within the option list.</param>
            public Option(string name, float? priority = null)
            {
                this.name = name;
                this.priority = priority;
            }
        }
    }
}
