using System;
using System.Linq;
using SilklessLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSync;

internal class UIAdder : MonoBehaviour
{
    private GameObject _oldButton;
    private GameObject _mainMenu;
    private GameObject _mainMenuContainer;
    private GameObject _mainMenuButton;
    private UnityEngine.UI.Text _mainMenuText;

    private void Update()
    {
        try
        {
            if (!_mainMenu) _mainMenu = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "MainMenuScreen");
            if (!_mainMenu) return;

            if (!_mainMenuContainer) _mainMenuContainer = _mainMenu.transform.Find("MainMenuButtons").gameObject;
            if (!_mainMenuContainer) return;

            if (!_oldButton) _oldButton = _mainMenuContainer.transform.Find("OptionsButton").gameObject;
            if (!_oldButton) return;

            if (!_mainMenuButton)
            {
                _mainMenuButton = Instantiate(_oldButton, _mainMenuContainer.transform, true);
                _mainMenuButton.transform.localScale = Vector3.one;
                _mainMenuButton.name = "MultiplayerButton";
                _mainMenuText = _mainMenuButton.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Text>();

                EventTrigger et = _mainMenuButton.GetComponent<EventTrigger>();
                et.triggers.Clear();

                EventTrigger.Entry e = new EventTrigger.Entry();
                e.callback.AddListener(_ => SilklessAPI.Toggle());
                et.triggers.Add(e);

                LogUtil.LogInfo("Added main menu button.");
            }

            if (SilklessAPI.Initialized)
            {
                _mainMenuText.text = SilklessAPI.Connected ? $"Disable Multiplayer [{ModConfig.ConnectionType}]" : $"Enable Multiplayer [{ModConfig.ConnectionType}]";
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
}