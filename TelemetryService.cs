using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ZeroTrustMonitor
{
    public class TelemetryService
    {
        private static TelemetryService _instance;
        public static TelemetryService Instance => _instance ??= new TelemetryService();

        private static readonly HttpClient _httpClient = new HttpClient();
        
        // Configuration from myfirewall2 project
        private const string MeasurementId = "G-3Y256NPRT9";
        private const string RegKeyPath = @"Software\ZeroTrustMonitor";

        private string _clientId;

        public TelemetryService()
        {
            EnsureClientId();
        }

        public static bool IsTelemetryEnabled
        {
            get
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegKeyPath);
                    if (key != null)
                    {
                        var val = key.GetValue("TelemetryEnabled");
                        if (val is int intVal) return intVal == 1;
                    }
                    return true; // Default to enabled
                }
                catch { return true; }
            }
            set
            {
                try
                {
                    using var key = Registry.CurrentUser.CreateSubKey(RegKeyPath);
                    key.SetValue("TelemetryEnabled", value ? 1 : 0, RegistryValueKind.DWord);
                }
                catch { }
            }
        }

        private void EnsureClientId()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegKeyPath);
                _clientId = key?.GetValue("ClientId") as string;
                if (string.IsNullOrEmpty(_clientId))
                {
                    _clientId = Guid.NewGuid().ToString();
                    key?.SetValue("ClientId", _clientId);
                }
            }
            catch
            {
                _clientId = Guid.NewGuid().ToString();
            }
        }

        public void TrackEvent(string eventName)
        {
            if (!IsTelemetryEnabled) return;

            try
            {
                // GA4 Protocol v2 Endpoint (Used in myfirewall2)
                var url = $"https://www.google-analytics.com/g/collect?v=2&tid={MeasurementId}&cid={_clientId}&en={Uri.EscapeDataString(eventName)}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("ZeroTrustMonitor/1.0 (Windows NT 10.0; Win64; x64)");
                
                // Non-blocking fire and forget call
                _ = _httpClient.SendAsync(request);
            }
            catch
            {
                // Fail silently to ensure telemetry never impacts application performance or stability
            }
        }
    }
}
