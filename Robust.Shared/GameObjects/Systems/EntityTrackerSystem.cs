using System;
using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects;

public sealed class EntityTrackerSystem : EntitySystem
{
    private EntityQuery<TrackedEntityComponent> _trackedEntityQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrackedEntityComponent, EntityTerminatingEvent>(TrackedOnTrackedEntityTerminating);
        SubscribeLocalEvent<TrackedEntityComponent, TrackedEntityTerminatingEvent>(TrackerOnTrackedEntityTerminating);
        SubscribeLocalEvent<TrackedEntityComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);

        _trackedEntityQuery = GetEntityQuery<TrackedEntityComponent>();
    }

    private void OnGetStateAttempt(Entity<TrackedEntityComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        // This event is only raised when all the trackers are secret.
        // When that happens, we want to prevent networking the component,
        // so that the secret tracking isn't revealed to clients.
        args.Cancelled = true;
    }

    private void TrackedOnTrackedEntityTerminating(Entity<TrackedEntityComponent> ent, ref EntityTerminatingEvent args)
    {
        // The tracked entity is terminating. Inform any trackers.
        var ev = new TrackedEntityTerminatingEvent(ent);

        foreach (var tracker in ent.Comp.Trackers)
        {
            RaiseLocalEvent(tracker, ref ev);
        }

        foreach (var secretTracker in ent.Comp.SecretTrackers)
        {
            RaiseLocalEvent(secretTracker, ref ev);
        }
    }

    private void TrackerOnTrackedEntityTerminating(Entity<TrackedEntityComponent> ent, ref TrackedEntityTerminatingEvent args)
    {
        // An entity we're tracking is terminating. Stop tracking it.
        ent.Comp.Trackers.Remove(args.TerminatingEntity);
        Dirty(ent);

        // Remove the component if we're not tracking anything.
        if (ent.Comp.Trackers.Count == 0)
            RemCompDeferred(ent, ent.Comp);
    }

    public void StartTracking(EntityUid tracker, EntityUid? tracked, bool secret = false)
    {
        if (tracked is not { } trackedEnt)
            return;

        // Record the tracker on the tracked entity so it knows who to talk to.
        AddTracker(tracker, trackedEnt, secret);

        // We also need the tracked entity to track the tracker so it can clean up
        // its reference to it if the tracker is deleted.
        AddTracker(trackedEnt, tracker, secret);
    }

    public void StopTracking(EntityUid tracker, EntityUid? tracked, bool secret = false)
    {
        if (tracked is not { } trackedEnt)
            return;

        RemoveTracker(tracker, trackedEnt, secret);
        RemoveTracker(trackedEnt, tracker, secret);
    }

    private void AddTracker(EntityUid tracker, EntityUid tracked, bool secret)
    {
        var comp = EnsureComp<TrackedEntityComponent>(tracked);

        if (secret)
        {
            comp.SecretTrackers.Add(tracker);
        }
        else
        {
            comp.Trackers.Add(tracker);
            Dirty(tracked, comp);
        }
    }

    private void RemoveTracker(EntityUid tracker, EntityUid tracked, bool secret)
    {
        if (_trackedEntityQuery.TryComp(tracked, out var comp))
        {
            if (secret)
            {
                comp.SecretTrackers.Remove(tracker);
            }
            else
            {
                comp.Trackers.Remove(tracker);
                Dirty(tracked, comp);
            }

            // If nothing is tracking the entity, remove the component.
            if (comp.Trackers.Count == 0 && comp.SecretTrackers.Count == 0)
                RemCompDeferred(tracked, comp);
        }
    }
}

[ByRefEvent]
public readonly record struct TrackedEntityTerminatingEvent(EntityUid TerminatingEntity);
