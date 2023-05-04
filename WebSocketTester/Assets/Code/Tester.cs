using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mikerochip.WebSocket;

namespace WebSocketTester
{
    public class Tester : MonoBehaviour
    {
        public TMP_InputField _Server;
        public Button _ConnectButton;
        public Button _DisconnectButton;
        public TMP_Text _State;
        public TMP_InputField _OutgoingMessage;
        public Button _SendButton;
        public TMP_Text _IncomingMessage;
        public WebSocketConnection _Connection;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _ConnectButton.onClick.AddListener(() => _Connection.Connect(_Server.text));
            _DisconnectButton.onClick.AddListener(() => _Connection.Disconnect());
            _SendButton.onClick.AddListener(() => _Connection.AddOutgoingMessage(_OutgoingMessage.text));
        }

        private void Update()
        {
            _State.text = _Connection.State.ToString();

            while (_Connection.TryRemoveIncomingMessage(out string message))
                _IncomingMessage.text = message;
        }
    }
}
