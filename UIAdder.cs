using BepInEx.Logging;
using SilklessCoop.Connectors;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SilklessCoop
{
    internal class UIAdder : MonoBehaviour
    {
        public ManualLogSource Logger;
        public ModConfig Config;

        private Connector _connector;

        private GameObject _oldButton;
        private GameObject _mainMenu;
        private GameObject _mainMenuContainer;
        private GameObject _mainMenuButton;
        private UnityEngine.UI.Text _mainMenuText;

        private void Start()
        {
            _connector = GetComponent<Connector>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(Config.MultiplayerToggleKey))
                ToggleConnector();

            if (!_mainMenu) _mainMenu = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "MainMenuScreen");
            if (!_mainMenu) return;

            if (!_mainMenuContainer) _mainMenuContainer = _mainMenu.transform.Find("MainMenuButtons").gameObject;
            if (!_mainMenuContainer) return;

            if (!_oldButton) _oldButton = _mainMenuContainer.transform.Find("OptionsButton").gameObject;
            if (!_oldButton) return;

            if (!_mainMenuButton)
            {
                _mainMenuButton = Instantiate(_oldButton);
                _mainMenuButton.transform.SetParent(_mainMenuContainer.transform);
                _mainMenuButton.transform.localScale = Vector3.one;
                _mainMenuButton.SetName("MultiplayerButton");
                _mainMenuText = _mainMenuButton.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Text>();

                EventTrigger et = _mainMenuButton.GetComponent<EventTrigger>();
                et.triggers.Clear();

                EventTrigger.Entry e = new EventTrigger.Entry();
                e.callback.AddListener((eventData) => ToggleConnector());
                et.triggers.Add(e);

                Logger.LogInfo("Added main menu button.");
            }

            if (!_connector) return;
            if (!_connector.Initialized) return;

            _mainMenuText.text = _connector.Enabled ? $"Disable Multiplayer [{Config.ConnectionType}]" : $"Enable Multiplayer [{Config.ConnectionType}]";
        }

        private void ToggleConnector()
        {
            if (!_connector.Initialized) return;

            if (_connector.Enabled) _connector.Disable();
            else _connector.Enable();
        }
    }
}
