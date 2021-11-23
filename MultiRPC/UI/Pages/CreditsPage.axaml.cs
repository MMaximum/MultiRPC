﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using MultiRPC.Extensions;
using MultiRPC.Utils;
using TinyUpdate.Core.Logging;
using TinyUpdate.Http.Extensions;

namespace MultiRPC.UI.Pages
{
    public partial class CreditsPage : SidePage
    {
        public override string IconLocation => "Icons/Credits";
        public override string LocalizableName => "Credits";
        public override void Initialize(bool loadXaml)
        {
            InitializeComponent(loadXaml);

            tblCommunityAdminsTitle.DataContext = new Language("CommunityAdmins");
            tblPatreonDonatorsTitle.DataContext = new Language("PatreonDonators");
            tblPaypalDonatorsTitle.DataContext = new Language("PaypalDonators");
            tblIconProvidersTitle.DataContext = new Language("IconProviders");
            NetworkChange.NetworkAddressChanged += NetworkChangeOnNetworkAddressChanged;
            UpdateCredits();
            _ = DownloadAndShow();
        }

        private async void NetworkChangeOnNetworkAddressChanged(object? sender, EventArgs e)
        {
            if (NetworkUtil.NetworkIsAvailable())
            {
                await DownloadAndShow();
            }
        }

        private static readonly string CreditsFileLocation = Path.Combine(Constants.SettingsFolder, "Credits.json");
        private CreditsList? _creditsList;
        private bool _downloadedCredit;

        private async Task DownloadAndShow()
        {
            await DownloadCredits();
            UpdateCredits();
        }
        
        private void UpdateCredits()
        {
            var creditsFileInfo = new FileInfo(CreditsFileLocation);
            if (!creditsFileInfo.Exists)
            {
                return;
            }

            using var reader = creditsFileInfo.OpenRead();
            _creditsList = JsonSerializer.Deserialize<CreditsList>(reader);
            if (_creditsList != null)
            {
                tblCommunityAdmins.Text = string.Join("\r\n\r\n", _creditsList.Admins);
                tblPatreonDonators.Text = string.Join("\r\n\r\n", _creditsList.Patreon);
                tblPaypalDonators.Text = string.Join("\r\n\r\n", _creditsList.Paypal);
            }

            if (!NetworkUtil.NetworkIsAvailable() && !_downloadedCredit)
            {
                tblLastUpdated.Text = $"{Language.GetText("WaitingForInternetUpdate")}...";
                return;
            }
            
            //TODO: Make this be updated on Lang change
            tblLastUpdated.Text = creditsFileInfo.LastWriteTime.Date == DateTime.Now.Date
                ? $"{Language.GetText("LastUpdatedAt")}: {creditsFileInfo.LastWriteTime.ToShortTimeString()}"
                : $"{Language.GetText("LastUpdatedOn")}: {creditsFileInfo.LastWriteTime.ToLongDateString()}";
        }

        private const string Url = Constants.WebsiteUrl + "Credits.json";
        private readonly ILogging _logger = LoggingCreator.CreateLogger(nameof(CreditsPage));
        private async Task DownloadCredits()
        {
            if (_downloadedCredit)
            {
                return;
            }

            for (int i = 0; i < Constants.RetryCount; i++)
            {
                this.RunUILogic(() => 
                    tblLastUpdated.Text = Language.GetText("CheckForUpdates").Replace("\r\n", " "));

                var req = await App.HttpClient.GetResponseMessage(new HttpRequestMessage(HttpMethod.Get, Url));
                if (req is null || !req.IsSuccessStatusCode)
                {
                    if (req == null)
                    {
                        _logger.Error("Credit request returned nothing");
                    }
                    else
                    {
                        _logger.Error("StatusCode: {0}, Reason: {1}", req.StatusCode, req.ReasonPhrase);
                    }
                    continue;
                }

                var creditStream = await req.Content.ReadAsStreamAsync();
                if (creditStream.Length == 0)
                {
                    _logger.Error("Credit stream contains nothing!");
                    continue;
                }

                if (File.Exists(CreditsFileLocation))
                {
                    File.Delete(CreditsFileLocation);
                }
                var fileStream = File.OpenWrite(CreditsFileLocation);
                await creditStream.CopyToAsync(fileStream);
                await creditStream.DisposeAsync();
                await fileStream.DisposeAsync();
                _downloadedCredit = true;
                break;
            }
        }
    }
}