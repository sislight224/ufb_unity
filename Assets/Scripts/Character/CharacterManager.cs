using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using UFB.StateSchema;
using UFB.Entities;
using UFB.Events;
using Colyseus;
using UFB.Network;
using UnityEngine;
using System.Linq;
using System;
using UFB.Map;
using UFB.Network.RoomMessageTypes;
using UFB.Core;
using System.Threading.Tasks;
using Colyseus.Schema;

namespace UFB.Events
{
    public class RequestCharacterMoveEvent
    {
        public Coordinates Destination { get; private set; }

        public RequestCharacterMoveEvent(Coordinates destination)
        {
            Destination = destination;
        }
    }

    public class CharacterPlacedEvent
    {
        public Character.CharacterController character;

        public CharacterPlacedEvent(Character.CharacterController character)
        {
            this.character = character;
        }
    }

    public class SelectedCharacterEvent
    {
        public Character.CharacterController controller;
        public bool isPlayer = false; // lets us set zombie
    }

    // public class CharacterStatsChangedEvent
    // {
    //     public CharacterState state;

    //     public CharacterStatsChangedEvent(CharacterState state)
    //     {
    //         this.state = state;
    //     }
    // }
}

namespace UFB.Character
{
    // will need to be used even before game scene starts, since main menu
    // will require access to characters
    public class CharacterManager : MonoBehaviourService
    {
        public CharacterController PlayerCharacter => _characters[_playerCharacterId];
        public CharacterController SelectedCharacter => _characters[_selectedCharacterId];

        // public MapSchema<CharacterState> State { get; private set; }

        [SerializeField]
        private GameObject _characterPrefab;

        private Dictionary<string, CharacterController> _characters =
            new Dictionary<string, CharacterController>();

        private string _playerCharacterId;
        private string _selectedCharacterId; // during zombie mode, we can set this to a different player

        private MapSchema<CharacterState> _characterStates;

        private void OnEnable()
        {
            ServiceLocator.Current.Register(this);

            var gameService = ServiceLocator.Current.Get<GameService>();
            _playerCharacterId = ServiceLocator.Current.Get<NetworkService>().ClientId;
            // _selectedCharacterId = _playerCharacterId;

            if (gameService.Room == null)
            {
                Debug.LogError("Room is null");
                return;
            }

            _characters = new Dictionary<string, CharacterController>();
            _characterStates = gameService.RoomState.characters;

            _characterStates.OnAdd(OnCharacterAdded);
            _characterStates.OnRemove(OnCharacterRemoved);

            gameService.SubscribeToRoomMessage<CharacterMovedMessage>(
                "characterMoved",
                OnCharacterMoved
            );

            gameService.SubscribeToRoomMessage<BecomeZombieMessage>(
                "becomeZombie",
                (message) =>
                {
                    _selectedCharacterId = message.playerId;
                }
            );

            _characterStates.ForEach(
                (key, character) =>
                {
                    character.stats.OnChange(() => {
                        // EventBus.Publish(new SelectedCharacterEvent {
                        //     character = _characters[key].Character,
                        //     state = character,
                        //     isPlayerControlled = key == _selectedCharacterId
                        // });
                    });
                }
            );

            _characterStates.OnChange(
                (newState, oldState) => {
                    /// subscribe to changes in player stats
                }
            );

            // message can be scoped on the server to send only to specific client
            // EventBus.Subscribe<RequestCharacterMoveEvent>(OnRequestCharacterMove);

            // for now, do this for default behavior, but eventually this will be triggered by some UI handler
            EventBus.Subscribe<TileClickedEvent>(
                (e) =>
                {
                    EventBus.Publish(new RequestCharacterMoveEvent(e.tile.Coordinates));
                }
            );
        }

        private void SetSelectedCharacter(string characterId)
        {
            _selectedCharacterId = characterId;

            // now it's up to any listeners to register events with these
            EventBus.Publish(
                new SelectedCharacterEvent
                {
                    controller = _characters[characterId],
                    isPlayer = characterId == _playerCharacterId
                }
            );
        }

        private void OnDisable()
        {
            // EventBus.Unsubscribe<RequestCharacterMoveEvent>(OnRequestCharacterMove);
            ServiceLocator.Current.Unregister<CharacterManager>();
        }

