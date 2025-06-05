using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Serializable]
	public struct CharacterPortrait
    {
		public string name;
		public char identifier;
		public Sprite[] portrait_spites;
		[HideInInspector] public bool left_side;
	}

	public enum Emotion
	{
		Neutral,
		Angry,
		Happy,
		Sad,
		Fearful
	}
	private readonly Dictionary<string, Emotion> scriptEmotionShortcuts = new Dictionary<string, Emotion>()
	{
		{ "neu", Emotion.Neutral },
		{ "ang", Emotion.Angry },
		{ "hap", Emotion.Happy },
		{ "sad", Emotion.Sad },
		{ "fea", Emotion.Fearful },
	};
	public static int GetValueFromEmotion(Emotion emotion)
	{
		return emotion switch
		{
			Emotion.Neutral => 0,
			Emotion.Angry => 1,
			Emotion.Happy => 2,
			Emotion.Sad => 3,
			Emotion.Fearful => 4,
			_ => 0
		};
	}
	public List<CharacterPortrait> character_portraits;
	public TextAsset current_dialogue_file;

	[SerializeField] private DialogueUIManager _dialogue_UI_manager;
	private List<string> _dialogue_formatted = new List<string>();
	private string _dialogue_raw_text;
	private FlagManager _flag_manager;
	private InputAction _dialogue_inputs;

	private bool _current_text_revealed = false;

	public event Action DialogueStarted;
	public event Action DialogueEnded;

	void Start()
	{
		_flag_manager = GetComponent<FlagManager>();
		_dialogue_UI_manager = FindAnyObjectByType<DialogueUIManager>(FindObjectsInactive.Include);
		//_dialogue_inputs = _dialogue_UI_manager.gameObject.GetComponent<PlayerInput>().actions["Dialogue Advancement"];

        TypewriterEffect.CompleteTextRevealed += TypewriterEffect_CompleteTextRevealed;
	}

    /// <summary>
    /// Loads the current text file, formats it, and starts the dialogue. This method without parameters is mainly for debudding 
    /// </summary>
    [Button]
	public void LoadDialogue()
	{
		try
		{
			LoadDialogue(current_dialogue_file);
		}
		catch
		{
			Debug.LogError("Current Dialogue File Not Valid");
		}
	}
	/// <summary>
	/// Loads the text file, formats it, and starts the dialogue. If using path, start the path at the resources folder with no file extension: "Dialogue/TestDialogue"
	/// </summary>
	/// <param name="dialogueFilePath"></param>
	public void LoadDialogue(string dialogueFilePath)
	{
		try
		{
			current_dialogue_file = Resources.Load<TextAsset>(dialogueFilePath);
			LoadDialogue(current_dialogue_file);
		}
		catch
		{
			Debug.LogError("Dialogue File Path Not Valid");
		}
	}
	public void LoadDialogue(TextAsset dialogueFile)
	{
		try
		{
			current_dialogue_file = dialogueFile;
			print("Loading Dialogue: " + dialogueFile.name);
		}
		catch
		{
			Debug.LogError("Dialogue File Not Valid");
			return;
		}

		_dialogue_raw_text = current_dialogue_file.ToString();
		_dialogue_formatted = _dialogue_raw_text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

		_dialogue_UI_manager = FindAnyObjectByType<DialogueUIManager>(FindObjectsInactive.Include);
		_dialogue_UI_manager.gameObject.SetActive(true);
		StartCoroutine(StartDialogue());
	}
	/// <summary>
	/// Starts the dialogue, going through each line of the script and translating it to speech, emotions, and flags
	/// </summary>
	/// <returns></returns>
	private IEnumerator StartDialogue()
	{
		string[] splitString;
		string[] flagStrings;
		DialogueStarted?.Invoke();
		// Going through each line of dialogue
		foreach (var dialogue in _dialogue_formatted)
		{
			// This splits between the first part which de-laminates between the portrait/speaker identification and the spoken dialogue
			splitString = dialogue.Split(':');
			// Flag Checker & Setter
			if (splitString.Length >= 3)
			{
				flagStrings = splitString[2].Replace(" ", "").Split(',');
				if (!CheckFlags(flagStrings))
					continue;
				SetFlags(flagStrings);
			}

			var characters = splitString[0].Replace(" ", "").Split(',');
			foreach (var character in characters)
			{
				if (_dialogue_UI_manager.character_portraits.Any(x => x.identifier == character[0]))
					_dialogue_UI_manager.SetEmotion(scriptEmotionShortcuts[character.Substring(1, 3).ToLower()], character[0]);
				else
				{
					try
					{
						if (_dialogue_UI_manager == null)
							print("Dialogue UI Manager is Null");
						if (character_portraits == null)
							print("Character Portraits is Null");
						_dialogue_UI_manager.SetCharacterPortrait(character_portraits.Find(x => x.identifier == character[0]),
							character[4] == 'L');
					}
					catch (IndexOutOfRangeException)
					{
						if (_dialogue_UI_manager == null)
							print("Dialogue UI Manager is Null");
						if (character_portraits == null)
							print("Character Portraits is Null");
						_dialogue_UI_manager.SetCharacterPortrait(character_portraits.Find(x => x.identifier == character[0]));
					}

					_dialogue_UI_manager.SetEmotion(scriptEmotionShortcuts[character.Substring(1, 3)], character[0]);
				}
			}

			_dialogue_UI_manager.Speak(splitString[1].Replace("\"", ""), characters[^1][0]);
			yield return AdvanceDialogue();
		}

		EndDialogue();
	}
	/// <summary>
	/// Waits for input to Advance/Skip dialogue
	/// </summary>
	/// <returns></returns>
	private IEnumerator AdvanceDialogue()
	{
		var _button_pressed = false;
		while (!_button_pressed)
		{
			/*if (Input.GetMouseButtonDown(0) && _current_text_revealed == true)
            {
				_button_pressed = true;
				_current_text_revealed = false;
            }
			else if (Input.GetMouseButtonDown(0) && _current_text_revealed == false)
				_dialogue_UI_manager.typewriter.Skip();*/
			if (_dialogue_inputs.WasPerformedThisFrame() && _current_text_revealed == true)
            {
				_button_pressed = true;
				_current_text_revealed = false;
			}
			else if (_dialogue_inputs.WasPerformedThisFrame() && _current_text_revealed == false)
				_dialogue_UI_manager.typewriter.Skip();
			yield return null;
		}
	}

	private void TypewriterEffect_CompleteTextRevealed()
	{
		_current_text_revealed = true;
	}
	/// <summary>
	/// Ends the dialogue
	/// </summary>
	public void EndDialogue()
	{
		DialogueEnded?.Invoke();
		_dialogue_UI_manager.character_portraits.Clear();
		_dialogue_UI_manager.gameObject.SetActive(false);
	}
	/// <summary>
	/// Checks for checked flags and finds if said flags are in the FlagManager
	/// </summary>
	/// <param name="flagsToCheck"></param>
	/// <returns></returns>
	private bool CheckFlags(IEnumerable<string> flagsToCheck)
	{
		var flagsFound = true;
		foreach (var flag in flagsToCheck)
		{
			var flagName = flag[(flag.IndexOf("\"", StringComparison.Ordinal) + 1)..].TrimEnd('"');
			if (flag.Contains("check") && _flag_manager.flags.Contains(flagName))
			{
				flagsFound = true;
				print("Found " + flagName + " flag");
			}
			else if (flag.Contains("check") && !_flag_manager.flags.Contains(flagName))
			{
				flagsFound = false;
				print("Did not find " + flagName + " flag");
			}
		}

		return flagsFound;
	}

	/// <summary>
	/// Sets flags in the FlagManager if not already set
	/// </summary>
	/// <param name="flagsToSet"></param>
	private void SetFlags(IEnumerable<string> flagsToSet)
	{
		foreach (var flag in flagsToSet)
		{
			var flagName = flag[(flag.IndexOf("\"", StringComparison.Ordinal) + 1)..].TrimEnd('"');
			if (!flag.Contains("set") || _flag_manager.flags.Contains(flagName)) continue;
			_flag_manager.flags.Add(flagName);
			print("Flag " + flagName + " added to flags");
		}
	}
}
