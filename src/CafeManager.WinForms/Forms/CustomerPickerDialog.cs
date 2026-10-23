using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Customers;
using CafeManager.WinForms.Modules.Customers;
namespace CafeManager.WinForms.Forms;
public sealed class CustomerPickerDialog : Form
{
    public CustomerRow? SelectedCustomer {get;private set;}
    public CustomerPickerDialog(CustomerService service,SessionContext session,bool allowGuest=false)
    {
        Text="Chọn khách hàng";ClientSize=new Size(900,600);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;
        var control=new CustomersControl(service,session,true,allowGuest){Dock=DockStyle.Fill};
        control.PageWorkspace.Compact=true;
        var layout=new UI.OrbitDialogLayout(this,control,new Label(),new FlowLayoutPanel());Controls.Add(layout);
        CancelButton=control.PageWorkspace.RowActions.Controls.OfType<Button>().Single(button=>button.Text=="Đóng");
        control.CustomerSelected+=customer=>{SelectedCustomer=customer;DialogResult=DialogResult.OK;Close();};
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);BackColor=Theme.OrbitManagementTheme.Background;ForeColor=Theme.OrbitManagementTheme.Text;Font=Theme.OrbitManagementTheme.Body;
        MinimumSize=new Size(560,420);Shown+=(_,_)=>layout.FitToContent(this,true);
    }
}
