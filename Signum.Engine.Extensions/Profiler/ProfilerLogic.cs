﻿using Signum.Engine.Authorization;
using Signum.Entities.Profiler;

namespace Signum.Engine.Profiler;

public static class ProfilerLogic
{
    static Variable<int?> SessionTimeoutVariable = Statics.SessionVariable<int?>("sessionTimeout");
    public static int? SessionTimeout
    {
        get { return SessionTimeoutVariable.Value; }
        set { SessionTimeoutVariable.Value = value; }
    }

    public static void Start(SchemaBuilder sb, bool timeTracker, bool heavyProfiler, bool overrideSessionTimeout)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            if (timeTracker)
                PermissionAuthLogic.RegisterPermissions(ProfilerPermission.ViewTimeTracker);

            if (heavyProfiler)
                PermissionAuthLogic.RegisterPermissions(ProfilerPermission.ViewHeavyProfiler);

            if (overrideSessionTimeout)
                PermissionAuthLogic.RegisterPermissions(ProfilerPermission.OverrideSessionTimeout);
        }
    }

    public static void ProfilerEntries(List<HeavyProfilerEntry> entries)
    {
        HeavyProfiler.ImportEntries(entries, rebaseTime: false);
    }
}
