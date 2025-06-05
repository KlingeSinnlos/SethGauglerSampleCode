using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private float _text_speed;
    [SerializeField] private float _skip_speed;

    public readonly List<DialogueManager.CharacterPortrait> character_portraits = new();

    private PortraitUIComponents _portrait_UI_components_left;
    private PortraitUIComponents _portrait_UI_components_right;

    private TypewriterEffect _typewriter_effect_left;
    private TypewriterEffect _typewriter_effect_right;
    public TypewriterEffect typewriter;

    private void Awake()
    {
        var portrait_objects_left = GameObject.FindWithTag("DialogueUIPortrait");
        var portrait_object_right = Instantiate(portrait_objects_left, portrait_objects_left.transform.parent);
        portrait_object_right.transform.rotation = Quaternion.Euler(0, 180, 0);
        portrait_object_right.name = "Right";

        GameObject[] portrait_objects = { portrait_objects_left, portrait_object_right };
        var left_side = true;
        foreach (var portrait_object in portrait_objects)
        {
            var current_portrait_UI_component = new PortraitUIComponents(
                portrait_object.transform.Find("Dialogue_Display").Find("Text").GetComponent<TMP_Text>(),
                portrait_object.transform.Find("Dialogue_Display").Find("Name_Tag").GetComponent<TMP_Text>(),
                portrait_object.transform.Find("Portrait").GetComponent<Image>(),
                portrait_object.transform.Find("Dialogue_Display").gameObject,
                portrait_object.transform.Find("Dialogue_Display").Find("Dialogue_Box").gameObject);
            if (left_side) _portrait_UI_components_left = current_portrait_UI_component;
            else _portrait_UI_components_right = current_portrait_UI_component;
            left_side = false;
        }
        _typewriter_effect_left = portrait_objects_left.transform.Find("Dialogue_Display").Find("Text").GetComponent<TypewriterEffect>();
        _typewriter_effect_right = portrait_object_right.transform.Find("Dialogue_Display").Find("Text").GetComponent<TypewriterEffect>();

        _portrait_UI_components_right.text.transform.rotation = Quaternion.Euler(0, -180, 0) * Quaternion.Euler(0, 180, 0);
        _portrait_UI_components_right.name_tag.transform.rotation = Quaternion.Euler(0, -180, 0) * Quaternion.Euler(0, 180, 0);
    }
    /// <summary>
    /// assigns a CharacterPortrait to specific UI Elements for the Dialogue on either the left or right side
    /// </summary>
    /// <param name="character_portrait"></param>
    /// <param name="setLeftSide">declares if the character is portrait on the left or right side, default = false</param>
    public void SetCharacterPortrait(DialogueManager.CharacterPortrait character_portrait, bool setLeftSide = false)
    {
        if (_portrait_UI_components_left == null)
            print("_portrait_UI_components_left is null");
        if (_portrait_UI_components_right == null)
            print("_portrait_UI_components_right is null");

        if (setLeftSide)
            character_portrait.left_side = true;
        var current_portrait_UI_component = character_portrait.left_side ? _portrait_UI_components_left : _portrait_UI_components_right;
        typewriter = character_portrait.left_side ? _typewriter_effect_left : _typewriter_effect_right;

        character_portraits.Add(character_portrait);
        current_portrait_UI_component.name_tag.text = character_portrait.name;
        current_portrait_UI_component.portrait_image.sprite = character_portrait.portrait_spites[0];
    }

    /// <summary>
    /// changes the text of the talking dialogue character and manages animations accordingly
    /// </summary>
    /// <param name="text"></param>
    /// <param name="identifier"></param>
    public void Speak(string text, char identifier)
    {
        var character_portrait = GetCharacterPortraitFromIdentifier(identifier);
        var current_portrait_UI_component = character_portrait.left_side ? _portrait_UI_components_left : _portrait_UI_components_right;
        typewriter = character_portrait.left_side ? _typewriter_effect_left : _typewriter_effect_right;

        (!character_portrait.left_side ? _portrait_UI_components_left : _portrait_UI_components_right).dialogue_display.SetActive(false);
        (!character_portrait.left_side ? _portrait_UI_components_left : _portrait_UI_components_right).portrait_image.gameObject.SetActive(false);
        current_portrait_UI_component.dialogue_display.SetActive(true);
        current_portrait_UI_component.portrait_image.gameObject.SetActive(true);
        current_portrait_UI_component.text.text = text;
        current_portrait_UI_component.text.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// changes the text and emotion of the talking dialogue character and manages animations accordingly
    /// </summary>
    /// <param name="text"></param>
    /// <param name="emotion"></param>
    /// <param name="identifier"></param>
    public void Speak(string text, DialogueManager.Emotion emotion, char identifier)
    {
        Speak(text, identifier);
        SetEmotion(emotion, identifier);
    }

    /// <summary>
    /// changes the emotion of a specific dialogue character and manages animations accordingly
    /// </summary>
    /// <param name="emotion"></param>
    /// <param name="identifier"></param>
    public void SetEmotion(DialogueManager.Emotion emotion, char identifier)
    {
        var character_portrait = GetCharacterPortraitFromIdentifier(identifier);
        var current_portrait_UI_component = character_portrait.left_side ? _portrait_UI_components_left : _portrait_UI_components_right;

        current_portrait_UI_component.name_tag.text = character_portrait.name;
        current_portrait_UI_component.portrait_image.sprite = character_portrait.portrait_spites[DialogueManager.GetValueFromEmotion(emotion)];
    }
    public DialogueManager.CharacterPortrait GetCharacterPortraitFromIdentifier(char identifier)
    {
        return character_portraits.Find(character_portrait => character_portrait.identifier == identifier);
    }

    private class PortraitUIComponents
    {
        public readonly TMP_Text text;
        public readonly TMP_Text name_tag;
        public readonly Image portrait_image;
        public readonly GameObject dialogue_display;
        public readonly GameObject dialogue_box;
        public readonly GameObject next_arrow;

        public PortraitUIComponents(TMP_Text text, TMP_Text name_tag, Image portrait_image, GameObject dialogue_display, GameObject dialogue_box)
        {
            this.text = text;
            this.name_tag = name_tag;
            this.portrait_image = portrait_image;
            this.dialogue_display = dialogue_display;
            this.dialogue_box = dialogue_box;
            //this.next_arrow = next_arrow;
        }
    }
}
