using System;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
//using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Collections;
using System.Collections.Specialized;

namespace FamServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            bool install = false, uninstall = false, rethrow = false;

            try
            {
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "-i":
                        case "-install":
                            install = true; break;
                        case "-u":
                        case "-uninstall":
                            uninstall = true; break;
                        case "-c":

                        default:
                            Console.Error.WriteLine("Argument not expected: " + arg);
                            break;
                    }
                }

                if (uninstall)
                {
                    Install(true, args);
                }

                else if (install)
                {
                    Install(false, args);
                }

                else if (!(install || uninstall))
                {
                    rethrow = true; // so that windows sees error...
                    ServiceBase[] services = { new FamServer() };
                    ServiceBase.Run(services);
                    rethrow = false;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (rethrow) throw;
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }
        
        static void Install(bool undo, string[] args)
        {
            try
            {
                Console.WriteLine(undo ? "uninstalling" : "installing");
                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                            try
                            {
                                ServiceController service = new ServiceController("DpFamService");
                                TimeSpan timeout = TimeSpan.FromMilliseconds(1000);

                                service.Start();
                                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                            }
                            catch
                            {
                                Console.WriteLine("Could not start the server\n");
                            }

                        }
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
