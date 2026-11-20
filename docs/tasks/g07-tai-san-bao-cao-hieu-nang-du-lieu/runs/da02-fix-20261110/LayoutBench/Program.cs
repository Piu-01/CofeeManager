using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;

var output = args.Length > 0 ? args[0] : "layout-bench-results.json";
var samples = new List<object>();
object? open = null;
object? comparison = null;
Exception? failure = null;
var connectionString = DevelopmentConnection.Read();
var factory = new ConnectionFactory(connectionString);
var auth = new AuthRepository(factory);
using var session = new SessionContext((await auth.ReadIdentityAsync(1))!);
var reports = new ReportService(new ReportRepository(factory), new PermissionService(auth));
var thread = new Thread(() =>
{
    Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
    using var main = new MainForm();
    main.ClientSize = new(1366, 768);
    using var dashboard = new AdminDashboardControl(reports, session, null);
    var content = (Panel)typeof(MainForm).GetField("content", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(main)!;
    content.Controls.Add(dashboard);
    FieldInfo Field(string name) => typeof(AdminDashboardControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
    IEnumerable<Control> Desc(Control parent) { yield return parent; foreach (Control child in parent.Controls) foreach (var item in Desc(child)) yield return item; }
    object Geometry(AdminDashboardControl dashboard)
    {
        var scroll = dashboard.Controls[0];
        var page = scroll.Controls[0];
        var alerts = (System.Collections.IDictionary)Field("alerts").GetValue(dashboard)!;
        object Bounds(Control c) => new { c.Width, c.Height, c.Left, c.Top };
        return new
        {
            Scroll = Bounds(scroll), ScrollClient = scroll.ClientSize, Page = Bounds(page),
            Metrics = Bounds((Control)Field("metrics").GetValue(dashboard)!),
            Charts = Bounds((Control)Field("charts").GetValue(dashboard)!),
            Bottom = Bounds((Control)Field("bottom").GetValue(dashboard)!),
            Products = Bounds((Control)Field("products").GetValue(dashboard)!),
            ProductsRows = ((Control)Field("products").GetValue(dashboard)!).Controls.Count,
            ProductsChildren = ((Control)Field("products").GetValue(dashboard)!).Controls.Cast<Control>().Select(Bounds).ToArray(),
            AlertHeights = alerts.Values.Cast<Control>().Select(c => c.Height).ToArray(),
            MetricValues = ((System.Collections.IDictionary)Field("values").GetValue(dashboard)!).Values.Cast<Label>().Select(l => l.Text).ToArray()
        };
    }
    (double Ms, int Layouts, int ControlsBefore, Dictionary<string, int> ByType) Measure(object dashboard, MethodInfo method)
    {
        var layouts = 0;
        var counters = new Dictionary<string, int>();
        var handlers = new List<(Control Control, LayoutEventHandler Handler)>();
        var controls = Desc((Control)dashboard).ToArray();
        foreach (var control in controls)
        {
            LayoutEventHandler handler = (_, _) => { layouts++; var key = control.GetType().Name; counters[key] = counters.GetValueOrDefault(key) + 1; };
            control.Layout += handler; handlers.Add((control, handler));
        }
        var watch = Stopwatch.StartNew(); method.Invoke(dashboard, null); watch.Stop();
        foreach (var pair in handlers) if (!pair.Control.IsDisposed) pair.Control.Layout -= pair.Handler;
        return (watch.Elapsed.TotalMilliseconds, layouts, controls.Length, counters);
    }
    main.Shown += async (_, _) =>
    {
        try
        {
            var started = Stopwatch.StartNew();
            for (var i = 0; i < 3000 && !(bool)Field("ready").GetValue(dashboard)!; i++) await Task.Delay(10);
            started.Stop();
            if (!(bool)Field("ready").GetValue(dashboard)!) throw new TimeoutException("Dashboard not ready");
            open = new { ReadyMs = started.Elapsed.TotalMilliseconds, State = ((Label)Field("state").GetValue(dashboard)!).Text };
            var saved = new[] { "pages", "indicators", "period" }.ToDictionary(k => k, k => Field(k).GetValue(dashboard));
            object Run(string label, MethodInfo method, bool restore)
            {
                if (restore) foreach (var item in saved) Field(item.Key).SetValue(dashboard, item.Value);
                var (ms, layouts, count, byType) = Measure(dashboard, method);
                return new { Label = label, Method = method.Name, ElapsedMs = ms, Layouts = layouts, ControlsBefore = count, ByType = byType, Geometry = Geometry(dashboard) };
            }
            var render = typeof(AdminDashboardControl).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var clear = typeof(AdminDashboardControl).GetMethod("ClearSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var renderCore = typeof(AdminDashboardControl).GetMethod("RenderCore", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var clearCore = typeof(AdminDashboardControl).GetMethod("ClearSnapshotCore", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var fastRender = Run("fast-render-1", render, false);
            samples.Add(fastRender);
            var clearSample = Run("fast-clear", clear, false);
            samples.Add(clearSample);
            var fastRender2 = Run("fast-render-2", render, true);
            samples.Add(fastRender2);
            var rawRender = Run("raw-render", renderCore, true);
            samples.Add(rawRender);
            samples.Add(Run("raw-clear", clearCore, false));
            var geometry = new[] { ("fast-render-1", fastRender), ("fast-render-2", fastRender2), ("raw-render", rawRender) }
                .Select(pair => new { pair.Item1, Json = JsonSerializer.Serialize(pair.Item2.GetType().GetProperty("Geometry")!.GetValue(pair.Item2)) }).ToArray();
            comparison = new
            {
                FastStable = geometry[0].Json == geometry[1].Json,
                FastMatchesRaw = geometry[0].Json == geometry[2].Json,
                Fast = geometry[0].Json,
                Raw = geometry[2].Json
            };
        }
        catch (Exception e) { failure = e; }
        finally { main.Close(); }
    };
    Application.Run(main);
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start(); thread.Join();
File.WriteAllText(output, JsonSerializer.Serialize(new { Open = open, Samples = samples, LayoutComparison = comparison, Error = failure?.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
if (failure is not null) Environment.ExitCode = 1;
