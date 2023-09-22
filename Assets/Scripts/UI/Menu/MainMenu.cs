using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UFB.Gameplay;

namespace UFB.UI
{
    public class MainMenu : Menu
    {
        public Menu newGameMenu;
        public Menu joinGameMenu;


        // public override async void Start()
        // {
        //     base.Start();
        //     await GameController.Instance.Initialize();
        // }

        private void OnEnable()
        {
            // ToggleButtonInteractability(false);
            // GameManager.Instance.OnConnect += OnConnect;
        }

        private void OnDisable()
        {
            // GameManager.Instance.OnConnect -= OnConnect;
        }

        private void OnConnect() => ToggleButtonInteractability(true);


        private void ToggleButtonInteractability(bool interactable)
        {
            var buttons = GetComponentsInChildren<Button>();
            foreach (var button in buttons)
            {
                button.interactable = interactable;
            }
        }

        public void OnNewGameButton() => _menuManager.OpenMenu(newGameMenu);
        public void OnJoinGameButton() => _menuManager.OpenMenu(joinGameMenu);
        public void OnSettingsButton()
        {
            Debug.Log("Settings not implemented yet");
        }
    }
}