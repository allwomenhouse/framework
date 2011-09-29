﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.237
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
    using Signum.Entities.DynamicQuery;
    using Signum.Entities.Reflection;
    using Signum.Engine.DynamicQuery;
    using Signum.Web.Properties;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MvcRazorClassGenerator", "1.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Signum/Views/FilterBuilder.cshtml")]
    public class _Page_Signum_Views_FilterBuilder_cshtml : System.Web.Mvc.WebViewPage<Context>
    {


        public _Page_Signum_Views_FilterBuilder_cshtml()
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






   
    List<FilterOption> filterOptions = (List<FilterOption>)ViewData[ViewDataKeys.FilterOptions];
    QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];


WriteLiteral("\r\n<div class=\"ui-widget-content ui-corner-bottom sf-filters-list\">\r\n    <span cla" +
"ss=\"sf-explanation\" style=\"");


                                    Write((filterOptions == null || filterOptions.Count == 0) ? "" : "display:none;");

WriteLiteral("\">");


                                                                                                                  Write(Resources.NoFiltersSpecified);

WriteLiteral("</span>\r\n    <table id=\"");


          Write(Model.Compose("tblFilters"));

WriteLiteral("\" style=\"");


                                                Write((filterOptions == null || filterOptions.Count == 0) ? "display:none;" : "");

WriteLiteral("\">\r\n        <thead>\r\n            <tr>\r\n                <th>\r\n                </th" +
">\r\n                <th class=\"sf-filter-field-header\">");


                                              Write(Resources.Field);

WriteLiteral("\r\n                </th>\r\n                <th>");


               Write(Resources.Operation);

WriteLiteral("\r\n                </th>\r\n                <th>");


               Write(Resources.Value);

WriteLiteral("\r\n                </th>\r\n            </tr>\r\n        </thead>\r\n        <tbody>\r\n");


             for (int i = 0; i < filterOptions.Count; i++)
            {
                FilterOption filter = filterOptions[i];
                
           Write(Html.NewFilter(queryDescription.QueryName, filter, Model, i));

                                                                             
            }

WriteLiteral("        </tbody>\r\n    </table>\r\n</div>");


        }
    }
}
