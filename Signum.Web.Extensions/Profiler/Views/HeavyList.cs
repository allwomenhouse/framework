﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using Signum.Utilities;
    using Signum.Entities;
    using Signum.Web;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Configuration;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Caching;
    using System.Web.DynamicData;
    using System.Web.SessionState;
    using System.Web.Profile;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;
    using Signum.Utilities.ExpressionTrees;
    using Signum.Web.Profiler;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Profiler/Views/HeavyList.cshtml")]
    public class _Page_Profiler_Views_HeavyList_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
#line hidden

        public _Page_Profiler_Views_HeavyList_cshtml()
        {
        }
        protected System.Web.HttpApplication ApplicationInstance
        {
            get
            {
                return ((System.Web.HttpApplication)(Context.ApplicationInstance));
            }
        }
        public override void Execute()
        {


WriteLiteral("<h2>");


Write(ViewData[ViewDataKeys.Title]);

WriteLiteral("</h2>\r\n<div>\r\n");


     if (HeavyProfiler.Enabled)
    {
        
   Write(Html.ActionLink("Disable", "Disable"));

                                              
    }
    else
    {
        
   Write(Html.ActionLink("Enable", "Enable"));

                                            
    }

WriteLiteral("    ");


Write(Html.ActionLink("Clean", "Clean"));

WriteLiteral("\r\n    ");


Write(Html.ActionLink("Slowest", "HeavySlowest"));

WriteLiteral("\r\n</div>\r\n");


   Html.RenderPartial("ProfilerTable", (object)Model);


        }
    }
}
