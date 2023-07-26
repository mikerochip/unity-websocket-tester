using MikeSchweitzer.WebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WebSocketTester
{
    public class Tester : MonoBehaviour
    {
        public string _DefaultServer;
        public TMP_InputField _Server;
        public Button _ConnectButton;
        public Button _DisconnectButton;
        public TMP_Text _State;
        public TMP_InputField _OutgoingMessage;
        public Button _SendButton;
        public Button _IncomingMessageButton;
        public TMP_Text _IncomingMessage;
        public TMP_InputField _Error;
        public WebSocketConnection _Connection;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _Connection.DesiredConfig = new WebSocketConfig
            {
                DisableSslValidation = true,
            };

            _Server.text = _DefaultServer;
            _ConnectButton.onClick.AddListener(() => _Connection.Connect(_Server.text));
            _DisconnectButton.onClick.AddListener(() => _Connection.Disconnect());
            _SendButton.onClick.AddListener(() => _Connection.AddOutgoingMessage(_OutgoingMessage.text));

            UpdateUI();
            _Connection.StateChanged += OnStateChanged;
            _Connection.MessageReceived += OnMessageReceived;
        }

        private void OnDestroy()
        {
            _Connection.StateChanged -= OnStateChanged;
            _Connection.MessageReceived -= OnMessageReceived;
        }

        private void OnStateChanged(WebSocketConnection connection, WebSocketState oldState, WebSocketState newState)
        {
            UpdateUI();
        }

        private void OnMessageReceived(WebSocketConnection connection, WebSocketMessage message)
        {
            _IncomingMessage.text = message.String;
        }

        private void UpdateUI()
        {
            _State.text = _Connection.State.ToString();

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
    }
}
