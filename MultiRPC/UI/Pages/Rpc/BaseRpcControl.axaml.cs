﻿using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MultiRPC.Extensions;
using MultiRPC.Rpc;
using MultiRPC.Rpc.Validation;
using TinyUpdate.Core.Extensions;

namespace MultiRPC.UI.Pages.Rpc
{
    public enum ImagesType
    {
        /// <summary>
        /// Use images from the user's RPC and not our images
        /// </summary>
        Custom,
        /// <summary>
        /// Use the images from us
        /// </summary>
        BuiltIn
    }
    
    public partial class BaseRpcControl : UserControl
    {
        public BaseRpcControl() { }

        public RichPresence RichPresence { get; set; } = null!;

        public ImagesType ImageType { get; set; }

        public bool GrabID { get; set; }

        public void Initialize(bool loadXaml)
        {
            InitializeComponent(loadXaml);

            if (GrabID)
            {
                txtClientID.IsVisible = true;
                txtClientID.AddRpcControl(new Language("ClientID"), null, s =>
                {
                    if (s.Length != 18)
                    {
                        return new CheckResult(false, Language.GetText("ClientIDMustBe18CharactersLong"));
                    }

                    _ = CheckID(s);
                    return new CheckResult(true);
                }, RichPresence.Profile.ID);
            }
            
            if (ImageType == ImagesType.Custom)
            {
                cboLargeKey.IsVisible = false;
                cboSmallKey.IsVisible = false;

                txtLargeKey.IsVisible = true;
                txtSmallKey.IsVisible = true;
                txtLargeKey.AddRpcControl(new Language("LargeKey"), s => RichPresence.Profile.LargeKey = s, s => Check(s, 32), RichPresence.Profile.LargeKey);
                txtSmallKey.AddRpcControl(new Language("SmallKey"), s => RichPresence.Profile.SmallKey = s, s => Check(s, 32), RichPresence.Profile.SmallKey);
            }
            else
            {
                cboLargeKey.Items = Data.MultiRPCImages.Keys;
                cboSmallKey.Items = Data.MultiRPCImages.Keys;
                var largeKey = Data.MultiRPCImages.Keys.IndexOf(x => x?.ToLower() == RichPresence.Profile.LargeKey);
                if (largeKey == -1)
                {
                    largeKey = 0;
                }
                cboLargeKey.SelectedIndex = largeKey;
                
                var smallKey = Data.MultiRPCImages.Keys.IndexOf(x => x?.ToLower() == RichPresence.Profile.SmallKey);
                if (smallKey == -1)
                {
                    smallKey = 0;
                }
                cboSmallKey.SelectedIndex = smallKey;
            }

            txtText1.AddRpcControl(new Language("Text1"), s => RichPresence.Profile.Details = s, s => Check(s), RichPresence.Profile.Details);
            txtText2.AddRpcControl(new Language("Text2"), s => RichPresence.Profile.State = s, s => Check(s), RichPresence.Profile.State);
            txtLargeText.AddRpcControl(new Language("LargeText"), s => RichPresence.Profile.LargeText = s, s => Check(s), RichPresence.Profile.LargeText);
            txtSmallText.AddRpcControl(new Language("SmallText"), s => RichPresence.Profile.SmallText = s, s => Check(s), RichPresence.Profile.SmallText);

            txtButton1Url.AddRpcControl(new Language("Button1Url"), s => RichPresence.Profile.Button1Url = s, CheckUrl, RichPresence.Profile.Button1Url);
            txtButton1Text.AddRpcControl(new Language("Button1Text"), s => RichPresence.Profile.Button1Text = s, s => Check(s, 32), RichPresence.Profile.Button1Text);
            txtButton2Url.AddRpcControl(new Language("Button2Url"), s => RichPresence.Profile.Button2Url = s, CheckUrl, RichPresence.Profile.Button2Url);
            txtButton2Text.AddRpcControl(new Language("Button2Text"), s => RichPresence.Profile.Button2Text = s, s => Check(s, 32), RichPresence.Profile.Button2Text);

            ckbElapsedTime.IsChecked = RichPresence.UseTimestamp;
            ckbElapsedTime.DataContext = new Language("ShowElapsedTime");
        }

        //TODO: Make it so we can disable start button or update presence button

        private async Task CheckID(string s)
        {
            txtClientID.Classes.Remove("error");

            string? error = null;
            if (long.TryParse(s, out var id))
            {
                txtClientID.Classes.Add("checking");
                var (successful, resultMessage) = await IDChecker.Check(id);
                txtClientID.Classes.Remove("checking");
                if (successful)
                {
                    ToolTip.SetTip(txtClientID, null);
                    RichPresence.ID = id;
                    RichPresence.Name = resultMessage!;
                    return;
                }
                error = resultMessage;
            }
            txtClientID.Classes.Add("error");
            error ??= Language.GetText("ClientIDIsNotValid");
            ToolTip.SetTip(txtClientID, error);
        }
        
        private CheckResult CheckUrl(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || Uri.TryCreate(s, UriKind.Absolute, out _))
            {
                return s.CheckBytes(512)
                    ? new CheckResult(true)
                    : new CheckResult(false, Language.GetText("UriTooBig"));
            }

            return new CheckResult(false, Language.GetText("InvalidUri"));
        }

        private static CheckResult Check(string s, int max = 128)
        {
            if (s.Length == 1)
            {
                return new CheckResult(false, Language.GetText("OneChar"));
            }

            return s.CheckBytes(max)
                ? new CheckResult(true)
                : new CheckResult(false, Language.GetText("TooManyChars"));
        }

        private void CboLargeKey_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var key = e.AddedItems[0]?.ToString();
            RichPresence.Profile.LargeKey = cboLargeKey.SelectedIndex != 0 ? 
                key?.ToLower() ?? string.Empty : string.Empty;

            RichPresence.CustomLargeImageUrl =
                key != null 
                && Data.TryGetImageValue(key, out var uriS)
                && Uri.TryCreate(uriS, UriKind.Absolute, out var uri)
                    ? uri 
                    : null;
        }

        private void CboSmallKey_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var key = e.AddedItems[0]?.ToString();
            RichPresence.Profile.SmallKey = cboSmallKey.SelectedIndex != 0 ? 
                key?.ToLower() ?? string.Empty : string.Empty;
            
            RichPresence.CustomSmallImageUrl =
                key != null 
                && Data.TryGetImageValue(key, out var uriS) 
                && Uri.TryCreate(uriS, UriKind.Absolute, out var uri) 
                    ? uri 
                    : null;
        }

        private void CkbElapsedTime_OnChange(object? sender, RoutedEventArgs e)
        {
            RichPresence.UseTimestamp = ckbElapsedTime.IsChecked.GetValueOrDefault(false);
        }
    }
}