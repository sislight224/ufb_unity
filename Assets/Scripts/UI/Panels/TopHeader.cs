using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine;
using UFB.StateSchema;
using UFB.Core;
using UFB.Events;
using TMPro;
using UFB.Character;
using UnityEngine.UI;

namespace UFB.UI
{
    public class TopHeader : MonoBehaviour
    {
        [SerializeField]
        private Image _avatarImage;

        [SerializeField]
        private TextMeshProUGUI _screenNameText;

        [SerializeField]
        private LinearIndicatorBar _healthBar;

        [SerializeField]
        private LinearIndicatorBar _energyBar;

        private void OnEnable()
        {
            EventBus.Subscribe<SelectedCharacterEvent>(OnSelectedCharacterEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SelectedCharacterEvent>(OnSelectedCharacterEvent);
        }

        private void OnSelectedCharacterEvent(SelectedCharacterEvent e)
        {
            _healthBar.SetRangedValueState(e.controller.State.stats.health);
            _energyBar.SetRangedValueState(e.controller.State.stats.energy);
            _screenNameText.text = e.controller.State.displayName;

            Addressables
                .LoadAssetAsync<UfbCharacter>("UfbCharacter/" + e.controller.State.characterClass)
                .Completed += (op) =>
            {
                if (
                    op.Status
                    == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded
                )
                    _avatarImage.sprite = op.Result.avatar;
                else
                    Debug.LogError(
                        "Failed to load character avatar: " + op.OperationException.Message
                    );
            };
        }
    }
}
