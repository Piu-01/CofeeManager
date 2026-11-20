using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
class Diagnostic {
 static async Task Main(string[] args) {
 var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false}.ConnectionString;
 await using var c=new SqlConnection(cs);await c.OpenAsync();using var marker=c.CreateCommand();marker.CommandText="SELECT RunId FROM dbo.G06TestMarker";if(Convert.ToString(await marker.ExecuteScalarAsync())!=args[1])throw new Exception("Marker");
 var rows=new List<object>();
 foreach(var value in new[]{12000000m,99999999999m,999999999999m,999999999999999999m})foreach(byte scale in new byte[]{18,0}){using var q=c.CreateCommand();q.CommandText="SELECT CONVERT(decimal(18,0),@v)";q.Parameters.Add(new SqlParameter("@v",SqlDbType.Decimal){Precision=38,Scale=scale,Value=value});try{rows.Add(new{Value=value,Scale=scale,Result=await q.ExecuteScalarAsync(),Status="PASS"});}catch(Exception e){rows.Add(new{Value=value,Scale=scale,Exception=e.ToString(),Status="FAIL"});}}
 using var literal=c.CreateCommand();literal.CommandText="SELECT CONVERT(decimal(18,0),999999999999999999)";rows.Add(new{Literal=await literal.ExecuteScalarAsync()});
 rows.Add(new{Format=12000000m.ToString("N0' ₫'",System.Globalization.CultureInfo.GetCultureInfo("vi-VN")),Expected="12.000.000 ₫"});File.WriteAllText(args[2],JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
 }
}
