﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18051
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Signum.Web.Extensions.Translation.Views
{
    using System;
    using System.Collections.Generic;
    
    #line 2 "..\..\Translation\Views\View.cshtml"
    using System.Globalization;
    
    #line default
    #line hidden
    using System.IO;
    using System.Linq;
    using System.Net;
    
    #line 3 "..\..\Translation\Views\View.cshtml"
    using System.Reflection;
    
    #line default
    #line hidden
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using Signum.Entities;
    
    #line 6 "..\..\Translation\Views\View.cshtml"
    using Signum.Entities.Translation;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Translation\Views\View.cshtml"
    using Signum.Utilities;
    
    #line default
    #line hidden
    using Signum.Web;
    
    #line 5 "..\..\Translation\Views\View.cshtml"
    using Signum.Web.Translation.Controllers;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Translation/Views/View.cshtml")]
    public partial class View : System.Web.Mvc.WebViewPage<Dictionary<CultureInfo, LocalizedAssembly>>
    {
        public View()
        {
        }
        public override void Execute()
        {







            
            #line 7 "..\..\Translation\Views\View.cshtml"
  
    CultureInfo culture = ViewBag.Culture;
    CultureInfo defaultCulture = ViewBag.DefaultCulture;
    Assembly assembly = ViewBag.Assembly;

    ViewBag.Title = TranslationMessage.View0In1.NiceToString().Formato(assembly.GetName().Name, culture == null ? TranslationMessage.AllLanguages.NiceToString() : culture.DisplayName);


    Func<CultureInfo, bool> editCulture = c => culture == null || culture.Name == c.Name;

    Func<LocalizedType, string> locKey = lt => lt.Type.Name + "." + lt.Assembly.Culture.Name;

    var defaultLocAssembly = Model.GetOrThrow(defaultCulture);


            
            #line default
            #line hidden

            
            #line 21 "..\..\Translation\Views\View.cshtml"
Write(Html.ScriptCss("~/Translation/Content/Translation.css"));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 22 "..\..\Translation\Views\View.cshtml"
Write(Html.ScriptsJs("~/Translation/Content/Translation.js"));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n\r\n");


            
            #line 25 "..\..\Translation\Views\View.cshtml"
 if (defaultLocAssembly.Types.IsEmpty())
{

            
            #line default
            #line hidden
WriteLiteral("    <h2>");


            
            #line 27 "..\..\Translation\Views\View.cshtml"
   Write(TranslationMessage.NothingToTranslateIn0.NiceToString().Formato(assembly.GetName().Name));

            
            #line default
            #line hidden
WriteLiteral("</h2>\r\n");


            
            #line 28 "..\..\Translation\Views\View.cshtml"
}
else
{

            
            #line default
            #line hidden
WriteLiteral("    <h2>");


            
            #line 31 "..\..\Translation\Views\View.cshtml"
   Write(ViewBag.Title);

            
            #line default
            #line hidden
WriteLiteral("</h2>\r\n");


            
            #line 32 "..\..\Translation\Views\View.cshtml"

    using (Html.BeginForm())
    {

            
            #line default
            #line hidden
WriteLiteral("        <table id=\"results\" style=\"width: 100%; margin: 0px\" class=\"st\" pluralAnd" +
"Gender=\"");


            
            #line 35 "..\..\Translation\Views\View.cshtml"
                                                                                    Write(Url.Action((TranslationController tc) => tc.PluralAndGender()));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 36 "..\..\Translation\Views\View.cshtml"
             foreach (var defaultLocType in defaultLocAssembly.Types.Values)
            {
                bool hasDescription = defaultLocType.Options.IsSet(DescriptionOptions.Description);

                bool hasPlural = defaultLocType.Options.IsSet(DescriptionOptions.PluralDescription);

                bool hasGenderOption = defaultLocType.Options.IsSet(DescriptionOptions.Gender);

                bool hasMembers = defaultLocType.Options.IsSet(DescriptionOptions.Members);  
                  

            
            #line default
            #line hidden
WriteLiteral("                <thead>\r\n                    <tr>\r\n                        <th cl" +
"ass=\"leftCell\">");


            
            #line 48 "..\..\Translation\Views\View.cshtml"
                                        Write(TranslationMessage.Type.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                        <th colspan=\"4\" class=\"titleCell\">");


            
            #line 49 "..\..\Translation\Views\View.cshtml"
                                                     Write(defaultLocType.Type.Name);

            
            #line default
            #line hidden
WriteLiteral(" (");


            
            #line 49 "..\..\Translation\Views\View.cshtml"
                                                                                 Write("/".Combine(hasDescription ? "Singular" : null, hasPlural ? "Plural" : null, hasGenderOption ? "Gender" : null, hasMembers ? "Members" : null));

            
            #line default
            #line hidden
WriteLiteral(")</th>\r\n                    </tr>\r\n                </thead>\r\n");


            
            #line 52 "..\..\Translation\Views\View.cshtml"
            
                if (defaultLocType.Options.IsSet(DescriptionOptions.Description))
                {
                    foreach (var locType in Model.Values.Select(la => la.Types[defaultLocType.Type]).Where(lt => editCulture(lt.Assembly.Culture) || lt.Description.HasText()))
                    {
                        bool hasGender = hasGenderOption && NaturalLanguageTools.HasGenders(locType.Assembly.Culture);


            
            #line default
            #line hidden
WriteLiteral("                <tr>\r\n                    <th class=\"leftCell\">");


            
            #line 60 "..\..\Translation\Views\View.cshtml"
                                    Write(locType.Assembly.Culture.Name);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                    <th class=\"smallCell monospaceCell\">\r\n");


            
            #line 62 "..\..\Translation\Views\View.cshtml"
                         if (hasGender)
                        {
                            if (editCulture(locType.Assembly.Culture))
                            {
                                var gd = NaturalLanguageTools.GenderDetectors.TryGetC(locType.Assembly.Culture.TwoLetterISOLanguageName);

                                var list = gd.TryCC(a => a.Pronoms).EmptyIfNull()
                                 .Select(pi => new SelectListItem { Value = pi.Gender.ToString(), Text = pi.Singular, Selected = pi.Gender == locType.Gender }).ToList();

                                if (locType.Gender == null)
                                {
                                    list.Insert(0, new SelectListItem { Value = "", Text = "-", Selected = true });
                                }
                            
            
            #line default
            #line hidden
            
            #line 75 "..\..\Translation\Views\View.cshtml"
                       Write(Html.DropDownList(locKey(locType) + ".Gender", list));

            
            #line default
            #line hidden
            
            #line 75 "..\..\Translation\Views\View.cshtml"
                                                                                 ;
                            }
                            else
                            {
                            
            
            #line default
            #line hidden
            
            #line 79 "..\..\Translation\Views\View.cshtml"
                        Write(locType.Gender != null ? NaturalLanguageTools.GetPronom(locType.Gender.Value, plural: false, culture: locType.Assembly.Culture) : "-");

            
            #line default
            #line hidden
            
            #line 79 "..\..\Translation\Views\View.cshtml"
                                                                                                                                                                    
                            }
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </th>\r\n                    <th class=\"monospaceCell\">\r\n");


            
            #line 84 "..\..\Translation\Views\View.cshtml"
                         if (editCulture(locType.Assembly.Culture))
                        {
                            
            
            #line default
            #line hidden
            
            #line 86 "..\..\Translation\Views\View.cshtml"
                       Write(Html.TextArea(locKey(locType) + ".Description", locType.Description, new { style = "width:90%;height:16px" }));

            
            #line default
            #line hidden
            
            #line 86 "..\..\Translation\Views\View.cshtml"
                                                                                                                                          
                        }
                        else
                        {
                            
            
            #line default
            #line hidden
            
            #line 90 "..\..\Translation\Views\View.cshtml"
                       Write(locType.Description);

            
            #line default
            #line hidden
            
            #line 90 "..\..\Translation\Views\View.cshtml"
                                                   
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </th>\r\n                    <th class=\"smallCell\">\r\n");


            
            #line 94 "..\..\Translation\Views\View.cshtml"
                         if (hasPlural && hasGender)
                        {
                            
            
            #line default
            #line hidden
            
            #line 96 "..\..\Translation\Views\View.cshtml"
                        Write(locType.Gender != null ? NaturalLanguageTools.GetPronom(locType.Gender.Value, plural: true, culture: locType.Assembly.Culture) : "-");

            
            #line default
            #line hidden
            
            #line 96 "..\..\Translation\Views\View.cshtml"
                                                                                                                                                                   
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </th>\r\n                    <th class=\"monospaceCell\">\r\n");


            
            #line 100 "..\..\Translation\Views\View.cshtml"
                         if (hasPlural)
                        {
                            if (editCulture(locType.Assembly.Culture))
                            {
                            
            
            #line default
            #line hidden
            
            #line 104 "..\..\Translation\Views\View.cshtml"
                       Write(Html.TextArea(locKey(locType) + ".PluralDescription", locType.PluralDescription, new { style = "width:90%;height:16px" }));

            
            #line default
            #line hidden
            
            #line 104 "..\..\Translation\Views\View.cshtml"
                                                                                                                                                         
                            }
                            else
                            {    
                            
            
            #line default
            #line hidden
            
            #line 108 "..\..\Translation\Views\View.cshtml"
                       Write(locType.PluralDescription);

            
            #line default
            #line hidden
            
            #line 108 "..\..\Translation\Views\View.cshtml"
                                                      
                            }
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </th>\r\n                </tr>\r\n");


            
            #line 113 "..\..\Translation\Views\View.cshtml"
                    }
                }


                if (defaultLocType.Options.IsSet(DescriptionOptions.Members))
                {
                    foreach (string key in defaultLocType.Members.Keys)
                    {

            
            #line default
            #line hidden
WriteLiteral("                <tr>\r\n                    <th class=\"leftCell\">");


            
            #line 122 "..\..\Translation\Views\View.cshtml"
                                    Write(TranslationMessage.Member.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </th>\r\n                    <th colspan=\"4\">");


            
            #line 124 "..\..\Translation\Views\View.cshtml"
                                Write(key);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </th>\r\n                </tr>\r\n");


            
            #line 127 "..\..\Translation\Views\View.cshtml"
            
                        foreach (var locType in Model.Values.Select(la => la.Types[defaultLocType.Type]).Where(lt => editCulture(lt.Assembly.Culture) || lt.Members.ContainsKey(key)))
                        {

            
            #line default
            #line hidden
WriteLiteral("                <tr>\r\n                    <td class=\"leftCell\">");


            
            #line 131 "..\..\Translation\Views\View.cshtml"
                                    Write(locType.Assembly.Culture.Name);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td colspan=\"4\" class=\"monospaceCell\">\r\n\r\n");


            
            #line 134 "..\..\Translation\Views\View.cshtml"
                         if (editCulture(locType.Assembly.Culture))
                        {
                            
            
            #line default
            #line hidden
            
            #line 136 "..\..\Translation\Views\View.cshtml"
                       Write(Html.TextArea(locKey(locType) + ".Member." + key, locType.Members.TryGetC(key), new { style = "width:90%;height:16px" }));

            
            #line default
            #line hidden
            
            #line 136 "..\..\Translation\Views\View.cshtml"
                                                                                                                                                     
                        }
                        else
                        {
                            
            
            #line default
            #line hidden
            
            #line 140 "..\..\Translation\Views\View.cshtml"
                       Write(locType.Members.TryGetC(key));

            
            #line default
            #line hidden
            
            #line 140 "..\..\Translation\Views\View.cshtml"
                                                         
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </td>\r\n                </tr>\r\n");


            
            #line 144 "..\..\Translation\Views\View.cshtml"
                        }
                    }
                }
            }

            
            #line default
            #line hidden
WriteLiteral("        </table>\r\n");



WriteLiteral("    <input type=\"submit\" value=\"");


            
            #line 149 "..\..\Translation\Views\View.cshtml"
                           Write(TranslationMessage.Save.NiceToString());

            
            #line default
            #line hidden
WriteLiteral("\" />\r\n");


            
            #line 150 "..\..\Translation\Views\View.cshtml"
    }
}

            
            #line default
            #line hidden

        }
    }
}
#pragma warning restore 1591
