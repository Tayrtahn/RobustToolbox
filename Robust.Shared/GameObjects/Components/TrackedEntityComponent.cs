using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrackedEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Trackers = [];

    [DataField(serverOnly: true)]
    public HashSet<EntityUid> SecretTrackers = [];

    // Only network this component if there are non-secret trackers.
    public override bool SessionSpecific => Trackers.Count == 0;
}
