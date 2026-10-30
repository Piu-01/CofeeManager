using System.Text.Json;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var results=new List<object>();
        foreach(var attach in new[]{false,true})
        {
            using var form=new Form();
            using var combo=new ComboBox{DisplayMember="Name",ValueMember="Code",DropDownStyle=ComboBoxStyle.DropDownList};
            if(attach)form.Controls.Add(combo);
            form.Shown+=(_,_)=>
            {
                Exception? error=null;
                combo.DataSource=new[]{new Option("","Tự giao")}.ToList();
                var count=combo.Items.Count;var before=combo.SelectedIndex;
                try{combo.SelectedValue="";if(combo.SelectedIndex<0)combo.SelectedIndex=0;}
                catch(Exception ex){error=ex;}
                results.Add(new{Attached=attach,ItemsAfterDataSource=count,IndexBefore=before,IndexAfter=combo.SelectedIndex,Error=error?.GetType().Name,Message=error?.Message});
                form.Close();
            };
            Application.Run(form);
        }
        File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(JsonSerializer.Serialize(results));
    }
    private sealed record Option(string Code,string Name);
}
