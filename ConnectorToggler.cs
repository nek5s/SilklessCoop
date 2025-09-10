using UnityEngine;

namespace SilklessCoop
{
    internal class ConnectorToggler : MonoBehaviour
    {
        public KeyCode MultiplayerToggleKey;

        private Connector _connector;

        private void Start()
        {
            _connector = GetComponent<Connector>();
        }

        private void Update()
        {
            if (!_connector.Initialized) return;

            if (Input.GetKeyDown(MultiplayerToggleKey))
            {
                if (_connector.Active) _connector.Disable();
                else _connector.Enable();
            }
        }
    }
}
