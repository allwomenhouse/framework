﻿using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Web.Extensions.Properties;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using Signum.Entities;
using Signum.Web.Controllers;
using System.Collections.Generic;
using Signum.Engine.Cache;
using Signum.Web.Cache;
using Signum.Entities.Cache;
using Signum.Engine.Maps;

namespace Signum.Web.Cache
{
    public class CacheController : Controller
    {
        public new ActionResult View()
        {
            CachePermissions.ViewCache.Authorize();

            var list = CacheLogic.Statistics();

            ViewData[ViewDataKeys.Title] = "Cache Statistics";

            return View(CacheClient.ViewPrefix.Formato("Statistics"), list);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Enable()
        {
            CachePermissions.ViewCache.Authorize();

            CacheLogic.GloballyDisabled = false;

            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Disable()
        {
            CachePermissions.ViewCache.Authorize();

            CacheLogic.GloballyDisabled = true;

            return null;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Clean()
        {
            CachePermissions.InvalidateCache.Authorize();

            CacheLogic.InvalidateAll();
            Schema.ResetAllLazy(); 
            return null;
        }
    }
}
