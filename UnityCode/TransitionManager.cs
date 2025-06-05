using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using System;

public class TransitionManager : MonoBehaviour
{
    private TransitionObject _ariving_transition;
    private FadeToBlack _fade_to_black;
    private PlayerHandler _player_handler;
    [ReadOnly][SerializeReference] private GameObject _player;
    [ReadOnly][SerializeReference] private GameObject _player_camera;

    [ReadOnly] public bool _transitioning;

    public event Action TransitionStart;
    public event Action TransitionComplete;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _player_handler = _player.GetComponent<PlayerHandler>();
        _player_camera = GameObject.Find("Player_Camera");
        _fade_to_black = FindAnyObjectByType<FadeToBlack>();
    }

    public void SceneLoad(TransitionObject transition_object)
    {
        if (!_fade_to_black._fading)
        {
            print("Loading Scene: " + transition_object.ariving_scene_object.scene_name);
            _ariving_transition = transition_object;
            StartCoroutine(AsyncSceneLoading());
        }
    }
    IEnumerator AsyncSceneLoading()
    {
        _player_handler.OverworldInputToggle(false);
        TransitionStart?.Invoke();

        _fade_to_black.FadeScreenToBlack();
        while (_fade_to_black._fading)
            yield return null;
        Scene current_scene = SceneManager.GetActiveScene();
        AsyncOperation asyncLoad;
        //Start Loading the Scene in the Background
        try
        {   
            asyncLoad = SceneManager.LoadSceneAsync(_ariving_transition.ariving_scene_object.scene_ID, LoadSceneMode.Additive);
        } catch
        {
            print("Error Loading Scene " + _ariving_transition.ariving_scene_object.scene_name);
            yield break;
        }
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        //Moving Game Objects
        Destroy(System.Array.Find(SceneManager.GetSceneByName(_ariving_transition.ariving_scene_object.scene_name).GetRootGameObjects(), o => o.tag == "Player"));
        SceneManager.MoveGameObjectToScene(_player, SceneManager.GetSceneByName(_ariving_transition.ariving_scene_object.scene_name));
        ObjectPlayerUpdate(_player.transform);

        Destroy(System.Array.Find(SceneManager.GetSceneByName(_ariving_transition.ariving_scene_object.scene_name).GetRootGameObjects(), o => o.name == "Player_Camera"));
        SceneManager.MoveGameObjectToScene(_player_camera, SceneManager.GetSceneByName(_ariving_transition.ariving_scene_object.scene_name));
        SceneManager.UnloadSceneAsync(current_scene);

        _fade_to_black.FadeScreenToBlack();
        TransitionComplete?.Invoke();
        _player_handler.OverworldInputToggle(true);
        while (_fade_to_black._fading)
            yield return null;
    }
    public void ObjectPlayerUpdate(Transform _player_object)
    {
        TransitionObject _new_transition_object = _ariving_transition.ariving_scene_object;
        if (_ariving_transition.ariving_scene_object.variable_cords == false)
        {
            _player_object.position = new Vector3(_new_transition_object.transition_cords_single.x, _new_transition_object.transition_cords_single.y, 0);
        }
        else if (_ariving_transition.ariving_scene_object.variable_cords == true)
        {
            Vector3 _new_position = new Vector3(0, 0, 0);
            switch (_ariving_transition.direction_facing)
            {
                case TransitionObject.DirectionFacing.NORTH:
                    _new_position.x = _new_transition_object.transition_cords_composite.x + (_ariving_transition.cords_offset * (_new_transition_object.transition_cords_prime.x - _new_transition_object.transition_cords_composite.x));
                    _new_position.y = _new_transition_object.transition_cords_prime.y;

                    break;
                case TransitionObject.DirectionFacing.SOUTH:
                    _new_position.x = _new_transition_object.transition_cords_composite.x + (_ariving_transition.cords_offset * (_new_transition_object.transition_cords_prime.x - _new_transition_object.transition_cords_composite.x));
                    _new_position.y = _new_transition_object.transition_cords_prime.y;
                    break;
                case TransitionObject.DirectionFacing.EAST:
                    _new_position.y = _new_transition_object.transition_cords_composite.y + (_ariving_transition.cords_offset * (_new_transition_object.transition_cords_prime.y - _new_transition_object.transition_cords_composite.y));
                    _new_position.x = _new_transition_object.transition_cords_prime.x;
                    break;
                case TransitionObject.DirectionFacing.WEST:
                    _new_position.y = _new_transition_object.transition_cords_composite.y + (_ariving_transition.cords_offset * (_new_transition_object.transition_cords_prime.y - _new_transition_object.transition_cords_composite.y));
                    _new_position.x = _new_transition_object.transition_cords_prime.x;
                    break;
            }

            _player_object.position = _new_position;
        }

        switch (_new_transition_object.direction_facing)
        {
            case TransitionObject.DirectionFacing.NORTH:
                _player_object.GetComponent<PlayerAnimator>().ChangeAnimationState(PlayerAnimator.AnimationState.IDLE, PlayerAnimator.AnimationDirection.UP);
                break;
            case TransitionObject.DirectionFacing.SOUTH:
                _player_object.GetComponent<PlayerAnimator>().ChangeAnimationState(PlayerAnimator.AnimationState.IDLE, PlayerAnimator.AnimationDirection.DOWN);
                break;
            case TransitionObject.DirectionFacing.EAST:
                _player_object.GetComponent<PlayerAnimator>().ChangeAnimationState(PlayerAnimator.AnimationState.IDLE, PlayerAnimator.AnimationDirection.RIGHT);
                break;
            case TransitionObject.DirectionFacing.WEST:
                _player_object.GetComponent<PlayerAnimator>().ChangeAnimationState(PlayerAnimator.AnimationState.IDLE, PlayerAnimator.AnimationDirection.LEFT);
                break;
        }
    }
}
