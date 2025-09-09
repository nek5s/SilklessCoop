using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilklessCoop
{
    internal class ConnectorToggler : MonoBehaviour
    {
        private Connector _connector;

        private void Start()
        {
            _connector = GetComponent<Connector>();
        }

        private void Update()
        {
            if (!_connector.Initialized) return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (_connector.Active) _connector.Disable();
                else _connector.Enable();
            }
        }
    }
}
