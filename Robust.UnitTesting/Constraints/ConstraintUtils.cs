using System;
using System.Collections;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

internal static class ConstraintUtils
{
    public static EntityUid GetEntityUid(object? actual)
    {
        EntityUid? uid = null;
        if (actual is EntityUid ent)
            uid = ent;
        else
        {
            var field = actual?.GetType().GetField("Owner") ?? actual?.GetType().GetField("Item1");
            if (field is not null)
            {
                uid = (EntityUid?)field.GetValue(actual);
            }
        }

        if (uid is null)
            throw new ArgumentException($"Expected EntityUid or Entity but was {actual?.GetType()}");
        return uid.Value;
    }
}
