using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Xamarin_App.Services;
using Plugin.NFC;

namespace Xamarin_App
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public const string MIME_TYPE = "application/com.companyname.nfcsample";

        bool _eventsAlreadySubscribed = false;
        bool _isDeviceiOS = false;
        NFCNdefTypeFormat _type;

        public bool DeviceIsListening
        {
            get => _deviceIsListening;
            set
            {
                _deviceIsListening = value;
                OnPropertyChanged(nameof(DeviceIsListening));
            }
        }
        private bool _deviceIsListening;

        private bool _nfcIsEnabled;
        public bool NfcIsEnabled
        {
            get => _nfcIsEnabled;
            set
            {
                _nfcIsEnabled = value;
                OnPropertyChanged(nameof(NfcIsEnabled));
                OnPropertyChanged(nameof(NfcIsDisabled));
            }
        }

        public bool NfcIsDisabled => !NfcIsEnabled;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            CrossNFC.Legacy = false;
            if (CrossNFC.IsSupported)
            {
                if(!CrossNFC.Current.IsAvailable)
                    DisplayAlert("Info", "NFC is not available", "OK");

                if (!CrossNFC.Current.IsEnabled)
                    DisplayAlert("Info", "NFC is disabled", "OK"); 

                if (Device.RuntimePlatform == Device.iOS)
                    _isDeviceiOS = true;

                SubscribeEvents();
                CrossNFC.Current.StartListening();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            UnsubscribeEvents();
            CrossNFC.Current.StopListening();
            return base.OnBackButtonPressed();
        }

        /// <summary>
        /// Subscribe to the NFC events
        /// </summary>
        void SubscribeEvents()
        {
            if (_eventsAlreadySubscribed)
                return;

            _eventsAlreadySubscribed = true;

            CrossNFC.Current.OnMessageReceived += Current_OnMessageReceived;
            CrossNFC.Current.OnMessagePublished += Current_OnMessagePublished;
            CrossNFC.Current.OnTagDiscovered += Current_OnTagDiscovered;
            CrossNFC.Current.OnNfcStatusChanged += Current_OnNfcStatusChanged;
            CrossNFC.Current.OnTagListeningStatusChanged += Current_OnTagListeningStatusChanged;

            //if (_isDeviceiOS)
            //    CrossNFC.Current.OniOSReadingSessionCancelled += Current_OniOSReadingSessionCancelled;
        }

        /// <summary>
        /// Unsubscribe from the NFC events
        /// </summary>
        void UnsubscribeEvents()
        {
            CrossNFC.Current.OnMessageReceived -= Current_OnMessageReceived;
            CrossNFC.Current.OnMessagePublished -= Current_OnMessagePublished;
            CrossNFC.Current.OnTagDiscovered -= Current_OnTagDiscovered;
            CrossNFC.Current.OnNfcStatusChanged -= Current_OnNfcStatusChanged;
            CrossNFC.Current.OnTagListeningStatusChanged -= Current_OnTagListeningStatusChanged;

            //if (_isDeviceiOS)
            //    CrossNFC.Current.OniOSReadingSessionCancelled -= Current_OniOSReadingSessionCancelled;
        }

        void Current_OnTagListeningStatusChanged(bool isListening) => DeviceIsListening = isListening;

        /// <summary>
        /// Event raised when NFC Status has changed
        /// </summary>
        /// <param name="isEnabled">NFC status</param>
        async void Current_OnNfcStatusChanged(bool isEnabled)
        {
            NfcIsEnabled = isEnabled;
            await DisplayAlert("NFC Status", $"NFC has been {(isEnabled ? "enabled" : "disabled")}","OK");
        }

        /// <summary>
        /// Event raised when a NDEF message is received
        /// </summary>
        /// <param name="tagInfo">Received <see cref="ITagInfo"/></param>
        async void Current_OnMessageReceived(ITagInfo tagInfo)
        {
            if (tagInfo == null)
            {
                await DisplayAlert("NFC Info","No tag found","OK");
                return;
            }

            await DisplayAlert("Event", "Current_OnMessageReceived", "OK");
            await DisplayAlert("Current_OnMessageReceived", tagInfo.Records[0].Message, "OK");

            // Customized serial number
            var identifier = tagInfo.Identifier;
            var serialNumber = NFCUtils.ByteArrayToHexString(identifier, ":");
            var title = !string.IsNullOrWhiteSpace(serialNumber) ? $"Tag [{serialNumber}]" : "Tag Info";

            if (!tagInfo.IsSupported)
            {
                await DisplayAlert(title,"Unsupported tag (app)", "OK");
            }
            else if (tagInfo.IsEmpty)
            {
                await DisplayAlert(title,"Empty tag", "OK");
            }
            else
            {
                var first = tagInfo.Records[0];
                await DisplayAlert(title,GetMessage(first),"OK");
            }
        }

        /// <summary>
        /// Returns the tag information from NDEF record
        /// </summary>
        /// <param name="record"><see cref="NFCNdefRecord"/></param>
        /// <returns>The tag information</returns>
        string GetMessage(NFCNdefRecord record)
        {
            var message = $"Message: {record.Message}";
            message += Environment.NewLine;
            message += $"RawMessage: {Encoding.UTF8.GetString(record.Payload)}";
            message += Environment.NewLine;
            message += $"Type: {record.TypeFormat}";

            if (!string.IsNullOrWhiteSpace(record.MimeType))
            {
                message += Environment.NewLine;
                message += $"MimeType: {record.MimeType}";
            }

            return message;
        }

        /// <summary>
        /// Event raised when data has been published on the tag
        /// </summary>
        /// <param name="tagInfo">Published <see cref="ITagInfo"/></param>
        async void Current_OnMessagePublished(ITagInfo tagInfo)
        {
            try
            {
                //ChkReadOnly.IsChecked = false;
                CrossNFC.Current.StopPublishing();
                if (tagInfo.IsEmpty)
                    await DisplayAlert("Info","Formatting tag operation successful","OK");
                else
                    await DisplayAlert("Info","Writing tag operation successful","OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Current_OnMessagePublished", ex.Message,"OK");
            }
        }

        /// <summary>
        /// Event raised when a NFC Tag is discovered
        /// </summary>
        /// <param name="tagInfo"><see cref="ITagInfo"/> to be published</param>
        /// <param name="format">Format the tag</param>
        async void Current_OnTagDiscovered(ITagInfo tagInfo, bool format)
        {
            if (!CrossNFC.Current.IsWritingTagSupported)
            {
                await DisplayAlert("Info","Writing tag is not supported on this device","OK");
                return;
            }

            await DisplayAlert("Event", "Current_OnTagDiscovered", "OK");

            try
            {
                NFCNdefRecord record = null;
                switch (_type)
                {
                    case NFCNdefTypeFormat.WellKnown:
                        record = new NFCNdefRecord
                        {
                            TypeFormat = NFCNdefTypeFormat.WellKnown,
                            MimeType = MIME_TYPE,
                            Payload = NFCUtils.EncodeToByteArray("Plugin.NFC is awesome!"),
                            LanguageCode = "en"
                        };
                        break;
                    case NFCNdefTypeFormat.Uri:
                        record = new NFCNdefRecord
                        {
                            TypeFormat = NFCNdefTypeFormat.Uri,
                            Payload = NFCUtils.EncodeToByteArray("https://github.com/franckbour/Plugin.NFC")
                        };
                        break;
                    case NFCNdefTypeFormat.Mime:
                        record = new NFCNdefRecord
                        {
                            TypeFormat = NFCNdefTypeFormat.Mime,
                            MimeType = MIME_TYPE,
                            Payload = NFCUtils.EncodeToByteArray("Plugin.NFC is awesome!")
                        };
                        break;
                    default:
                        break;
                }

                if (!format && record == null)
                    throw new Exception("Record can't be null.");

                tagInfo.Records = new[] { record };

                if (format)
                    CrossNFC.Current.ClearMessage(tagInfo);

            }
            catch (Exception ex)
            {
                await DisplayAlert("Current_OnTagDiscovered", ex.Message,"OK");
            }
        }

        /// <summary>
        /// Biometric Authentication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Clicked(object sender, EventArgs e)
        {
            var biometricAvailable = await CrossFingerprint.Current.IsAvailableAsync();
            if (!biometricAvailable)
            {
                await DisplayAlert("warning!", "Biometric authentication not available", "OK");
                return;
            }

            var authResult = await CrossFingerprint.Current.AuthenticateAsync(
                new AuthenticationRequestConfiguration("Biometric Auth","Biometric authentication for login"));

            if (authResult.Authenticated)
            {
                await DisplayAlert("Successful", "You have logged in successfully", "OK");
                return;
            }
        }

        private void CreateAccount_Clicked(object sender, EventArgs e)
        {

            //var obj = new APIAgentServer();
            //obj.CreateWebForm(null);
        }

        /// <summary>
        /// Start NFC Listening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Clicked_StartListening(object sender, EventArgs e)
        {
            try
            {
                CrossNFC.Current.StartListening();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", ex.Message, "OK");
            }
        }

        /// <summary>
        /// Stops NFC listening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Clicked_StopListening(object sender, EventArgs e)
        {
            try
            {
                CrossNFC.Current.StopListening();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
