﻿
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { Lite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { ScheduledTaskLogEntity, ScheduledTaskEntity, ScheduleRuleMinutelyEntity, ScheduleRuleMonthsEntity, 
    ScheduleRuleWeekDaysEntity, HolidayCalendarEntity, SchedulerPermission } from './Signum.Entities.Scheduler'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";


export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/scheduler/view" onImportModule={() => _import("./SchedulerPanelPage")} />);

    Navigator.addSettings(new EntitySettings(ScheduledTaskEntity, e => _import('./Templates/SchedulerTask')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMinutelyEntity, e => _import('./Templates/ScheduleRuleMinutely')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleWeekDaysEntity, e => _import('./Templates/ScheduleRuleWeekDays')));
    Navigator.addSettings(new EntitySettings(ScheduleRuleMonthsEntity, e => _import('./Templates/ScheduleRuleMonths')));
    Navigator.addSettings(new EntitySettings(HolidayCalendarEntity, e => _import('./Templates/HolidayCalendar')));
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(SchedulerPermission.ViewSchedulerPanel),
        key: "SchedulerPanel",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("~/scheduler/view"))
    });
}


export module API {

    export function start(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/scheduler/start" }, undefined);
    }

    export function stop(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/scheduler/stop" }, undefined);
    }

    export function view(): Promise<SchedulerState> {
        return ajaxGet<SchedulerState>({ url: "~/api/scheduler/view" });
    }
}


export interface SchedulerState
{
    Running: boolean;
    SchedulerMargin: string;
    NextExecution: string;
    Queue: SchedulerItemState[];
}

export interface SchedulerItemState
{
    ScheduledTask: Lite<ScheduledTaskEntity>;
    Rule: string;
    NextExecution: string;
}
