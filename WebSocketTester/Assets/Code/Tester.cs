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
        public TMP_InputField _Server;
        public Button _ConnectButton;
        public Button _DisconnectButton;
        public Button _AddConnectionButton;
        public Button _RemoveConnectionButton;
        public TMP_Text _ConnectionCount;
        public TMP_Text _State;
        public TMP_InputField _OutgoingMessage;
        public Button _SendButton;
        public Button _IncomingMessageButton;
        public TMP_Text _IncomingMessage;
        public TMP_InputField _Error;
        public TMP_Text _Fps;
        public WebSocketConnection _Connection;
        #endregion

        #region Private Fields
        private List<WebSocketConnection> _connections = new();
        private float _fps = 60.0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

            InitConnection(_Connection);
            _connections.Add(_Connection);

            _Server.text = _DefaultServer;
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
            _Fps.text = $"FPS: {_fps:N2}";
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
            UpdateUI();
        }

        private void OnMessageReceived(WebSocketConnection connection, WebSocketMessage message)
        {
            _IncomingMessage.text = message.String;
        }
        #endregion

        #region UI Methods
        private void UpdateUI()
        {
            _State.text = _Connection.State.ToString();

            _ConnectionCount.text = $"Conns: {_connections.Count}";
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
            _OutgoingMessage.interactable = _SendButton.interactable;
            if (!_SendButton.interactable)
                _IncomingMessage.text = "Incoming Message...";

            if (_Connection.ErrorMessage == null)
            {
                _Error.gameObject.SetActive(false);
            }
            else
            {
                _Error.gameObject.SetActive(true);
                _Error.text = _Connection.ErrorMessage;
            }
        }

        private void OnConnectButtonClicked()
        {
            foreach (var connection in _connections)
                connection.Connect(_Server.text);
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
                connection.Connect(_Server.text);

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
                    connection.AddOutgoingMessage(_OutgoingMessage.text);
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
        }

        private void ShutdownConnection(WebSocketConnection connection)
        {
            connection.StateChanged -= OnStateChanged;
            connection.MessageReceived -= OnMessageReceived;
            connection.Disconnect();
        }
        #endregion
    }
}
