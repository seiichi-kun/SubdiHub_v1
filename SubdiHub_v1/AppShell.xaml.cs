using Microsoft.Maui.Controls;
using SubdiHub_v1.Components.Authentication.Login;
using SubdiHub_v1.Components.Authentication.Signup;
using SubdiHub_v1.Components.Dashboards.Admin;
using SubdiHub_v1.Components.Dashboards.Seller;
using SubdiHub_v1.Components.Dashboards.Rider;
using SubdiHub_v1.Components.Dashboards.Resident;

namespace SubdiHub_v1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Edit_profile_A), typeof(Edit_profile_A));
            Routing.RegisterRoute(nameof(Edit_profile_Re), typeof(Edit_profile_Re));
            Routing.RegisterRoute(nameof(Edit_profile_S), typeof(Edit_profile_S));
            Routing.RegisterRoute("EmergencyContact", typeof(SubdiHub_v1.Components.Dashboards.Admin.EmergencyContact));
            Routing.RegisterRoute("SubdiHub_v1.Components.Dashboards.Admin.EmergencyContact", typeof(SubdiHub_v1.Components.Dashboards.Admin.EmergencyContact));

        }
    }
}