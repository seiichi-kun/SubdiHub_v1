using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SubdiHub_v1.Components.Dashboards.Admin
{
    public partial class EmergencyContact : ContentPage
    {
        private readonly Dictionary<string, string> _emergencyNumbers = new Dictionary<string, string>
        {
            { "911", "911" },
            { "Police", "+11234567890" }, // Replace with actual local police number
            { "Barangay Hall", "+639876543210" } // Replace with actual Barangay Hall number
        };

        private bool _isSosDialing = false;
        private bool _isCallInProgress = false;

        public EmergencyContact()
        {
            InitializeComponent();
        }

        private async void OnContactClicked(object sender, EventArgs e)
        {
            await InitiateCallAsync(sender);
        }

        private async Task InitiateCallAsync(object sender)
        {
            try
            {
                if (sender is not Button button)
                {
                    await this.DisplayAlert("Error", "Invalid contact selected.", "OK");
                    return;
                }

                string contact = button.Text;

                if (string.IsNullOrEmpty(contact) || !_emergencyNumbers.ContainsKey(contact))
                {
                    await this.DisplayAlert("Error", "Invalid contact selected.", "OK");
                    return;
                }

                string phoneNumber = _emergencyNumbers[contact];

                // Request permissions on Android
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.Phone>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.Phone>();
                        if (status != PermissionStatus.Granted)
                        {
                            await this.DisplayAlert("Error", "Phone call permission denied. Cannot make calls.", "OK");
                            return;
                        }
                    }
                }

                // Show confirmation prompt before dialing
                bool confirm = await this.DisplayAlert("Confirm Call", $"Are you sure you want to call {contact} ({phoneNumber})?", "Yes", "No");
                if (!confirm)
                    return;

                if (!PhoneDialer.IsSupported)
                {
                    // Detailed debug info for unsupported dialer
                    string debugInfo = $"PhoneDialer.IsSupported: False\n" +
                                      $"Platform: {DeviceInfo.Platform}\n" +
                                      $"Model: {DeviceInfo.Model}\n" +
                                      $"Manufacturer: {DeviceInfo.Manufacturer}\n" +
                                      $"Version: {DeviceInfo.VersionString}";
                    Debug.WriteLine(debugInfo); // Log for debugging

                    // Fallback to Launcher.OpenAsync
                    try
                    {
                        await Launcher.OpenAsync($"tel:{phoneNumber}");
                        ShowCallInProgress(contact);
                        return;
                    }
                    catch (Exception ex)
                    {
                        await this.DisplayAlert("Error", $"Phone dialing is not supported on this device.\nDetails:\n{debugInfo}\nFallback failed: {ex.Message}\nPlease ensure you have a SIM card and a dialer app installed.", "OK");
                        return;
                    }
                }

                // Normal dialing path
                PhoneDialer.Open(phoneNumber);
                ShowCallInProgress(contact);
            }
            catch (FeatureNotSupportedException ex)
            {
                await this.DisplayAlert("Error", $"Phone dialing failed: {ex.Message}. Please ensure you have a SIM card and a dialer app installed.", "OK");
            }
            catch (Exception ex)
            {
                await this.DisplayAlert("Error", $"Failed to dial: {ex.Message}. Please check your network, SIM card, or permissions.", "OK");
            }
        }

        private async void OnSosClicked(object sender, EventArgs e)
        {
            SosButton.IsVisible = false;
            SosMessageContainer.IsVisible = true;
            _isSosDialing = true;

            // 5-second countdown with dynamic label update
            for (int i = 5; i >= 0; i--)
            {
                if (!_isSosDialing)
                    return; // Canceled by user

                SosCountdownLabel.Text = $"Contacting 911 in {i} seconds... Press Cancel to stop.";
                await Task.Delay(1000); // Wait 1 second
            }

            if (_isSosDialing)
            {
                await InitiateCallAsync(new Button { Text = "911" });
                ResetSosState();
            }
        }

        private void OnSosCancelClicked(object sender, EventArgs e)
        {
            _isSosDialing = false;
            ResetSosState();
        }

        private void OnEndCallClicked(object sender, EventArgs e)
        {
            _isCallInProgress = false;
            ResetCallState();
        }

        private void ShowCallInProgress(string contact)
        {
            _isCallInProgress = true;
            SosButton.IsVisible = false;
            SosMessageContainer.IsVisible = false;
            CallInProgressContainer.IsVisible = true;

            // Simulate location sharing (mock)
            // In a real app, you would use Geolocation API to get and share the user's location
        }

        private void ResetSosState()
        {
            SosButton.IsVisible = true;
            SosMessageContainer.IsVisible = false;
        }

        private void ResetCallState()
        {
            SosButton.IsVisible = true;
            CallInProgressContainer.IsVisible = false;
        }
    }
}