﻿using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using GridProxy;
using GridProxyGUI;
using System.Reflection;

namespace GridProxyGUI
{
    public class PluginInfo
    {
        public bool LoadOnStartup;
        public string Path;
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(Path)) return string.Empty;
                return System.IO.Path.GetFileName(Path);
            }
        }
        public string Dir
        {
            get
            {
                if (string.IsNullOrEmpty(Path)) return string.Empty;
                return System.IO.Path.GetDirectoryName(Path);
            }
        }
    }

    public class PluginsScroller : TreeView
    {

        ListStore Store;

        public PluginsScroller()
        {
            TreeViewColumn col = new TreeViewColumn();
            col.Title = "Load on Starup";
            CellRendererToggle cell = new CellRendererToggle();
            cell.Toggled += new ToggledHandler((sender, e) =>
            {
                TreeIter iter;
                if (Store.GetIterFromString(out iter, e.Path))
                {
                    PluginInfo item = Store.GetValue(iter, 0) as PluginInfo;
                    if (null != item)
                    {
                        item.LoadOnStartup = !item.LoadOnStartup;
                        Store.SetValue(iter, 0, item);
                    }
                }
            });
            cell.Activatable = true;
            col.PackStart(cell, true);
            col.SetCellDataFunc(cell, (TreeViewColumn column, CellRenderer xcell, TreeModel model, TreeIter iter) =>
            {
                var item = Store.GetValue(iter, 0) as PluginInfo;
                if (item != null)
                {
                    ((CellRendererToggle)cell).Active = item.LoadOnStartup;
                }
            });
            AppendColumn(col);

            col = new TreeViewColumn();
            col.Title = "Plugin";
            CellRendererText cellText = new CellRendererText();
            col.PackStart(cellText, true);
            col.SetCellDataFunc(cellText, (TreeViewColumn column, CellRenderer xcell, TreeModel model, TreeIter iter) =>
            {
                var item = Store.GetValue(iter, 0) as PluginInfo;
                if (item != null)
                {
                    ((CellRendererText)xcell).Text = item.Name;
                }
            });
            AppendColumn(col);

            col = new TreeViewColumn();
            col.Title = "Path";
            cellText = new CellRendererText();
            col.PackStart(cellText, true);
            col.SetCellDataFunc(cellText, (TreeViewColumn column, CellRenderer xcell, TreeModel model, TreeIter iter) =>
            {
                var item = Store.GetValue(iter, 0) as PluginInfo;
                if (item != null)
                {
                    ((CellRendererText)xcell).Text = item.Path;
                }
            });
            AppendColumn(col);

            Store = new ListStore(typeof(PluginInfo));
            Model = Store;
            HeadersVisible = true;
            ShowAll();
        }

        List<FileFilter> GetFileFilters()
        {
            List<FileFilter> filters = new List<FileFilter>();

            FileFilter filter = new FileFilter();
            filter.Name = "Grid Proxy Plugin (*.dll; *.exe)";
            filter.AddPattern("*.dll");
            filter.AddPattern("*.exe");
            filters.Add(filter);

            filter = new FileFilter();
            filter.Name = "All Files (*.*)";
            filter.AddPattern("*.*");
            filters.Add(filter);

            return filters;
        }


        public bool LoadAssembly(string path, ProxyFrame proxy)
        {

            try
            {
                Assembly assembly = Assembly.LoadFile(System.IO.Path.GetFullPath(path));
                foreach (Type t in assembly.GetTypes())
                {
                    if (t.IsSubclassOf(typeof(ProxyPlugin)))
                    {
                        ConstructorInfo info = t.GetConstructor(new Type[] { typeof(ProxyFrame) });
                        ProxyPlugin plugin = (ProxyPlugin)info.Invoke(new object[] { proxy });
                        plugin.Init();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                OpenMetaverse.Logger.Log("Failed loading plugin: " + e.Message + Environment.NewLine + e.StackTrace, OpenMetaverse.Helpers.LogLevel.Error);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        public void LoadPlugin(ProxyFrame proxy)
        {
            if (proxy == null) return;

            var od = new Gtk.FileChooserDialog(null, "Load Plugin", null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            foreach (var filter in GetFileFilters()) od.AddFilter(filter);

            if (od.Run() == (int)ResponseType.Accept)
            {
                PluginInfo plugin = new PluginInfo();
                plugin.Path = od.Filename;
                bool found = false;
                Store.Foreach((model, path, iter) =>
                {
                    var item = model.GetValue(iter, 0) as PluginInfo;
                    if (null != item && item.Path == plugin.Path)
                    {
                        return found = true;
                    }

                    return false;
                });

                if (!found && LoadAssembly(plugin.Path, proxy))
                {
                    Store.AppendValues(plugin);
                }
            }
            od.Destroy();

        }

    }
}
