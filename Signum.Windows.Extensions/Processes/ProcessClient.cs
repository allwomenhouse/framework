﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Processes;
using Signum.Windows.Processes;
using Signum.Windows.Operations;
using System.Windows.Media.Imaging;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Windows;

namespace Signum.Windows.Processes
{
    public static class ProcessClient
    {
        public static void AsserIsStarted()
        {
            Navigator.Manager.AssertDefined(ReflectionTools.GetMethodInfo(()=>Start()));
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<ProcessDN>(EntityType.ServerOnly) { View = e => new ProcessUI(), IsReadOnly = (_, a) => true, IsCreable = a => false, Icon = Image("process.png") });
                Navigator.AddSetting(new EntitySettings<ProcessExecutionDN>(EntityType.ServerOnly) { View = e => new ProcessExecution(), Icon = Image("processExecution.png") });

                OperationClient.AddSettings(new List<OperationSettings>()
                {
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.FromProcess){ Icon = Image("execute.png") },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Plan){ Icon = Image("plan.png"), Click = ProcessOperation_Plan },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Cancel){ Icon = Image("stop.png") },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Execute){ Icon = Image("play.png") },
                    new EntityOperationSettings<ProcessExecutionDN>(ProcessOperation.Suspend){ Icon = Image("pause.png") },
                }); 

                Navigator.AddSetting(new EntitySettings<PackageDN>(EntityType.ServerOnly) { View = e => new Package(), Icon = Image("package.png") });
                Navigator.AddSetting(new EntitySettings<PackageLineDN>(EntityType.ServerOnly) { View = e => new PackageLine(), IsReadOnly = (_, a) => true, IsCreable = a => false, Icon = Image("packageLine.png") }); 
            }
        }

        static ProcessExecutionDN ProcessOperation_Plan(EntityOperationEventArgs<ProcessExecutionDN> args)
        {
            DateTime plan = TimeZoneManager.Now;
            if (ValueLineBox.Show(ref plan, "Choose planned date", "Please, choose the date you want the process to start", "Planned date", null, null, Window.GetWindow(args.SenderButton)))
            {
                return  ((ProcessExecutionDN)args.Entity).ToLite().ExecuteLite(ProcessOperation.Plan, plan); 
            }
            return null; 
        }

        static BitmapFrame Image(string name)
        {
            return ImageLoader.LoadIcon(PackUriHelper.Reference("Images/" + name, typeof(ProcessClient)));
        }
    }
}
