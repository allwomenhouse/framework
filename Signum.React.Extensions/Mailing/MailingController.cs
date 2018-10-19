﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.Engine.Cache;
using Signum.Engine;
using Signum.Entities.Cache;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using System.Threading;
using Signum.React.ApiControllers;
using Signum.Engine.Basics;
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;

namespace Signum.React.Mailing
{
    public class MailingController : ApiController
    {
        [HttpGet("api/asyncEmailSender/view")]
        public AsyncEmailSenderState View()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderState state = AsyncEmailSenderLogic.ExecutionState();

            return state;
        }

        [HttpPost("api/asyncEmailSender/start")]
        public void Start()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.StartRunningEmailSenderAsync(0);

            Thread.Sleep(1000);
        }

        [HttpPost("api/asyncEmailSender/stop")]
        public void Stop()
        {
            AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel.AssertAuthorized();

            AsyncEmailSenderLogic.Stop();

            Thread.Sleep(1000);
        }



#pragma warning disable IDE1006 // Naming Styles
        public class CreateEmailRequest
        {
            public Lite<EmailTemplateEntity> template { get; set; }
            public Lite<Entity> lite { get; set; }
            public ModifiableEntity entity { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

        [HttpPost("api/email/constructorType")]
        public string GetConstructorType([FromBody]SystemEmailEntity systemEmailTemplate)
        {
            var type = SystemEmailLogic.GetEntityType(systemEmailTemplate.ToType());

            return ReflectionServer.GetTypeName(type);
        }

        [HttpPost("api/email/emailTemplates")]
        public List<Lite<EmailTemplateEntity>> GetEmailTemplates(string queryKey, EmailTemplateVisibleOn visibleOn, [FromBody]Lite<Entity> lite)
        {
            object queryName = QueryLogic.ToQueryName(queryKey);

            var entity = lite?.RetrieveAndForget();

            return EmailTemplateLogic.GetApplicableEmailTemplates(queryName, entity, visibleOn);
        }
    }
}