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
    using Signum.Engine;
    using Signum.Entities.Authorization;
    using Signum.Web.Auth;
    using Signum.Web.Extensions.Properties;
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
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/AuthAdmin/Views/Properties.cshtml")]
    public class _Page_AuthAdmin_Views_Properties_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
#line hidden

        public _Page_AuthAdmin_Views_Properties_cshtml()
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

Write(Html.DynamicCss("~/authAdmin/Content/authAdmin.css"));

WriteLiteral("\r\n");


 using (var tc = Html.TypeContext<PropertyRulePack>())
{
    
Write(Html.EntityLine(tc, f => f.Role));

                                     ;
    
Write(Html.ValueLine(tc, f => f.DefaultRule, vl => { vl.UnitText = tc.Value.DefaultLabel; }));

                                                                                           ;
    
Write(Html.EntityLine(tc, f => f.Type));

                                     ;


WriteLiteral("    <table class=\"ruleTable\" id=\"properties\">\r\n        <thead>\r\n            <tr>\r" +
"\n                <th>\r\n                    ");


               Write(Resources.PropertiesAscx_Property);

WriteLiteral("\r\n                </th>\r\n                <th>\r\n                    ");


               Write(Resources.PropertiesAscx_Modify);

WriteLiteral("\r\n                </th>\r\n                <th>\r\n                    ");


               Write(Resources.PropertiesAscx_Read);

WriteLiteral("\r\n                </th>\r\n                <th>\r\n                    ");


               Write(Resources.PropertiesAscx_None);

WriteLiteral("\r\n                </th>\r\n                <th>\r\n                    ");


               Write(Resources.PropertiesAscx_Overriden);

WriteLiteral("\r\n                </th>\r\n            </tr>\r\n        </thead>\r\n");


         foreach (var item in tc.TypeElementContext(p => p.Rules))
        {

WriteLiteral("            <tr>\r\n                <td>\r\n                    ");


               Write(Html.Span(null, item.Value.Resource.Path));

WriteLiteral("\r\n                    ");


               Write(Html.Hidden(item.Compose("Resource_Path"), item.Value.Resource.Path));

WriteLiteral("\r\n                    ");


               Write(Html.Hidden(item.Compose("AllowedBase"), item.Value.AllowedBase));

WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    <a class=\"cbLi" +
"nk modify\">\r\n                        ");


                   Write(Html.RadioButton(item.Compose("Allowed"), "Modify", item.Value.Allowed == PropertyAllowed.Modify));

WriteLiteral("\r\n                    </a>\r\n                </td>\r\n                <td>\r\n        " +
"            <a class=\"cbLink read\">\r\n                        ");


                   Write(Html.RadioButton(item.Compose("Allowed"), "Read", item.Value.Allowed == PropertyAllowed.Read));

WriteLiteral("\r\n                    </a>\r\n                </td>\r\n                <td>\r\n        " +
"            <a class=\"cbLink none\">\r\n                        ");


                   Write(Html.RadioButton(item.Compose("Allowed"), "None", item.Value.Allowed == PropertyAllowed.None));

WriteLiteral("\r\n                    </a>\r\n                </td>\r\n                <td>\r\n        " +
"            ");


               Write(Html.CheckBox(item.Compose("Overriden"), item.Value.Overriden, new { disabled = "disabled", @class = "overriden" }));

WriteLiteral("\r\n                </td>\r\n            </tr>\r\n");


        }

WriteLiteral("    </table>\r\n");


}
WriteLiteral(" ");


        }
    }
}
