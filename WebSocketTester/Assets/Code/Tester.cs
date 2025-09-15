using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using MikeSchweitzer.WebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WebSocketTester
{
    public class Tester : MonoBehaviour
    {
        #region Serialized Fields
        public string _DefaultServer;
        public TMP_InputField _ServerField;
        public Button _ConnectButton;
        public Button _DisconnectButton;
        public Button _AddConnectionButton;
        public Button _RemoveConnectionButton;
        public TMP_Text _ConnectionCountText;
        public TMP_Text _StateText;
        public TMP_InputField _OutgoingMessageField;
        public Button _SendButton;
        public Button _IncomingMessageButton;
        public TMP_Text _IncomingMessageText;
        public TMP_InputField _ClosedField;
        public TMP_InputField _ErrorField;
        public TMP_Text _FpsText;
        public TMP_Text _RttText;
        public TextAsset _SelfSignedCert;
        public TextAsset _SelfSignedCertPassword;
        public WebSocketConnection _Connection;
        #endregion

        #region Cert Support
        private class SelfSignedCertTrustPolicy : ICertificatePolicy
        {
            public bool CheckValidationResult(
                ServicePoint servicePoint,
                X509Certificate certificate,
                WebRequest request,
                int certificateProblem)
            {
                return true;
            }
        }
        #endregion

        #region Private Fields
        private List<WebSocketConnection> _connections = new();
        private float _fps = 60.0f;
        private TimeSpan _lastPingPongInterval = TimeSpan.Zero;
        #endregion

        #region Private Properties
        private WebSocketConfig Config => new WebSocketConfig
        {
            Url = _ServerField.text,
            DotNetSelfSignedCert = _SelfSignedCert == null
                ? null
                : _SelfSignedCert.bytes,
            DotNetSelfSignedCertPassword = _SelfSignedCertPassword == null
                ? null
                : _SelfSignedCertPassword.text.ToCharArray(),
        };
        #endregion

        #region Unity Methods
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

#pragma warning disable CS0618 // Type or member is obsolete
            ServicePointManager.CertificatePolicy = new SelfSignedCertTrustPolicy();
#pragma warning restore CS0618 // Type or member is obsolete

            InitConnection(_Connection);
            _connections.Add(_Connection);

            _ServerField.text = string.IsNullOrEmpty(Settings.ServerUrl) ? _DefaultServer : Settings.ServerUrl;
            _ServerField.onEndEdit.AddListener(OnServerFieldEndEdit);
            _ConnectButton.onClick.AddListener(OnConnectButtonClicked);
            _DisconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
            _AddConnectionButton.onClick.AddListener(OnAddConnectionButtonClicked);
            _RemoveConnectionButton.onClick.AddListener(OnRemoveConnectionButtonClicked);
            _SendButton.onClick.AddListener(OnSendButtonClicked);

            UpdateUI();
        }

        private void Update()
        {
            var fps = 1.0f / Time.unscaledDeltaTime;
            _fps = Mathf.Lerp(_fps, fps, 0.0005f);
            _FpsText.text = $"FPS: {_fps:N2}";

            _RttText.text = $"RTT: {_Connection.LastPingPongInterval.TotalSeconds:N2}s";
        }

        private void OnDestroy()
        {
            foreach (var connection in _connections)
                ShutdownConnection(connection);
        }
        #endregion

        #region WebSocketConnection Events
        private void OnStateChanged(WebSocketConnection connection, WebSocketState oldState, WebSocketState newState)
        {
            Debug.Log($"[{connection.GetInstanceID()}] oldState={oldState} newState={newState}", connection);

            UpdateUI();
        }

        private void OnMessageReceived(WebSocketConnection connection, WebSocketMessage message)
        {
            var type = message.Type == WebSocketDataType.Text ? "Txt" : "Bin";
            Debug.Log($"[{connection.GetInstanceID()}] Recv: [{type}] {message.String}", connection);

            _IncomingMessageText.text = message.String;
        }

        private void OnClosed(WebSocketConnection connection, WebSocketCloseCode closeCode, string reason)
        {
            Debug.Log($"[{connection.GetInstanceID()}] Closed: code={closeCode} reason=\"{reason}\"", connection);

            UpdateUI();
        }

        private void OnErrorMessageReceived(WebSocketConnection connection, string errorMessage)
        {
            Debug.LogError($"[{connection.GetInstanceID()}] Err: {errorMessage}", connection);

            UpdateUI();
        }

        private void OnPingSent(WebSocketConnection connection, DateTime timestamp)
        {
            Debug.Log($"[{connection.GetInstanceID()}] Ping: {timestamp:HH:mm:ss.ffff}", connection);
        }

        private void OnPongReceived(WebSocketConnection connection, DateTime timestamp)
        {
            Debug.Log($"[{connection.GetInstanceID()}] Pong: {timestamp:HH:mm:ss.ffff}", connection);
            Debug.Log($"[{connection.GetInstanceID()}] Ping-Pong RTT: {connection.LastPingPongInterval:ss\\.ffff}", connection);
        }
        #endregion

        #region UI Methods
        private void UpdateUI()
        {
            _StateText.text = _Connection.State.ToString();

            _ConnectionCountText.text = $"Conns: {_connections.Count}";
            switch (_Connection.State)
            {
                case WebSocketState.Invalid:
                case WebSocketState.Disconnected:
                    _ConnectButton.interactable = true;
                    break;

                default:
                    _ConnectButton.interactable = false;
                    break;
            }
            _DisconnectButton.interactable = !_ConnectButton.interactable;

            _SendButton.interactable = _Connection.State == WebSocketState.Connected;
            _IncomingMessageButton.interactable = _SendButton.interactable;
            _OutgoingMessageField.interactable = _SendButton.interactable;
            if (!_SendButton.interactable)
                _IncomingMessageText.text = "Incoming Message...";

            if (_Connection.CloseCode == 0 && _Connection.CloseReason == null)
            {
                _ClosedField.gameObject.SetActive(false);
            }
            else
            {
                _ClosedField.gameObject.SetActive(true);
                _ClosedField.text = $"Closed: {_Connection.CloseCode} ({_Connection.CloseReason})";
            }

            if (_Connection.ErrorMessage == null)
            {
                _ErrorField.gameObject.SetActive(false);
            }
            else
            {
                _ErrorField.gameObject.SetActive(true);
                _ErrorField.text = $"Err: {_Connection.ErrorMessage}";
            }
        }

        private void OnServerFieldEndEdit(string text)
        {
            Settings.ServerUrl = text;
        }

        private void OnConnectButtonClicked()
        {
            foreach (var connection in _connections)
            {
                connection.DesiredConfig = Config;
                connection.Connect();
            }
        }

        private void OnDisconnectButtonClicked()
        {
            foreach (var connection in _connections)
                connection.Disconnect();
        }

        private void OnAddConnectionButtonClicked()
        {
            var connection = Instantiate(_Connection);
            _connections.Add(connection);

            InitConnection(connection);

            if (_Connection.State is WebSocketState.Connected or WebSocketState.Connecting)
            {
                connection.DesiredConfig = Config;
                connection.Connect();
            }

            UpdateUI();
        }

        private void OnRemoveConnectionButtonClicked()
        {
            if (_connections.Count == 1)
                return;

            var connection = _connections[^1];
            _connections.RemoveAt(_connections.Count - 1);

            ShutdownConnection(connection);
            Destroy(connection.gameObject);

            UpdateUI();
        }

        private void OnSendButtonClicked()
        {
            foreach (var connection in _connections)
            {
                if (connection.State == WebSocketState.Connected)
                    connection.AddOutgoingMessage(_OutgoingMessageField.text);
            }
        }
        #endregion

        #region WebSocket Management
        private void InitConnection(WebSocketConnection connection)
        {
            connection.DesiredConfig = new WebSocketConfig
            {
                PingInterval = TimeSpan.FromSeconds(3.0),
                ShouldPingWaitForPong = true,
                CanDebugLog = Debug.isDebugBuild,
            };
            connection.StateChanged += OnStateChanged;
            connection.MessageReceived += OnMessageReceived;
            connection.Closed += OnClosed;
            connection.ErrorMessageReceived += OnErrorMessageReceived;
            connection.PingSent += OnPingSent;
            connection.PongReceived += OnPongReceived;
        }

        private void ShutdownConnection(WebSocketConnection connection)
        {
            connection.StateChanged -= OnStateChanged;
            connection.MessageReceived -= OnMessageReceived;
            connection.Closed -= OnClosed;
            connection.ErrorMessageReceived -= OnErrorMessageReceived;
            connection.PingSent -= OnPingSent;
            connection.PongReceived -= OnPongReceived;
            connection.Disconnect();
        }
        #endregion
    }
}
