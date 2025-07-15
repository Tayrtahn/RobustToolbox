using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Audio;

[Prototype]
public sealed partial class SoundCollectionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Are the files in this collection allowed to have more than one channel?
    /// </summary>
    /// <remarks>
    /// This should be left false for collections that can be played positionally,
    /// because playing a stereo audio file positionally causes a clientside error.
    /// Set this to true for audio that is only played globally, like lobby music.
    /// </remarks>
    [DataField]
    public bool AllowStereo;

    [DataField("files")]
    public List<ResPath> PickFiles { get; private set; } = new();
}
