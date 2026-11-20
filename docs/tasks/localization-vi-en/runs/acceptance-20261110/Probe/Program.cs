using System.Reflection;
using System.Text.Json;
using CafeManager.Models.Localization;
using Microsoft.Data.SqlClient;

internal static class Program
{
    internal static readonly List<object> Results=[];
    internal static string Output="";
    internal static void Save()=>File.WriteAllText(Path.Combine(Output,"probe-results.json"),JsonSerializer.Serialize(Results,new JsonSerializerOptions{WriteIndented=true}));
    internal static void Result(string id,string language,string expected,bool pass,object actual)
    {Results.Add(new{Id=id,Language=language,Expected=expected,Status=pass?"PASS":"FAIL",Actual=actual});Save();}
    [STAThread]
    static void Main(string[] args)
    {
        Output=Path.GetFullPath(args[0]);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetColorMode(SystemColorMode.Dark);
        Application.ThreadException+=(_,e)=>Result("UI-unhandled",LanguageService.Current.ToString(),"No UI exception",false,new{Type=e.Exception.GetType().Name,Message=e.Exception.Message});
        using var host=new Form{Text="CoffeeManager VI/EN acceptance runner",Width=500,Height=150};
        host.Shown+=async(_,_)=>
        {
            try
            {
                using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(Output,"manifest.json")));
                var marker=manifest.RootElement.GetProperty("RunId").GetString();
                var databases=manifest.RootElement.GetProperty("Created").EnumerateArray().ToDictionary(x=>x.GetProperty("Language").GetString()!,x=>x.GetProperty("Database").GetString()!);
                foreach(var language in new[]{"vi","en"})
                {
                    LanguageService.Set(language=="vi"?AppLanguage.Vietnamese:AppLanguage.English);
                    var database=databases[language];
                    if(!System.Text.RegularExpressions.Regex.IsMatch(database,@"^CoffeeManager_Test_LOC_20261110_\d{6}_[a-f0-9]{8}_(vi|en)$"))throw new InvalidOperationException("Non-test target");
                    var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=database,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
                    await using var owner=new SqlConnection(builder.ConnectionString);await owner.OpenAsync();
                    if((string?)await StateSnapshot.Scalar(owner,"SELECT RunId FROM dbo.LocalizationTestMarker")!=marker)throw new InvalidOperationException("Marker mismatch");
                    var evidence=new Dictionary<string,object?>();
                    try {await OperationProbe.Run(owner,builder.ConnectionString,"Demo",evidence);Result("auth-bootstrap-runtime",language,"Bootstrap + real admin login + runtime permission",true,evidence);}
                    catch(Exception e){Result("auth-bootstrap-runtime",language,"Bootstrap + real admin login",false,new{Type=e.GetType().Name,Message=e.Message});continue;}
                    try {await E2EProbe.Run(owner,builder.ConnectionString,evidence);}
                    catch(Exception e){Result("e2e-suite",language,"Existing SQL E2E assertions",false,new{Type=e.GetType().Name,Message=e.Message});}
                    File.WriteAllText(Path.Combine(Output,language+"-e2e.json"),JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
                    try {await UiProbe.Run(owner,builder.ConnectionString,language,Output);}
                    catch(Exception e){Result("ui-suite",language,"Complete UI component sweep",false,new{Type=e.GetType().Name,Message=e.Message,Stack=e.StackTrace});}
                }
                foreach(var language in new[]{AppLanguage.Vietnamese,AppLanguage.English})
                {
                    LanguageService.Set(language);
                    using var bootstrap=new CafeManager.WinForms.Forms.BootstrapForm();bootstrap.Show();await Task.Delay(150);
                    UiProbe.Inspect(bootstrap,"bootstrap-initial",language.ToString());bootstrap.Dispose();
                }
                await UiProbe.LoginCases();
            }
            catch(Exception e){Result("probe-fatal","both","Complete approved probe",false,new{Type=e.GetType().Name,Message=e.Message,Stack=e.StackTrace});}
            finally{Save();host.Dispose();Application.ExitThread();}
        };
        Application.Run(host);
    }
}