        private async void OnCharacterAdded(string key, CharacterState characterState)
        {
            EventBus.Publish(
                new ToastMessageEvent($"Player {characterState.id} has joined the game!")
            );
            Debug.Log($"[CharacterManager] Player {characterState.id} has joined the game!");

            try
            {
                UfbCharacter ufbCharacter = await LoadCharacter(characterState.characterClass);
                GameObject templateCharacter = Instantiate(_characterPrefab, transform);
                var character = templateCharacter.GetComponent<CharacterController>();

                // if it's an NPC, don't play the intro
                await character.Initialize(ufbCharacter, characterState, true);
                _characters.Add(characterState.id, character);

                if (character.Id == _playerCharacterId)
                {
                    SetSelectedCharacter(character.Id);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void OnCharacterRemoved(string key, CharacterState characterState)
        {
            EventBus.Publish(
                new ToastMessageEvent($"Player {characterState.id} has left the game!")
            );
            Destroy(_characters[characterState.id].gameObject);
            _characters.Remove(characterState.id);
        }

        private async Task<UfbCharacter> LoadCharacter(string characterId)
        {
            try
            {
                var task = Addressables.LoadAssetAsync<UfbCharacter>("UfbCharacter/" + characterId);
                EventBus.Publish(new DownloadProgressEvent(task, $"Character {characterId}"));
                await task.Task;

                if (task.Status == AsyncOperationStatus.Failed)
                    throw new Exception(
                        $"Failed to load character {characterId}: {task.OperationException.Message}"
                    );
                return task.Result;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw new Exception($"Failed to load character {characterId}: {e.Message}");
            }
        }

        private void OnCharacterMoved(CharacterMovedMessage m)
        {
            // var coordinates = m.path.Select(p => p.coord.ToCoordinates());
            var tileIds = m.path.Select(p => p.tileId);
            tileIds.Reverse();
            /// select all tiles from tiles using tileIds

            var path = ServiceLocator.Current.Get<GameBoard>().GetTilesByIds(tileIds);

            Debug.Log($"[CharacterManager] Moving character {m.characterId} along path");
            var task = _characters[m.characterId].MoveAlongPath(path);

            task.ContinueWith(
                (t) =>
                {
                    EventBus.Publish(new CharacterPlacedEvent(_characters[m.characterId]));
                }
            );
        }

        // private async void OnRequestCharacterMove(RequestCharacterMoveEvent e)
        // {
        //     Debug.Log($"[PlayerManager] Requesting move to {e.Destination.ToString()}");
        //     await _room.Send(
        //         "move",
        //         new Dictionary<string, object>() { { "destination", e.Destination.ToDictionary() } }
        //     );
        // }

        // public void SavePlayerConfiguration(string fileName)
        // {

        //     // this will eventually be handled by the Player object, which PlayerEntity has a reference
        //     // to. It will handle loading/unloading the JSON into the player state. For now, quick solution
        //     var json = JsonConvert.SerializeObject(_players.Select(p => new PlayerConfiguration
        //     {
        //         CharacterName = p.CharacterName,
        //         TileCoordinates = p.CurrentTile.Coordinates
        //     }).ToList());

        //     // save the json
        //     ApplicationData.SaveJSON(json, "gamestate/player-config", fileName + ".json");
        // }


        //         public void LoadPlayerConfiguration(string fileName)
        // {
        //     // load the json
        //     var playerConfigurations = ApplicationData.LoadJSON<List<PlayerConfiguration>>("gamestate/player-config", fileName + ".json");

        //     // iterate through the players and set their tile coordinates
        //     foreach (var playerConfiguration in playerConfigurations)
        //     {
        //         var player = _players.FirstOrDefault(p => p.CharacterName == playerConfiguration.CharacterName);
        //         if (player == null)
        //         {
        //             Debug.LogError($"Player with character name {playerConfiguration.CharacterName} not found");
        //             continue;
        //         }
        //         var tile = GameManager.Instance.GameBoard.GetTileByCoordinates(playerConfiguration.TileCoordinates);
        //         if (tile == null)
        //         {
        //             Debug.LogError($"Tile with coordinates {playerConfiguration.TileCoordinates} not found");
        //             continue;
        //         }
        //         player.ForceMoveToTile(tile);
        //     }
        // }
    }
}
