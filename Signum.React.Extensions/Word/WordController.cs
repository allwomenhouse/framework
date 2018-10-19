﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Signum.Entities.Word;
using Signum.Engine.Word;
using System.Web;
using Signum.React.Files;
using System.IO;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.Word
{
    public class WordController : ApiController
    {
        [HttpPost("api/word/createReport")]
        public FileStreamResult View([FromBody]CreateWordReportRequest request)
        {
            var template = request.template.Retrieve();
            var model = request.entity ?? request.lite.Retrieve();

            var bytes = template.CreateReport(model);
            
            return FilesController.GetFileStreamResult(new MemoryStream(bytes), template.FileName);            
        }

#pragma warning disable IDE1006 // Naming Styles
        public class CreateWordReportRequest
        {
            public Lite<WordTemplateEntity> template { get; set; }
            public Lite<Entity> lite { get; set; }
            public ModifiableEntity entity { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

        [HttpPost("api/word/constructorType")]
        public string GetConstructorType([FromBody]SystemWordTemplateEntity systemWordTemplate)
        {
            var type = SystemWordTemplateLogic.GetEntityType(systemWordTemplate.ToType());

            return ReflectionServer.GetTypeName(type);
        }

        [HttpPost("api/word/wordTemplates")]
        public List<Lite<WordTemplateEntity>> GetWordTemplates(string queryKey, WordTemplateVisibleOn visibleOn, [FromBody]Lite<Entity> lite)
        {
            object type = QueryLogic.ToQueryName(queryKey);

            var entity = lite?.RetrieveAndForget();

            return WordTemplateLogic.GetApplicableWordTemplates(type, entity, visibleOn);
        }

    
    }
}