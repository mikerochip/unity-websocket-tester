using System.Collections.Generic;
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
        public string _Server;
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
        public TMP_InputField _ErrorField;
        public TMP_Text _FpsText;
        public WebSocketConnection _Connection;
        #endregion

        #region Private Fields
        private List<WebSocketConnection> _connections = new();
        private float _fps = 60.0f;
        #endregion

        #region Private Properties
        private string ServerUrl => _ServerField.text;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

            InitConnection(_Connection);
            _connections.Add(_Connection);

            _ServerField.text = string.IsNullOrEmpty(_Server) ? _DefaultServer : _Server;
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

        private void OnErrorMessageReceived(WebSocketConnection connection, string errorMessage)
        {
            Debug.LogError($"[{connection.GetInstanceID()}] Err: {errorMessage}", connection);

            UpdateUI();
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

            if (_Connection.ErrorMessage == null)
            {
                _ErrorField.gameObject.SetActive(false);
            }
            else
            {
                _ErrorField.gameObject.SetActive(true);
                _ErrorField.text = _Connection.ErrorMessage;
            }
        }

        private void OnConnectButtonClicked()
        {
            foreach (var connection in _connections)
                connection.Connect(ServerUrl);
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
                connection.Connect(ServerUrl);

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
                CanDebugLog = Debug.isDebugBuild,
            };
            connection.StateChanged += OnStateChanged;
            connection.MessageReceived += OnMessageReceived;
            connection.ErrorMessageReceived += OnErrorMessageReceived;
        }

        private void ShutdownConnection(WebSocketConnection connection)
        {
            connection.StateChanged -= OnStateChanged;
            connection.MessageReceived -= OnMessageReceived;
            connection.ErrorMessageReceived -= OnErrorMessageReceived;
            connection.Disconnect();
        }
        #endregion
    }
}
